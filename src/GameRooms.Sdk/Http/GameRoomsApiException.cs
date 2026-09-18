using System;
using System.Net;

namespace GameRooms.Sdk.Http;

public enum GameRoomsErrorCode
{
    Unknown,
    RoomNotFound,
    RoomLocked,
    RoomFull,
    Unauthorized,
    ValidationFailed
}

public sealed class GameRoomsApiException : Exception
{
    public GameRoomsApiException(HttpStatusCode statusCode, GameRoomsErrorCode errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public GameRoomsErrorCode ErrorCode { get; }
}
