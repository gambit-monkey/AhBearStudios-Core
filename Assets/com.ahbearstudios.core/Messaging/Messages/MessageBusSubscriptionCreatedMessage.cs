using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a new message subscription is created.
/// Replaces SubscriptionCreatedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusSubscriptionCreatedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusSubscriptionCreatedMessage;

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
    /// Gets the unique identifier of the subscription.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the type of message being subscribed to.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the name of the subscriber.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the timestamp when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the subscription category.
    /// </summary>
    public FixedString64Bytes Category { get; init; }

    /// <summary>
    /// Gets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets additional context about the subscription.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageBusSubscriptionCreatedMessage struct.
    /// </summary>
    public MessageBusSubscriptionCreatedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        SubscriptionId = default;
        MessageType = default;
        SubscriberName = default;
        CreatedAt = default;
        Category = default;
        IsActive = default;
        Context = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageBusSubscriptionCreatedMessage.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription</param>
    /// <param name="messageType">The type of message being subscribed to</param>
    /// <param name="subscriberName">The name of the subscriber</param>
    /// <param name="category">The subscription category</param>
    /// <param name="isActive">Whether the subscription is active</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriptionCreatedMessage instance</returns>
    public static MessageBusSubscriptionCreatedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string subscriberName = null,
        string category = null,
        bool isActive = true,
        string context = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageBusSubscriptionCreatedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusSubscriptionCreatedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Subscription events are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            CreatedAt = DateTime.UtcNow,
            Category = category?.Length <= 64 ? category : category?[..64] ?? "Default",
            IsActive = isActive,
            Context = context?.Length <= 256 ? context : context?[..256] ?? string.Empty
        };
    }
}