using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Represents an inbound realtime server envelope.
/// </summary>
public sealed class RealtimeServerEnvelope
{
    /// <summary>
    /// Gets or sets the response correlation sequence.
    /// </summary>
    [JsonPropertyName("pc")]
    public long? Pc { get; set; }

    /// <summary>
    /// Gets or sets the event or response opcode.
    /// </summary>
    [JsonPropertyName("opcode")]
    public string? Opcode { get; set; }

    /// <summary>
    /// Gets or sets the response or event payload.
    /// </summary>
    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }

    /// <summary>
    /// Gets or sets error details when the command fails.
    /// </summary>
    [JsonPropertyName("re")]
    public RealtimeError? Error { get; set; }
}
