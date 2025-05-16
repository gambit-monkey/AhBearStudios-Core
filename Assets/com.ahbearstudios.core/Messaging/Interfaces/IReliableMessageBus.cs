using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Interface for a message bus that guarantees message delivery even across application restarts.
    /// Extends IMessageBus with reliability features.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public interface IReliableMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Starts the reliable message processor.
        /// </summary>
        void Start();
        
        /// <summary>
        /// Stops the reliable message processor.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Gets the number of pending messages waiting for delivery.
        /// </summary>
        /// <returns>The number of pending messages.</returns>
        int GetPendingMessageCount();
        
        /// <summary>
        /// Clears all pending messages from the store.
        /// </summary>
        void ClearPendingMessages();
        
        /// <summary>
        /// Gets the IDs of all pending messages.
        /// </summary>
        /// <returns>A list of pending message IDs.</returns>
        List<Guid> GetPendingMessageIds();
        
        /// <summary>
        /// Manually triggers redelivery of a specific message.
        /// </summary>
        /// <param name="messageId">The ID of the message to redeliver.</param>
        void RedeliverMessage(Guid messageId);
    }
}