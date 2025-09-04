using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message processing fails in the message bus.
/// Replaces MessageProcessingFailedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageProcessingFailedEventMessage : IMessage
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
    /// Gets the message type code for efficient routing and filtering.
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
    /// Gets the message that failed processing.
    /// </summary>
    public IMessage FailedMessage { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// Gets the timestamp when the failure occurred.
    /// </summary>
    public DateTime FailedAt { get; init; }

    /// <summary>
    /// Gets the number of processing attempts made.
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// Gets whether this was a final failure (no more retries).
    /// </summary>
    public bool IsFinalFailure { get; init; }

    /// <summary>
    /// Gets the subscriber that failed to process the message.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets additional context about the failure.
    /// </summary>
    public FixedString512Bytes FailureContext { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageProcessingFailedEventMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="failedMessage">The message that failed processing</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="attemptCount">The number of attempts made</param>
    /// <param name="isFinalFailure">Whether this was a final failure</param>
    /// <param name="subscriberName">The subscriber that failed</param>
    /// <param name="failureContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageProcessingFailedEventMessage instance</returns>
    public static MessageProcessingFailedEventMessage CreateFromFixedStrings(
        IMessage failedMessage,
        Exception exception,
        int attemptCount,
        bool isFinalFailure,
        string subscriberName,
        string failureContext,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusProcessing", null)
            : correlationId;

        return new MessageProcessingFailedEventMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageProcessingFailedEventMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusProcessingFailedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.High,
            CorrelationId = finalCorrelationId,
            FailedMessage = failedMessage ?? throw new ArgumentNullException(nameof(failedMessage)),
            Exception = exception ?? throw new ArgumentNullException(nameof(exception)),
            FailedAt = DateTime.UtcNow,
            AttemptCount = Math.Max(1, attemptCount),
            IsFinalFailure = isFinalFailure,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            FailureContext = failureContext?.Length <= 512 ? failureContext : failureContext?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageProcessingFailedEventMessage using string parameters.
    /// </summary>
    /// <param name="failedMessage">The message that failed processing</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="attemptCount">The number of attempts made</param>
    /// <param name="isFinalFailure">Whether this was a final failure</param>
    /// <param name="subscriberName">The subscriber that failed</param>
    /// <param name="failureContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageProcessingFailedEventMessage instance</returns>
    public static MessageProcessingFailedEventMessage Create(
        IMessage failedMessage,
        Exception exception,
        int attemptCount = 1,
        bool isFinalFailure = false,
        string subscriberName = null,
        string failureContext = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            failedMessage,
            exception,
            attemptCount,
            isFinalFailure,
            subscriberName,
            failureContext,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}