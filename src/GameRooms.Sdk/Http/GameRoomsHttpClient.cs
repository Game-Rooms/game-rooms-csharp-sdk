using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameRooms.Sdk.Models;

namespace GameRooms.Sdk.Http;

/// <summary>
/// Provides HTTP operations for the Game Rooms API surface.
/// </summary>
public sealed class GameRoomsHttpClient
{
    /// <summary>
    /// Gets shared serializer settings used by this client.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Gets the underlying HTTP client.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Gets the SDK endpoint options.
    /// </summary>
    private readonly GameRoomsClientOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameRoomsHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send requests.</param>
    /// <param name="options">The endpoint configuration options.</param>
    /// <exception cref="ArgumentException">Thrown when required options are missing.</exception>
    public GameRoomsHttpClient(HttpClient httpClient, GameRoomsClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_options.HttpBaseUri is null)
        {
            throw new ArgumentException("HttpBaseUri is required.", nameof(options));
        }
    }

    /// <summary>
    /// Creates a new room.
    /// </summary>
    /// <param name="request">The room creation request payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created room response.</returns>
    public Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SendAsync<CreateRoomResponse>(HttpMethod.Post, _options.CreateRoomPath, request, cancellationToken);
    }

    /// <summary>
    /// Looks up a room by short code.
    /// </summary>
    /// <param name="roomCode">The short room code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The room lookup response.</returns>
    public Task<RoomLookupResponse> GetRoomByCodeAsync(string roomCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new ArgumentException("Room code is required.", nameof(roomCode));
        }

        var path = _options.RoomLookupPathTemplate.Replace("{code}", Uri.EscapeDataString(roomCode.Trim()), StringComparison.Ordinal);
        return SendAsync<RoomLookupResponse>(HttpMethod.Get, path, body: null, cancellationToken, notFoundErrorCode: GameRoomsErrorCode.RoomNotFound);
    }

    /// <summary>
    /// Retrieves configuration for a game application.
    /// </summary>
    /// <param name="appId">The application identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The application configuration response.</returns>
    public Task<AppConfigResponse> GetAppConfigAsync(string appId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            throw new ArgumentException("App ID is required.", nameof(appId));
        }

        var path = _options.AppConfigPathTemplate.Replace("{appId}", Uri.EscapeDataString(appId.Trim()), StringComparison.Ordinal);
        return SendAsync<AppConfigResponse>(HttpMethod.Get, path, body: null, cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP request and deserializes a successful JSON payload.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="method">The HTTP method.</param>
    /// <param name="relativePath">The relative API path.</param>
    /// <param name="body">The optional request body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="notFoundErrorCode">The optional error code to map HTTP 404 responses.</param>
    /// <returns>The deserialized response payload.</returns>
    private async Task<T> SendAsync<T>(HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken, GameRoomsErrorCode? notFoundErrorCode = null)
    {
        var requestUri = new Uri(_options.HttpBaseUri, relativePath);
        using var request = new HttpRequestMessage(method, requestUri);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response.StatusCode, contentStream, cancellationToken, notFoundErrorCode).ConfigureAwait(false);
        }

        var payload = await JsonSerializer.DeserializeAsync<T>(contentStream, JsonOptions, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            throw new InvalidOperationException("API returned an empty payload.");
        }

        return payload;
    }

    /// <summary>
    /// Creates a typed API exception from an HTTP error response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="contentStream">The response body stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="notFoundErrorCode">The optional error code to map HTTP 404 responses.</param>
    /// <returns>A classified API exception instance.</returns>
    private static async Task<GameRoomsApiException> CreateExceptionAsync(HttpStatusCode statusCode, Stream contentStream, CancellationToken cancellationToken, GameRoomsErrorCode? notFoundErrorCode)
    {
        ApiErrorResponse? errorPayload = null;

        try
        {
            errorPayload = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(contentStream, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // Fall through to status-code-only classification.
        }

        var errorCode = ClassifyError(statusCode, errorPayload?.Error, notFoundErrorCode);
        var message = errorPayload?.Message ?? errorPayload?.Error ?? $"Game Rooms API call failed with HTTP {(int)statusCode}.";
        return new GameRoomsApiException(statusCode, errorCode, message);
    }

    /// <summary>
    /// Maps HTTP and protocol-level errors to SDK error codes.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The optional protocol error code.</param>
    /// <param name="notFoundErrorCode">The optional error code to map HTTP 404 responses.</param>
    /// <returns>The SDK error classification.</returns>
    private static GameRoomsErrorCode ClassifyError(HttpStatusCode statusCode, string? error, GameRoomsErrorCode? notFoundErrorCode)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return error.ToUpperInvariant() switch
            {
                "ROOM_NOT_FOUND" => GameRoomsErrorCode.RoomNotFound,
                "ROOM_LOCKED" => GameRoomsErrorCode.RoomLocked,
                "ROOM_FULL" => GameRoomsErrorCode.RoomFull,
                "UNAUTHORIZED" => GameRoomsErrorCode.Unauthorized,
                "VALIDATION_FAILED" => GameRoomsErrorCode.ValidationFailed,
                _ => GameRoomsErrorCode.Unknown
            };
        }

        return statusCode switch
        {
            HttpStatusCode.NotFound when notFoundErrorCode.HasValue => notFoundErrorCode.Value,
            HttpStatusCode.Locked => GameRoomsErrorCode.RoomLocked,
            HttpStatusCode.Conflict => GameRoomsErrorCode.RoomFull,
            HttpStatusCode.Unauthorized => GameRoomsErrorCode.Unauthorized,
            HttpStatusCode.BadRequest => GameRoomsErrorCode.ValidationFailed,
            _ => GameRoomsErrorCode.Unknown
        };
    }
}
