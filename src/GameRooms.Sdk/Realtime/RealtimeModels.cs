using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Realtime;

public enum GameRoomsConnectionRole
{
    Host,
    Player
}

public enum GameRoomsObjectType
{
    Object,
    Text,
    Number
}

public sealed class RealtimeCommandEnvelope
{
    [JsonPropertyName("opcode")]
    public string Opcode { get; set; } = string.Empty;

    [JsonPropertyName("seq")]
    public long Seq { get; set; }

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public sealed class RealtimeServerEnvelope
{
    [JsonPropertyName("pc")]
    public long? Pc { get; set; }

    [JsonPropertyName("opcode")]
    public string? Opcode { get; set; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }

    [JsonPropertyName("re")]
    public RealtimeError? Error { get; set; }
}

public sealed class RealtimeError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class RealtimeEventMessage
{
    public string Opcode { get; set; } = string.Empty;

    public JsonElement? Payload { get; set; }
}

public sealed class RealtimeRequestException : System.Exception
{
    public RealtimeRequestException(string message, RealtimeError? error = null)
        : base(message)
    {
        Error = error;
    }

    public RealtimeError? Error { get; }
}
