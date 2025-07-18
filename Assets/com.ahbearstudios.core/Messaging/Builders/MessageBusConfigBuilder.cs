using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders
{
    /// <summary>
    /// Fluent builder for MessageBusConfig with comprehensive validation and defaults.
    /// Follows the Builder pattern from AhBearStudios Core Development Guidelines.
    /// </summary>
    public sealed class MessageBusConfigBuilder
    {
        private readonly MessageBusConfig _config;

        /// <summary>
        /// Initializes a new instance of the MessageBusConfigBuilder class.
        /// </summary>
        public MessageBusConfigBuilder()
        {
            _config = new MessageBusConfig();
        }

        /// <summary>
        /// Initializes a new instance of the MessageBusConfigBuilder class with an existing configuration.
        /// </summary>
        /// <param name="existingConfig">The existing configuration to start with</param>
        /// <exception cref="ArgumentNullException">Thrown when existingConfig is null</exception>
        public MessageBusConfigBuilder(MessageBusConfig existingConfig)
        {
            _config = existingConfig?.Clone() ?? throw new ArgumentNullException(nameof(existingConfig));
        }

        #region Core Configuration

        /// <summary>
        /// Sets the instance name for the message bus.
        /// </summary>
        /// <param name="instanceName">The instance name</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when instanceName is null or empty</exception>
        public MessageBusConfigBuilder WithInstanceName(string instanceName)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                throw new ArgumentNullException(nameof(instanceName));

            _config.InstanceName = instanceName;
            return this;
        }

        /// <summary>
        /// Configures async support for message handling.
        /// </summary>
        /// <param name="enabled">Whether async support is enabled</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithAsyncSupport(bool enabled = true)
        {
            _config.AsyncSupport = enabled;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent message handlers.
        /// </summary>
        /// <param name="maxHandlers">The maximum number of concurrent handlers</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxHandlers is less than 1</exception>
        public MessageBusConfigBuilder WithMaxConcurrentHandlers(int maxHandlers)
        {
            if (maxHandlers < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHandlers), "Must be at least 1");

            _config.MaxConcurrentHandlers = maxHandlers;
            return this;
        }

        /// <summary>
        /// Sets the maximum queue size before backpressure.
        /// </summary>
        /// <param name="maxQueueSize">The maximum queue size</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxQueueSize is less than 1</exception>
        public MessageBusConfigBuilder WithMaxQueueSize(int maxQueueSize)
        {
            if (maxQueueSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxQueueSize), "Must be at least 1");

            _config.MaxQueueSize = maxQueueSize;
            return this;
        }

        /// <summary>
        /// Sets the timeout for individual message handlers.
        /// </summary>
        /// <param name="timeout">The handler timeout</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is zero or negative</exception>
        public MessageBusConfigBuilder WithHandlerTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Must be greater than zero");

            _config.HandlerTimeout = timeout;
            return this;
        }

        #endregion

        #region Feature Configuration

        /// <summary>
        /// Configures performance monitoring.
        /// </summary>
        /// <param name="enabled">Whether performance monitoring is enabled</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithPerformanceMonitoring(bool enabled = true)
        {
            _config.PerformanceMonitoring = enabled;
            return this;
        }

        /// <summary>
        /// Configures health checks.
        /// </summary>
        /// <param name="enabled">Whether health checks are enabled</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithHealthChecks(bool enabled = true)
        {
            _config.HealthChecksEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Configures alerts.
        /// </summary>
        /// <param name="enabled">Whether alerts are enabled</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithAlerts(bool enabled = true)
        {
            _config.AlertsEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Configures object pooling for performance optimization.
        /// </summary>
        /// <param name="enabled">Whether object pooling is enabled</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithObjectPooling(bool enabled = true)
        {
            _config.UseObjectPooling = enabled;
            return this;
        }

        /// <summary>
        /// Configures memory pre-allocation for performance.
        /// </summary>
        /// <param name="enabled">Whether to pre-allocate memory</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithPreAllocateMemory(bool enabled = true)
        {
            _config.PreAllocateMemory = enabled;
            return this;
        }

        /// <summary>
        /// Configures thread pool warm-up on startup.
        /// </summary>
        /// <param name="enabled">Whether to warm up the thread pool</param>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder WithWarmUpThreadPool(bool enabled = true)
        {
            _config.WarmUpThreadPool = enabled;
            return this;
        }

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
        public MessageBusConfigBuilder WithRetryPolicy(bool enabled = true, int maxAttempts = 3, TimeSpan? delay = null)
        {
            _config.RetryFailedMessages = enabled;

            if (enabled)
            {
                if (maxAttempts < 1)
                    throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be at least 1");

                var retryDelay = delay ?? TimeSpan.FromSeconds(1);
                if (retryDelay <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(delay), "Must be greater than zero");

                _config.MaxRetryAttempts = maxAttempts;
                _config.RetryDelay = retryDelay;
            }

            return this;
        }

        /// <summary>
        /// Sets the retry processing interval.
        /// </summary>
        /// <param name="interval">The retry processing interval</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
        public MessageBusConfigBuilder WithRetryInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Must be greater than zero");

            _config.RetryInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of messages to retry in a single batch.
        /// </summary>
        /// <param name="batchSize">The maximum retry batch size</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1</exception>
        public MessageBusConfigBuilder WithMaxRetryBatchSize(int batchSize)
        {
            if (batchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Must be at least 1");

            _config.MaxRetryBatchSize = batchSize;
            return this;
        }

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
        public MessageBusConfigBuilder WithCircuitBreaker(
            bool enabled = true,
            int failureThreshold = 5,
            TimeSpan? openTimeout = null,
            int halfOpenSuccessThreshold = 2)
        {
            _config.UseCircuitBreaker = enabled;

            if (enabled)
            {
                if (failureThreshold < 1)
                    throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Must be at least 1");

                var timeout = openTimeout ?? TimeSpan.FromSeconds(30);
                if (timeout <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(openTimeout), "Must be greater than zero");

                if (halfOpenSuccessThreshold < 1)
                    throw new ArgumentOutOfRangeException(nameof(halfOpenSuccessThreshold), "Must be at least 1");

                _config.CircuitBreakerConfig = new CircuitBreakerConfig
                {
                    FailureThreshold = failureThreshold,
                    OpenTimeout = timeout,
                    HalfOpenSuccessThreshold = halfOpenSuccessThreshold
                };
            }

            return this;
        }

        /// <summary>
        /// Sets the circuit breaker check interval.
        /// </summary>
        /// <param name="interval">The check interval</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
        public MessageBusConfigBuilder WithCircuitBreakerCheckInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Must be greater than zero");

            _config.CircuitBreakerCheckInterval = interval;
            return this;
        }

        #endregion

        #region Monitoring Configuration

        /// <summary>
        /// Sets the statistics update interval.
        /// </summary>
        /// <param name="interval">The statistics update interval</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
        public MessageBusConfigBuilder WithStatisticsUpdateInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Must be greater than zero");

            _config.StatisticsUpdateInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the health check interval.
        /// </summary>
        /// <param name="interval">The health check interval</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
        public MessageBusConfigBuilder WithHealthCheckInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Must be greater than zero");

            _config.HealthCheckInterval = interval;
            return this;
        }

        /// <summary>
        /// Configures error rate thresholds for monitoring.
        /// </summary>
        /// <param name="warningThreshold">Warning threshold (0.0 to 1.0)</param>
        /// <param name="criticalThreshold">Critical threshold (0.0 to 1.0)</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are invalid</exception>
        public MessageBusConfigBuilder WithErrorRateThresholds(double warningThreshold, double criticalThreshold)
        {
            if (warningThreshold < 0 || warningThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(warningThreshold), "Must be between 0 and 1");

            if (criticalThreshold < 0 || criticalThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(criticalThreshold), "Must be between 0 and 1");

            if (criticalThreshold <= warningThreshold)
                throw new ArgumentOutOfRangeException(nameof(criticalThreshold), "Must be greater than warning threshold");

            _config.WarningErrorRateThreshold = warningThreshold;
            _config.CriticalErrorRateThreshold = criticalThreshold;
            return this;
        }

        /// <summary>
        /// Configures queue size thresholds for monitoring.
        /// </summary>
        /// <param name="warningThreshold">Warning threshold</param>
        /// <param name="criticalThreshold">Critical threshold</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are invalid</exception>
        public MessageBusConfigBuilder WithQueueSizeThresholds(int warningThreshold, int criticalThreshold)
        {
            if (warningThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(warningThreshold), "Must be non-negative");

            if (criticalThreshold <= warningThreshold)
                throw new ArgumentOutOfRangeException(nameof(criticalThreshold), "Must be greater than warning threshold");

            _config.WarningQueueSizeThreshold = warningThreshold;
            _config.CriticalQueueSizeThreshold = criticalThreshold;
            return this;
        }

        /// <summary>
        /// Configures processing time thresholds for monitoring.
        /// </summary>
        /// <param name="warningThreshold">Warning threshold</param>
        /// <param name="criticalThreshold">Critical threshold</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when thresholds are invalid</exception>
        public MessageBusConfigBuilder WithProcessingTimeThresholds(TimeSpan warningThreshold, TimeSpan criticalThreshold)
        {
            if (warningThreshold <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(warningThreshold), "Must be greater than zero");

            if (criticalThreshold <= warningThreshold)
                throw new ArgumentOutOfRangeException(nameof(criticalThreshold), "Must be greater than warning threshold");

            _config.WarningProcessingTimeThreshold = warningThreshold;
            _config.CriticalProcessingTimeThreshold = criticalThreshold;
            return this;
        }

        #endregion

        #region Memory Configuration

        /// <summary>
        /// Sets the maximum memory pressure threshold.
        /// </summary>
        /// <param name="maxMemoryPressure">Maximum memory pressure in bytes</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxMemoryPressure is less than 1</exception>
        public MessageBusConfigBuilder WithMaxMemoryPressure(long maxMemoryPressure)
        {
            if (maxMemoryPressure < 1)
                throw new ArgumentOutOfRangeException(nameof(maxMemoryPressure), "Must be at least 1");

            _config.MaxMemoryPressure = maxMemoryPressure;
            return this;
        }

        #endregion

        #region Predefined Configurations

        /// <summary>
        /// Configures the builder for high-performance scenarios.
        /// </summary>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder ForHighPerformance()
        {
            return WithInstanceName("HighPerformanceMessageBus")
                .WithAsyncSupport(true)
                .WithPerformanceMonitoring(true)
                .WithHealthChecks(true)
                .WithAlerts(false) // Disabled for maximum performance
                .WithRetryPolicy(false) // Disabled for maximum performance
                .WithCircuitBreaker(false) // Disabled for maximum performance
                .WithObjectPooling(true)
                .WithMaxConcurrentHandlers(Environment.ProcessorCount * 4)
                .WithMaxQueueSize(50000)
                .WithHandlerTimeout(TimeSpan.FromSeconds(5))
                .WithStatisticsUpdateInterval(TimeSpan.FromSeconds(30))
                .WithHealthCheckInterval(TimeSpan.FromMinutes(1))
                .WithPreAllocateMemory(true)
                .WithWarmUpThreadPool(true);
        }

        /// <summary>
        /// Configures the builder for reliability-focused scenarios.
        /// </summary>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder ForReliability()
        {
            return WithInstanceName("ReliableMessageBus")
                .WithAsyncSupport(true)
                .WithPerformanceMonitoring(true)
                .WithHealthChecks(true)
                .WithAlerts(true)
                .WithRetryPolicy(true, 5, TimeSpan.FromSeconds(2))
                .WithCircuitBreaker(true)
                .WithObjectPooling(true)
                .WithMaxConcurrentHandlers(Environment.ProcessorCount)
                .WithMaxQueueSize(10000)
                .WithHandlerTimeout(TimeSpan.FromSeconds(60))
                .WithStatisticsUpdateInterval(TimeSpan.FromSeconds(5))
                .WithHealthCheckInterval(TimeSpan.FromSeconds(15))
                .WithErrorRateThresholds(0.02, 0.05) // 2% warning, 5% critical
                .WithQueueSizeThresholds(500, 2000) // 500 warning, 2000 critical
                .WithProcessingTimeThresholds(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Configures the builder for development scenarios.
        /// </summary>
        /// <returns>This builder for chaining</returns>
        public MessageBusConfigBuilder ForDevelopment()
        {
            return WithInstanceName("DevelopmentMessageBus")
                .WithAsyncSupport(true)
                .WithPerformanceMonitoring(true)
                .WithHealthChecks(true)
                .WithAlerts(true)
                .WithRetryPolicy(true, 2, TimeSpan.FromMilliseconds(500))
                .WithCircuitBreaker(true)
                .WithObjectPooling(false) // Disabled for easier debugging
                .WithMaxConcurrentHandlers(2)
                .WithMaxQueueSize(1000)
                .WithHandlerTimeout(TimeSpan.FromMinutes(5)) // Longer for debugging
                .WithStatisticsUpdateInterval(TimeSpan.FromSeconds(1))
                .WithHealthCheckInterval(TimeSpan.FromSeconds(5))
                .WithErrorRateThresholds(0.10, 0.25) // More lenient thresholds
                .WithQueueSizeThresholds(100, 500)
                .WithProcessingTimeThresholds(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        }

        #endregion

        #region Advanced Configuration

        /// <summary>
        /// Applies a custom configuration action.
        /// </summary>
        /// <param name="configureAction">Action to configure the message bus configSo</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when configureAction is null</exception>
        public MessageBusConfigBuilder Configure(Action<MessageBusConfig> configureAction)
        {
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            configureAction(_config);
            return this;
        }

        /// <summary>
        /// Applies conditional configuration based on a predicate.
        /// </summary>
        /// <param name="condition">Condition to evaluate</param>
        /// <param name="configureAction">Action to apply if condition is true</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when configureAction is null</exception>
        public MessageBusConfigBuilder When(bool condition, Action<MessageBusConfigBuilder> configureAction)
        {
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            if (condition)
            {
                configureAction(this);
            }

            return this;
        }

        /// <summary>
        /// Applies conditional configuration based on a predicate function.
        /// </summary>
        /// <param name="predicate">Predicate function to evaluate</param>
        /// <param name="configureAction">Action to apply if predicate returns true</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate or configureAction is null</exception>
        public MessageBusConfigBuilder When(Func<MessageBusConfig, bool> predicate, Action<MessageBusConfigBuilder> configureAction)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            if (predicate(_config))
            {
                configureAction(this);
            }

            return this;
        }

        /// <summary>
        /// Merges settings from another configuration.
        /// </summary>
        /// <param name="otherConfig">Configuration to merge from</param>
        /// <param name="overrideExisting">Whether to override existing values</param>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when otherConfig is null</exception>
        public MessageBusConfigBuilder MergeFrom(MessageBusConfig otherConfig, bool overrideExisting = false)
        {
            if (otherConfig == null)
                throw new ArgumentNullException(nameof(otherConfig));

            if (overrideExisting || string.IsNullOrWhiteSpace(_config.InstanceName))
                _config.InstanceName = otherConfig.InstanceName;

            if (overrideExisting || !_config.AsyncSupport)
                _config.AsyncSupport = otherConfig.AsyncSupport;

            if (overrideExisting || _config.MaxConcurrentHandlers == Environment.ProcessorCount * 2)
                _config.MaxConcurrentHandlers = otherConfig.MaxConcurrentHandlers;

            if (overrideExisting || _config.MaxQueueSize == 10000)
                _config.MaxQueueSize = otherConfig.MaxQueueSize;

            if (overrideExisting || _config.HandlerTimeout == TimeSpan.FromSeconds(30))
                _config.HandlerTimeout = otherConfig.HandlerTimeout;

            // Continue merging other properties as needed...
            return this;
        }

        #endregion

        #region Validation and Building

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>This builder for chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public MessageBusConfigBuilder Validate()
        {
            var errors = _config.GetValidationErrors();
            if (!string.IsNullOrEmpty(errors))
            {
                throw new InvalidOperationException($"Configuration validation failed:\n{errors}");
            }

            return this;
        }

        /// <summary>
        /// Builds and returns the configured MessageBusConfig.
        /// </summary>
        /// <returns>The configured MessageBusConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public MessageBusConfig Build()
        {
            // Validate before building
            Validate();

            // Return a clone to prevent external modification
            return _config.Clone();
        }

        /// <summary>
        /// Builds and returns the configured MessageBusConfig without validation.
        /// Use this method only when you're certain the configuration is valid.
        /// </summary>
        /// <returns>The configured MessageBusConfig instance</returns>
        public MessageBusConfig BuildUnsafe()
        {
            return _config.Clone();
        }

        #endregion

        #region Implicit Conversion

        /// <summary>
        /// Implicit conversion from builder to configuration.
        /// </summary>
        /// <param name="builder">The builder to convert</param>
        /// <returns>The built configuration</returns>
        public static implicit operator MessageBusConfig(MessageBusConfigBuilder builder)
        {
            return builder?.Build();
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns a string representation of the current configuration.
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public override string ToString()
        {
            return $"MessageBusConfigBuilder: {_config}";
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for MessageBusConfigBuilder to provide additional fluent API capabilities.
    /// </summary>
    public static class MessageBusConfigBuilderExtensions
    {
        /// <summary>
        /// Configures the message bus for Unity-specific optimizations.
        /// </summary>
        /// <param name="builder">The configuration builder</param>
        /// <param name="targetFrameRate">Target frame rate for Unity optimization</param>
        /// <returns>The builder for chaining</returns>
        public static MessageBusConfigBuilder ForUnity(this MessageBusConfigBuilder builder, int targetFrameRate = 60)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Unity-specific optimizations
            var updateInterval = TimeSpan.FromMilliseconds(1000.0 / targetFrameRate * 2); // 2 frames interval
            
            return builder
                .WithStatisticsUpdateInterval(updateInterval)
                .WithHealthCheckInterval(TimeSpan.FromSeconds(2))
                .WithMaxConcurrentHandlers(Environment.ProcessorCount) // Conservative for Unity
                .WithObjectPooling(true) // Important for Unity GC
                .WithPreAllocateMemory(true) // Reduce GC pressure
                .Configure(config =>
                {
                    // Unity-specific memory constraints
                    config.MaxMemoryPressure = 50 * 1024 * 1024; // 50MB limit for mobile
                });
        }

        /// <summary>
        /// Configures the message bus for mobile device constraints.
        /// </summary>
        /// <param name="builder">The configuration builder</param>
        /// <returns>The builder for chaining</returns>
        public static MessageBusConfigBuilder ForMobile(this MessageBusConfigBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder
                .WithMaxConcurrentHandlers(2) // Limited CPU cores
                .WithMaxQueueSize(1000) // Limited memory
                .WithMaxMemoryPressure(25 * 1024 * 1024) // 25MB limit
                .WithObjectPooling(true) // Critical for mobile GC
                .WithPreAllocateMemory(false) // Avoid large allocations
                .WithStatisticsUpdateInterval(TimeSpan.FromSeconds(5)) // Reduce CPU usage
                .WithHealthCheckInterval(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Configures the message bus for server/desktop high-performance scenarios.
        /// </summary>
        /// <param name="builder">The configuration builder</param>
        /// <returns>The builder for chaining</returns>
        public static MessageBusConfigBuilder ForServer(this MessageBusConfigBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder
                .WithMaxConcurrentHandlers(Environment.ProcessorCount * 4) // Utilize all CPU
                .WithMaxQueueSize(100000) // Large queue for high throughput
                .WithMaxMemoryPressure(500 * 1024 * 1024) // 500MB limit
                .WithObjectPooling(true)
                .WithPreAllocateMemory(true)
                .WithStatisticsUpdateInterval(TimeSpan.FromSeconds(1)) // Frequent monitoring
                .WithHealthCheckInterval(TimeSpan.FromSeconds(5))
                .WithRetryPolicy(true, 5, TimeSpan.FromMilliseconds(100)) // Fast retry
                .WithCircuitBreaker(true, 10, TimeSpan.FromSeconds(10)); // Aggressive circuit breaking
        }

        /// <summary>
        /// Applies environment-specific configuration based on build settings.
        /// </summary>
        /// <param name="builder">The configuration builder</param>
        /// <param name="environment">The target environment</param>
        /// <returns>The builder for chaining</returns>
        public static MessageBusConfigBuilder ForEnvironment(this MessageBusConfigBuilder builder, string environment)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return environment?.ToLowerInvariant() switch
            {
                "development" or "dev" => builder.ForDevelopment(),
                "staging" or "test" => builder.ForReliability(),
                "production" or "prod" => builder.ForHighPerformance(),
                "mobile" => builder.ForMobile(),
                "server" => builder.ForServer(),
                _ => builder
            };
        }
    }
}