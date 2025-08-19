using System;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Interface for fluent MessageBusConfig builder with comprehensive validation and defaults.
/// Follows the Builder pattern from AhBearStudios Core Development Guidelines.
/// </summary>
public interface IMessageBusConfigBuilder
{
    #region Core Configuration

    /// <summary>
    /// Sets the instance name for the message bus.
    /// </summary>
    /// <param name="instanceName">The instance name</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when instanceName is null or empty</exception>
    IMessageBusConfigBuilder WithInstanceName(string instanceName);

    /// <summary>
    /// Configures async support for message handling.
    /// </summary>
    /// <param name="enabled">Whether async support is enabled</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithAsyncSupport(bool enabled = true);

    /// <summary>
    /// Sets the maximum number of concurrent message handlers.
    /// </summary>
    /// <param name="maxHandlers">The maximum number of concurrent handlers</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxHandlers is less than 1</exception>
    IMessageBusConfigBuilder WithMaxConcurrentHandlers(int maxHandlers);

    /// <summary>
    /// Sets the maximum queue size before backpressure.
    /// </summary>
    /// <param name="maxQueueSize">The maximum queue size</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxQueueSize is less than 1</exception>
    IMessageBusConfigBuilder WithMaxQueueSize(int maxQueueSize);

    /// <summary>
    /// Sets the timeout for individual message handlers.
    /// </summary>
    /// <param name="timeout">The handler timeout</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is zero or negative</exception>
    IMessageBusConfigBuilder WithHandlerTimeout(TimeSpan timeout);

    #endregion

    #region Feature Configuration

    /// <summary>
    /// Configures performance monitoring.
    /// </summary>
    /// <param name="enabled">Whether performance monitoring is enabled</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithPerformanceMonitoring(bool enabled = true);

    /// <summary>
    /// Configures health checks.
    /// </summary>
    /// <param name="enabled">Whether health checks are enabled</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithHealthChecks(bool enabled = true);

    /// <summary>
    /// Configures alerts.
    /// </summary>
    /// <param name="enabled">Whether alerts are enabled</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithAlerts(bool enabled = true);

    /// <summary>
    /// Configures object pooling for performance optimization.
    /// </summary>
    /// <param name="enabled">Whether object pooling is enabled</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithObjectPooling(bool enabled = true);

    /// <summary>
    /// Configures memory pre-allocation for performance.
    /// </summary>
    /// <param name="enabled">Whether to pre-allocate memory</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithPreAllocateMemory(bool enabled = true);

    /// <summary>
    /// Configures thread pool warm-up on startup.
    /// </summary>
    /// <param name="enabled">Whether to warm up the thread pool</param>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder WithWarmUpThreadPool(bool enabled = true);

    #endregion

    #region Retry Policy Configuration

    /// <summary>
    /// Configures retry policy for failed messages.
    /// </summary>
    /// <param name="enabled">Whether retry is enabled</param>
    /// <param name="maxAttempts">Maximum number of retry attempts</param>
    /// <param name="delay">Initial delay between retries</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
    IMessageBusConfigBuilder WithRetryPolicy(bool enabled = true, int maxAttempts = 3, TimeSpan? delay = null);

    /// <summary>
    /// Sets the retry processing interval.
    /// </summary>
    /// <param name="interval">The retry processing interval</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
    IMessageBusConfigBuilder WithRetryInterval(TimeSpan interval);

    /// <summary>
    /// Sets the maximum number of messages to retry in a single batch.
    /// </summary>
    /// <param name="batchSize">The maximum retry batch size</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1</exception>
    IMessageBusConfigBuilder WithMaxRetryBatchSize(int batchSize);

    #endregion

    #region Dead Letter Queue Configuration

    /// <summary>
    /// Configures dead letter queue for failed messages.
    /// </summary>
    /// <param name="enabled">Whether dead letter queue is enabled</param>
    /// <param name="maxSize">Maximum size of the dead letter queue</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxSize is invalid</exception>
    IMessageBusConfigBuilder WithDeadLetterQueue(bool enabled = true, int maxSize = 1000);

    #endregion

    #region Circuit Breaker Configuration

    /// <summary>
    /// Configures circuit breaker pattern.
    /// </summary>
    /// <param name="enabled">Whether circuit breaker is enabled</param>
    /// <param name="failureThreshold">Number of failures to open circuit</param>
    /// <param name="openTimeout">Timeout before transitioning to half-open</param>
    /// <param name="halfOpenSuccessThreshold">Successes needed to close circuit</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
    IMessageBusConfigBuilder WithCircuitBreaker(
        bool enabled = true,
        int failureThreshold = 5,
        TimeSpan? openTimeout = null,
        int halfOpenSuccessThreshold = 2);

