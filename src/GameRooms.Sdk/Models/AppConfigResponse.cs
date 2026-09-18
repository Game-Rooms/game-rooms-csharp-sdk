using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Models;

/// <summary>
/// Represents the API response for application configuration.
/// </summary>
public sealed class AppConfigResponse
{
    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    [JsonPropertyName("appId")]
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets the configuration values returned by the API.
    /// </summary>
    [JsonPropertyName("config")]
    public Dictionary<string, JsonElement>? Config { get; set; }

    /// <summary>
    /// Gets or sets the optional raw payload.
    /// </summary>
    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}
