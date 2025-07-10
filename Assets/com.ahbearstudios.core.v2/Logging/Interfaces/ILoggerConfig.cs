using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Configuration interface for loggers across the system.
    /// Provides settings that control logging behavior.
    /// </summary>
    public interface ILoggerConfig
    {
        /// <summary>
        /// The minimum log level that will be processed.
        /// Messages below this level will be ignored.
        /// </summary>
        LogLevel MinimumLevel { get; }
        
        /// <summary>
        /// Maximum number of messages to process per batch.
        /// Used by LogBatchProcessor to prevent frame spikes.
        /// </summary>
        int MaxMessagesPerBatch { get; }
        
        /// <summary>
        /// Default tag to use when no tag is specified.
        /// </summary>
        Tagging.LogTag DefaultTag { get; }
        
        /// <summary>
        /// Gets or sets whether async logging is enabled
        /// </summary>
        bool EnableAsyncLogging { get; set; }
    
        /// <summary>
        /// Gets or sets the async queue capacity
        /// </summary>
        int AsyncQueueCapacity { get; set; }
    
        /// <summary>
        /// Gets or sets the flush timeout for async operations
        /// </summary>
        float AsyncFlushTimeoutSeconds { get; set; }
    }
}