using System;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Builders
{
    /// <summary>
    /// Fluent builder for constructing <see cref="MessageBusConfig"/> instances.
    /// </summary>
    public sealed class MessageBusConfigBuilder : IMessageBusConfigBuilder<MessageBusConfig, MessageBusConfigBuilder>
    {
        private readonly MessageBusConfig _config = new MessageBusConfig();

        /// <summary>
        /// Sets a unique identifier for the configuration.
        /// </summary>
        /// <param name="configId">Non-empty configuration ID.</param>
        /// <returns>Fluent builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configId"/> is null or whitespace.</exception>
        public MessageBusConfigBuilder WithConfigId(string configId)
        {
            if (string.IsNullOrWhiteSpace(configId))
                throw new ArgumentNullException(nameof(configId), "ConfigId must be non-empty.");
            _config.ConfigId = configId;
            return this;
        }

        /// <summary>
        /// Limits the number of messages processed per frame.
        /// </summary>
        /// <param name="maxMessages">Must be >= 1.</param>
        public MessageBusConfigBuilder WithMaxMessagesPerFrame(int maxMessages)
        {
            _config.MaxMessagesPerFrame = maxMessages;
            return this;
        }

        /// <summary>
        /// Sets initial capacity for internal message queues.
        /// </summary>
        /// <param name="capacity">Must be >= 1.</param>
        public MessageBusConfigBuilder WithInitialQueueCapacity(int capacity)
        {
            _config.InitialMessageQueueCapacity = capacity;
            return this;
        }

        /// <summary>
        /// Defines the time slice (in milliseconds) allotted for processing messages each update.
        /// </summary>
        /// <param name="milliseconds">Must be > 0.</param>
        public MessageBusConfigBuilder WithProcessingTimeSlice(float milliseconds)
        {
            _config.MessageProcessingTimeSliceMs = milliseconds;
            return this;
        }

        /// <summary>
        /// Toggles pooling of message objects to reduce allocations.
        /// </summary>
        public MessageBusConfigBuilder EnableMessagePooling(bool enabled)
        {
            _config.EnableMessagePooling = enabled;
            return this;
        }

        /// <summary>
        /// Sets the initial size of the message pool.
        /// </summary>
        /// <param name="initialSize">Must be >= 1.</param>
        public MessageBusConfigBuilder WithPoolInitialSize(int initialSize)
        {
            _config.MessagePoolInitialSize = initialSize;
            return this;
        }

        /// <summary>
        /// Sets the maximum size of the message pool.
        /// </summary>
        /// <param name="maxSize">Must be >= PoolInitialSize.</param>
        public MessageBusConfigBuilder WithPoolMaxSize(int maxSize)
        {
            _config.MessagePoolMaxSize = maxSize;
            return this;
        }

        /// <summary>
        /// Toggles Burst-compatible serialization for high throughput.
        /// </summary>
        public MessageBusConfigBuilder EnableBurstSerialization(bool enabled)
        {
            _config.EnableBurstSerialization = enabled;
            return this;
        }

        /// <summary>
        /// Toggles network serialization support.
        /// </summary>
        public MessageBusConfigBuilder EnableNetworkSerialization(bool enabled)
        {
            _config.EnableNetworkSerialization = enabled;
            return this;
        }

        /// <summary>
        /// Enables compression when sending messages over the network.
        /// </summary>
        public MessageBusConfigBuilder EnableNetworkCompression(bool enabled)
        {
            _config.EnableCompressionForNetwork = enabled;
            return this;
        }

        /// <summary>
        /// Enables reliable delivery with automatic retries.
        /// </summary>
        public MessageBusConfigBuilder EnableReliableDelivery(bool enabled)
        {
            _config.EnableReliableDelivery = enabled;
            return this;
        }

        /// <summary>
        /// Sets the maximum retry attempts for reliable delivery.
        /// </summary>
        public MessageBusConfigBuilder WithMaxDeliveryRetries(int retries)
        {
            _config.MaxDeliveryRetries = retries;
            return this;
        }

        /// <summary>
        /// Sets the delivery timeout (in seconds) before retrying.
        /// </summary>
        public MessageBusConfigBuilder WithDeliveryTimeout(float seconds)
        {
            _config.DeliveryTimeoutSeconds = seconds;
            return this;
        }

        /// <summary>
        /// Sets the exponential backoff multiplier for retries.
        /// </summary>
        public MessageBusConfigBuilder WithRetryBackoffMultiplier(float multiplier)
        {
            _config.RetryBackoffMultiplier = multiplier;
            return this;
        }

        /// <summary>
        /// Toggles collection of runtime statistics.
        /// </summary>
        public MessageBusConfigBuilder EnableStatisticsCollection(bool enabled)
        {
            _config.EnableStatisticsCollection = enabled;
            return this;
        }

        /// <summary>
        /// Toggles delivery tracking for debugging purposes.
        /// </summary>
        public MessageBusConfigBuilder EnableDeliveryTracking(bool enabled)
        {
            _config.EnableDeliveryTracking = enabled;
            return this;
        }

        /// <summary>
        /// Toggles performance metrics gathering.
        /// </summary>
        public MessageBusConfigBuilder EnablePerformanceMetrics(bool enabled)
        {
            _config.EnablePerformanceMetrics = enabled;
            return this;
        }

        /// <summary>
        /// Toggles message logging to console or file.
        /// </summary>
        public MessageBusConfigBuilder EnableMessageLogging(bool enabled)
        {
            _config.EnableMessageLogging = enabled;
            return this;
        }

        /// <summary>
        /// Toggles verbose logging (detailed debug information).
        /// </summary>
        public MessageBusConfigBuilder EnableVerboseLogging(bool enabled)
        {
            _config.EnableVerboseLogging = enabled;
            return this;
        }

        /// <summary>
        /// Toggles logging of failed deliveries.
        /// </summary>
        public MessageBusConfigBuilder LogFailedDeliveries(bool enabled)
        {
            _config.LogFailedDeliveries = enabled;
            return this;
        }

        /// <summary>
        /// Toggles multithreaded message processing.
        /// </summary>
        public MessageBusConfigBuilder EnableMultithreading(bool enabled)
        {
            _config.EnableMultithreading = enabled;
            return this;
        }

        /// <summary>
        /// Sets the number of worker threads for parallel processing.
        /// </summary>
        public MessageBusConfigBuilder WithWorkerThreadCount(int count)
        {
            _config.WorkerThreadCount = count;
            return this;
        }

        /// <summary>
        /// Toggles use of the Unity Job System for processing.
        /// </summary>
        public MessageBusConfigBuilder UseJobSystem(bool enabled)
        {
            _config.UseJobSystemForProcessing = enabled;
            return this;
        }

        /// <summary>
        /// Finalizes, validates, and returns a cloned configuration instance.
        /// </summary>
        /// <returns>Immutable <see cref="MessageBusConfig"/> ready for use.</returns>
        public MessageBusConfig Build()
        {
            _config.Validate();
            return (MessageBusConfig)_config.Clone();
        }
    }
}