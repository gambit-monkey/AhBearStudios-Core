using System;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Statistics for a MessagePipe subscription.
/// </summary>
public sealed record MessagePipeSubscriptionStatistics
{
    /// <summary>
    /// Gets the subscription identifier.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets the number of messages received.
    /// </summary>
    public long MessagesReceived { get; init; }

    /// <summary>
    /// Gets the total processing time in milliseconds.
    /// </summary>
    public long TotalProcessingTime { get; init; }

    /// <summary>
    /// Gets the average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime { get; init; }
}