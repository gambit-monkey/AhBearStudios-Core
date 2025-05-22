using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for monitoring message activity
    /// </summary>
    public interface IMessageMonitor
    {
        /// <summary>
        /// Records a message being published
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message being published</param>
        void RecordPublish<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Records a message being delivered to a subscriber
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message being delivered</param>
        /// <param name="subscriberId">The ID of the subscriber</param>
        void RecordDelivery<TMessage>(TMessage message, Guid subscriberId) where TMessage : IMessage;
    
        /// <summary>
        /// Gets the total count of messages published by type
        /// </summary>
        /// <returns>A dictionary of message types and counts</returns>
        IDictionary<Type, int> GetMessageCounts();
    
        /// <summary>
        /// Gets the most recent messages of any type
        /// </summary>
        /// <param name="count">The maximum number of messages to return</param>
        /// <returns>The most recent messages</returns>
        IEnumerable<MessageLogEntry> GetRecentMessages(int count = 100);
    
        /// <summary>
        /// Gets the most recent messages of a specific type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="count">The maximum number of messages to return</param>
        /// <returns>The most recent messages of the specified type</returns>
        IEnumerable<MessageLogEntry> GetRecentMessages<TMessage>(int count = 100) where TMessage : IMessage;
    
        /// <summary>
        /// Clears all message history
        /// </summary>
        void ClearHistory();
    }
}