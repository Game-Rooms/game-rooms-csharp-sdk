using System;
using System.Net.Http;
using System.Threading.Tasks;
using GameRooms.Sdk.Http;
using GameRooms.Sdk.Realtime;

namespace GameRooms.Sdk;

public sealed class GameRoomsSdkClient : IAsyncDisposable
{
    public GameRoomsSdkClient(HttpClient httpClient, GameRoomsClientOptions options)
    {
        Http = new GameRoomsHttpClient(httpClient, options);
        Realtime = new GameRoomsRealtimeClient();
    }

    public GameRoomsHttpClient Http { get; }

    public GameRoomsRealtimeClient Realtime { get; }

    public Uri BuildRealtimeUri(string relativeOrAbsolute)
    {
        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        throw new InvalidOperationException("Use absolute websocket URI from room create/lookup responses.");
    }

    public ValueTask DisposeAsync()
    {
        return Realtime.DisposeAsync();
    }
}
