using System;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Interface for MessagePipe subscription wrapper that provides disposal tracking and statistics.
/// Follows CLAUDE.md guidelines for proper interface design and Unity performance optimization.
/// </summary>
public interface IMessagePipeSubscriptionWrapper : IDisposable
{
    /// <summary>
    /// Gets the unique subscription identifier.
    /// </summary>
    Guid SubscriptionId { get; }

    /// <summary>
    /// Gets the message type for this subscription.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Gets when this subscription was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets whether this subscription has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets the total number of messages received by this subscription.
    /// </summary>
    long MessagesReceived { get; }

    /// <summary>
    /// Gets the total processing time in milliseconds for all messages.
    /// </summary>
    long TotalProcessingTimeMs { get; }

    /// <summary>
    /// Gets the subscription statistics.
    /// </summary>
    /// <returns>Current subscription statistics</returns>
    MessagePipeSubscriptionStatistics GetStatistics();

    /// <summary>
    /// Records a message receipt for statistics tracking.
    /// </summary>
    /// <param name="processingTimeMs">Processing time in milliseconds</param>
    void RecordMessageReceived(long processingTimeMs);
}