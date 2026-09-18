using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Provides a <see cref="ClientWebSocket"/> implementation of <see cref="IGameRoomsSocketTransport"/>.
/// </summary>
public sealed class ClientWebSocketTransport : IGameRoomsSocketTransport
{
    /// <summary>
    /// Gets the underlying websocket instance.
    /// </summary>
    private readonly ClientWebSocket _socket = new();

    /// <inheritdoc />
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => _socket.ConnectAsync(uri, cancellationToken);

    /// <inheritdoc />
    public async Task SendTextAsync(string payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> ReceiveTextAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4 * 1024);
        try
        {
            var segment = new ArraySegment<byte>(buffer);
            using var aggregate = new MemoryStream();

            while (true)
            {
                var result = await _socket.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                if (result.Count > 0)
                {
                    aggregate.Write(buffer, 0, result.Count);
                }

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(aggregate.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closing", cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Closes and disposes the websocket transport.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await CloseAsync(CancellationToken.None).ConfigureAwait(false);
        _socket.Dispose();
    }
}
