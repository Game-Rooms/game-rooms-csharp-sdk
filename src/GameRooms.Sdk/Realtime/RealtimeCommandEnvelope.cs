using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Represents an outbound realtime command envelope.
/// </summary>
public sealed class RealtimeCommandEnvelope
{
    /// <summary>
    /// Gets or sets the command opcode.
    /// </summary>
    [JsonPropertyName("opcode")]
    public string Opcode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command sequence identifier.
    /// </summary>
    [JsonPropertyName("seq")]
    public long Seq { get; set; }

    /// <summary>
    /// Gets or sets the command parameters payload.
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}
