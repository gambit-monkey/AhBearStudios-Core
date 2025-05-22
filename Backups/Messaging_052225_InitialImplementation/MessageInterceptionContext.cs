using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Context for message interception
    /// </summary>
    public class MessageInterceptionContext
    {
        /// <summary>
        /// Gets the type of message being intercepted
        /// </summary>
        public Type MessageType { get; }
    
        /// <summary>
        /// Gets or sets a value indicating whether to cancel the publication
        /// </summary>
        public bool Cancel { get; set; }
    
        /// <summary>
        /// Gets a dictionary of custom data for the interception
        /// </summary>
        public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();
    
        public MessageInterceptionContext(Type messageType)
        {
            MessageType = messageType;
        }
    }
}