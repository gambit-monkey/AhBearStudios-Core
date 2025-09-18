using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a circuit breaker trips (opens) due to failures.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckCircuitBreakerTripMessage : IMessage
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
        /// Gets the name of the circuit breaker that tripped.
        /// </summary>
        public FixedString64Bytes CircuitBreakerName { get; init; }

        /// <summary>
        /// Gets the health check name associated with the circuit breaker.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the failure threshold that was reached.
        /// </summary>
        public int FailureThreshold { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures that caused the trip.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the time window in seconds for the failure count.
        /// </summary>
        public double TimeWindowSeconds { get; init; }

        /// <summary>
        /// Gets the last error message that contributed to the trip.
        /// </summary>
        public FixedString512Bytes LastErrorMessage { get; init; }

        /// <summary>
        /// Gets the duration in seconds the circuit breaker will remain open.
        /// </summary>
        public double OpenDurationSeconds { get; init; }

        /// <summary>
        /// Gets the total number of times this circuit breaker has tripped.
        /// </summary>
        public long TotalTripCount { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckCircuitBreakerTripMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="circuitBreakerName">Name of the circuit breaker</param>
        /// <param name="healthCheckName">Associated health check name</param>
        /// <param name="failureThreshold">Failure threshold that was reached</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="timeWindowSeconds">Time window for failure counting</param>
        /// <param name="lastErrorMessage">Last error message</param>
        /// <param name="openDurationSeconds">Duration the breaker will remain open</param>
        /// <param name="totalTripCount">Total number of trips</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckCircuitBreakerTripMessage instance</returns>
        public static HealthCheckCircuitBreakerTripMessage Create(
            string circuitBreakerName,
            string healthCheckName = null,
            int failureThreshold = 5,
            int consecutiveFailures = 0,
            double timeWindowSeconds = 60,
            string lastErrorMessage = null,
            double openDurationSeconds = 30,
            long totalTripCount = 1,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(circuitBreakerName))
                throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(circuitBreakerName));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthCircuitBreakerManager" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckCircuitBreakerTripMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerTrip", circuitBreakerName)
                : correlationId;

            return new HealthCheckCircuitBreakerTripMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckCircuitBreakerTripMessage,
                Source = source.IsEmpty ? "HealthCircuitBreakerManager" : source,
                Priority = MessagePriority.Critical,
                CorrelationId = finalCorrelationId,
                CircuitBreakerName = circuitBreakerName.Length <= 64 ? circuitBreakerName : circuitBreakerName[..64],
                HealthCheckName = healthCheckName?.Length <= 64 ? healthCheckName : healthCheckName?[..64] ?? string.Empty,
                FailureThreshold = failureThreshold,
                ConsecutiveFailures = consecutiveFailures,
                TimeWindowSeconds = timeWindowSeconds,
                LastErrorMessage = lastErrorMessage?.Length <= 512 ? lastErrorMessage : lastErrorMessage?[..512] ?? "No error message",
                OpenDurationSeconds = openDurationSeconds,
                TotalTripCount = totalTripCount
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Circuit breaker trip message string representation</returns>
        public override string ToString()
        {
            return $"CircuitBreakerTrip: {CircuitBreakerName} - {ConsecutiveFailures}/{FailureThreshold} failures in {TimeWindowSeconds}s (Open for {OpenDurationSeconds}s, Total trips: {TotalTripCount})";
        }

        #endregion
    }
}