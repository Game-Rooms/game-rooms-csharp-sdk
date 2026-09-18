using System;

namespace GameRooms.Sdk;

public sealed class GameRoomsClientOptions
{
    public Uri HttpBaseUri { get; set; } = null!;
    public Uri? WebSocketBaseUri { get; set; }

    public string CreateRoomPath { get; set; } = "/rooms";
    public string RoomLookupPathTemplate { get; set; } = "/rooms/{code}";
    public string AppConfigPathTemplate { get; set; } = "/apps/{appId}/config";
}
