using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Models;

/// <summary>
/// Represents the payload used to create a new game room.
/// </summary>
public sealed class CreateRoomRequest
{
    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    [JsonPropertyName("appId")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional host metadata payload.
    /// </summary>
    [JsonPropertyName("host")]
    public JsonElement? Host { get; set; }

    /// <summary>
    /// Gets or sets optional custom room metadata.
    /// </summary>
    [JsonPropertyName("meta")]
    public JsonElement? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the maximum player count.
    /// </summary>
    [JsonPropertyName("maxPlayers")]
    public int? MaxPlayers { get; set; }
}
