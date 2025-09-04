using System;
using AhBearStudios.Core.Common.Utilities;
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
    #region IMessage Implementation

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
    public ushort TypeCode { get; init; }

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

    #endregion

    #region Message-Specific Properties

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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageBusPublishFailedMessage using FixedString parameters for optimal performance.
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
    public static MessageBusPublishFailedMessage CreateFromFixedStrings(
        IMessage failedMessage,
        Exception exception,
        FixedString64Bytes publisherName = default,
        int attemptCount = 1,
        bool isFinalFailure = false,
        FixedString512Bytes failureContext = default,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? (failedMessage?.CorrelationId ?? DeterministicIdGenerator.GenerateCorrelationId("MessageBusPublishFailed", publisherName.ToString()))
            : correlationId;

        return new MessageBusPublishFailedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusPublishFailedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusPublishFailedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.High, // Publish failures are high priority
            CorrelationId = finalCorrelationId,
            FailedMessage = failedMessage ?? throw new ArgumentNullException(nameof(failedMessage)),
            Exception = exception ?? throw new ArgumentNullException(nameof(exception)),
            FailedAt = DateTime.UtcNow,
            PublisherName = publisherName.IsEmpty ? "Unknown" : publisherName,
            AttemptCount = Math.Max(1, attemptCount),
            IsFinalFailure = isFinalFailure,
            FailureContext = failureContext.IsEmpty ? string.Empty : failureContext
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusPublishFailedMessage using string parameters.
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
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            failedMessage,
            exception,
            publisherName?.Length <= 64 ? publisherName : publisherName?[..64] ?? "Unknown",
            attemptCount,
            isFinalFailure,
            failureContext?.Length <= 256 ? failureContext : failureContext?[..256] ?? string.Empty,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}