using System;

namespace AhBearStudios.Core.Logging.Events
{
    /// <summary>
    /// Event arguments for log flush events.
    /// </summary>
    public class LogFlushEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the number of messages processed during the flush.
        /// </summary>
        public int ProcessedCount { get; }

        /// <summary>
        /// Gets the duration of the flush operation in milliseconds.
        /// </summary>
        public float DurationMs { get; }

        /// <summary>
        /// Creates new event arguments with the specified processed count and duration.
        /// </summary>
        /// <param name="processedCount">The number of messages processed.</param>
        /// <param name="durationMs">The duration in milliseconds.</param>
        public LogFlushEventArgs(int processedCount, float durationMs)
        {
            ProcessedCount = processedCount;
            DurationMs = durationMs;
        }
    }
}