using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for circuit breaker configuration with advanced fault tolerance settings
    /// </summary>
    public interface ICircuitBreakerConfig
    {
        /// <summary>
        /// Unique identifier for this circuit breaker configuration
        /// </summary>
        FixedString64Bytes Id { get; }

        /// <summary>
        /// Display name for this circuit breaker configuration
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Number of consecutive failures required to open the circuit
        /// </summary>
        int FailureThreshold { get; }

        /// <summary>
        /// Time to wait in open state before transitioning to half-open
        /// </summary>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Duration to collect failure statistics for threshold calculation
        /// </summary>
        TimeSpan SamplingDuration { get; }

        /// <summary>
        /// Minimum number of requests in sampling period before circuit can open
        /// </summary>
        int MinimumThroughput { get; }

        /// <summary>
        /// Success threshold percentage (0-100) required to close circuit from half-open
        /// </summary>
        double SuccessThreshold { get; }

        /// <summary>
        /// Number of test requests allowed in half-open state
        /// </summary>
        int HalfOpenMaxCalls { get; }

        /// <summary>
        /// Whether to use sliding window for failure rate calculation
        /// </summary>
        bool UseSlidingWindow { get; }

        /// <summary>
        /// Type of sliding window to use
        /// </summary>
        SlidingWindowType SlidingWindowType { get; }

        /// <summary>
        /// Size of the sliding window (requests for count-based, duration for time-based)
        /// </summary>
        int SlidingWindowSize { get; }

        /// <summary>
        /// Time-based sliding window duration (used when SlidingWindowType is TimeBased)
        /// </summary>
        TimeSpan SlidingWindowDuration { get; }

        /// <summary>
        /// Whether to automatically attempt recovery when circuit is open
        /// </summary>
        bool EnableAutomaticRecovery { get; }

        /// <summary>
        /// Maximum number of automatic recovery attempts
        /// </summary>
        int MaxRecoveryAttempts { get; }

        /// <summary>
        /// Multiplier for extending timeout on repeated failures
        /// </summary>
        double TimeoutMultiplier { get; }

        /// <summary>
        /// Maximum timeout value to prevent indefinite waiting
        /// </summary>
        TimeSpan MaxTimeout { get; }

        /// <summary>
        /// Types of exceptions that should be ignored by the circuit breaker
        /// </summary>
        HashSet<Type> IgnoredExceptions { get; }

        /// <summary>
        /// Types of exceptions that should immediately open the circuit
        /// </summary>
        HashSet<Type> ImmediateFailureExceptions { get; }

        /// <summary>
        /// Custom failure predicates for determining if an exception counts as a failure
        /// </summary>
        List<Func<Exception, bool>> FailurePredicates { get; }

        /// <summary>
        /// Whether to enable detailed metrics collection
        /// </summary>
        bool EnableMetrics { get; }

        /// <summary>
        /// Whether to enable event notifications for state changes
        /// </summary>
        bool EnableEvents { get; }

        /// <summary>
        /// Custom tags for categorizing this circuit breaker
        /// </summary>
        HashSet<FixedString64Bytes> Tags { get; }

        /// <summary>
        /// Custom metadata for this circuit breaker configuration
        /// </summary>
        Dictionary<string, object> Metadata { get; }

        /// <summary>
        /// Slow call detection configuration
        /// </summary>
        ISlowCallConfig SlowCallConfig { get; }

        /// <summary>
        /// Bulkhead isolation configuration
        /// </summary>
        IBulkheadConfig BulkheadConfig { get; }

        /// <summary>
        /// Rate limiting configuration when circuit is closed
        /// </summary>
        IRateLimitConfig RateLimitConfig { get; }

        /// <summary>
        /// Failover configuration for when circuit is open
        /// </summary>
        IFailoverConfig FailoverConfig { get; }

        /// <summary>
        /// Validates the circuit breaker configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        List<string> Validate();
    }
}