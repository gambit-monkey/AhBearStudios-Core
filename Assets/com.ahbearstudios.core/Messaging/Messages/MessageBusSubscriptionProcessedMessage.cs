using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a subscription successfully processes a message.
/// Provides processing metrics and performance information for monitoring.
/// </summary>
public readonly record struct MessageBusSubscriptionProcessedMessage : IMessage
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
    /// Gets the subscription ID that processed the message.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type that was processed.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the timestamp when processing was completed.
    /// </summary>
    public DateTime ProcessedAt { get; init; }

    /// <summary>
    /// Gets the subscriber name that processed the message.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the processing duration.
    /// </summary>
    public TimeSpan ProcessingDuration { get; init; }

    /// <summary>
    /// Gets whether the processing was successful.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Gets additional context about the processing operation.
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
    /// Creates a new instance of MessageBusSubscriptionProcessedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID that processed the message</param>
    /// <param name="messageType">The message type that was processed</param>
    /// <param name="subscriberName">The subscriber name that processed the message</param>
    /// <param name="processingDuration">The processing duration</param>
    /// <param name="isSuccessful">Whether the processing was successful</param>
    /// <param name="context">Additional context about the processing operation</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriptionProcessedMessage instance</returns>
    public static MessageBusSubscriptionProcessedMessage CreateFromFixedStrings(
        Guid subscriptionId,
        Type messageType,
        string subscriberName,
        TimeSpan processingDuration,
        bool isSuccessful,
        string context,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusSubscriptionProcessing", null)
            : correlationId;

        return new MessageBusSubscriptionProcessedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusSubscriptionProcessedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusSubscriptionProcessedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            ProcessedAt = DateTime.UtcNow,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            ProcessingDuration = processingDuration,
            IsSuccessful = isSuccessful,
            Context = context?.Length <= 512 ? context : context?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusSubscriptionProcessedMessage using string parameters.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID that processed the message</param>
    /// <param name="messageType">The message type that was processed</param>
    /// <param name="subscriberName">The subscriber name that processed the message</param>
    /// <param name="processingDuration">The processing duration</param>
    /// <param name="isSuccessful">Whether the processing was successful</param>
    /// <param name="context">Additional context about the processing operation</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriptionProcessedMessage instance</returns>
    public static MessageBusSubscriptionProcessedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string subscriberName = null,
        TimeSpan processingDuration = default,
        bool isSuccessful = true,
        string context = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            subscriptionId,
            messageType,
            subscriberName,
            processingDuration,
            isSuccessful,
            context,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}