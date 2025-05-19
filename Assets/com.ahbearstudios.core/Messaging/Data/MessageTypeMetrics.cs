using System;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Contains metrics for a specific message type.
    /// </summary>
    public class MessageTypeMetrics
    {
        /// <summary>
        /// Gets or sets the name of the message type.
        /// </summary>
        public string MessageTypeName { get; set; }
        
        /// <summary>
        /// Gets or sets the number of messages published of this type.
        /// </summary>
        public long MessagesPublished { get; set; }
        
        /// <summary>
        /// Gets or sets the number of messages processed of this type.
        /// </summary>
        public long MessagesProcessed { get; set; }
        
        /// <summary>
        /// Gets or sets the total time spent processing messages of this type.
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }
        
        /// <summary>
        /// Gets or sets the average processing time in milliseconds for messages of this type.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the time when the last message of this type was received.
        /// </summary>
        public DateTime? LastMessageTime { get; set; }
        
        /// <summary>
        /// Gets or sets the peak message rate per second for this message type.
        /// </summary>
        public double PeakMessageRatePerSecond { get; set; }
        
        /// <summary>
        /// Gets or sets the number of errors encountered while processing messages of this type.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Creates a clone of these metrics.
        /// </summary>
        /// <returns>A cloned copy of these metrics.</returns>
        public MessageTypeMetrics Clone()
        {
            return new MessageTypeMetrics
            {
                MessageTypeName = MessageTypeName,
                MessagesPublished = MessagesPublished,
                MessagesProcessed = MessagesProcessed,
                TotalProcessingTime = TotalProcessingTime,
                AverageProcessingTimeMs = AverageProcessingTimeMs,
                LastMessageTime = LastMessageTime,
                PeakMessageRatePerSecond = PeakMessageRatePerSecond,
                ErrorCount = ErrorCount
            };
        }
    }

    
}