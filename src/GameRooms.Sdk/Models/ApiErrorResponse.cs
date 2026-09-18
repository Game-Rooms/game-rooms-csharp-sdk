using System.Text.Json.Serialization;

namespace GameRooms.Sdk.Models;

/// <summary>
/// Represents an API error payload returned from HTTP endpoints.
/// </summary>
internal sealed class ApiErrorResponse
{
    /// <summary>
    /// Gets or sets the machine-readable error code.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
