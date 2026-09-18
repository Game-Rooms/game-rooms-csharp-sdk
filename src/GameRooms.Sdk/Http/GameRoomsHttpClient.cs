using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameRooms.Sdk.Models;

namespace GameRooms.Sdk.Http;

public sealed class GameRoomsHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly GameRoomsClientOptions _options;

    public GameRoomsHttpClient(HttpClient httpClient, GameRoomsClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_options.HttpBaseUri is null)
        {
            throw new ArgumentException("HttpBaseUri is required.", nameof(options));
        }
    }

    public Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SendAsync<CreateRoomResponse>(HttpMethod.Post, _options.CreateRoomPath, request, cancellationToken);
    }

    public Task<RoomLookupResponse> GetRoomByCodeAsync(string roomCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new ArgumentException("Room code is required.", nameof(roomCode));
        }

        var path = _options.RoomLookupPathTemplate.Replace("{code}", Uri.EscapeDataString(roomCode.Trim()), StringComparison.Ordinal);
        return SendAsync<RoomLookupResponse>(HttpMethod.Get, path, body: null, cancellationToken);
    }

    public Task<AppConfigResponse> GetAppConfigAsync(string appId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            throw new ArgumentException("App ID is required.", nameof(appId));
        }

        var path = _options.AppConfigPathTemplate.Replace("{appId}", Uri.EscapeDataString(appId.Trim()), StringComparison.Ordinal);
        return SendAsync<AppConfigResponse>(HttpMethod.Get, path, body: null, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken)
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
            throw await CreateExceptionAsync(response.StatusCode, contentStream, cancellationToken).ConfigureAwait(false);
        }

        var payload = await JsonSerializer.DeserializeAsync<T>(contentStream, JsonOptions, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            throw new InvalidOperationException("API returned an empty payload.");
        }

        return payload;
    }

    private static async Task<GameRoomsApiException> CreateExceptionAsync(HttpStatusCode statusCode, System.IO.Stream contentStream, CancellationToken cancellationToken)
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

        var errorCode = ClassifyError(statusCode, errorPayload?.Error);
        var message = errorPayload?.Message ?? errorPayload?.Error ?? $"Game Rooms API call failed with HTTP {(int)statusCode}.";
        return new GameRoomsApiException(statusCode, errorCode, message);
    }

    private static GameRoomsErrorCode ClassifyError(HttpStatusCode statusCode, string? error)
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
            HttpStatusCode.NotFound => GameRoomsErrorCode.RoomNotFound,
            HttpStatusCode.Locked => GameRoomsErrorCode.RoomLocked,
            HttpStatusCode.Conflict => GameRoomsErrorCode.RoomFull,
            HttpStatusCode.Unauthorized => GameRoomsErrorCode.Unauthorized,
            HttpStatusCode.BadRequest => GameRoomsErrorCode.ValidationFailed,
            _ => GameRoomsErrorCode.Unknown
        };
    }
}
