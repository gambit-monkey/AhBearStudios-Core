using System;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents an entry in the message log
    /// </summary>
    public class MessageLogEntry
    {
        /// <summary>
        /// Gets the message type
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the message
        /// </summary>
        public IMessage Message { get; }

        /// <summary>
        /// Gets the timestamp when the message was logged
        /// </summary>
        public DateTime Timestamp { get; }

        public MessageLogEntry(Type messageType, IMessage message)
        {
            MessageType = messageType;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}