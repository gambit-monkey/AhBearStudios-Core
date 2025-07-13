using System;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Configs
{
    /// <summary>
    /// Configuration settings for the message bus system.
    /// Defines performance, reliability, and monitoring parameters.
    /// </summary>
    public sealed record MessageBusConfig
    {
        /// <summary>
        /// Gets whether async message handling is enabled.
        /// </summary>
        public bool AsyncSupport { get; init; } = true;

        /// <summary>
        /// Gets whether performance monitoring is enabled.
        /// </summary>
        public bool PerformanceMonitoring { get; init; } = true;

        /// <summary>
        /// Gets whether message filtering capabilities are enabled.
        /// </summary>
        public bool FilteringEnabled { get; init; } = true;

        /// <summary>
        /// Gets whether message routing is enabled.
        /// </summary>
        public bool RoutingEnabled { get; init; } = true;

        /// <summary>
        /// Gets whether health checks are enabled for the message bus.
        /// </summary>
        public bool HealthChecksEnabled { get; init; } = true;

        /// <summary>
        /// Gets whether alerts should be raised for message bus issues.
        /// </summary>
        public bool AlertsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the maximum number of concurrent message handlers.
        /// </summary>
        public int MaxConcurrentHandlers { get; init; } = 100;

        /// <summary>
        /// Gets the maximum message queue size before backpressure is applied.
        /// </summary>
        public int MaxQueueSize { get; init; } = 10000;

        /// <summary>
        /// Gets the timeout for message handler execution.
        /// </summary>
        public TimeSpan HandlerTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the interval for health check execution.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets the retention period for message history.
        /// </summary>
        public TimeSpan MessageHistoryRetention { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets the maximum number of retry attempts for failed message handling.
        /// </summary>
        public int MaxRetryAttempts { get; init; } = 3;

        /// <summary>
        /// Gets the base delay for exponential backoff retry strategy.
        /// </summary>
        public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets the multiplier for exponential backoff retry strategy.
        /// </summary>
        public double RetryBackoffMultiplier { get; init; } = 2.0;

        /// <summary>
        /// Gets the name identifier for this message bus instance.
        /// </summary>
        public FixedString64Bytes InstanceName { get; init; } = "MessageBus";

        /// <summary>
        /// Gets whether message serialization is enabled for persistence.
        /// </summary>
        public bool SerializationEnabled { get; init; } = false;

        /// <summary>
        /// Gets whether message compression is enabled.
        /// </summary>
        public bool CompressionEnabled { get; init; } = false;

        /// <summary>
        /// Gets the threshold in bytes above which messages should be compressed.
        /// </summary>
        public int CompressionThreshold { get; init; } = 1024;

        /// <summary>
        /// Gets whether dead letter queue functionality is enabled.
        /// </summary>
        public bool DeadLetterQueueEnabled { get; init; } = true;

        /// <summary>
        /// Gets the maximum number of messages to retain in the dead letter queue.
        /// </summary>
        public int DeadLetterQueueMaxSize { get; init; } = 1000;

        /// <summary>
        /// Validates that all configuration values are within acceptable ranges.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            return MaxConcurrentHandlers > 0 &&
                   MaxQueueSize > 0 &&
                   HandlerTimeout > TimeSpan.Zero &&
                   HealthCheckInterval > TimeSpan.Zero &&
                   MessageHistoryRetention > TimeSpan.Zero &&
                   MaxRetryAttempts >= 0 &&
                   RetryBaseDelay >= TimeSpan.Zero &&
                   RetryBackoffMultiplier > 0 &&
                   CompressionThreshold >= 0 &&
                   DeadLetterQueueMaxSize >= 0;
        }

        /// <summary>
        /// Creates a default configuration suitable for most scenarios.
        /// </summary>
        /// <returns>A MessageBusConfig with sensible defaults</returns>
        public static MessageBusConfig Default => new();

        /// <summary>
        /// Creates a high-performance configuration optimized for throughput.
        /// </summary>
        /// <returns>A MessageBusConfig optimized for high throughput scenarios</returns>
        public static MessageBusConfig HighPerformance => new()
        {
            MaxConcurrentHandlers = 500,
            MaxQueueSize = 50000,
            HandlerTimeout = TimeSpan.FromSeconds(10),
            PerformanceMonitoring = true,
            CompressionEnabled = false,
            SerializationEnabled = false
        };

        /// <summary>
        /// Creates a reliable configuration optimized for durability.
        /// </summary>
        /// <returns>A MessageBusConfig optimized for reliability scenarios</returns>
        public static MessageBusConfig Reliable => new()
        {
            MaxRetryAttempts = 5,
            RetryBaseDelay = TimeSpan.FromSeconds(1),
            DeadLetterQueueEnabled = true,
            SerializationEnabled = true,
            HealthCheckInterval = TimeSpan.FromSeconds(30),
            AlertsEnabled = true
        };
    }
}