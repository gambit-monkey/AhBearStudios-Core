using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a circuit breaker changes state.
    /// </summary>
    public readonly record struct PoolCircuitBreakerStateChangedMessage : IMessage
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
        /// Gets the number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the total number of circuit breaker activations.
        /// </summary>
        public int TotalActivations { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the state changed.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolCircuitBreakerStateChangedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="strategyName">Strategy where state changed</param>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="totalActivations">Total number of activations</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolCircuitBreakerStateChangedMessage instance</returns>
        public static PoolCircuitBreakerStateChangedMessage CreateFromFixedStrings(
            FixedString64Bytes strategyName,
            FixedString64Bytes oldState,
            FixedString64Bytes newState,
            int consecutiveFailures,
            int totalActivations,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolCircuitBreakerStateChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolCircuitBreakerStateChange", $"{strategyName}-{oldState}-{newState}")
                : correlationId;
            
            return new PoolCircuitBreakerStateChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolCircuitBreakerStateChangedMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.Critical, // Circuit breaker changes are critical
                CorrelationId = finalCorrelationId,
                
                StrategyName = strategyName,
                OldState = oldState,
                NewState = newState,
                ConsecutiveFailures = consecutiveFailures,
                TotalActivations = totalActivations
            };
        }

        /// <summary>
        /// Creates a new PoolCircuitBreakerStateChangedMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy where state changed</param>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="totalActivations">Total number of activations</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolCircuitBreakerStateChangedMessage instance</returns>
        public static PoolCircuitBreakerStateChangedMessage Create(
            string strategyName,
            string oldState,
            string newState,
            int consecutiveFailures,
            int totalActivations,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown"),
                new FixedString64Bytes(oldState?.Length <= 64 ? oldState : oldState?[..64] ?? "Unknown"),
                new FixedString64Bytes(newState?.Length <= 64 ? newState : newState?[..64] ?? "Unknown"),
                consecutiveFailures,
                totalActivations,
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}