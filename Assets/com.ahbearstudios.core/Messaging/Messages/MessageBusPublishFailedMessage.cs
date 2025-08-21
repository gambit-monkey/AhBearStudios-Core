using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message publishing fails.
/// Replaces MessagePublishFailedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusPublishFailedMessage : IMessage
{
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the unique type code for this message type.
    /// </summary>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusPublishFailedMessage;

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the priority level for message processing.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets optional correlation ID for message tracing across systems.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the message that failed to publish.
    /// </summary>
    public IMessage FailedMessage { get; init; }

    /// <summary>
    /// Gets the exception that caused the publishing failure.
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// Gets the timestamp when the failure occurred.
    /// </summary>
    public DateTime FailedAt { get; init; }

    /// <summary>
    /// Gets the name of the publisher that failed.
    /// </summary>
    public FixedString64Bytes PublisherName { get; init; }

    /// <summary>
    /// Gets the number of publish attempts made.
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// Gets whether this was a final failure (no more retries).
    /// </summary>
    public bool IsFinalFailure { get; init; }

    /// <summary>
    /// Gets additional context about the failure.
    /// </summary>
    public FixedString512Bytes FailureContext { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageBusPublishFailedMessage struct.
    /// </summary>
    public MessageBusPublishFailedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        FailedMessage = default;
        Exception = default;
        FailedAt = default;
        PublisherName = default;
        AttemptCount = default;
        IsFinalFailure = default;
        FailureContext = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageBusPublishFailedMessage.
    /// </summary>
    /// <param name="failedMessage">The message that failed to publish</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="publisherName">The name of the publisher</param>
    /// <param name="attemptCount">The number of attempts made</param>
    /// <param name="isFinalFailure">Whether this was a final failure</param>
    /// <param name="failureContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusPublishFailedMessage instance</returns>
    public static MessageBusPublishFailedMessage Create(
        IMessage failedMessage,
        Exception exception,
        string publisherName = null,
        int attemptCount = 1,
        bool isFinalFailure = false,
        string failureContext = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageBusPublishFailedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusPublishFailedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.High, // Publish failures are high priority
            CorrelationId = correlationId == default ? failedMessage?.CorrelationId ?? Guid.NewGuid() : correlationId,
            FailedMessage = failedMessage ?? throw new ArgumentNullException(nameof(failedMessage)),
            Exception = exception ?? throw new ArgumentNullException(nameof(exception)),
            FailedAt = DateTime.UtcNow,
            PublisherName = publisherName?.Length <= 64 ? publisherName : publisherName?[..64] ?? "Unknown",
            AttemptCount = Math.Max(1, attemptCount),
            IsFinalFailure = isFinalFailure,
            FailureContext = failureContext?.Length <= 256 ? failureContext : failureContext?[..256] ?? string.Empty
        };
    }
}