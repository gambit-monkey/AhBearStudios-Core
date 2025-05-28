using UnityEngine;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Configuration
{
    /// <summary>
    /// Builder for creating and configuring MessageBus configurations using a fluent interface.
    /// Provides methods to set all properties of a message bus configuration.
    /// </summary>
    public sealed class MessageBusConfigBuilder : IMessageBusConfigBuilder<IMessageBusConfig, MessageBusConfigBuilder>
    {
        private readonly IMessageBusConfig _config;
        private IMessageBusConfig _optimizedConfig;

        /// <summary>
        /// Initializes a new instance of the MessageBusConfigBuilder with a default configuration.
        /// </summary>
        public MessageBusConfigBuilder()
        {
            _config = ScriptableObject.CreateInstance<MessageBusConfig>();
            _optimizedConfig = null;
        }

        /// <summary>
        /// Initializes a new instance of the MessageBusConfigBuilder with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to start with</param>
        public MessageBusConfigBuilder(IMessageBusConfig config)
        {
            _config = config.Clone();
            _optimizedConfig = null;
        }
        
        /// <summary>
        /// Creates a new builder initialized with settings from an existing configuration.
        /// </summary>
        /// <param name="config">The existing configuration to use as a starting point</param>
        /// <returns>A new builder instance with copied settings</returns>
        public static MessageBusConfigBuilder FromExisting(IMessageBusConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config), "Cannot create builder from null configuration");
            }
            
            return new MessageBusConfigBuilder(config);
        }

        /// <summary>
        /// Sets the configuration ID.
        /// </summary>
        /// <param name="configId">The unique identifier for this configuration</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithConfigId(string configId)
        {
            GetActiveConfig().ConfigId = configId;
            return this;
        }

        /// <summary>
        /// Sets the maximum messages per frame.
        /// </summary>
        /// <param name="maxMessages">The maximum number of messages to process per frame</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithMaxMessagesPerFrame(int maxMessages)
        {
            GetActiveConfig().MaxMessagesPerFrame = maxMessages;
            return this;
        }

        /// <summary>
        /// Sets the initial message queue capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity for message queues</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithInitialQueueCapacity(int capacity)
        {
            GetActiveConfig().InitialMessageQueueCapacity = capacity;
            return this;
        }

        /// <summary>
        /// Sets the message processing time slice.
        /// </summary>
        /// <param name="timeSliceMs">The time slice in milliseconds for message processing per frame</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithProcessingTimeSlice(float timeSliceMs)
        {
            GetActiveConfig().MessageProcessingTimeSliceMs = timeSliceMs;
            return this;
        }

        /// <summary>
        /// Configures message pooling settings.
        /// </summary>
        /// <param name="enabled">Whether message pooling is enabled</param>
        /// <param name="initialSize">The initial size of the message pool</param>
        /// <param name="maxSize">The maximum size of the message pool</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithMessagePooling(bool enabled, int initialSize = 100, int maxSize = 1000)
        {
            IMessageBusConfig config = GetActiveConfig();
            config.EnableMessagePooling = enabled;
            config.MessagePoolInitialSize = initialSize;
            config.MessagePoolMaxSize = maxSize;
            return this;
        }

        /// <summary>
        /// Configures serialization settings.
        /// </summary>
        /// <param name="enableBurst">Whether Burst-compatible serialization is enabled</param>
        /// <param name="enableNetwork">Whether network serialization is enabled</param>
        /// <param name="enableCompression">Whether compression is enabled for network serialization</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithSerialization(bool enableBurst = true, bool enableNetwork = false, bool enableCompression = true)
        {
            IMessageBusConfig config = GetActiveConfig();
            config.EnableBurstSerialization = enableBurst;
            config.EnableNetworkSerialization = enableNetwork;
            config.EnableCompressionForNetwork = enableCompression;
            return this;
        }

        /// <summary>
        /// Configures reliable delivery settings.
        /// </summary>
        /// <param name="enabled">Whether reliable delivery is enabled</param>
        /// <param name="maxRetries">The maximum number of delivery retry attempts</param>
        /// <param name="timeoutSeconds">The timeout in seconds for message delivery</param>
        /// <param name="backoffMultiplier">The backoff multiplier for retry attempts</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithReliableDelivery(bool enabled, int maxRetries = 3, float timeoutSeconds = 5f, float backoffMultiplier = 2f)
        {
            IMessageBusConfig config = GetActiveConfig();
            config.EnableReliableDelivery = enabled;
            config.MaxDeliveryRetries = maxRetries;
            config.DeliveryTimeoutSeconds = timeoutSeconds;
            config.RetryBackoffMultiplier = backoffMultiplier;
            return this;
        }

        /// <summary>
        /// Configures statistics and monitoring settings.
        /// </summary>
        /// <param name="enableStats">Whether statistics collection is enabled</param>
        /// <param name="enableTracking">Whether delivery tracking is enabled</param>
        /// <param name="enableMetrics">Whether performance metrics collection is enabled</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithStatistics(bool enableStats = true, bool enableTracking = true, bool enableMetrics = true)
        {
            IMessageBusConfig config = GetActiveConfig();
            config.EnableStatisticsCollection = enableStats;
            config.EnableDeliveryTracking = enableTracking;
            config.EnablePerformanceMetrics = enableMetrics;
            return this;
        }

        /// <summary>
        /// Configures logging settings.
        /// </summary>
        /// <param name="enableMessageLogging">Whether message logging is enabled for debugging</param>
        /// <param name="enableVerbose">Whether verbose logging is enabled</param>
        /// <param name="logFailures">Whether failed deliveries should be logged</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithLogging(bool enableMessageLogging = false, bool enableVerbose = false, bool logFailures = true)
        {
            IMessageBusConfig config = GetActiveConfig();
            config.EnableMessageLogging = enableMessageLogging;
            config.EnableVerboseLogging = enableVerbose;
            config.LogFailedDeliveries = logFailures;
            return this;
        }

        /// <summary>
        /// Configures threading settings.
        /// </summary>
        /// <param name="enableMultithreading">Whether multithreading is enabled for message processing</param>
        /// <param name="workerThreadCount">The number of worker threads for message processing</param>
        /// <param name="useJobSystem">Whether the Unity Job System should be used for processing</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithThreading(bool enableMultithreading = true, int workerThreadCount = 2, bool useJobSystem = true)
        {
            IMessageBusConfig config = GetActiveConfig();
            config.EnableMultithreading = enableMultithreading;
            config.WorkerThreadCount = workerThreadCount;
            config.UseJobSystemForProcessing = useJobSystem;
            return this;
        }

        /// <summary>
        /// Configures platform optimization options.
        /// </summary>
        /// <param name="enableMobileOptimizations">Whether to enable mobile-specific optimizations</param>
        /// <param name="enableConsoleOptimizations">Whether to enable console-specific optimizations</param>
        /// <param name="enableEditorDebugging">Whether to enable additional debugging in the Unity Editor</param>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithPlatformOptimizations(bool enableMobileOptimizations = true,
            bool enableConsoleOptimizations = true, bool enableEditorDebugging = true)
        {
            if (GetActiveConfig() is MessageBusConfig config)
            {
                // Use reflection-free property access for MessageBusConfig
                var property = typeof(MessageBusConfig).GetProperty("EnableMobileOptimizations");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, enableMobileOptimizations);
                }
                
                property = typeof(MessageBusConfig).GetProperty("EnableConsoleOptimizations");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, enableConsoleOptimizations);
                }
                
                property = typeof(MessageBusConfig).GetProperty("EnableEditorDebugging");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, enableEditorDebugging);
                }
            }
            
            return this;
        }

        /// <summary>
        /// Applies mobile-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
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
        /// <returns>This builder instance for method chaining</returns>
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
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder WithDevelopmentOptimizations()
        {
            return WithLogging(true, true, true)
                .WithStatistics(true, true, true)
                .WithReliableDelivery(true, 5, 10f, 1.5f);
        }

        /// <summary>
        /// Creates an optimized configuration for the current platform.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public MessageBusConfigBuilder OptimizeForCurrentPlatform()
        {
            if (GetActiveConfig() is MessageBusConfig config)
            {
                _optimizedConfig = config.GetPlatformOptimizedConfig();
            }
            
            return this;
        }

        /// <summary>
        /// Builds the final message bus configuration.
        /// </summary>
        /// <returns>The configured message bus configuration</returns>
        public IMessageBusConfig Build()
        {
            return GetActiveConfig().Clone();
        }
        
        /// <summary>
        /// Gets the active configuration (optimized if available, original otherwise).
        /// </summary>
        /// <returns>The active configuration</returns>
        private IMessageBusConfig GetActiveConfig()
        {
            return _optimizedConfig ?? _config;
        }
    }
}