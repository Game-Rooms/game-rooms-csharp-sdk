using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameRooms.Sdk.Realtime;

public sealed class GameRoomsRealtimeClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Func<IGameRoomsSocketTransport> _transportFactory;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<RealtimeServerEnvelope>> _pending = new();
    private readonly CancellationTokenSource _readerCts = new();

    private IGameRoomsSocketTransport? _transport;
    private Task? _readerLoop;
    private long _seq;

    public GameRoomsRealtimeClient()
        : this(() => new ClientWebSocketTransport())
    {
    }

    public GameRoomsRealtimeClient(Func<IGameRoomsSocketTransport> transportFactory)
    {
        _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
    }

    public event EventHandler<RealtimeEventMessage>? EventReceived;

    public async Task ConnectAsync(Uri websocketUri, CancellationToken cancellationToken = default)
    {
        if (websocketUri is null)
        {
            throw new ArgumentNullException(nameof(websocketUri));
        }

        _transport = _transportFactory();
        await _transport.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);
        _readerLoop = Task.Run(() => ReaderLoopAsync(_readerCts.Token), CancellationToken.None);
    }

    public Task<T?> SendCommandAsync<T>(string opcode, object? parameters = null, CancellationToken cancellationToken = default)
    {
        return SendCommandInternalAsync<T>(opcode, parameters, cancellationToken);
    }

    public Task<T?> CreateObjectAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var payload = new { type = "object", key, value };
        return SendCommandAsync<T>("object:create", payload, cancellationToken);
    }

    public Task<string?> CreateTextAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var payload = new { type = "text", key, value };
        return SendCommandAsync<string>("object:create", payload, cancellationToken);
    }

    public Task<T?> CreateNumberAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : struct
    {
        var payload = new { type = "number", key, value };
        return SendCommandAsync<T?>("object:create", payload, cancellationToken);
    }

    public Task<T?> UpdateObjectAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var payload = new { key, value };
        return SendCommandAsync<T>("object:update", payload, cancellationToken);
    }

    public Task<T?> GetObjectAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = new { key };
        return SendCommandAsync<T>("object:get", payload, cancellationToken);
    }

    public Task<bool?> LockObjectAsync(string key, bool locked = true, CancellationToken cancellationToken = default)
    {
        var payload = new { key, locked };
        return SendCommandAsync<bool?>("object:lock", payload, cancellationToken);
    }

    public Task<T?> RelayAsync<T>(object relayPayload, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync<T>("object:relay", relayPayload, cancellationToken);
    }

    private async Task<T?> SendCommandInternalAsync<T>(string opcode, object? parameters, CancellationToken cancellationToken)
    {
        if (_transport is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        if (string.IsNullOrWhiteSpace(opcode))
        {
            throw new ArgumentException("Opcode is required.", nameof(opcode));
        }

        var seq = Interlocked.Increment(ref _seq);
        var tcs = new TaskCompletionSource<RealtimeServerEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[seq] = tcs;

        try
        {
            var envelope = new RealtimeCommandEnvelope
            {
                Opcode = opcode,
                Seq = seq,
                Params = parameters
            };

            var json = JsonSerializer.Serialize(envelope, JsonOptions);
            await _transport.SendTextAsync(json, cancellationToken).ConfigureAwait(false);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _readerCts.Token);
            using var registration = linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token));

            var response = await tcs.Task.ConfigureAwait(false);
            if (response.Error is not null)
            {
                throw new RealtimeRequestException(response.Error.Message ?? response.Error.Code ?? "Realtime request failed.", response.Error);
            }

            if (response.Result is null)
            {
                return default;
            }

            return response.Result.Value.Deserialize<T>(JsonOptions);
        }
        finally
        {
            _pending.TryRemove(seq, out _);
        }
    }

    private async Task ReaderLoopAsync(CancellationToken cancellationToken)
    {
        if (_transport is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            string? json;
            try
            {
                json = await _transport.ReceiveTextAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                FailPending(ex);
                break;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                break;
            }

            RealtimeServerEnvelope? message;
            try
            {
                message = JsonSerializer.Deserialize<RealtimeServerEnvelope>(json, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (message is null)
            {
                continue;
            }

            if (message.Pc is long responseSeq && _pending.TryGetValue(responseSeq, out var pendingRequest))
            {
                pendingRequest.TrySetResult(message);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(message.Opcode))
            {
                EventReceived?.Invoke(this, new RealtimeEventMessage
                {
                    Opcode = message.Opcode,
                    Payload = message.Result
                });
            }
        }

        FailPending(new OperationCanceledException("Realtime connection closed."));
    }

    private void FailPending(Exception exception)
    {
        foreach (var kvp in _pending)
        {
            kvp.Value.TrySetException(exception);
        }

        _pending.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        _readerCts.Cancel();

        if (_transport is not null)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
        }

        if (_readerLoop is not null)
        {
            try
            {
                await _readerLoop.ConfigureAwait(false);
            }
            catch
            {
                // Ignore shutdown errors.
            }
        }

        _readerCts.Dispose();
    }
}
