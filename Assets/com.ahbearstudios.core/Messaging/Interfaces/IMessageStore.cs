using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Interface for a storage system that persists messages.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to store.</typeparam>
    public interface IMessageStore<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Stores a message for reliable delivery.
        /// </summary>
        /// <param name="message">The message to store.</param>
        void StoreMessage(TMessage message);
        
        /// <summary>
        /// Stores a message asynchronously for reliable delivery.
        /// </summary>
        /// <param name="message">The message to store.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreMessageAsync(TMessage message, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a message by its ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to retrieve.</param>
        /// <returns>The message if found, or default(TMessage) if not found.</returns>
        TMessage GetMessage(Guid messageId);
        
        /// <summary>
        /// Gets a message asynchronously by its ID.
        /// </summary>
        /// <param name="messageId">The ID of the message to retrieve.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The message if found, or default(TMessage) if not found.</returns>
        Task<TMessage> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Removes a message from the store.
        /// </summary>
        /// <param name="messageId">The ID of the message to remove.</param>
        void RemoveMessage(Guid messageId);
        
        /// <summary>
        /// Removes a message asynchronously from the store.
        /// </summary>
        /// <param name="messageId">The ID of the message to remove.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the number of messages in the store.
        /// </summary>
        /// <returns>The number of messages.</returns>
        int GetMessageCount();
        
        /// <summary>
        /// Gets the IDs of all messages in the store.
        /// </summary>
        /// <returns>A list of message IDs.</returns>
        List<Guid> GetMessageIds();
        
        /// <summary>
        /// Gets all messages in the store.
        /// </summary>
        /// <returns>A list of all messages.</returns>
        List<TMessage> GetAllMessages();
        
        /// <summary>
        /// Gets all messages in the store asynchronously.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A list of all messages.</returns>
        Task<List<TMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Clears all messages from the store.
        /// </summary>
        void ClearMessages();
        
        /// <summary>
        /// Clears all messages from the store asynchronously.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearMessagesAsync(CancellationToken cancellationToken = default);
    }
}