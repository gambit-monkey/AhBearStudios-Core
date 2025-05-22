namespace AhBearStudios.Core.Messaging.Configuration
{
    /// <summary>
    /// Options for configuring a message bus
    /// </summary>
    public class MessageBusOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use thread-safe operations
        /// </summary>
        public bool ThreadSafe { get; set; } = true;
    
        /// <summary>
        /// Gets or sets the thread affinity mode
        /// </summary>
        public ThreadAffinityMode ThreadAffinityMode { get; set; } = ThreadAffinityMode.None;
    
        /// <summary>
        /// Gets or sets a value indicating whether to continue delivery to other subscribers if an error occurs
        /// </summary>
        public bool ContinueOnError { get; set; } = true;
    
        /// <summary>
        /// Gets or sets a value indicating whether to rethrow exceptions after handling
        /// </summary>
        public bool RethrowExceptions { get; set; } = false;
    
        /// <summary>
        /// Gets or sets the logging level
        /// </summary>
        public MessageLogLevel LogLevel { get; set; } = MessageLogLevel.Info;
    
        /// <summary>
        /// Gets or sets the maximum queue size
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000;
    
        /// <summary>
        /// Gets or sets a value indicating whether to use batch processing
        /// </summary>
        public bool UseBatchProcessing { get; set; } = false;
    
        /// <summary>
        /// Gets or sets the batch size
        /// </summary>
        public int BatchSize { get; set; } = 10;
    
        /// <summary>
        /// Gets or sets a value indicating whether to track message history
        /// </summary>
        public bool TrackMessageHistory { get; set; } = false;
    
        /// <summary>
        /// Gets or sets the maximum message history size
        /// </summary>
        public int MaxMessageHistorySize { get; set; } = 100;
    }
}