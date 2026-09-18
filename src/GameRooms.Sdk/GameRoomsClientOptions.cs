using System;

namespace GameRooms.Sdk;

/// <summary>
/// Represents configurable endpoint options for the Game Rooms SDK.
/// </summary>
public sealed class GameRoomsClientOptions
{
    /// <summary>
    /// Gets or sets the base URI for HTTP API requests.
    /// </summary>
    public Uri HttpBaseUri { get; set; } = null!;

    /// <summary>
    /// Gets or sets an optional websocket base URI.
    /// </summary>
    public Uri? WebSocketBaseUri { get; set; }

    /// <summary>
    /// Gets or sets the relative path used for room creation requests.
    /// </summary>
    public string CreateRoomPath { get; set; } = "/rooms";

    /// <summary>
    /// Gets or sets the room-lookup path template containing a <c>{code}</c> token.
    /// </summary>
    public string RoomLookupPathTemplate { get; set; } = "/rooms/{code}";

    /// <summary>
    /// Gets or sets the app configuration path template containing an <c>{appId}</c> token.
    /// </summary>
    public string AppConfigPathTemplate { get; set; } = "/apps/{appId}/config";
}
