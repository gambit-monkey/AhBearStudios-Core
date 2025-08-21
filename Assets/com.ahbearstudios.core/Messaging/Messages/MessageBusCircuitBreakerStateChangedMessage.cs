using System;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a circuit breaker state changes for a message type.
/// Replaces event-based notification with IMessage pattern following CLAUDE.md guidelines.
/// </summary>
public record struct MessageBusCircuitBreakerStateChangedMessage(
    Type MessageType,
    CircuitBreakerState OldState,
    CircuitBreakerState NewState,
    string Reason,
    DateTime Timestamp,
    string CircuitBreakerName,
    Guid CorrelationId = default)
    : IMessage
{
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; } = DateTime.UtcNow.Ticks;

    /// <summary>
    /// Gets the message type code for efficient routing and filtering.
    /// </summary>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusCircuitBreakerStateChangedMessage;

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; } = "MessageCircuitBreaker";

    /// <summary>
    /// Gets the priority level for message processing.
    /// </summary>
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;

    /// <summary>
    /// Gets optional correlation ID for message tracing across systems.
    /// </summary>
    public Guid CorrelationId { get; init; } = CorrelationId;

    /// <summary>
    /// The message type that the circuit breaker is protecting
    /// </summary>
    public Type MessageType { get; init; } = MessageType;

    /// <summary>
    /// Previous circuit breaker state
    /// </summary>
    public CircuitBreakerState OldState { get; init; } = OldState;

    /// <summary>
    /// New circuit breaker state
    /// </summary>
    public CircuitBreakerState NewState { get; init; } = NewState;

    /// <summary>
    /// Reason for the state change
    /// </summary>
    public string Reason { get; init; } = Reason;

    /// <summary>
    /// Timestamp when the state changed
    /// </summary>
    public DateTime Timestamp { get; init; } = Timestamp;

    /// <summary>
    /// Name of the circuit breaker that changed state
    /// </summary>
    public string CircuitBreakerName { get; init; } = CircuitBreakerName;
}