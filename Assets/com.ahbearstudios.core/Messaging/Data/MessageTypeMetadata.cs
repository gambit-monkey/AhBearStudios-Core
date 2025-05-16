using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Metadata about a message type
    /// </summary>
    public class MessageTypeMetadata
    {
        /// <summary>
        /// Gets the message type
        /// </summary>
        public Type MessageType { get; }
    
        /// <summary>
        /// Gets the category of the message
        /// </summary>
        public string Category { get; }
    
        /// <summary>
        /// Gets a description of the message
        /// </summary>
        public string Description { get; }
    
        /// <summary>
        /// Gets a value indicating whether this message is transient
        /// </summary>
        public bool IsTransient { get; }
    
        /// <summary>
        /// Gets the version of the message
        /// </summary>
        public int Version { get; }
    
        /// <summary>
        /// Gets or sets the source of this message definition
        /// </summary>
        public string Source { get; set; }
    
        /// <summary>
        /// Gets or sets the target(s) of this message
        /// </summary>
        public List<string> Targets { get; set; } = new List<string>();
    
        /// <summary>
        /// Gets or sets custom properties for this message type
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    
        public MessageTypeMetadata(Type messageType, string category = "General", string description = null, bool isTransient = false, int version = 1)
        {
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Category = category;
            Description = description ?? messageType.Name;
            IsTransient = isTransient;
            Version = version;
        }
    }
}