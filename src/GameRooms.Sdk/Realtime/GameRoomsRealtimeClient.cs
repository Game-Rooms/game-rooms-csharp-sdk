using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Provides realtime websocket operations for the Game Rooms protocol.
/// </summary>
public sealed class GameRoomsRealtimeClient : IAsyncDisposable
{
    /// <summary>
    /// Gets shared serializer settings used by this client.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Gets the transport factory used to create websocket transports.
    /// </summary>
    private readonly Func<IGameRoomsSocketTransport> _transportFactory;

    /// <summary>
    /// Gets pending requests keyed by sequence number.
    /// </summary>
    private readonly ConcurrentDictionary<long, TaskCompletionSource<RealtimeServerEnvelope>> _pending = new();

    /// <summary>
    /// Gets a cancellation source used to terminate the reader loop.
    /// </summary>
    private readonly CancellationTokenSource _readerCts = new();

    /// <summary>
    /// Gets a lock used to serialize connect and dispose lifecycle actions.
    /// </summary>
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    /// <summary>
    /// Holds the active transport instance.
    /// </summary>
    private IGameRoomsSocketTransport? _transport;

    /// <summary>
    /// Holds the background reader task.
    /// </summary>
    private Task? _readerLoop;

    /// <summary>
    /// Holds the last command sequence value.
    /// </summary>
    private long _seq;

    /// <summary>
    /// Holds the dispose state flag.
    /// </summary>
    private int _disposeState;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameRoomsRealtimeClient"/> class.
    /// </summary>
    public GameRoomsRealtimeClient()
        : this(() => new ClientWebSocketTransport())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameRoomsRealtimeClient"/> class.
    /// </summary>
    /// <param name="transportFactory">The websocket transport factory.</param>
    public GameRoomsRealtimeClient(Func<IGameRoomsSocketTransport> transportFactory)
    {
        _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
    }

    /// <summary>
    /// Occurs when an uncorrelated server event is received.
    /// </summary>
    public event EventHandler<RealtimeEventMessage>? EventReceived;

    /// <summary>
    /// Occurs when an <see cref="EventReceived"/> subscriber throws.
    /// </summary>
    public event EventHandler<Exception>? EventDispatchError;

    /// <summary>
    /// Connects the realtime client to a websocket endpoint.
    /// </summary>
    /// <param name="websocketUri">The websocket endpoint URI.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
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

    /// <summary>
    /// Sends a command and waits for a correlated response.
    /// </summary>
    /// <typeparam name="T">The expected response payload type.</typeparam>
    /// <param name="opcode">The outbound command opcode.</param>
    /// <param name="parameters">The optional command parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response payload.</returns>
    public Task<T?> SendCommandAsync<T>(string opcode, object? parameters = null, CancellationToken cancellationToken = default)
    {
        return SendCommandInternalAsync<T>(opcode, parameters, cancellationToken);
    }

    /// <summary>
    /// Creates an object value in the room object store.
    /// </summary>
    /// <typeparam name="T">The object value type.</typeparam>
    /// <param name="key">The object key.</param>
    /// <param name="value">The object value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<T?> CreateObjectAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var payload = new { type = "object", key, value };
        return SendCommandAsync<T>("object:create", payload, cancellationToken);
    }

    /// <summary>
    /// Creates a text value in the room object store.
    /// </summary>
    /// <param name="key">The object key.</param>
    /// <param name="value">The text value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<string?> CreateTextAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var payload = new { type = "text", key, value };
        return SendCommandAsync<string>("object:create", payload, cancellationToken);
    }

    /// <summary>
    /// Creates a numeric value in the room object store.
    /// </summary>
    /// <typeparam name="T">The numeric value type.</typeparam>
    /// <param name="key">The object key.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<T?> CreateNumberAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : struct
    {
        var payload = new { type = "number", key, value };
        return SendCommandAsync<T?>("object:create", payload, cancellationToken);
    }

    /// <summary>
    /// Updates an object value in the room object store.
    /// </summary>
    /// <typeparam name="T">The object value type.</typeparam>
    /// <param name="key">The object key.</param>
    /// <param name="value">The updated object value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<T?> UpdateObjectAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var payload = new { key, value };
        return SendCommandAsync<T>("object:update", payload, cancellationToken);
    }

    /// <summary>
    /// Reads an object value from the room object store.
    /// </summary>
    /// <typeparam name="T">The object value type.</typeparam>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<T?> GetObjectAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = new { key };
        return SendCommandAsync<T>("object:get", payload, cancellationToken);
    }

    /// <summary>
    /// Locks or unlocks an object in the room object store.
    /// </summary>
    /// <param name="key">The object key.</param>
    /// <param name="locked">Whether to lock the object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<bool?> LockObjectAsync(string key, bool locked = true, CancellationToken cancellationToken = default)
    {
        var payload = new { key, locked };
        return SendCommandAsync<bool?>("object:lock", payload, cancellationToken);
    }

    /// <summary>
    /// Relays a custom payload through the realtime protocol.
    /// </summary>
    /// <typeparam name="T">The expected response payload type.</typeparam>
    /// <param name="relayPayload">The relay payload object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The server response payload.</returns>
    public Task<T?> RelayAsync<T>(object relayPayload, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync<T>("object:relay", relayPayload, cancellationToken);
    }

    /// <summary>
    /// Sends a command and awaits the correlated server response envelope.
    /// </summary>
    /// <typeparam name="T">The expected response payload type.</typeparam>
    /// <param name="opcode">The outbound command opcode.</param>
    /// <param name="parameters">The optional command parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response payload.</returns>
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

    /// <summary>
    /// Runs the background receive loop and dispatches responses/events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous loop.</returns>
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

    /// <summary>
    /// Fails all pending requests with a shared exception.
    /// </summary>
    /// <param name="exception">The exception to apply to all pending requests.</param>
    private void FailPending(Exception exception)
    {
        foreach (var kvp in _pending)
        {
            kvp.Value.TrySetException(exception);
        }

        _pending.Clear();
    }

    /// <summary>
    /// Disposes the realtime client and closes active websocket resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
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
