using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Metadata about a stored message
    /// </summary>
    public class MessageMetadata
    {
        /// <summary>
        /// Gets or sets the type of the message
        /// </summary>
        public string MessageType { get; set; }
    
        /// <summary>
        /// Gets or sets the creation time of the message
        /// </summary>
        public DateTime CreationTime { get; set; }
    
        /// <summary>
        /// Gets or sets the priority of the message
        /// </summary>
        public MessagePriority Priority { get; set; }
    
        /// <summary>
        /// Gets or sets the retry count for the message
        /// </summary>
        public int RetryCount { get; set; }
    
        /// <summary>
        /// Gets or sets the maximum number of retries for the message
        /// </summary>
        public int MaxRetries { get; set; }
    
        /// <summary>
        /// Gets or sets the next retry time for the message
        /// </summary>
        public DateTime? NextRetryTime { get; set; }
    
        /// <summary>
        /// Gets or sets a value indicating whether the message has been delivered
        /// </summary>
        public bool IsDelivered { get; set; }
    
        /// <summary>
        /// Gets or sets the delivery time of the message
        /// </summary>
        public DateTime? DeliveryTime { get; set; }
    
        /// <summary>
        /// Gets or sets custom properties for the message
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }
}