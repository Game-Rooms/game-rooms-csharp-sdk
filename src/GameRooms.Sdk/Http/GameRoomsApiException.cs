using System;
using System.Net;

namespace GameRooms.Sdk.Http;

/// <summary>
/// Represents an HTTP API exception from Game Rooms endpoints.
/// </summary>
public sealed class GameRoomsApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameRoomsApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorCode">The classified SDK error code.</param>
    /// <param name="message">The exception message.</param>
    public GameRoomsApiException(HttpStatusCode statusCode, GameRoomsErrorCode errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the HTTP status code associated with the failure.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the classified SDK error code.
    /// </summary>
    public GameRoomsErrorCode ErrorCode { get; }
}
