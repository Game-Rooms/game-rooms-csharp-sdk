namespace GameRooms.Sdk.Http;

/// <summary>
/// Represents classified SDK-level API error outcomes.
/// </summary>
public enum GameRoomsErrorCode
{
    /// <summary>
    /// Represents an unclassified error.
    /// </summary>
    Unknown,

    /// <summary>
    /// Represents a missing room.
    /// </summary>
    RoomNotFound,

    /// <summary>
    /// Represents a locked room.
    /// </summary>
    RoomLocked,

    /// <summary>
    /// Represents a full room.
    /// </summary>
    RoomFull,

    /// <summary>
    /// Represents an unauthorized request.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Represents a validation failure.
    /// </summary>
    ValidationFailed
}
