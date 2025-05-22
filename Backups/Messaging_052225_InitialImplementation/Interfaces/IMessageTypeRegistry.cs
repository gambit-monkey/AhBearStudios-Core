using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Data;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for registering message types
    /// </summary>
    public interface IMessageTypeRegistry
    {
        /// <summary>
        /// Registers a message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to register</typeparam>
        /// <param name="metadata">Optional metadata about the message type</param>
        void RegisterMessageType<TMessage>(MessageTypeMetadata metadata = null) where TMessage : IMessage;
    
        /// <summary>
        /// Registers a message type
        /// </summary>
        /// <param name="messageType">The type of message to register</param>
        /// <param name="metadata">Optional metadata about the message type</param>
        void RegisterMessageType(Type messageType, MessageTypeMetadata metadata = null);
    
        /// <summary>
        /// Gets the metadata for a message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>The metadata for the message type</returns>
        MessageTypeMetadata GetMessageTypeMetadata<TMessage>() where TMessage : IMessage;
    
        /// <summary>
        /// Gets the metadata for a message type
        /// </summary>
        /// <param name="messageType">The type of message</param>
        /// <returns>The metadata for the message type</returns>
        MessageTypeMetadata GetMessageTypeMetadata(Type messageType);
    
        /// <summary>
        /// Gets all registered message types
        /// </summary>
        /// <returns>The registered message types</returns>
        IEnumerable<Type> GetAllMessageTypes();
    
        /// <summary>
        /// Gets all registered message types in the specified category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>The registered message types in the category</returns>
        IEnumerable<Type> GetMessageTypesByCategory(string category);
    }
}