using System;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a message flow from a source to a target
    /// </summary>
    public class MessageFlow
    {
        /// <summary>
        /// Gets the source of the message
        /// </summary>
        public string Source { get; }
    
        /// <summary>
        /// Gets the target of the message
        /// </summary>
        public string Target { get; }
    
        /// <summary>
        /// Gets the type of message
        /// </summary>
        public Type MessageType { get; }
    
        /// <summary>
        /// Gets a description of the flow
        /// </summary>
        public string Description { get; }
    
        public MessageFlow(string source, string target, Type messageType, string description)
        {
            Source = source;
            Target = target;
            MessageType = messageType;
            Description = description;
        }
    }
}