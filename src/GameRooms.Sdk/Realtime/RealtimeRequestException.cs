using System;

namespace GameRooms.Sdk.Realtime;

/// <summary>
/// Represents a request-level realtime failure.
/// </summary>
public sealed class RealtimeRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeRequestException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="error">The protocol error payload, when available.</param>
    public RealtimeRequestException(string message, RealtimeError? error = null)
        : base(message)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the protocol error payload.
    /// </summary>
    public RealtimeError? Error { get; }
}
