using System;

namespace AhBearStudios.Core.Messaging.Configuration
{
    /// <summary>
    /// Configuration for the batch-optimized delivery service.
    /// </summary>
    public sealed class BatchOptimizedConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of messages to process in a single batch.
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the interval at which batches are processed.
        /// </summary>
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMilliseconds(50);
        
        /// <summary>
        /// Gets or sets the interval at which all pending messages are flushed regardless of batch size.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(200);
        
        /// <summary>
        /// Gets or sets the timeout for confirmation messages.
        /// </summary>
        public TimeSpan ConfirmationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Gets or sets whether reliable messages should be processed immediately rather than batched.
        /// </summary>
        public bool ImmediateProcessingForReliable { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent batch processing operations.
        /// </summary>
        public int MaxConcurrentBatches { get; set; } = 4;
        
        /// <summary>
        /// Gets or sets whether to group messages by type for optimized batch processing.
        /// </summary>
        public bool GroupMessagesByType { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the threshold for triggering immediate batch processing based on queue size.
        /// </summary>
        public int ImmediateProcessingThreshold { get; set; } = 50;
        
        /// <summary>
        /// Gets or sets whether to enable adaptive batching that adjusts batch size based on throughput.
        /// </summary>
        public bool EnableAdaptiveBatching { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the target throughput (messages per second) for adaptive batching.
        /// </summary>
        public int TargetThroughput { get; set; } = 1000;
    }
}