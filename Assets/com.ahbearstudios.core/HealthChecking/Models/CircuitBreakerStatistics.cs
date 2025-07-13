using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.HealthCheck.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
    /// Statistics collected by circuit breakers
    /// </summary>
    public sealed record CircuitBreakerStatistics
    {
        /// <summary>
        /// Name of the circuit breaker
        /// </summary>
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public CircuitBreakerState State { get; init; }

        /// <summary>
        /// Current failure count
        /// </summary>
        public int FailureCount { get; init; }

        /// <summary>
        /// Total number of requests processed
        /// </summary>
        public long TotalRequests { get; init; }

        /// <summary>
        /// Number of successful requests
        /// </summary>
        public long SuccessfulRequests { get; init; }

        /// <summary>
        /// Number of failed requests
        /// </summary>
        public long FailedRequests { get; init; }

        /// <summary>
        /// Success rate (0.0 to 1.0)
        /// </summary>
        public double SuccessRate { get; init; }

        /// <summary>
        /// Timestamp of last failure
        /// </summary>
        public DateTime? LastFailureTime { get; init; }

        /// <summary>
        /// Timestamp of last state change
        /// </summary>
        public DateTime LastStateChangeTime { get; init; }

        /// <summary>
        /// Reason for last state change
        /// </summary>
        public string LastStateChangeReason { get; init; }

        /// <summary>
        /// Configuration used by this circuit breaker
        /// </summary>
        public CircuitBreakerConfig Configuration { get; init; }

        /// <summary>
        /// Number of slow calls detected
        /// </summary>
        public long SlowCalls { get; init; }

        /// <summary>
        /// Average response time
        /// </summary>
        public TimeSpan AverageResponseTime { get; init; }

        /// <summary>
        /// Number of times circuit has been opened
        /// </summary>
        public int CircuitOpenedCount { get; init; }

        /// <summary>
        /// Total time spent in open state
        /// </summary>
        public TimeSpan TotalTimeInOpenState { get; init; }
    }