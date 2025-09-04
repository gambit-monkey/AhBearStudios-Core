using System;
using AhBearStudios.Core.Common.Utilities;
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
    /// Gets the unique type code for this message type.
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
    public FixedString512Bytes Reason { get; init; }

    /// <summary>
    /// Gets additional context about the health change.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    #endregion

    #region Computed Properties


    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new MessageBusHealthChangedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="previousStatus">The previous health status</param>
    /// <param name="currentStatus">The current health status</param>
    /// <param name="reason">The reason for the change</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusHealthChangedMessage instance</returns>
    public static MessageBusHealthChangedMessage CreateFromFixedStrings(
        HealthStatus previousStatus,
        HealthStatus currentStatus,
        FixedString512Bytes reason = default,
        FixedString512Bytes context = default,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusHealth", previousStatus.ToString())
            : correlationId;

        return new MessageBusHealthChangedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusHealthChangedMessage", "MessageBusService", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusHealthChangedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.High,
            CorrelationId = finalCorrelationId,
            PreviousStatus = previousStatus,
            CurrentStatus = currentStatus,
            ChangedAt = DateTime.UtcNow,
            Reason = reason,
            Context = context
        };
    }

    /// <summary>
    /// Creates a new MessageBusHealthChangedMessage using string parameters.
    /// </summary>
    /// <param name="previousStatus">The previous health status</param>
    /// <param name="currentStatus">The current health status</param>
    /// <param name="reason">The reason for the change</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusHealthChangedMessage instance</returns>
    public static MessageBusHealthChangedMessage Create(
        HealthStatus previousStatus,
        HealthStatus currentStatus,
        string reason = null,
        string context = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            previousStatus,
            currentStatus,
            new FixedString512Bytes(reason?.Length <= 256 ? reason : reason?[..256] ?? string.Empty),
            new FixedString512Bytes(context?.Length <= 512 ? context : context?[..512] ?? string.Empty),
            new FixedString64Bytes(source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService"),
            correlationId);
    }

    #endregion
}