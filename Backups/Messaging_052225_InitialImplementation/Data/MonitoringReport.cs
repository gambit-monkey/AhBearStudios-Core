using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Contains a snapshot of monitoring metrics.
    /// </summary>
    public class MonitoringReport
    {
        /// <summary>
        /// Gets or sets the time when this report was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the time when monitoring started.
        /// </summary>
        public DateTime MonitoringStartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the duration of monitoring.
        /// </summary>
        public TimeSpan MonitoringDuration { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of messages published.
        /// </summary>
        public long TotalMessagesPublished { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of messages processed.
        /// </summary>
        public long TotalMessagesProcessed { get; set; }
        
        /// <summary>
        /// Gets or sets the average processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the message rate per second.
        /// </summary>
        public double MessageRatePerSecond { get; set; }
        
        /// <summary>
        /// Gets or sets the metrics for each message type.
        /// </summary>
        public Dictionary<Type, MessageTypeMetrics> TypeMetrics { get; set; }
        
        /// <summary>
        /// Gets or sets the recent message history, or null if history tracking is disabled.
        /// </summary>
        public List<MessageHistoryEntry> RecentMessageHistory { get; set; }
    }
}