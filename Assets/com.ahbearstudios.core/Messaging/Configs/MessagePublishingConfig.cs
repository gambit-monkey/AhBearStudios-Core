using System;

namespace AhBearStudios.Core.Messaging.Configs
{
    /// <summary>
    /// Configuration for message publishing service.
    /// Focused on publishing-specific settings and performance tuning.
    /// </summary>
    public sealed class MessagePublishingConfig
    {
        #region Core Publishing Configuration

        /// <summary>
        /// Gets or sets the maximum number of concurrent publishing operations.
        /// </summary>
        public int MaxConcurrentPublishers { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Gets or sets the timeout for individual publishing operations.
        /// </summary>
        public TimeSpan PublishingTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether batch publishing is enabled.
        /// </summary>
        public bool BatchPublishingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum batch size for batch operations.
        /// </summary>
        public int MaxBatchSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether async publishing is supported.
        /// </summary>
        public bool AsyncPublishingEnabled { get; set; } = true;

        #endregion

        #region Performance Configuration

        /// <summary>
        /// Gets or sets whether performance monitoring is enabled for publishing.
        /// </summary>
        public bool PerformanceMonitoringEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether object pooling is used for publishers.
        /// </summary>
        public bool UseObjectPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to pre-allocate memory for better performance.
        /// </summary>
        public bool PreAllocateMemory { get; set; } = false;

        /// <summary>
        /// Gets or sets the initial capacity for publisher collections.
        /// </summary>
        public int InitialPublisherCapacity { get; set; } = 64;

        #endregion

        #region Circuit Breaker Configuration

        /// <summary>
        /// Gets or sets whether circuit breaker pattern is enabled for publishing.
        /// </summary>
        public bool CircuitBreakerEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the failure threshold for opening the circuit breaker.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the timeout before transitioning to half-open state.
        /// </summary>
        public TimeSpan CircuitBreakerOpenTimeout { get; set; } = TimeSpan.FromSeconds(30);

        #endregion

        #region Statistics Configuration

        /// <summary>
        /// Gets or sets the interval for updating publishing statistics.
        /// </summary>
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets whether to track per-message-type statistics.
        /// </summary>
        public bool TrackPerTypeStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of message types to track statistics for.
        /// </summary>
        public int MaxTrackedMessageTypes { get; set; } = 1000;

        #endregion

        #region Health Check Configuration

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
        /// Gets or sets the warning threshold for average publishing time in milliseconds.
        /// </summary>
        public double WarningPublishingTimeThreshold { get; set; } = 1000; // 1 second

        /// <summary>
        /// Gets or sets the critical threshold for average publishing time in milliseconds.
        /// </summary>
        public double CriticalPublishingTimeThreshold { get; set; } = 5000; // 5 seconds

        #endregion

        #region Memory Management

        /// <summary>
        /// Gets or sets the maximum memory pressure threshold in bytes.
        /// </summary>
        public long MaxMemoryPressure { get; set; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Gets or sets whether to force garbage collection when memory pressure is high.
        /// </summary>
        public bool ForceGCOnHighMemoryPressure { get; set; } = false;

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration for correctness and completeness.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            if (MaxConcurrentPublishers <= 0) return false;
            if (PublishingTimeout <= TimeSpan.Zero) return false;
            if (MaxBatchSize <= 0) return false;
            if (InitialPublisherCapacity <= 0) return false;
            if (CircuitBreakerFailureThreshold <= 0) return false;
            if (CircuitBreakerOpenTimeout <= TimeSpan.Zero) return false;
            if (StatisticsUpdateInterval <= TimeSpan.Zero) return false;
            if (HealthCheckInterval <= TimeSpan.Zero) return false;
            if (WarningErrorRateThreshold < 0 || WarningErrorRateThreshold > 1) return false;
            if (CriticalErrorRateThreshold < 0 || CriticalErrorRateThreshold > 1) return false;
            if (CriticalErrorRateThreshold <= WarningErrorRateThreshold) return false;
            if (WarningPublishingTimeThreshold <= 0) return false;
            if (CriticalPublishingTimeThreshold <= WarningPublishingTimeThreshold) return false;
            if (MaxMemoryPressure <= 0) return false;
            if (MaxTrackedMessageTypes <= 0) return false;

            return true;
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>Deep copy of the configuration</returns>
        public MessagePublishingConfig Clone()
        {
            return new MessagePublishingConfig
            {
                MaxConcurrentPublishers = MaxConcurrentPublishers,
                PublishingTimeout = PublishingTimeout,
                BatchPublishingEnabled = BatchPublishingEnabled,
                MaxBatchSize = MaxBatchSize,
                AsyncPublishingEnabled = AsyncPublishingEnabled,
                PerformanceMonitoringEnabled = PerformanceMonitoringEnabled,
                UseObjectPooling = UseObjectPooling,
                PreAllocateMemory = PreAllocateMemory,
                InitialPublisherCapacity = InitialPublisherCapacity,
                CircuitBreakerEnabled = CircuitBreakerEnabled,
                CircuitBreakerFailureThreshold = CircuitBreakerFailureThreshold,
                CircuitBreakerOpenTimeout = CircuitBreakerOpenTimeout,
                StatisticsUpdateInterval = StatisticsUpdateInterval,
                TrackPerTypeStatistics = TrackPerTypeStatistics,
                MaxTrackedMessageTypes = MaxTrackedMessageTypes,
                HealthCheckInterval = HealthCheckInterval,
                WarningErrorRateThreshold = WarningErrorRateThreshold,
                CriticalErrorRateThreshold = CriticalErrorRateThreshold,
                WarningPublishingTimeThreshold = WarningPublishingTimeThreshold,
                CriticalPublishingTimeThreshold = CriticalPublishingTimeThreshold,
                MaxMemoryPressure = MaxMemoryPressure,
                ForceGCOnHighMemoryPressure = ForceGCOnHighMemoryPressure
            };
        }

        /// <summary>
        /// Returns a string representation of the configuration.
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public override string ToString()
        {
            return $"MessagePublishingConfig: " +
                   $"Publishers={MaxConcurrentPublishers}, " +
                   $"Timeout={PublishingTimeout.TotalSeconds}s, " +
                   $"BatchEnabled={BatchPublishingEnabled}, " +
                   $"MaxBatch={MaxBatchSize}, " +
                   $"AsyncEnabled={AsyncPublishingEnabled}, " +
                   $"CircuitBreaker={CircuitBreakerEnabled}, " +
                   $"Pooling={UseObjectPooling}";
        }

        #endregion
    }
}