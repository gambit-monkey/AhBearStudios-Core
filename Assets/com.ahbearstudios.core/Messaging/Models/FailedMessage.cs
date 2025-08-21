using System;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Represents a failed message in the dead letter queue.
/// Immutable record for thread-safe operations and consistent data handling.
/// </summary>
public sealed record FailedMessage
{
    /// <summary>
    /// Gets the failed message.
    /// </summary>
    public object Message { get; init; }

    /// <summary>
    /// Gets the message type.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the failure reason.
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Gets the timestamp when the message failed.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// Gets the number of attempts made before final failure.
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// Gets the name of the component that failed to process the message.
    /// </summary>
    public string FailedComponent { get; init; }

    /// <summary>
    /// Gets additional failure context.
    /// </summary>
    public string FailureContext { get; init; }

    /// <summary>
    /// Initializes a new instance of the FailedMessage record.
    /// </summary>
    /// <param name="message">The failed message</param>
    /// <param name="messageType">The message type</param>
    /// <param name="reason">The failure reason</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="attemptCount">The number of attempts made</param>
    /// <param name="failedComponent">The component that failed</param>
    /// <param name="failureContext">Additional failure context</param>
    public FailedMessage(
        object message,
        Type messageType,
        string reason,
        Exception exception = null,
        int attemptCount = 1,
        string failedComponent = null,
        string failureContext = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        Timestamp = DateTime.UtcNow;
        Exception = exception;
        AttemptCount = attemptCount;
        FailedComponent = failedComponent ?? string.Empty;
        FailureContext = failureContext ?? string.Empty;
    }
}