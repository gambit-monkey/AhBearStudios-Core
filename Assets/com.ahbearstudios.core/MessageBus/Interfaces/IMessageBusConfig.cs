namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for message bus configuration that controls behavior and performance characteristics.
    /// </summary>
    public interface IMessageBusConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        string ConfigId { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of messages to process per frame.
        /// </summary>
        int MaxMessagesPerFrame { get; set; }
        
        /// <summary>
        /// Gets or sets the initial capacity for message queues.
        /// </summary>
        int InitialMessageQueueCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the time slice in milliseconds for message processing per frame.
        /// </summary>
        float MessageProcessingTimeSliceMs { get; set; }
        
        /// <summary>
        /// Gets or sets whether message pooling is enabled for performance optimization.
        /// </summary>
        bool EnableMessagePooling { get; set; }
        
        /// <summary>
        /// Gets or sets the initial size of the message pool.
        /// </summary>
        int MessagePoolInitialSize { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum size of the message pool.
        /// </summary>
        int MessagePoolMaxSize { get; set; }
        
        /// <summary>
        /// Gets or sets whether Burst-compatible serialization is enabled.
        /// </summary>
        bool EnableBurstSerialization { get; set; }
        
        /// <summary>
        /// Gets or sets whether network serialization is enabled.
        /// </summary>
        bool EnableNetworkSerialization { get; set; }
        
        /// <summary>
        /// Gets or sets whether compression is enabled for network serialization.
        /// </summary>
        bool EnableCompressionForNetwork { get; set; }
        
        /// <summary>
        /// Gets or sets whether reliable message delivery is enabled.
        /// </summary>
        bool EnableReliableDelivery { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of delivery retry attempts.
        /// </summary>
        int MaxDeliveryRetries { get; set; }
        
        /// <summary>
        /// Gets or sets the timeout in seconds for message delivery.
        /// </summary>
        float DeliveryTimeoutSeconds { get; set; }
        
        /// <summary>
        /// Gets or sets the backoff multiplier for retry attempts.
        /// </summary>
        float RetryBackoffMultiplier { get; set; }
        
        /// <summary>
        /// Gets or sets whether statistics collection is enabled.
        /// </summary>
        bool EnableStatisticsCollection { get; set; }
        
        /// <summary>
        /// Gets or sets whether delivery tracking is enabled.
        /// </summary>
        bool EnableDeliveryTracking { get; set; }
        
        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// </summary>
        bool EnablePerformanceMetrics { get; set; }
        
        /// <summary>
        /// Gets or sets whether message logging is enabled for debugging.
        /// </summary>
        bool EnableMessageLogging { get; set; }
        
        /// <summary>
        /// Gets or sets whether verbose logging is enabled.
        /// </summary>
        bool EnableVerboseLogging { get; set; }
        
        /// <summary>
        /// Gets or sets whether failed deliveries should be logged.
        /// </summary>
        bool LogFailedDeliveries { get; set; }
        
        /// <summary>
        /// Gets or sets whether multithreading is enabled for message processing.
        /// </summary>
        bool EnableMultithreading { get; set; }
        
        /// <summary>
        /// Gets or sets the number of worker threads for message processing.
        /// </summary>
        int WorkerThreadCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether the Unity Job System should be used for processing.
        /// </summary>
        bool UseJobSystemForProcessing { get; set; }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        IMessageBusConfig Clone();
    }
}