using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a circuit breaker state changes for a message type.
/// Replaces event-based notification with IMessage pattern following CLAUDE.md guidelines.
/// </summary>
public readonly record struct MessageBusCircuitBreakerStateChangedMessage : IMessage
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
    /// The message type that the circuit breaker is protecting
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Previous circuit breaker state
    /// </summary>
    public CircuitBreakerState OldState { get; init; }

    /// <summary>
    /// New circuit breaker state
    /// </summary>
    public CircuitBreakerState NewState { get; init; }

    /// <summary>
    /// Reason for the state change
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Timestamp when the state changed
    /// </summary>
    public DateTime StateChangeTimestamp { get; init; }

    /// <summary>
    /// Name of the circuit breaker that changed state
    /// </summary>
    public string CircuitBreakerName { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageBusCircuitBreakerStateChangedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="messageType">The message type that the circuit breaker is protecting</param>
    /// <param name="oldState">Previous circuit breaker state</param>
    /// <param name="newState">New circuit breaker state</param>
    /// <param name="reason">Reason for the state change</param>
    /// <param name="stateChangeTimestamp">Timestamp when the state changed</param>
    /// <param name="circuitBreakerName">Name of the circuit breaker that changed state</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusCircuitBreakerStateChangedMessage instance</returns>
    public static MessageBusCircuitBreakerStateChangedMessage CreateFromFixedStrings(
        Type messageType,
        CircuitBreakerState oldState,
        CircuitBreakerState newState,
        string reason,
        DateTime stateChangeTimestamp,
        string circuitBreakerName,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusCircuitBreaker", circuitBreakerName)
            : correlationId;

        return new MessageBusCircuitBreakerStateChangedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusCircuitBreakerStateChangedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusCircuitBreakerStateChangedMessage,
            Source = source.IsEmpty ? "MessageCircuitBreaker" : source,
            Priority = MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            MessageType = messageType,
            OldState = oldState,
            NewState = newState,
            Reason = reason ?? string.Empty,
            StateChangeTimestamp = stateChangeTimestamp,
            CircuitBreakerName = circuitBreakerName ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusCircuitBreakerStateChangedMessage using string parameters.
    /// </summary>
    /// <param name="messageType">The message type that the circuit breaker is protecting</param>
    /// <param name="oldState">Previous circuit breaker state</param>
    /// <param name="newState">New circuit breaker state</param>
    /// <param name="reason">Reason for the state change</param>
    /// <param name="stateChangeTimestamp">Timestamp when the state changed</param>
    /// <param name="circuitBreakerName">Name of the circuit breaker that changed state</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusCircuitBreakerStateChangedMessage instance</returns>
    public static MessageBusCircuitBreakerStateChangedMessage Create(
        Type messageType,
        CircuitBreakerState oldState,
        CircuitBreakerState newState,
        string reason,
        DateTime stateChangeTimestamp,
        string circuitBreakerName,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            messageType,
            oldState,
            newState,
            reason,
            stateChangeTimestamp,
            circuitBreakerName,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageCircuitBreaker",
            correlationId);
    }

    #endregion
}