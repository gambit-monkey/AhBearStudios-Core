using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Builders
{
    /// <summary>
    /// Interface for building CircuitBreakerConfig instances with comprehensive fault tolerance options.
    /// Provides a fluent API for configuring circuit breaker behavior, monitoring, and recovery strategies.
    /// </summary>
    public interface ICircuitBreakerConfigBuilder
    {
        /// <summary>
        /// Sets the name for the circuit breaker
        /// </summary>
        /// <param name="name">Circuit breaker name (required)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithName(string name);

        /// <summary>
        /// Sets the failure threshold for opening the circuit
        /// </summary>
        /// <param name="threshold">Number of consecutive failures (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithFailureThreshold(int threshold);

        /// <summary>
        /// Sets the timeout before transitioning from open to half-open
        /// </summary>
        /// <param name="timeout">Timeout duration (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Sets the sampling duration for failure rate calculation
        /// </summary>
        /// <param name="duration">Sampling duration (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithSamplingDuration(TimeSpan duration);

        /// <summary>
        /// Sets the minimum throughput required before circuit can open
        /// </summary>
        /// <param name="minimum">Minimum number of requests (must be non-negative)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithMinimumThroughput(int minimum);

        /// <summary>
        /// Sets the success threshold for closing circuit from half-open
        /// </summary>
        /// <param name="threshold">Success rate percentage (0-100)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithSuccessThreshold(double threshold);

        /// <summary>
        /// Sets the maximum calls allowed in half-open state
        /// </summary>
        /// <param name="maxCalls">Maximum calls (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithHalfOpenMaxCalls(int maxCalls);

        /// <summary>
        /// Configures sliding window settings
        /// </summary>
        /// <param name="enabled">Whether to use sliding window</param>
        /// <param name="type">Type of sliding window</param>
        /// <param name="size">Window size (for count-based) or duration (for time-based)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithSlidingWindow(bool enabled = true, SlidingWindowType type = SlidingWindowType.CountBased, int size = 100);

        /// <summary>
        /// Sets the duration for time-based sliding windows
        /// </summary>
        /// <param name="duration">Window duration (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithSlidingWindowDuration(TimeSpan duration);

        /// <summary>
        /// Configures automatic recovery settings
        /// </summary>
        /// <param name="enabled">Whether to enable automatic recovery</param>
        /// <param name="maxAttempts">Maximum recovery attempts</param>
        /// <param name="timeoutMultiplier">Multiplier for extending timeout on failures</param>
        /// <param name="maxTimeout">Maximum timeout value</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithAutomaticRecovery(bool enabled = true, int maxAttempts = 5, double timeoutMultiplier = 1.5, TimeSpan? maxTimeout = null);

        /// <summary>
        /// Adds exceptions to ignore (won't count as failures)
        /// </summary>
        /// <param name="exceptionTypes">Exception types to ignore</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithIgnoredExceptions(params Type[] exceptionTypes);

        /// <summary>
        /// Adds exceptions that immediately open the circuit
        /// </summary>
        /// <param name="exceptionTypes">Exception types that cause immediate failure</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithImmediateFailureExceptions(params Type[] exceptionTypes);

        /// <summary>
        /// Adds custom failure predicates for determining failures
        /// </summary>
        /// <param name="predicates">Custom failure predicate functions</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithFailurePredicates(params Func<Exception, bool>[] predicates);

        /// <summary>
        /// Configures monitoring and events
        /// </summary>
        /// <param name="enableMetrics">Whether to enable metrics collection</param>
        /// <param name="enableEvents">Whether to enable event notifications</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithMonitoring(bool enableMetrics = true, bool enableEvents = true);

        /// <summary>
        /// Adds tags for categorizing the circuit breaker
        /// </summary>
        /// <param name="tags">Tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithTags(params FixedString64Bytes[] tags);

        /// <summary>
        /// Adds metadata to the circuit breaker
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithMetadata(string key, object value);

        /// <summary>
        /// Configures slow call detection
        /// </summary>
        /// <param name="threshold">Duration threshold for slow calls</param>
        /// <param name="rateThreshold">Percentage of slow calls that triggers action</param>
        /// <param name="minimumCalls">Minimum slow calls before evaluation</param>
        /// <param name="treatAsFailures">Whether to treat slow calls as failures</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithSlowCallDetection(TimeSpan threshold, double rateThreshold = 50.0, int minimumCalls = 5, bool treatAsFailures = true);

        /// <summary>
        /// Configures bulkhead isolation
        /// </summary>
        /// <param name="enabled">Whether to enable bulkhead</param>
        /// <param name="maxConcurrentCalls">Maximum concurrent calls</param>
        /// <param name="maxWaitDuration">Maximum wait time for a call slot</param>
        /// <param name="maxQueueSize">Maximum queue size for waiting calls</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithBulkhead(bool enabled = false, int maxConcurrentCalls = 10, TimeSpan? maxWaitDuration = null, int maxQueueSize = 100);

        /// <summary>
        /// Configures rate limiting
        /// </summary>
        /// <param name="enabled">Whether to enable rate limiting</param>
        /// <param name="requestsPerSecond">Maximum requests per second</param>
        /// <param name="burstSize">Burst allowance</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithRateLimit(bool enabled = false, double requestsPerSecond = 100.0, int burstSize = 150);

        /// <summary>
        /// Configures failover behavior
        /// </summary>
        /// <param name="enabled">Whether to enable failover</param>
        /// <param name="strategy">Failover strategy</param>
        /// <param name="defaultValue">Default value for ReturnDefault strategy</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder WithFailover(bool enabled = false, FailoverStrategy strategy = FailoverStrategy.ReturnDefault, object defaultValue = null);

        /// <summary>
        /// Applies a preset configuration for the specified scenario
        /// </summary>
        /// <param name="scenario">Circuit breaker scenario</param>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder ForScenario(CircuitBreakerScenario scenario);

        /// <summary>
        /// Validates the current configuration without building
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        List<string> Validate();

        /// <summary>
        /// Builds the CircuitBreakerConfig instance
        /// </summary>
        /// <returns>Configured CircuitBreakerConfig instance</returns>
        CircuitBreakerConfig Build();

        /// <summary>
        /// Resets the builder to allow building a new configuration
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        ICircuitBreakerConfigBuilder Reset();
    }
}