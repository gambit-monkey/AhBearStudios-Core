using System;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Represents a single performance data point for trend analysis.
    /// </summary>
    public sealed class PerformanceDataPoint
    {
        /// <summary>
        /// Gets or sets the timestamp of this data point.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the messages per second at this point.
        /// </summary>
        public double MessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the error rate at this point.
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Gets or sets the average processing time at this point.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the memory usage at this point.
        /// </summary>
        public long MemoryUsageBytes { get; set; }
    }
}