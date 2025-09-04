using System;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Statistics for an individual circuit breaker.
    /// </summary>
    public sealed record CircuitBreakerStatistics
    {
        /// <summary>
        /// Gets the name of the circuit breaker.
        /// </summary>
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState State { get; init; }

        /// <summary>
        /// Gets the total number of executions through this circuit breaker.
        /// </summary>
        public long TotalExecutions { get; init; }

        /// <summary>
        /// Gets the total number of failures.
        /// </summary>
        public long TotalFailures { get; init; }

        /// <summary>
        /// Gets the total number of successful operations.
        /// </summary>
        public long TotalSuccesses { get; init; }

        /// <summary>
        /// Gets the current failure rate (0.0 to 1.0).
        /// </summary>
        public double FailureRate => TotalExecutions > 0 ? (double)TotalFailures / TotalExecutions : 0.0;

        /// <summary>
        /// Gets the current success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)TotalSuccesses / TotalExecutions : 0.0;

        /// <summary>
        /// Gets the timestamp of the last state change.
        /// </summary>
        public DateTime LastStateChange { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets how long the circuit breaker has been in its current state.
        /// </summary>
        public TimeSpan TimeInCurrentState => DateTime.UtcNow - LastStateChange;

        /// <summary>
        /// Returns a string representation of the circuit breaker statistics.
        /// </summary>
        /// <returns>Circuit breaker statistics string</returns>
        public override string ToString()
        {
            return $"CircuitBreaker {Name}: {State} ({SuccessRate:P1} success rate, {TotalExecutions} executions)";
        }
    }
}