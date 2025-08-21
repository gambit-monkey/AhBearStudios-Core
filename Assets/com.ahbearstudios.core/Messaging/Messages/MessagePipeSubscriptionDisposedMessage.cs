using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a MessagePipe subscription is disposed.
/// Replaces MessagePipe subscription disposal events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipeSubscriptionDisposedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessagePipeSubscriptionDisposedMessage;

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
    /// Gets the subscription identifier that was disposed.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type for the disposed subscription.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name for the disposed subscription.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Gets the subscriber name or identifier.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the subscription duration in milliseconds.
    /// </summary>
    public double SubscriptionDurationMs { get; init; }

    /// <summary>
    /// Gets the total number of messages received by this subscription.
    /// </summary>
    public int MessagesReceived { get; init; }

    /// <summary>
    /// Gets the reason for disposal.
    /// </summary>
    public FixedString128Bytes DisposalReason { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessagePipeSubscriptionDisposedMessage struct.
    /// </summary>
    public MessagePipeSubscriptionDisposedMessage()
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
        SubscriptionDurationMs = default;
        MessagesReceived = default;
        DisposalReason = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the subscription duration as a TimeSpan.
    /// </summary>
    public TimeSpan SubscriptionDuration => TimeSpan.FromMilliseconds(SubscriptionDurationMs);

    /// <summary>
    /// Creates a new instance of the MessagePipeSubscriptionDisposedMessage.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier that was disposed</param>
    /// <param name="messageType">The message type for the disposed subscription</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="subscriptionDurationMs">The subscription duration in milliseconds</param>
    /// <param name="messagesReceived">Total messages received by this subscription</param>
    /// <param name="disposalReason">The reason for disposal</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeSubscriptionDisposedMessage instance</returns>
    public static MessagePipeSubscriptionDisposedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string channelName = null,
        string subscriberName = null,
        double subscriptionDurationMs = 0,
        int messagesReceived = 0,
        string disposalReason = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessagePipeSubscriptionDisposedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipeSubscriptionDisposedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Low, // Subscription disposal is informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            ChannelName = channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default",
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            SubscriptionDurationMs = Math.Max(0, subscriptionDurationMs),
            MessagesReceived = Math.Max(0, messagesReceived),
            DisposalReason = disposalReason?.Length <= 128 ? disposalReason : disposalReason?[..128] ?? "Normal disposal"
        };
    }
}