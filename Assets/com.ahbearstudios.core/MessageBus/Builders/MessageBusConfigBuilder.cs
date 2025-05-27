using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Configuration
{
    /// <summary>
    /// Builder for creating MessageBus configurations using fluent API pattern.
    /// </summary>
    public sealed class MessageBusConfigBuilder : IPoolConfigBuilder<IMessageBusConfig, MessageBusConfigBuilder>
    {
        private readonly MessageBusConfig config;
        
        /// <summary>
        /// Initializes a new instance of the MessageBusConfigBuilder.
        /// </summary>
        public MessageBusConfigBuilder()
        {
            config = new MessageBusConfig();
        }
        
        /// <summary>
        /// Sets the configuration identifier.
        /// </summary>
        /// <param name="configId">The unique identifier for this configuration.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithConfigId(string configId)
        {
            config.ConfigId = configId;
            return this;
        }
        
        /// <summary>
        /// Sets the maximum messages per frame.
        /// </summary>
        /// <param name="maxMessages">The maximum number of messages to process per frame.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithMaxMessagesPerFrame(int maxMessages)
        {
            config.MaxMessagesPerFrame = maxMessages;
            return this;
        }
        
        /// <summary>
        /// Sets the initial message queue capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity for message queues.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithInitialQueueCapacity(int capacity)
        {
            config.InitialMessageQueueCapacity = capacity;
            return this;
        }
        
        /// <summary>
        /// Sets the message processing time slice.
        /// </summary>
        /// <param name="timeSliceMs">The time slice in milliseconds for message processing per frame.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithProcessingTimeSlice(float timeSliceMs)
        {
            config.MessageProcessingTimeSliceMs = timeSliceMs;
            return this;
        }
        
        /// <summary>
        /// Configures message pooling settings.
        /// </summary>
        /// <param name="enabled">Whether message pooling is enabled.</param>
        /// <param name="initialSize">The initial size of the message pool.</param>
        /// <param name="maxSize">The maximum size of the message pool.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithMessagePooling(bool enabled, int initialSize = 100, int maxSize = 1000)
        {
            config.EnableMessagePooling = enabled;
            config.MessagePoolInitialSize = initialSize;
            config.MessagePoolMaxSize = maxSize;
            return this;
        }
        
        /// <summary>
        /// Configures serialization settings.
        /// </summary>
        /// <param name="enableBurst">Whether Burst-compatible serialization is enabled.</param>
        /// <param name="enableNetwork">Whether network serialization is enabled.</param>
        /// <param name="enableCompression">Whether compression is enabled for network serialization.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithSerialization(bool enableBurst = true, bool enableNetwork = false, bool enableCompression = true)
        {
            config.EnableBurstSerialization = enableBurst;
            config.EnableNetworkSerialization = enableNetwork;
            config.EnableCompressionForNetwork = enableCompression;
            return this;
        }
        
        /// <summary>
        /// Configures reliable delivery settings.
        /// </summary>
        /// <param name="enabled">Whether reliable delivery is enabled.</param>
        /// <param name="maxRetries">The maximum number of delivery retry attempts.</param>
        /// <param name="timeoutSeconds">The timeout in seconds for message delivery.</param>
        /// <param name="backoffMultiplier">The backoff multiplier for retry attempts.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithReliableDelivery(bool enabled, int maxRetries = 3, float timeoutSeconds = 5f, float backoffMultiplier = 2f)
        {
            config.EnableReliableDelivery = enabled;
            config.MaxDeliveryRetries = maxRetries;
            config.DeliveryTimeoutSeconds = timeoutSeconds;
            config.RetryBackoffMultiplier = backoffMultiplier;
            return this;
        }
        
        /// <summary>
        /// Configures statistics and monitoring settings.
        /// </summary>
        /// <param name="enableStats">Whether statistics collection is enabled.</param>
        /// <param name="enableTracking">Whether delivery tracking is enabled.</param>
        /// <param name="enableMetrics">Whether performance metrics collection is enabled.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithStatistics(bool enableStats = true, bool enableTracking = true, bool enableMetrics = true)
        {
            config.EnableStatisticsCollection = enableStats;
            config.EnableDeliveryTracking = enableTracking;
            config.EnablePerformanceMetrics = enableMetrics;
            return this;
        }
        
        /// <summary>
        /// Configures logging settings.
        /// </summary>
        /// <param name="enableMessageLogging">Whether message logging is enabled for debugging.</param>
        /// <param name="enableVerbose">Whether verbose logging is enabled.</param>
        /// <param name="logFailures">Whether failed deliveries should be logged.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithLogging(bool enableMessageLogging = false, bool enableVerbose = false, bool logFailures = true)
        {
            config.EnableMessageLogging = enableMessageLogging;
            config.EnableVerboseLogging = enableVerbose;
            config.LogFailedDeliveries = logFailures;
            return this;
        }
        
        /// <summary>
        /// Configures threading settings.
        /// </summary>
        /// <param name="enableMultithreading">Whether multithreading is enabled for message processing.</param>
        /// <param name="workerThreadCount">The number of worker threads for message processing.</param>
        /// <param name="useJobSystem">Whether the Unity Job System should be used for processing.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithThreading(bool enableMultithreading = true, int workerThreadCount = 2, bool useJobSystem = true)
        {
            config.EnableMultithreading = enableMultithreading;
            config.WorkerThreadCount = workerThreadCount;
            config.UseJobSystemForProcessing = useJobSystem;
            return this;
        }
        
        /// <summary>
        /// Applies mobile-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithMobileOptimizations()
        {
            return WithMaxMessagesPerFrame(50)
                .WithProcessingTimeSlice(0.020f)
                .WithMessagePooling(true, 50, 500)
                .WithStatistics(false, false, false)
                .WithLogging(false, false, true)
                .WithThreading(false, 1, true);
        }
        
        /// <summary>
        /// Applies console-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithConsoleOptimizations()
        {
            return WithMaxMessagesPerFrame(200)
                .WithProcessingTimeSlice(0.008f)
                .WithMessagePooling(true, 200, 2000)
                .WithStatistics(true, true, true)
                .WithThreading(true, 4, true);
        }
        
        /// <summary>
        /// Applies development-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public MessageBusConfigBuilder WithDevelopmentOptimizations()
        {
            return WithLogging(true, true, true)
                .WithStatistics(true, true, true)
                .WithReliableDelivery(true, 5, 10f, 1.5f);
        }
        
        /// <summary>
        /// Builds the final configuration.
        /// </summary>
        /// <returns>The configured MessageBus configuration.</returns>
        public IMessageBusConfig Build()
        {
            return config.Clone();
        }
    }
}