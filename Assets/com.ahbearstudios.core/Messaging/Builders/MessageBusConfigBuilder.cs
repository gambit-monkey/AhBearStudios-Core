using AhBearStudios.Core.Messaging.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Builders
{
    /// <summary>
    /// Builder for constructing MessageBusConfig instances with fluent API.
    /// Provides validation and sensible defaults for all configuration options.
    /// </summary>
    public sealed class MessageBusConfigBuilder
    {
        private bool _asyncSupport = true;
        private bool _performanceMonitoring = true;
        private bool _filteringEnabled = true;
        private bool _routingEnabled = true;
        private bool _healthChecksEnabled = true;
        private bool _alertsEnabled = true;
        private int _maxConcurrentHandlers = 100;
        private int _maxQueueSize = 10000;
        private TimeSpan _handlerTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        private TimeSpan _messageHistoryRetention = TimeSpan.FromHours(24);
        private int _maxRetryAttempts = 3;
        private TimeSpan _retryBaseDelay = TimeSpan.FromMilliseconds(100);
        private double _retryBackoffMultiplier = 2.0;
        private FixedString64Bytes _instanceName = "MessageBus";
        private bool _serializationEnabled = false;
        private bool _compressionEnabled = false;
        private int _compressionThreshold = 1024;
        private bool _deadLetterQueueEnabled = true;
        private int _deadLetterQueueMaxSize = 1000;

        /// <summary>
        /// Sets whether async message handling is enabled.
        /// </summary>
        /// <param name="enabled">True to enable async support, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithAsyncSupport(bool enabled)
        {
            _asyncSupport = enabled;
            return this;
        }

        /// <summary>
        /// Sets whether performance monitoring is enabled.
        /// </summary>
        /// <param name="enabled">True to enable performance monitoring, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithPerformanceMonitoring(bool enabled)
        {
            _performanceMonitoring = enabled;
            return this;
        }

        /// <summary>
        /// Sets whether message filtering capabilities are enabled.
        /// </summary>
        /// <param name="enabled">True to enable filtering, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithFiltering(bool enabled)
        {
            _filteringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets whether message routing is enabled.
        /// </summary>
        /// <param name="enabled">True to enable routing, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithRouting(bool enabled)
        {
            _routingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets whether health checks are enabled.
        /// </summary>
        /// <param name="enabled">True to enable health checks, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithHealthChecks(bool enabled)
        {
            _healthChecksEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets whether alerts are enabled.
        /// </summary>
        /// <param name="enabled">True to enable alerts, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithAlerts(bool enabled)
        {
            _alertsEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent message handlers.
        /// </summary>
        /// <param name="maxHandlers">Maximum concurrent handlers (must be positive)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxHandlers is not positive</exception>
        public MessageBusConfigBuilder WithMaxConcurrentHandlers(int maxHandlers)
        {
            if (maxHandlers <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHandlers), "Max concurrent handlers must be positive");

            _maxConcurrentHandlers = maxHandlers;
            return this;
        }

        /// <summary>
        /// Sets the maximum message queue size.
        /// </summary>
        /// <param name="maxSize">Maximum queue size (must be positive)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxSize is not positive</exception>
        public MessageBusConfigBuilder WithMaxQueueSize(int maxSize)
        {
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max queue size must be positive");

            _maxQueueSize = maxSize;
            return this;
        }

        /// <summary>
        /// Sets the timeout for message handler execution.
        /// </summary>
        /// <param name="timeout">Handler timeout (must be positive)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is not positive</exception>
        public MessageBusConfigBuilder WithHandlerTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Handler timeout must be positive");

            _handlerTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the health check execution interval.
        /// </summary>
        /// <param name="interval">Health check interval (must be positive)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is not positive</exception>
        public MessageBusConfigBuilder WithHealthCheckInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Health check interval must be positive");

            _healthCheckInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the message history retention period.
        /// </summary>
        /// <param name="retention">Retention period (must be positive)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when retention is not positive</exception>
        public MessageBusConfigBuilder WithMessageHistoryRetention(TimeSpan retention)
        {
            if (retention <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retention), "Message history retention must be positive");

            _messageHistoryRetention = retention;
            return this;
        }

        /// <summary>
        /// Sets the retry configuration for failed message handling.
        /// </summary>
        /// <param name="maxAttempts">Maximum retry attempts (must be non-negative)</param>
        /// <param name="baseDelay">Base delay for exponential backoff (must be non-negative)</param>
        /// <param name="backoffMultiplier">Backoff multiplier (must be positive)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
        public MessageBusConfigBuilder WithRetryPolicy(int maxAttempts, TimeSpan baseDelay, double backoffMultiplier = 2.0)
        {
            if (maxAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max retry attempts must be non-negative");
            if (baseDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(baseDelay), "Base delay must be non-negative");
            if (backoffMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be positive");

            _maxRetryAttempts = maxAttempts;
            _retryBaseDelay = baseDelay;
            _retryBackoffMultiplier = backoffMultiplier;
            return this;
        }

        /// <summary>
        /// Sets the instance name for this message bus.
        /// </summary>
        /// <param name="name">Instance name</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when name is null</exception>
        public MessageBusConfigBuilder WithInstanceName(FixedString64Bytes name)
        {
            _instanceName = name;
            return this;
        }

        /// <summary>
        /// Sets whether message serialization is enabled.
        /// </summary>
        /// <param name="enabled">True to enable serialization, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithSerialization(bool enabled)
        {
            _serializationEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets compression configuration for messages.
        /// </summary>
        /// <param name="enabled">True to enable compression, false otherwise</param>
        /// <param name="threshold">Size threshold in bytes for compression (must be non-negative)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative</exception>
        public MessageBusConfigBuilder WithCompression(bool enabled, int threshold = 1024)
        {
            if (threshold < 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Compression threshold must be non-negative");

            _compressionEnabled = enabled;
            _compressionThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Sets dead letter queue configuration.
        /// </summary>
        /// <param name="enabled">True to enable dead letter queue, false otherwise</param>
        /// <param name="maxSize">Maximum number of messages in dead letter queue (must be non-negative)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxSize is negative</exception>
        public MessageBusConfigBuilder WithDeadLetterQueue(bool enabled, int maxSize = 1000)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Dead letter queue max size must be non-negative");

            _deadLetterQueueEnabled = enabled;
            _deadLetterQueueMaxSize = maxSize;
            return this;
        }

        /// <summary>
        /// Builds and validates the MessageBusConfig instance.
        /// </summary>
        /// <returns>A validated MessageBusConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public MessageBusConfig Build()
        {
            var config = new MessageBusConfig
            {
                AsyncSupport = _asyncSupport,
                PerformanceMonitoring = _performanceMonitoring,
                FilteringEnabled = _filteringEnabled,
                RoutingEnabled = _routingEnabled,
                HealthChecksEnabled = _healthChecksEnabled,
                AlertsEnabled = _alertsEnabled,
                MaxConcurrentHandlers = _maxConcurrentHandlers,
                MaxQueueSize = _maxQueueSize,
                HandlerTimeout = _handlerTimeout,
                HealthCheckInterval = _healthCheckInterval,
                MessageHistoryRetention = _messageHistoryRetention,
                MaxRetryAttempts = _maxRetryAttempts,
                RetryBaseDelay = _retryBaseDelay,
                RetryBackoffMultiplier = _retryBackoffMultiplier,
                InstanceName = _instanceName,
                SerializationEnabled = _serializationEnabled,
                CompressionEnabled = _compressionEnabled,
                CompressionThreshold = _compressionThreshold,
                DeadLetterQueueEnabled = _deadLetterQueueEnabled,
                DeadLetterQueueMaxSize = _deadLetterQueueMaxSize
            };

            if (!config.IsValid())
                throw new InvalidOperationException("Built configuration is invalid");

            return config;
        }

        /// <summary>
        /// Creates a builder instance pre-configured for high performance scenarios.
        /// </summary>
        /// <returns>A MessageBusConfigBuilder configured for high performance</returns>
        public static MessageBusConfigBuilder ForHighPerformance() => new MessageBusConfigBuilder()
            .WithMaxConcurrentHandlers(500)
            .WithMaxQueueSize(50000)
            .WithHandlerTimeout(TimeSpan.FromSeconds(10))
            .WithPerformanceMonitoring(true)
            .WithCompression(false)
            .WithSerialization(false);

        /// <summary>
        /// Creates a builder instance pre-configured for reliability scenarios.
        /// </summary>
        /// <returns>A MessageBusConfigBuilder configured for reliability</returns>
        public static MessageBusConfigBuilder ForReliability() => new MessageBusConfigBuilder()
            .WithRetryPolicy(5, TimeSpan.FromSeconds(1))
            .WithDeadLetterQueue(true, 5000)
            .WithSerialization(true)
            .WithHealthCheckInterval(TimeSpan.FromSeconds(30))
            .WithAlerts(true);
    }
}