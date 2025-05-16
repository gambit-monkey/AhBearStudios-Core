using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a message schema
    /// </summary>
    public class MessageSchema
    {
        /// <summary>
        /// Gets the message type
        /// </summary>
        public Type MessageType { get; }
    
        /// <summary>
        /// Gets the properties of the message
        /// </summary>
        public List<PropertySchema> Properties { get; } = new List<PropertySchema>();
    
        public MessageSchema(Type messageType)
        {
            MessageType = messageType;
        }
    }
}