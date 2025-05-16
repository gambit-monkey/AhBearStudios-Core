using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Data;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for persisting messages for reliable delivery
    /// </summary>
    public interface IMessageStore
    {
        /// <summary>
        /// Stores a message for reliable delivery
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="messageId">The ID of the message</param>
        /// <param name="message">The message to store</param>
        /// <param name="metadata">Optional metadata about the message</param>
        Task StoreMessageAsync<TMessage>(string messageId, TMessage message, MessageMetadata metadata = null) where TMessage : IMessage;
    
        /// <summary>
        /// Marks a message as delivered
        /// </summary>
        /// <param name="messageId">The ID of the message</param>
        Task MarkMessageDeliveredAsync(string messageId);
    
        /// <summary>
        /// Gets all pending messages of a specific type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>The pending messages</returns>
        Task<IEnumerable<StoredMessage<TMessage>>> GetPendingMessagesAsync<TMessage>() where TMessage : IMessage;
    
        /// <summary>
        /// Gets a message by ID
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="messageId">The ID of the message</param>
        /// <returns>The stored message, or null if not found</returns>
        Task<StoredMessage<TMessage>> GetMessageAsync<TMessage>(string messageId) where TMessage : IMessage;
    
        /// <summary>
        /// Removes messages older than the specified timespan
        /// </summary>
        /// <param name="age">The maximum age of messages to keep</param>
        /// <returns>The number of messages removed</returns>
        Task<int> PurgeOldMessagesAsync(TimeSpan age);
    }
}