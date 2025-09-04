using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a subscription fails to process a message.
/// Provides error information and failure diagnostics for monitoring.
/// </summary>
public readonly record struct MessageBusSubscriptionFailedMessage : IMessage
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
    /// Gets the subscription ID that failed to process the message.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type that failed to be processed.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the timestamp when the failure occurred.
    /// </summary>
    public DateTime FailedAt { get; init; }

    /// <summary>
    /// Gets the subscriber name that failed to process the message.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; init; }

    /// <summary>
    /// Gets whether this is the final failure (no more retries).
    /// </summary>
    public bool IsFinalFailure { get; init; }

    /// <summary>
    /// Gets additional context about the failure.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageBusSubscriptionFailedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID that failed to process the message</param>
    /// <param name="messageType">The message type that failed to be processed</param>
    /// <param name="subscriberName">The subscriber name that failed to process the message</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="retryAttempts">The number of retry attempts made</param>
    /// <param name="isFinalFailure">Whether this is the final failure</param>
    /// <param name="context">Additional context about the failure</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriptionFailedMessage instance</returns>
    public static MessageBusSubscriptionFailedMessage CreateFromFixedStrings(
        Guid subscriptionId,
        Type messageType,
        string subscriberName,
        Exception exception,
        int retryAttempts,
        bool isFinalFailure,
        string context,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusSubscriptionFailure", null)
            : correlationId;

        return new MessageBusSubscriptionFailedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusSubscriptionFailedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusSubscriptionFailedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.High,
            CorrelationId = finalCorrelationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            FailedAt = DateTime.UtcNow,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            Exception = exception ?? throw new ArgumentNullException(nameof(exception)),
            RetryAttempts = Math.Max(0, retryAttempts),
            IsFinalFailure = isFinalFailure,
            Context = context?.Length <= 512 ? context : context?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusSubscriptionFailedMessage using string parameters.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID that failed to process the message</param>
    /// <param name="messageType">The message type that failed to be processed</param>
    /// <param name="subscriberName">The subscriber name that failed to process the message</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="retryAttempts">The number of retry attempts made</param>
    /// <param name="isFinalFailure">Whether this is the final failure</param>
    /// <param name="context">Additional context about the failure</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriptionFailedMessage instance</returns>
    public static MessageBusSubscriptionFailedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string subscriberName = null,
        Exception exception = null,
        int retryAttempts = 0,
        bool isFinalFailure = false,
        string context = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            subscriptionId,
            messageType,
            subscriberName,
            exception,
            retryAttempts,
            isFinalFailure,
            context,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}