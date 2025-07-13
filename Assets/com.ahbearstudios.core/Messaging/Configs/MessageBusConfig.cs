using System.Text;
using AhBearStudios.Core.Messaging.Builders;

namespace AhBearStudios.Core.Messaging.Configs
{
    /// <summary>
    /// Comprehensive configuration for MessageBusService with full production-ready settings.
    /// Supports all enterprise features including circuit breakers, retry policies, and monitoring thresholds.
    /// </summary>
    public sealed class MessageBusConfig
    {
        #region Core Configuration

        /// <summary>
        /// Gets or sets the instance name for this message bus.
        /// </summary>
        public string InstanceName { get; set; } = "DefaultMessageBus";

        /// <summary>
        /// Gets or sets whether async message handling is supported.
        /// </summary>
        public bool AsyncSupport { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of concurrent message handlers.
        /// </summary>
        public int MaxConcurrentHandlers { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Gets or sets the maximum queue size before backpressure kicks in.
        /// </summary>
        public int MaxQueueSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the timeout for individual message handlers.
        /// </summary>
        public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromSeconds(30);

        #endregion

        #region Feature Toggles

        /// <summary>
        /// Gets or sets whether performance monitoring is enabled.
        /// </summary>
        public bool PerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether health checks are enabled.
        /// </summary>
        public bool HealthChecksEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether alerts are enabled.
        /// </summary>
        public bool AlertsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether failed messages should be retried.
        /// </summary>
        public bool RetryFailedMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets whether circuit breaker pattern is enabled.
        /// </summary>
        public bool UseCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets whether object pooling is enabled for performance.
        /// </summary>
        public bool UseObjectPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to pre-allocate memory for better performance.
        /// </summary>
        public bool PreAllocateMemory { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to warm up the thread pool on startup.
        /// </summary>
        public bool WarmUpThreadPool { get; set; } = true;

        #endregion

        #region Retry Configuration

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed messages.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the interval for processing the retry queue.
        /// </summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum number of messages to retry in a single batch.
        /// </summary>
        public int MaxRetryBatchSize { get; set; } = 100;

        #endregion

        #region Circuit Breaker Configuration

        /// <summary>
        /// Gets or sets the circuit breaker configuration.
        /// </summary>
        public CircuitBreakerConfig CircuitBreakerConfig { get; set; } = new CircuitBreakerConfig();

        /// <summary>
        /// Gets or sets the interval for checking circuit breaker states.
        /// </summary>
        public TimeSpan CircuitBreakerCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

        #endregion

        #region Monitoring and Statistics

        /// <summary>
        /// Gets or sets the interval for updating statistics.
        /// </summary>
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the interval for performing health checks.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the warning threshold for error rate (0.0 to 1.0).
        /// </summary>
        public double WarningErrorRateThreshold { get; set; } = 0.05; // 5%

        /// <summary>
        /// Gets or sets the critical threshold for error rate (0.0 to 1.0).
        /// </summary>
        public double CriticalErrorRateThreshold { get; set; } = 0.10; // 10%

        /// <summary>
        /// Gets or sets the warning threshold for queue size.
        /// </summary>
        public int WarningQueueSizeThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the critical threshold for queue size.
        /// </summary>
        public int CriticalQueueSizeThreshold { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the warning threshold for message processing time.
        /// </summary>
        public TimeSpan WarningProcessingTimeThreshold { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the critical threshold for message processing time.
        /// </summary>
        public TimeSpan CriticalProcessingTimeThreshold { get; set; } = TimeSpan.FromSeconds(5);

        #endregion

        #region Memory and Performance

        /// <summary>
        /// Gets the estimated memory requirement for this configuration.
        /// </summary>
        public long EstimatedMemoryRequirement
        {
            get
            {
                // Rough estimation based on configuration
                var baseMemory = 1024 * 1024; // 1MB base
                var handlerMemory = MaxConcurrentHandlers * 64 * 1024; // 64KB per handler
                var queueMemory = MaxQueueSize * 256; // 256 bytes per queued message estimate
                
                return baseMemory + handlerMemory + queueMemory;
            }
        }

        /// <summary>
        /// Gets or sets the maximum memory pressure threshold (in bytes).
        /// </summary>
        public long MaxMemoryPressure { get; set; } = 100 * 1024 * 1024; // 100MB

        #endregion

        #region Predefined Configurations

        /// <summary>
        /// Gets a high-performance configuration optimized for speed.
        /// </summary>
        public static MessageBusConfig HighPerformance => new MessageBusConfigBuilder()
            .WithInstanceName("HighPerformanceMessageBus")
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
            .WithWarmUpThreadPool(true)
            .Build();

        /// <summary>
        /// Gets a reliable configuration optimized for error handling and monitoring.
        /// </summary>
        public static MessageBusConfig Reliable => new MessageBusConfigBuilder()
            .WithInstanceName("ReliableMessageBus")
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
            .WithProcessingTimeThresholds(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2))
            .Build();

        /// <summary>
        /// Gets a development configuration optimized for debugging and testing.
        /// </summary>
        public static MessageBusConfig Development => new MessageBusConfigBuilder()
            .WithInstanceName("DevelopmentMessageBus")
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
            .WithProcessingTimeThresholds(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30))
            .Build();

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration for correctness and completeness.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                var errors = GetValidationErrors();
                return string.IsNullOrEmpty(errors);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets detailed validation error messages for invalid configuration.
        /// </summary>
        /// <returns>Validation error messages, or empty string if valid</returns>
        public string GetValidationErrors()
        {
            var errors = new StringBuilder();

            // Validate core configuration
            if (string.IsNullOrWhiteSpace(InstanceName))
                errors.AppendLine("InstanceName cannot be null or empty");

            if (MaxConcurrentHandlers <= 0)
                errors.AppendLine("MaxConcurrentHandlers must be greater than 0");

            if (MaxQueueSize <= 0)
                errors.AppendLine("MaxQueueSize must be greater than 0");

            if (HandlerTimeout <= TimeSpan.Zero)
                errors.AppendLine("HandlerTimeout must be greater than zero");

            // Validate retry configuration
            if (RetryFailedMessages)
            {
                if (MaxRetryAttempts <= 0)
                    errors.AppendLine("MaxRetryAttempts must be greater than 0 when retry is enabled");

                if (RetryDelay <= TimeSpan.Zero)
                    errors.AppendLine("RetryDelay must be greater than zero when retry is enabled");

                if (MaxRetryBatchSize <= 0)
                    errors.AppendLine("MaxRetryBatchSize must be greater than 0 when retry is enabled");
            }

            // Validate circuit breaker configuration
            if (UseCircuitBreaker && CircuitBreakerConfig != null)
            {
                if (!CircuitBreakerConfig.IsValid())
                    errors.AppendLine("CircuitBreakerConfig is invalid");
            }

            // Validate monitoring intervals
            if (StatisticsUpdateInterval <= TimeSpan.Zero)
                errors.AppendLine("StatisticsUpdateInterval must be greater than zero");

            if (HealthCheckInterval <= TimeSpan.Zero)
                errors.AppendLine("HealthCheckInterval must be greater than zero");

            // Validate thresholds
            if (WarningErrorRateThreshold < 0 || WarningErrorRateThreshold > 1)
                errors.AppendLine("WarningErrorRateThreshold must be between 0 and 1");

            if (CriticalErrorRateThreshold < 0 || CriticalErrorRateThreshold > 1)
                errors.AppendLine("CriticalErrorRateThreshold must be between 0 and 1");

            if (CriticalErrorRateThreshold <= WarningErrorRateThreshold)
                errors.AppendLine("CriticalErrorRateThreshold must be greater than WarningErrorRateThreshold");

            if (WarningQueueSizeThreshold < 0)
                errors.AppendLine("WarningQueueSizeThreshold must be non-negative");

            if (CriticalQueueSizeThreshold <= WarningQueueSizeThreshold)
                errors.AppendLine("CriticalQueueSizeThreshold must be greater than WarningQueueSizeThreshold");

            if (WarningProcessingTimeThreshold <= TimeSpan.Zero)
                errors.AppendLine("WarningProcessingTimeThreshold must be greater than zero");

            if (CriticalProcessingTimeThreshold <= WarningProcessingTimeThreshold)
                errors.AppendLine("CriticalProcessingTimeThreshold must be greater than WarningProcessingTimeThreshold");

            // Validate memory settings
            if (MaxMemoryPressure <= 0)
                errors.AppendLine("MaxMemoryPressure must be greater than 0");

            // Validate logical constraints
            if (MaxConcurrentHandlers > MaxQueueSize)
                errors.AppendLine("MaxConcurrentHandlers should not exceed MaxQueueSize for optimal performance");

            return errors.ToString().Trim();
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns a string representation of the configuration.
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public override string ToString()
        {
            return $"MessageBusConfig[{InstanceName}]: " +
                   $"Handlers={MaxConcurrentHandlers}, " +
                   $"QueueSize={MaxQueueSize}, " +
                   $"Async={AsyncSupport}, " +
                   $"Monitoring={PerformanceMonitoring}, " +
                   $"HealthChecks={HealthChecksEnabled}, " +
                   $"Alerts={AlertsEnabled}, " +
                   $"Retry={RetryFailedMessages}, " +
                   $"CircuitBreaker={UseCircuitBreaker}, " +
                   $"Pooling={UseObjectPooling}";
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>Deep copy of the configuration</returns>
        public MessageBusConfig Clone()
        {
            return new MessageBusConfig
            {
                InstanceName = InstanceName,
                AsyncSupport = AsyncSupport,
                MaxConcurrentHandlers = MaxConcurrentHandlers,
                MaxQueueSize = MaxQueueSize,
                HandlerTimeout = HandlerTimeout,
                PerformanceMonitoring = PerformanceMonitoring,
                HealthChecksEnabled = HealthChecksEnabled,
                AlertsEnabled = AlertsEnabled,
                RetryFailedMessages = RetryFailedMessages,
                UseCircuitBreaker = UseCircuitBreaker,
                UseObjectPooling = UseObjectPooling,
                PreAllocateMemory = PreAllocateMemory,
                WarmUpThreadPool = WarmUpThreadPool,
                MaxRetryAttempts = MaxRetryAttempts,
                RetryDelay = RetryDelay,
                RetryInterval = RetryInterval,
                MaxRetryBatchSize = MaxRetryBatchSize,
                CircuitBreakerConfig = new CircuitBreakerConfig
                {
                    FailureThreshold = CircuitBreakerConfig.FailureThreshold,
                    OpenTimeout = CircuitBreakerConfig.OpenTimeout,
                    HalfOpenSuccessThreshold = CircuitBreakerConfig.HalfOpenSuccessThreshold
                },
                CircuitBreakerCheckInterval = CircuitBreakerCheckInterval,
                StatisticsUpdateInterval = StatisticsUpdateInterval,
                HealthCheckInterval = HealthCheckInterval,
                WarningErrorRateThreshold = WarningErrorRateThreshold,
                CriticalErrorRateThreshold = CriticalErrorRateThreshold,
                WarningQueueSizeThreshold = WarningQueueSizeThreshold,
                CriticalQueueSizeThreshold = CriticalQueueSizeThreshold,
                WarningProcessingTimeThreshold = WarningProcessingTimeThreshold,
                CriticalProcessingTimeThreshold = CriticalProcessingTimeThreshold,
                MaxMemoryPressure = MaxMemoryPressure
            };
        }

        #endregion
    }
}