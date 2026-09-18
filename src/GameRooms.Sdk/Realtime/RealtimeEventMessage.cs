using System.Text.Json;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Represents an event emitted by the realtime client.
/// </summary>
public sealed class RealtimeEventMessage
{
    /// <summary>
    /// Gets or sets the event opcode.
    /// </summary>
    public string Opcode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event payload.
    /// </summary>
    public JsonElement? Payload { get; set; }
}
