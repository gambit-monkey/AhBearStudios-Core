using System;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message bus health status changes.
/// Replaces HealthStatusChangedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusHealthChangedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusHealthChangedMessage;

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
    /// Gets the previous health status.
    /// </summary>
    public HealthStatus PreviousStatus { get; init; }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    public HealthStatus CurrentStatus { get; init; }

    /// <summary>
    /// Gets the timestamp when the health status changed.
    /// </summary>
    public DateTime ChangedAt { get; init; }

    /// <summary>
    /// Gets the reason for the health status change.
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Gets additional context about the health change.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageBusHealthChangedMessage struct.
    /// </summary>
    public MessageBusHealthChangedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        PreviousStatus = default;
        CurrentStatus = default;
        ChangedAt = default;
        Reason = default;
        Context = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Initializes a new instance of the MessageBusHealthChangedMessage with specified values.
    /// </summary>
    /// <param name="previousStatus">The previous health status</param>
    /// <param name="currentStatus">The current health status</param>
    /// <param name="reason">The reason for the change</param>
    /// <param name="context">Additional context</param>
    public static MessageBusHealthChangedMessage Create(
        HealthStatus previousStatus,
        HealthStatus currentStatus,
        string reason = null,
        string context = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageBusHealthChangedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusHealthChangedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.High, // Health changes are high priority
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            PreviousStatus = previousStatus,
            CurrentStatus = currentStatus,
            ChangedAt = DateTime.UtcNow,
            Reason = reason ?? string.Empty,
            Context = context?.Length <= 256 ? context : context?[..256] ?? string.Empty
        };
    }
}