    /// <summary>
    /// Sets the circuit breaker check interval.
    /// </summary>
    /// <param name="interval">The check interval</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
    IMessageBusConfigBuilder WithCircuitBreakerCheckInterval(TimeSpan interval);

    #endregion

    #region Monitoring Configuration

    /// <summary>
    /// Sets the statistics update interval.
    /// </summary>
    /// <param name="interval">The statistics update interval</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
    IMessageBusConfigBuilder WithStatisticsUpdateInterval(TimeSpan interval);

    /// <summary>
    /// Sets the health check interval.
    /// </summary>
    /// <param name="interval">The health check interval</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
    IMessageBusConfigBuilder WithHealthCheckInterval(TimeSpan interval);

    /// <summary>
    /// Configures error rate thresholds for monitoring.
    /// </summary>
    /// <param name="warningThreshold">Warning threshold (0.0 to 1.0)</param>
    /// <param name="criticalThreshold">Critical threshold (0.0 to 1.0)</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are invalid</exception>
    IMessageBusConfigBuilder WithErrorRateThresholds(double warningThreshold, double criticalThreshold);

    /// <summary>
    /// Configures queue size thresholds for monitoring.
    /// </summary>
    /// <param name="warningThreshold">Warning threshold</param>
    /// <param name="criticalThreshold">Critical threshold</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are invalid</exception>
    IMessageBusConfigBuilder WithQueueSizeThresholds(int warningThreshold, int criticalThreshold);

    /// <summary>
    /// Configures processing time thresholds for monitoring.
    /// </summary>
    /// <param name="warningThreshold">Warning threshold</param>
    /// <param name="criticalThreshold">Critical threshold</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are invalid</exception>
    IMessageBusConfigBuilder WithProcessingTimeThresholds(TimeSpan warningThreshold, TimeSpan criticalThreshold);

    #endregion

    #region Memory Configuration

    /// <summary>
    /// Sets the maximum memory pressure threshold.
    /// </summary>
    /// <param name="maxMemoryPressure">Maximum memory pressure in bytes</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxMemoryPressure is less than 1</exception>
    IMessageBusConfigBuilder WithMaxMemoryPressure(long maxMemoryPressure);

    #endregion

    #region Predefined Configurations

    /// <summary>
    /// Configures the builder for high-performance scenarios.
    /// </summary>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder ForHighPerformance();

    /// <summary>
    /// Configures the builder for reliability-focused scenarios.
    /// </summary>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder ForReliability();

    /// <summary>
    /// Configures the builder for development scenarios.
    /// </summary>
    /// <returns>This builder for chaining</returns>
    IMessageBusConfigBuilder ForDevelopment();

    #endregion

    #region Advanced Configuration

    /// <summary>
    /// Applies a custom configuration action.
    /// </summary>
    /// <param name="configureAction">Action to configure the message bus config</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when configureAction is null</exception>
    IMessageBusConfigBuilder Configure(Action<MessageBusConfig> configureAction);

    /// <summary>
    /// Applies conditional configuration based on a predicate.
    /// </summary>
    /// <param name="condition">Condition to evaluate</param>
    /// <param name="configureAction">Action to apply if condition is true</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when configureAction is null</exception>
    IMessageBusConfigBuilder When(bool condition, Action<IMessageBusConfigBuilder> configureAction);

    /// <summary>
    /// Applies conditional configuration based on a predicate function.
    /// </summary>
    /// <param name="predicate">Predicate function to evaluate</param>
    /// <param name="configureAction">Action to apply if predicate returns true</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate or configureAction is null</exception>
    IMessageBusConfigBuilder When(Func<MessageBusConfig, bool> predicate, Action<IMessageBusConfigBuilder> configureAction);

    /// <summary>
    /// Merges settings from another configuration.
    /// </summary>
    /// <param name="otherConfig">Configuration to merge from</param>
    /// <param name="overrideExisting">Whether to override existing values</param>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when otherConfig is null</exception>
    IMessageBusConfigBuilder MergeFrom(MessageBusConfig otherConfig, bool overrideExisting = false);

    #endregion

    #region Validation and Building

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>This builder for chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    IMessageBusConfigBuilder Validate();

    /// <summary>
    /// Builds and returns the configured MessageBusConfig.
    /// </summary>
    /// <returns>The configured MessageBusConfig instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    MessageBusConfig Build();

    /// <summary>
    /// Builds and returns the configured MessageBusConfig without validation.
    /// Use this method only when you're certain the configuration is valid.
    /// </summary>
    /// <returns>The configured MessageBusConfig instance</returns>
    MessageBusConfig BuildUnsafe();

    #endregion
}