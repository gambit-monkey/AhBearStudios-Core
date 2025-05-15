using System;

namespace AhBearStudios.Core.Logging.Events
{
    /// <summary>
    /// Event arguments for log processing events.
    /// </summary>
    public class LogProcessingEventArgs : EventArgs
    {
        /// <summary>
        /// The number of messages processed in this batch.
        /// </summary>
        public int ProcessedCount { get; }

        /// <summary>
        /// The number of messages still queued for processing.
        /// </summary>
        public int RemainingCount { get; }

        /// <summary>
        /// The time it took to process this batch in milliseconds.
        /// </summary>
        public float ProcessingTimeMs { get; }

        /// <summary>
        /// Creates a new instance with the specified processing statistics.
        /// </summary>
        /// <param name="processedCount">The number of messages processed.</param>
        /// <param name="remainingCount">The number of messages remaining.</param>
        /// <param name="processingTimeMs">The processing time in milliseconds.</param>
        public LogProcessingEventArgs(int processedCount, int remainingCount, float processingTimeMs)
        {
            ProcessedCount = processedCount;
            RemainingCount = remainingCount;
            ProcessingTimeMs = processingTimeMs;
        }
    }
}