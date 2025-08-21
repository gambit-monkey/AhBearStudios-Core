using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a MessagePipe subscription is created.
/// Replaces MessagePipe subscription creation events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipeSubscriptionCreatedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessagePipeSubscriptionCreatedMessage;

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
    /// Gets the subscription identifier.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type for the subscription.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name for the subscription.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Gets the subscriber name or identifier.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets whether the subscription is for a persistent channel.
    /// </summary>
    public bool IsPersistent { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessagePipeSubscriptionCreatedMessage struct.
    /// </summary>
    public MessagePipeSubscriptionCreatedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        SubscriptionId = default;
        MessageType = default;
        ChannelName = default;
        SubscriberName = default;
        IsPersistent = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessagePipeSubscriptionCreatedMessage.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type for the subscription</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="isPersistent">Whether the subscription is persistent</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeSubscriptionCreatedMessage instance</returns>
    public static MessagePipeSubscriptionCreatedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string channelName = null,
        string subscriberName = null,
        bool isPersistent = false,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessagePipeSubscriptionCreatedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipeSubscriptionCreatedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Low, // Subscription creation is informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            ChannelName = channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default",
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            IsPersistent = isPersistent
        };
    }
}