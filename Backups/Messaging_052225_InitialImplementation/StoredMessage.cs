using AhBearStudios.Core.Messaging.Data;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a stored message with metadata
    /// </summary>
    /// <typeparam name="TMessage">The type of message</typeparam>
    public class StoredMessage<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the ID of the message
        /// </summary>
        public string Id { get; set; }
    
        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public TMessage Message { get; set; }
    
        /// <summary>
        /// Gets or sets the metadata for the message
        /// </summary>
        public MessageMetadata Metadata { get; set; }
    }
}