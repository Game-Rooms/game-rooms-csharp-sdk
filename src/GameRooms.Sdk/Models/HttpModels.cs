using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Models;

public sealed class CreateRoomRequest
{
    [JsonPropertyName("appId")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public JsonElement? Host { get; set; }

    [JsonPropertyName("meta")]
    public JsonElement? Metadata { get; set; }

    [JsonPropertyName("maxPlayers")]
    public int? MaxPlayers { get; set; }
}

public sealed class CreateRoomResponse
{
    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("hostUrl")]
    public string? HostUrl { get; set; }

    [JsonPropertyName("playerUrl")]
    public string? PlayerUrl { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}

public sealed class RoomLookupResponse
{
    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("locked")]
    public bool? Locked { get; set; }

    [JsonPropertyName("full")]
    public bool? Full { get; set; }

    [JsonPropertyName("hostUrl")]
    public string? HostUrl { get; set; }

    [JsonPropertyName("playerUrl")]
    public string? PlayerUrl { get; set; }

    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}

public sealed class AppConfigResponse
{
    [JsonPropertyName("appId")]
    public string? AppId { get; set; }

    [JsonPropertyName("config")]
    public Dictionary<string, JsonElement>? Config { get; set; }

    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}

internal sealed class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
