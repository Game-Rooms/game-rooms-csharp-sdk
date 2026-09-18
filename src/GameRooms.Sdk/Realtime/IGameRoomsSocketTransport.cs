using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Defines websocket transport operations used by the realtime client.
/// </summary>
public interface IGameRoomsSocketTransport : IAsyncDisposable
{
    /// <summary>
    /// Connects the websocket transport to the provided URI.
    /// </summary>
    /// <param name="uri">The websocket endpoint URI.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a text message over the websocket transport.
    /// </summary>
    /// <param name="payload">The text payload to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendTextAsync(string payload, CancellationToken cancellationToken);

    /// <summary>
    /// Receives a text message from the websocket transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The received text payload, or <see langword="null"/> when closed.</returns>
    Task<string?> ReceiveTextAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Closes the websocket transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CloseAsync(CancellationToken cancellationToken);
}
