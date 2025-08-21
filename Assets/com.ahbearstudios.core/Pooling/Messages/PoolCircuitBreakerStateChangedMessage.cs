using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a circuit breaker changes state.
    /// </summary>
    public readonly record struct PoolCircuitBreakerStateChangedMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolCircuitBreakerStateChangedMessage;

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
        /// Gets the name of the strategy where state changed.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the old state.
        /// </summary>
        public FixedString64Bytes OldState { get; init; }

        /// <summary>
        /// Gets the new state.
        /// </summary>
        public FixedString64Bytes NewState { get; init; }

        /// <summary>
        /// Initializes a new instance of the PoolCircuitBreakerStateChangedMessage struct.
        /// </summary>
        public PoolCircuitBreakerStateChangedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            StrategyName = default;
            OldState = default;
            NewState = default;
            ConsecutiveFailures = default;
            TotalActivations = default;
        }

        /// <summary>
        /// Gets the timestamp when the state changed.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the total number of circuit breaker activations.
        /// </summary>
        public int TotalActivations { get; init; }

        /// <summary>
        /// Creates a new PoolCircuitBreakerStateChangedMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy where state changed</param>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="totalActivations">Total number of activations</param>
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolCircuitBreakerStateChangedMessage instance</returns>
        public static PoolCircuitBreakerStateChangedMessage Create(
            string strategyName,
            string oldState,
            string newState,
            int consecutiveFailures,
            int totalActivations,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolCircuitBreakerStateChangedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolCircuitBreakerStateChangedMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.Critical, // Circuit breaker changes are critical
                CorrelationId = correlationId,
                StrategyName = strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown",
                OldState = oldState?.Length <= 64 ? oldState : oldState?[..64] ?? "Unknown",
                NewState = newState?.Length <= 64 ? newState : newState?[..64] ?? "Unknown",
                ConsecutiveFailures = consecutiveFailures,
                TotalActivations = totalActivations
            };
        }
    }
}