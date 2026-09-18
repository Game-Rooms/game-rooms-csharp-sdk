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
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private IGameRoomsSocketTransport? _transport;
    private Task? _readerLoop;
    private long _seq;
    private int _disposeState;

    public GameRoomsRealtimeClient()
        : this(() => new ClientWebSocketTransport())
    {
    }

    public GameRoomsRealtimeClient(Func<IGameRoomsSocketTransport> transportFactory)
    {
        _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
    }

    public event EventHandler<RealtimeEventMessage>? EventReceived;
    public event EventHandler<Exception>? EventDispatchError;

    public async Task ConnectAsync(Uri websocketUri, CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
        if (websocketUri is null)
        {
            throw new ArgumentNullException(nameof(websocketUri));
        }
        if (_transport is not null)
        {
            throw new InvalidOperationException("Client is already connected.");
        }

        var transport = _transportFactory();
        try
        {
            await transport.ConnectAsync(websocketUri, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        _transport = transport;
        _readerLoop = Task.Run(() => ReaderLoopAsync(_readerCts.Token), CancellationToken.None);
        }
        finally
        {
            _lifecycleLock.Release();
        }
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
        cancellationToken.ThrowIfCancellationRequested();

        if (_transport is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }
        var transport = _transport;

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
            await transport.SendTextAsync(json, cancellationToken).ConfigureAwait(false);

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
        Exception? shutdownException = null;

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
                shutdownException = new InvalidOperationException("Realtime connection closed by server.");
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
                try
                {
                    EventReceived?.Invoke(this, new RealtimeEventMessage
                    {
                        Opcode = message.Opcode,
                        Payload = message.Result
                    });
                }
                catch (Exception ex)
                {
                    try
                    {
                        EventDispatchError?.Invoke(this, ex);
                    }
                    catch
                    {
                        // Ignore observer exceptions.
                    }
                }
            }
        }

        if (shutdownException is null)
        {
            shutdownException = cancellationToken.IsCancellationRequested
                ? new OperationCanceledException("Realtime connection closed.")
                : new InvalidOperationException("Realtime connection closed.");
        }

        FailPending(shutdownException);
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
        if (Interlocked.Exchange(ref _disposeState, 1) == 1)
        {
            return;
        }

        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _readerCts.Cancel();

            if (_transport is not null)
            {
                await _transport.CloseAsync(CancellationToken.None).ConfigureAwait(false);
                await _transport.DisposeAsync().ConfigureAwait(false);
                _transport = null;
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
        finally
        {
            _lifecycleLock.Release();
        }
    }
}
