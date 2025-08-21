using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a message subscription is disposed.
/// Replaces SubscriptionDisposedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusSubscriptionDisposedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusSubscriptionDisposedMessage;

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
    /// Gets the unique identifier of the disposed subscription.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the type of message that was being subscribed to.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the name of the subscriber.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the timestamp when the subscription was disposed.
    /// </summary>
    public DateTime DisposedAt { get; init; }

    /// <summary>
    /// Gets the reason for disposal.
    /// </summary>
    public FixedString128Bytes DisposalReason { get; init; }

    /// <summary>
    /// Gets how long the subscription was active.
    /// </summary>
    public TimeSpan ActiveDuration { get; init; }

    /// <summary>
    /// Gets the number of messages processed during the subscription lifetime.
    /// </summary>
    public long MessagesProcessed { get; init; }

    /// <summary>
    /// Gets additional context about the disposal.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageBusSubscriptionDisposedMessage struct.
    /// </summary>
    public MessageBusSubscriptionDisposedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        SubscriptionId = default;
        MessageType = default;
        SubscriberName = default;
        DisposedAt = default;
        DisposalReason = default;
        ActiveDuration = default;
        MessagesProcessed = default;
        Context = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageBusSubscriptionDisposedMessage.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription</param>
    /// <param name="messageType">The type of message being subscribed to</param>
    /// <param name="subscriberName">The name of the subscriber</param>
    /// <param name="disposalReason">The reason for disposal</param>
    /// <param name="activeDuration">How long the subscription was active</param>
    /// <param name="messagesProcessed">Number of messages processed</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriptionDisposedMessage instance</returns>
    public static MessageBusSubscriptionDisposedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string subscriberName = null,
        string disposalReason = null,
        TimeSpan activeDuration = default,
        long messagesProcessed = 0,
        string context = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageBusSubscriptionDisposedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusSubscriptionDisposedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Disposal events are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            DisposedAt = DateTime.UtcNow,
            DisposalReason = disposalReason?.Length <= 128 ? disposalReason : disposalReason?[..128] ?? "Normal disposal",
            ActiveDuration = activeDuration,
            MessagesProcessed = Math.Max(0, messagesProcessed),
            Context = context?.Length <= 256 ? context : context?[..256] ?? string.Empty
        };
    }
}