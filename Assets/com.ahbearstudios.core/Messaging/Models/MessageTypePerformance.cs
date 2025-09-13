using System;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Represents the performance metrics for a specific message type.
    /// </summary>
    public sealed class MessageTypePerformance
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public Type MessageType { get; set; }

        /// <summary>
        /// Gets or sets the total messages processed.
        /// </summary>
        public long TotalMessages { get; set; }

        /// <summary>
        /// Gets or sets the success rate.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the average processing time.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the throughput in messages per second.
        /// </summary>
        public double MessagesPerSecond { get; set; }
    }
}