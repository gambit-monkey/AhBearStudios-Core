using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a circuit breaker state changes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckCircuitBreakerStateChangedMessage : IMessage
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
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode { get; init; } = MessageTypeCodes.HealthCheckCircuitBreakerStateChangedMessage;

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

        // Circuit breaker-specific properties
        /// <summary>
        /// Gets the name of the circuit breaker that changed state.
        /// </summary>
        public FixedString64Bytes CircuitBreakerName { get; init; }

        /// <summary>
        /// Gets the previous circuit breaker state before the change.
        /// </summary>
        public CircuitBreakerState OldState { get; init; }

        /// <summary>
        /// Gets the new circuit breaker state after the change.
        /// </summary>
        public CircuitBreakerState NewState { get; init; }

        /// <summary>
        /// Gets the reason for the state change.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures that led to the state change.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the total number of circuit breaker activations.
        /// </summary>
        public long TotalActivations { get; init; }

        /// <summary>
        /// Initializes a new instance of the HealthCheckCircuitBreakerStateChangedMessage struct.
        /// </summary>
        public HealthCheckCircuitBreakerStateChangedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            CircuitBreakerName = default;
            OldState = default;
            NewState = default;
            Reason = string.Empty;
            ConsecutiveFailures = 0;
            TotalActivations = 0;
        }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new HealthCheckCircuitBreakerStateChangedMessage.
        /// </summary>
        /// <param name="circuitBreakerName">Name of the circuit breaker</param>
        /// <param name="oldState">Previous circuit breaker state</param>
        /// <param name="newState">New circuit breaker state</param>
        /// <param name="reason">Reason for the state change</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="totalActivations">Total circuit breaker activations</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New HealthCheckCircuitBreakerStateChangedMessage instance</returns>
        public static HealthCheckCircuitBreakerStateChangedMessage Create(
            FixedString64Bytes circuitBreakerName,
            CircuitBreakerState oldState,
            CircuitBreakerState newState,
            string reason = null,
            int consecutiveFailures = 0,
            long totalActivations = 0,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new HealthCheckCircuitBreakerStateChangedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckCircuitBreakerStateChangedMessage,
                Source = source.IsEmpty ? "HealthCheckService" : source,
                Priority = newState == CircuitBreakerState.Open ? MessagePriority.High : MessagePriority.Normal,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                CircuitBreakerName = circuitBreakerName,
                OldState = oldState,
                NewState = newState,
                Reason = reason ?? string.Empty,
                ConsecutiveFailures = consecutiveFailures,
                TotalActivations = totalActivations
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Circuit breaker state change message string representation</returns>
        public override string ToString()
        {
            return $"CircuitBreaker '{CircuitBreakerName}': {OldState} -> {NewState} (Failures: {ConsecutiveFailures}, Reason: {Reason ?? "None"})";
        }
    }
}