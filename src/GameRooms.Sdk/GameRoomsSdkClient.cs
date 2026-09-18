using System;
using System.Net.Http;
using System.Threading.Tasks;
using GameRooms.Sdk.Http;
using GameRooms.Sdk.Realtime;

namespace GameRooms.Sdk;

/// <summary>
/// Provides an SDK entry point that exposes HTTP and realtime clients.
/// </summary>
public sealed class GameRoomsSdkClient : IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameRoomsSdkClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for API requests.</param>
    /// <param name="options">The SDK endpoint configuration options.</param>
    public GameRoomsSdkClient(HttpClient httpClient, GameRoomsClientOptions options)
    {
        Http = new GameRoomsHttpClient(httpClient, options);
        Realtime = new GameRoomsRealtimeClient();
    }

    /// <summary>
    /// Gets the HTTP API client.
    /// </summary>
    public GameRoomsHttpClient Http { get; }

    /// <summary>
    /// Gets the realtime websocket client.
    /// </summary>
    public GameRoomsRealtimeClient Realtime { get; }

    /// <summary>
    /// Builds an absolute realtime URI from a returned endpoint string.
    /// </summary>
    /// <param name="relativeOrAbsolute">The endpoint URI string.</param>
    /// <returns>The absolute URI.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the URI is not absolute.</exception>
    public Uri BuildRealtimeUri(string relativeOrAbsolute)
    {
        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        throw new InvalidOperationException("Use absolute websocket URI from room create/lookup responses.");
    }

    /// <summary>
    /// Disposes the underlying realtime client.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        return Realtime.DisposeAsync();
    }
}
