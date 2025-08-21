using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a message type is unregistered from the registry.
/// Replaces MessageTypeUnregisteredEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageTypeUnregisteredEventMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusTypeUnregisteredMessage;

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
    /// Gets the message type that was unregistered.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the type code that was assigned to the unregistered message type.
    /// </summary>
    public ushort PreviousTypeCode { get; init; }

    /// <summary>
    /// Gets the timestamp when the unregistration occurred.
    /// </summary>
    public DateTime UnregisteredAt { get; init; }

    /// <summary>
    /// Gets the reason for unregistration.
    /// </summary>
    public FixedString128Bytes UnregistrationReason { get; init; }

    /// <summary>
    /// Gets the number of active subscribers when unregistered.
    /// </summary>
    public int ActiveSubscribersCount { get; init; }

    /// <summary>
    /// Gets additional unregistration context.
    /// </summary>
    public FixedString512Bytes UnregistrationContext { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageTypeUnregisteredEventMessage struct.
    /// </summary>
    public MessageTypeUnregisteredEventMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MessageType = default;
        PreviousTypeCode = default;
        UnregisteredAt = default;
        UnregistrationReason = default;
        ActiveSubscribersCount = default;
        UnregistrationContext = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageTypeUnregisteredEventMessage.
    /// </summary>
    /// <param name="messageType">The message type that was unregistered</param>
    /// <param name="previousTypeCode">The type code that was assigned</param>
    /// <param name="unregistrationReason">The reason for unregistration</param>
    /// <param name="activeSubscribersCount">Number of active subscribers</param>
    /// <param name="unregistrationContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageTypeUnregisteredEventMessage instance</returns>
    public static MessageTypeUnregisteredEventMessage Create(
        Type messageType,
        ushort previousTypeCode,
        string unregistrationReason = null,
        int activeSubscribersCount = 0,
        string unregistrationContext = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageTypeUnregisteredEventMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusTypeUnregisteredMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Type unregistration events are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            PreviousTypeCode = previousTypeCode,
            UnregisteredAt = DateTime.UtcNow,
            UnregistrationReason = unregistrationReason?.Length <= 128 ? unregistrationReason : unregistrationReason?[..128] ?? "Manual unregistration",
            ActiveSubscribersCount = Math.Max(0, activeSubscribersCount),
            UnregistrationContext = unregistrationContext?.Length <= 256 ? unregistrationContext : unregistrationContext?[..256] ?? string.Empty
        };
    }
}