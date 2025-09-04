using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a message subscriber is created.
/// Provides information about new subscriber instances for monitoring and diagnostics.
/// </summary>
public readonly record struct MessageBusSubscriberCreatedMessage : IMessage
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
    /// Gets the subscriber ID that was created.
    /// </summary>
    public Guid SubscriberId { get; init; }

    /// <summary>
    /// Gets the message type that the subscriber handles.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the subscription category.
    /// </summary>
    public FixedString64Bytes Category { get; init; }

    /// <summary>
    /// Gets the timestamp when the subscriber was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the subscriber name or identifier.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets additional context about the subscriber creation.
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
    /// Creates a new instance of MessageBusSubscriberCreatedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID that was created</param>
    /// <param name="messageType">The message type that the subscriber handles</param>
    /// <param name="category">The subscription category</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="context">Additional context about the subscriber creation</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriberCreatedMessage instance</returns>
    public static MessageBusSubscriberCreatedMessage CreateFromFixedStrings(
        Guid subscriberId,
        Type messageType,
        string category,
        string subscriberName,
        string context,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusSubscriber", null)
            : correlationId;

        return new MessageBusSubscriberCreatedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusSubscriberCreatedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusSubscriberCreatedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            SubscriberId = subscriberId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            Category = category?.Length <= 64 ? category : category?[..64] ?? "Default",
            CreatedAt = DateTime.UtcNow,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            Context = context?.Length <= 512 ? context : context?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusSubscriberCreatedMessage using string parameters.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID that was created</param>
    /// <param name="messageType">The message type that the subscriber handles</param>
    /// <param name="category">The subscription category</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="context">Additional context about the subscriber creation</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriberCreatedMessage instance</returns>
    public static MessageBusSubscriberCreatedMessage Create(
        Guid subscriberId,
        Type messageType,
        string category = null,
        string subscriberName = null,
        string context = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            subscriberId,
            messageType,
            category,
            subscriberName,
            context,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}