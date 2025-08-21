using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when MessagePipe adapter health status changes.
/// Replaces MessagePipe health change events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipeHealthChangedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessagePipeHealthChangedMessage;

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
    /// Gets the previous operational status.
    /// </summary>
    public bool PreviousOperational { get; init; }

    /// <summary>
    /// Gets the current operational status.
    /// </summary>
    public bool CurrentOperational { get; init; }

    /// <summary>
    /// Gets the current error rate (0.0 to 1.0).
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Gets the reason for the health status change.
    /// </summary>
    public FixedString512Bytes Reason { get; init; }

    /// <summary>
    /// Gets the MessagePipe instance identifier.
    /// </summary>
    public FixedString64Bytes InstanceId { get; init; }

    /// <summary>
    /// Gets the number of active subscriptions at the time of health change.
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    /// Gets the timestamp when the health change occurred.
    /// </summary>
    public DateTime HealthChangedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessagePipeHealthChangedMessage struct.
    /// </summary>
    public MessagePipeHealthChangedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        PreviousOperational = default;
        CurrentOperational = default;
        ErrorRate = default;
        Reason = default;
        InstanceId = default;
        ActiveSubscriptions = default;
        HealthChangedAt = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets whether health status improved.
    /// </summary>
    public bool IsHealthImproved => !PreviousOperational && CurrentOperational;

    /// <summary>
    /// Gets whether health status degraded.
    /// </summary>
    public bool IsHealthDegraded => PreviousOperational && !CurrentOperational;

    /// <summary>
    /// Creates a new instance of the MessagePipeHealthChangedMessage.
    /// </summary>
    /// <param name="previousOperational">The previous operational status</param>
    /// <param name="currentOperational">The current operational status</param>
    /// <param name="errorRate">The current error rate (0.0 to 1.0)</param>
    /// <param name="reason">The reason for the health status change</param>
    /// <param name="instanceId">The MessagePipe instance identifier</param>
    /// <param name="activeSubscriptions">Number of active subscriptions</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeHealthChangedMessage instance</returns>
    public static MessagePipeHealthChangedMessage Create(
        bool previousOperational,
        bool currentOperational,
        double errorRate = 0.0,
        string reason = null,
        string instanceId = null,
        int activeSubscriptions = 0,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        // Determine priority based on health change
        var priority = (!previousOperational && currentOperational) ? MessagePriority.Normal :  // Recovery
                      (previousOperational && !currentOperational) ? MessagePriority.High :    // Degradation
                      MessagePriority.Low;  // No operational change

        return new MessagePipeHealthChangedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipeHealthChangedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = priority,
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            PreviousOperational = previousOperational,
            CurrentOperational = currentOperational,
            ErrorRate = Math.Max(0.0, Math.Min(1.0, errorRate)), // Clamp between 0.0 and 1.0
            Reason = reason?.Length <= 256 ? reason : reason?[..256] ?? "Health status changed",
            InstanceId = instanceId?.Length <= 64 ? instanceId : instanceId?[..64] ?? "Default",
            ActiveSubscriptions = Math.Max(0, activeSubscriptions),
            HealthChangedAt = DateTime.UtcNow
        };
    }
}