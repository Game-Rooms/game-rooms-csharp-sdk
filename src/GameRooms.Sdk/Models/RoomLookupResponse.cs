using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Models;

/// <summary>
/// Represents the API response when resolving a room by code.
/// </summary>
public sealed class RoomLookupResponse
{
    /// <summary>
    /// Gets or sets the unique room identifier.
    /// </summary>
    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }

    /// <summary>
    /// Gets or sets the short room code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets whether the room is locked.
    /// </summary>
    [JsonPropertyName("locked")]
    public bool? Locked { get; set; }

    /// <summary>
    /// Gets or sets whether the room is full.
    /// </summary>
    [JsonPropertyName("full")]
    public bool? Full { get; set; }

    /// <summary>
    /// Gets or sets the host websocket URL.
    /// </summary>
    [JsonPropertyName("hostUrl")]
    public string? HostUrl { get; set; }

    /// <summary>
    /// Gets or sets the player websocket URL.
    /// </summary>
    [JsonPropertyName("playerUrl")]
    public string? PlayerUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional raw payload.
    /// </summary>
    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}
