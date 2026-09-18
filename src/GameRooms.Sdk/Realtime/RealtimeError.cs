using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Represents a realtime protocol error payload.
/// </summary>
public sealed class RealtimeError
{
    /// <summary>
    /// Gets or sets the machine-readable error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
