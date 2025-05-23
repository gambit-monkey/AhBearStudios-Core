using System;

namespace AhBearStudios.Core.MessageBus.Configuration
{
    /// <summary>
    /// Configuration for message delivery services.
    /// </summary>
    public sealed class DeliveryServiceConfiguration
    {
        /// <summary>
        /// Gets or sets the default timeout for message delivery operations.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Gets or sets the default maximum number of delivery attempts for reliable messages.
        /// </summary>
        public int DefaultMaxDeliveryAttempts { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the interval for processing pending deliveries.
        /// </summary>
        public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(1);
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent deliveries.
        /// </summary>
        public int MaxConcurrentDeliveries { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets whether to enable delivery statistics collection.
        /// </summary>
        public bool EnableStatistics { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to enable delivery performance profiling.
        /// </summary>
        public bool EnableProfiling { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to log delivery operations.
        /// </summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the log level for delivery operations.
        /// </summary>
        /// <summary>
        /// Gets or sets the log level for delivery operations.
        /// </summary>
        public byte LogLevel { get; set; } = AhBearStudios.Core.Logging.LogLevel.Debug;
        
        /// <summary>
        /// Gets or sets the exponential backoff base multiplier for retry delays.
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;
        
        /// <summary>
        /// Gets or sets the maximum retry delay.
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    }
}