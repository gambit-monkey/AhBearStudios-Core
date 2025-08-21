using System;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Represents a message pending retry.
/// Immutable record for thread-safe operations and consistent data handling.
/// </summary>
public sealed record PendingMessage
{
    /// <summary>
    /// Gets the message to retry.
    /// </summary>
    public object Message { get; init; }

    /// <summary>
    /// Gets the message type.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the number of failed attempts.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the last attempt.
    /// </summary>
    public DateTime LastAttempt { get; init; }

    /// <summary>
    /// Gets the timestamp for the next retry attempt.
    /// </summary>
    public DateTime NextRetry { get; init; }

    /// <summary>
    /// Gets the original failure exception.
    /// </summary>
    public Exception OriginalException { get; init; }

    /// <summary>
    /// Gets the retry attempt context.
    /// </summary>
    public string RetryContext { get; init; }

    /// <summary>
    /// Initializes a new instance of the PendingMessage record.
    /// </summary>
    /// <param name="message">The message to retry</param>
    /// <param name="messageType">The message type</param>
    /// <param name="failureCount">The number of failed attempts</param>
    /// <param name="lastAttempt">The timestamp of the last attempt</param>
    /// <param name="nextRetry">The timestamp for the next retry</param>
    /// <param name="originalException">The original failure exception</param>
    /// <param name="retryContext">The retry context</param>
    public PendingMessage(
        object message,
        Type messageType,
        int failureCount = 0,
        DateTime lastAttempt = default,
        DateTime nextRetry = default,
        Exception originalException = null,
        string retryContext = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        FailureCount = failureCount;
        LastAttempt = lastAttempt == default ? DateTime.UtcNow : lastAttempt;
        NextRetry = nextRetry == default ? DateTime.UtcNow.AddMinutes(1) : nextRetry;
        OriginalException = originalException;
        RetryContext = retryContext ?? string.Empty;
    }
}