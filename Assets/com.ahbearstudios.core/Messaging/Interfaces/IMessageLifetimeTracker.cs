using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for tracking message lifetimes
    /// </summary>
    public interface IMessageLifetimeTracker
    {
        /// <summary>
        /// Tracks the lifetime of a message
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message to track</param>
        /// <returns>A token that can be used to end tracking</returns>
        IDisposable TrackMessage<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Gets all active messages
        /// </summary>
        /// <returns>The active messages</returns>
        IEnumerable<IMessage> GetActiveMessages();
    
        /// <summary>
        /// Gets all active messages of a specific type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>The active messages of the specified type</returns>
        IEnumerable<TMessage> GetActiveMessages<TMessage>() where TMessage : IMessage;
    
        /// <summary>
        /// Gets a value indicating whether a message is active
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message to check</param>
        /// <returns>True if the message is active; otherwise, false</returns>
        bool IsMessageActive<TMessage>(TMessage message) where TMessage : IMessage;
    }
}