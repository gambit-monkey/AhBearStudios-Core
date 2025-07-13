namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Represents a failed message in the dead letter queue.
/// </summary>
public sealed class FailedMessage
{
    /// <summary>
    /// Gets or sets the failed message.
    /// </summary>
    public object Message { get; set; }

    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public Type MessageType { get; set; }

    /// <summary>
    /// Gets or sets the failure reason.
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message failed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}