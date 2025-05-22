using System;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents an entry in the message history.
    /// </summary>
    public struct MessageHistoryEntry
    {
        /// <summary>
        /// Gets or sets the ID of the message.
        /// </summary>
        public Guid MessageId { get; set; }
        
        /// <summary>
        /// Gets or sets the type name of the message.
        /// </summary>
        public string MessageType { get; set; }
        
        /// <summary>
        /// Gets or sets the time when the message was received.
        /// </summary>
        public DateTime ReceivedTime { get; set; }
        
        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public double ProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the error message if an error occurred during processing, or null if successful.
        /// </summary>
        public string Error { get; set; }
    }


}