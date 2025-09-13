using System;

namespace AhBearStudios.Core.Messaging.Configs
{
    /// <summary>
    /// Configuration for message subscription service.
    /// Focused on subscription-specific settings and performance tuning.
    /// </summary>
    public sealed class MessageSubscriptionConfig
    {
        #region Core Subscription Configuration

        /// <summary>
        /// Gets or sets the maximum number of concurrent message handlers per subscription.
        /// </summary>
        public int MaxConcurrentHandlers { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Gets or sets the timeout for individual message processing operations.
        /// </summary>
        public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether async message handling is supported.
        /// </summary>
        public bool AsyncHandlingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether message filtering is enabled.
        /// </summary>
        public bool FilteringEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether priority-based message routing is enabled.
        /// </summary>
        public bool PriorityRoutingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of subscriptions per message type.
        /// </summary>
        public int MaxSubscriptionsPerType { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum total number of subscriptions.
        /// </summary>
        public int MaxTotalSubscriptions { get; set; } = 10000;

        #endregion

        #region Performance Configuration

        /// <summary>
        /// Gets or sets whether performance monitoring is enabled for subscriptions.
        /// </summary>
        public bool PerformanceMonitoringEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether object pooling is used for subscribers.
        /// </summary>
        public bool UseObjectPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to pre-allocate memory for better performance.
        /// </summary>
        public bool PreAllocateMemory { get; set; } = false;

        /// <summary>
        /// Gets or sets the initial capacity for subscriber collections.
        /// </summary>
        public int InitialSubscriberCapacity { get; set; } = 64;

        /// <summary>
        /// Gets or sets whether to enable message bus integration.
        /// </summary>
        public bool MessageBusIntegrationEnabled { get; set; } = true;

        #endregion

        #region Circuit Breaker Configuration

        /// <summary>
        /// Gets or sets whether circuit breaker pattern is enabled for subscriptions.
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

        #region Error Handling Configuration

        /// <summary>
        /// Gets or sets whether to retry failed message processing.
        /// </summary>
        public bool RetryFailedMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        #endregion

        #region Statistics Configuration

        /// <summary>
        /// Gets or sets the interval for updating subscription statistics.
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

        /// <summary>
        /// Gets or sets whether to track scope statistics.
        /// </summary>
        public bool TrackScopeStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of scopes to track statistics for.
        /// </summary>
        public int MaxTrackedScopes { get; set; } = 1000;

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
        /// Gets or sets the warning threshold for average processing time in milliseconds.
        /// </summary>
        public double WarningProcessingTimeThreshold { get; set; } = 1000; // 1 second

        /// <summary>
        /// Gets or sets the critical threshold for average processing time in milliseconds.
        /// </summary>
        public double CriticalProcessingTimeThreshold { get; set; } = 5000; // 5 seconds

        /// <summary>
        /// Gets or sets the warning threshold for active subscriptions.
        /// </summary>
        public int WarningActiveSubscriptionsThreshold { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the critical threshold for active subscriptions.
        /// </summary>
        public int CriticalActiveSubscriptionsThreshold { get; set; } = 8000;

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
            if (MaxConcurrentHandlers <= 0) return false;
            if (ProcessingTimeout <= TimeSpan.Zero) return false;
            if (MaxSubscriptionsPerType <= 0) return false;
            if (MaxTotalSubscriptions <= 0) return false;
            if (InitialSubscriberCapacity <= 0) return false;
            if (CircuitBreakerFailureThreshold <= 0) return false;
            if (CircuitBreakerOpenTimeout <= TimeSpan.Zero) return false;
            if (MaxRetryAttempts < 0) return false;
            if (RetryDelay < TimeSpan.Zero) return false;
            if (StatisticsUpdateInterval <= TimeSpan.Zero) return false;
            if (HealthCheckInterval <= TimeSpan.Zero) return false;
            if (WarningErrorRateThreshold < 0 || WarningErrorRateThreshold > 1) return false;
            if (CriticalErrorRateThreshold < 0 || CriticalErrorRateThreshold > 1) return false;
            if (CriticalErrorRateThreshold <= WarningErrorRateThreshold) return false;
            if (WarningProcessingTimeThreshold <= 0) return false;
            if (CriticalProcessingTimeThreshold <= WarningProcessingTimeThreshold) return false;
            if (WarningActiveSubscriptionsThreshold <= 0) return false;
            if (CriticalActiveSubscriptionsThreshold <= WarningActiveSubscriptionsThreshold) return false;
            if (MaxMemoryPressure <= 0) return false;
            if (MaxTrackedMessageTypes <= 0) return false;
            if (MaxTrackedScopes <= 0) return false;

            // Logical validations
            if (MaxSubscriptionsPerType > MaxTotalSubscriptions) return false;

            return true;
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>Deep copy of the configuration</returns>
        public MessageSubscriptionConfig Clone()
        {
            return new MessageSubscriptionConfig
            {
                MaxConcurrentHandlers = MaxConcurrentHandlers,
                ProcessingTimeout = ProcessingTimeout,
                AsyncHandlingEnabled = AsyncHandlingEnabled,
                FilteringEnabled = FilteringEnabled,
                PriorityRoutingEnabled = PriorityRoutingEnabled,
                MaxSubscriptionsPerType = MaxSubscriptionsPerType,
                MaxTotalSubscriptions = MaxTotalSubscriptions,
                PerformanceMonitoringEnabled = PerformanceMonitoringEnabled,
                UseObjectPooling = UseObjectPooling,
                PreAllocateMemory = PreAllocateMemory,
                InitialSubscriberCapacity = InitialSubscriberCapacity,
                MessageBusIntegrationEnabled = MessageBusIntegrationEnabled,
                CircuitBreakerEnabled = CircuitBreakerEnabled,
                CircuitBreakerFailureThreshold = CircuitBreakerFailureThreshold,
                CircuitBreakerOpenTimeout = CircuitBreakerOpenTimeout,
                RetryFailedMessages = RetryFailedMessages,
                MaxRetryAttempts = MaxRetryAttempts,
                RetryDelay = RetryDelay,
                StatisticsUpdateInterval = StatisticsUpdateInterval,
                TrackPerTypeStatistics = TrackPerTypeStatistics,
                MaxTrackedMessageTypes = MaxTrackedMessageTypes,
                TrackScopeStatistics = TrackScopeStatistics,
                MaxTrackedScopes = MaxTrackedScopes,
                HealthCheckInterval = HealthCheckInterval,
                WarningErrorRateThreshold = WarningErrorRateThreshold,
                CriticalErrorRateThreshold = CriticalErrorRateThreshold,
                WarningProcessingTimeThreshold = WarningProcessingTimeThreshold,
                CriticalProcessingTimeThreshold = CriticalProcessingTimeThreshold,
                WarningActiveSubscriptionsThreshold = WarningActiveSubscriptionsThreshold,
                CriticalActiveSubscriptionsThreshold = CriticalActiveSubscriptionsThreshold,
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
            return $"MessageSubscriptionConfig: " +
                   $"Handlers={MaxConcurrentHandlers}, " +
                   $"Timeout={ProcessingTimeout.TotalSeconds}s, " +
                   $"AsyncEnabled={AsyncHandlingEnabled}, " +
                   $"FilteringEnabled={FilteringEnabled}, " +
                   $"MaxSubs={MaxTotalSubscriptions}, " +
                   $"CircuitBreaker={CircuitBreakerEnabled}, " +
                   $"Pooling={UseObjectPooling}";
        }

        #endregion
    }
}