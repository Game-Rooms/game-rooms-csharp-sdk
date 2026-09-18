using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Models;

/// <summary>
/// Represents the API response for room creation.
/// </summary>
public sealed class CreateRoomResponse
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
    /// Gets or sets the room expiration time.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the optional raw payload.
    /// </summary>
    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}
