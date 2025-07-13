namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Represents a message pending retry.
/// </summary>
public sealed class PendingMessage
{
    /// <summary>
    /// Gets or sets the message to retry.
    /// </summary>
    public object Message { get; set; }

    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public Type MessageType { get; set; }

    /// <summary>
    /// Gets or sets the number of failed attempts.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last attempt.
    /// </summary>
    public DateTime LastAttempt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for the next retry attempt.
    /// </summary>
    public DateTime NextRetry { get; set; }
}