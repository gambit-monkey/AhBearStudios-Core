
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for log target configuration that controls behavior and output characteristics.
    /// Provides a common abstraction for different logging target configurations.
    /// </summary>
    public interface ILogTargetConfig
    {
        /// <summary>
        /// Gets or sets the unique name of this log target.
        /// </summary>
        string TargetName { get; set; }
        
        /// <summary>
        /// Gets or sets whether this log target is enabled.
        /// </summary>
        bool Enabled { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// </summary>
        LogLevel MinimumLevel { get; set; }
        
        /// <summary>
        /// Gets or sets the tags that should be included by this log target.
        /// If empty, all tags are included.
        /// </summary>
        string[] IncludedTags { get; set; }
        
        /// <summary>
        /// Gets or sets the tags that should be excluded by this log target.
        /// </summary>
        string[] ExcludedTags { get; set; }
        
        /// <summary>
        /// Gets or sets whether to process untagged log messages.
        /// </summary>
        bool ProcessUntaggedMessages { get; set; }
        
        /// <summary>
        /// Gets or sets whether this target will forward Unity's internal logs.
        /// </summary>
        bool CaptureUnityLogs { get; set; }
        
        /// <summary>
        /// Gets or sets whether stack traces should be included in log output.
        /// </summary>
        bool IncludeStackTraces { get; set; }
        
        /// <summary>
        /// Gets or sets whether to include timestamps in log messages.
        /// </summary>
        bool IncludeTimestamps { get; set; }
        
        /// <summary>
        /// Gets or sets the format string for timestamps.
        /// </summary>
        string TimestampFormat { get; set; }
        
        /// <summary>
        /// Gets or sets whether to include source context information in log messages.
        /// </summary>
        bool IncludeSourceContext { get; set; }
        
        /// <summary>
        /// Gets or sets whether to include thread ID in log messages.
        /// </summary>
        bool IncludeThreadId { get; set; }
        
        /// <summary>
        /// Gets or sets whether structured logging is enabled.
        /// </summary>
        bool EnableStructuredLogging { get; set; }
        
        /// <summary>
        /// Gets or sets whether this target should flush immediately on each log.
        /// </summary>
        bool AutoFlush { get; set; }
        
        /// <summary>
        /// Gets or sets the buffer size for batched logging (0 = disabled).
        /// </summary>
        int BufferSize { get; set; }
        
        /// <summary>
        /// Gets or sets the flush interval in seconds (0 = disabled).
        /// </summary>
        float FlushIntervalSeconds { get; set; }
        
        /// <summary>
        /// Gets or sets whether to limit message length.
        /// </summary>
        bool LimitMessageLength { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum message length when limiting is enabled.
        /// </summary>
        int MaxMessageLength { get; set; }
        
        /// <summary>
        /// Creates a log target based on this configuration.
        /// </summary>
        /// <returns>A configured log target.</returns>
        ILogTarget CreateTarget();
        
        /// <summary>
        /// Creates a log target based on this configuration with optional message bus.
        /// </summary>
        /// <param name="messageBusService">Optional message bus for publishing log events.</param>
        /// <returns>A configured log target.</returns>
        ILogTarget CreateTarget(IMessageBusService messageBusService);
        
        /// <summary>
        /// Applies the tag filters to the specified log target.
        /// </summary>
        /// <param name="target">The log target to configure with filters.</param>
        void ApplyTagFilters(ILogTarget target);
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        ILogTargetConfig Clone();
    }
}