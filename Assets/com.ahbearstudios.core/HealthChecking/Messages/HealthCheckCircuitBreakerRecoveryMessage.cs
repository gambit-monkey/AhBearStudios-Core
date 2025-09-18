using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a circuit breaker attempts recovery or successfully recovers.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckCircuitBreakerRecoveryMessage : IMessage
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
        /// Gets the name of the circuit breaker attempting recovery.
        /// </summary>
        public FixedString64Bytes CircuitBreakerName { get; init; }

        /// <summary>
        /// Gets the health check name associated with the circuit breaker.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets whether the recovery was successful.
        /// </summary>
        public bool IsSuccessful { get; init; }

        /// <summary>
        /// Gets the current state of the circuit breaker after recovery attempt.
        /// </summary>
        public CircuitBreakerState CurrentState { get; init; }

        /// <summary>
        /// Gets the number of recovery attempts made.
        /// </summary>
        public int RecoveryAttemptNumber { get; init; }

        /// <summary>
        /// Gets the duration the circuit breaker was open (in seconds).
        /// </summary>
        public double OpenDurationSeconds { get; init; }

        /// <summary>
        /// Gets the reason for the recovery attempt.
        /// </summary>
        public FixedString512Bytes RecoveryReason { get; init; }

        /// <summary>
        /// Gets the success rate of recent health checks (0.0 to 1.0).
        /// </summary>
        public double RecentSuccessRate { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckCircuitBreakerRecoveryMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="circuitBreakerName">Name of the circuit breaker</param>
        /// <param name="healthCheckName">Associated health check name</param>
        /// <param name="isSuccessful">Whether the recovery was successful</param>
        /// <param name="currentState">Current circuit breaker state</param>
        /// <param name="recoveryAttemptNumber">Number of recovery attempts</param>
        /// <param name="openDurationSeconds">How long the breaker was open</param>
        /// <param name="recoveryReason">Reason for the recovery attempt</param>
        /// <param name="recentSuccessRate">Recent success rate (0.0 to 1.0)</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckCircuitBreakerRecoveryMessage instance</returns>
        public static HealthCheckCircuitBreakerRecoveryMessage Create(
            string circuitBreakerName,
            string healthCheckName = null,
            bool isSuccessful = false,
            CircuitBreakerState currentState = CircuitBreakerState.HalfOpen,
            int recoveryAttemptNumber = 1,
            double openDurationSeconds = 0,
            string recoveryReason = "Automatic recovery attempt",
            double recentSuccessRate = 0,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(circuitBreakerName))
                throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(circuitBreakerName));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthCircuitBreakerManager" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckCircuitBreakerRecoveryMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerRecovery", $"{circuitBreakerName}-{recoveryAttemptNumber}")
                : correlationId;

            return new HealthCheckCircuitBreakerRecoveryMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckCircuitBreakerRecoveryMessage,
                Source = source.IsEmpty ? "HealthCircuitBreakerManager" : source,
                Priority = isSuccessful ? MessagePriority.Normal : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                CircuitBreakerName = circuitBreakerName.Length <= 64 ? circuitBreakerName : circuitBreakerName[..64],
                HealthCheckName = healthCheckName?.Length <= 64 ? healthCheckName : healthCheckName?[..64] ?? string.Empty,
                IsSuccessful = isSuccessful,
                CurrentState = currentState,
                RecoveryAttemptNumber = recoveryAttemptNumber,
                OpenDurationSeconds = openDurationSeconds,
                RecoveryReason = recoveryReason?.Length <= 512 ? recoveryReason : recoveryReason?[..512] ?? "Automatic recovery attempt",
                RecentSuccessRate = Math.Clamp(recentSuccessRate, 0.0, 1.0)
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Circuit breaker recovery message string representation</returns>
        public override string ToString()
        {
            var status = IsSuccessful ? "Successful" : "Failed";
            return $"CircuitBreakerRecovery: {CircuitBreakerName} - {status} (Attempt #{RecoveryAttemptNumber}, State: {CurrentState}, Success Rate: {RecentSuccessRate:P0})";
        }

        #endregion
    }
}