using System;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders
{
    /// <summary>
    /// Builder for creating MessagePublishingConfig instances.
    /// Provides a fluent API for configuring message publishing behavior.
    /// </summary>
    public sealed class MessagePublishingConfigBuilder
    {
        private int _maxConcurrentPublishers = Environment.ProcessorCount * 2;
        private int _batchSize = 100;
        private TimeSpan _publishingTimeout = TimeSpan.FromSeconds(30);
        private bool _circuitBreakerEnabled = true;
        private bool _retryEnabled = true;
        private bool _performanceMonitoringEnabled = false;

        /// <summary>
        /// Sets the maximum number of concurrent publishers.
        /// </summary>
        /// <param name="maxConcurrentPublishers">Maximum concurrent publishers</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessagePublishingConfigBuilder WithMaxConcurrentPublishers(int maxConcurrentPublishers)
        {
            _maxConcurrentPublishers = maxConcurrentPublishers;
            return this;
        }

        /// <summary>
        /// Sets the batch size for batch publishing operations.
        /// </summary>
        /// <param name="batchSize">Batch size</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessagePublishingConfigBuilder WithBatchSize(int batchSize)
        {
            _batchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Sets the publish timeout.
        /// </summary>
        /// <param name="publishTimeout">Publish timeout</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessagePublishingConfigBuilder WithPublishTimeout(TimeSpan publishTimeout)
        {
            _publishingTimeout = publishTimeout;
            return this;
        }

        /// <summary>
        /// Enables or disables circuit breaker functionality.
        /// </summary>
        /// <param name="enabled">Whether circuit breaker is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessagePublishingConfigBuilder WithCircuitBreakerEnabled(bool enabled)
        {
            _circuitBreakerEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables retry functionality.
        /// </summary>
        /// <param name="enabled">Whether retry is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessagePublishingConfigBuilder WithRetryEnabled(bool enabled)
        {
            _retryEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables performance monitoring.
        /// </summary>
        /// <param name="enabled">Whether performance monitoring is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessagePublishingConfigBuilder WithPerformanceMonitoring(bool enabled)
        {
            _performanceMonitoringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Builds the MessagePublishingConfig instance with the configured values.
        /// </summary>
        /// <returns>A new MessagePublishingConfig instance</returns>
        public MessagePublishingConfig Build()
        {
            return new MessagePublishingConfig
            {
                MaxConcurrentPublishers = _maxConcurrentPublishers,
                MaxBatchSize = _batchSize,
                PublishingTimeout = _publishingTimeout,
                CircuitBreakerEnabled = _circuitBreakerEnabled,
                PerformanceMonitoringEnabled = _performanceMonitoringEnabled
            };
        }
    }
}