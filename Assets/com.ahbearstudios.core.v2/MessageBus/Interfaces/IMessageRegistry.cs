using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for message registry implementations that provide discovery and cataloging of messages.
    /// </summary>
    public interface IMessageRegistry
    {
        /// <summary>
        /// Discovers and registers all message types in the loaded assemblies.
        /// </summary>
        void DiscoverMessages();
        
        /// <summary>
        /// Registers a message type with the registry.
        /// </summary>
        /// <param name="messageType">The message type to register.</param>
        void RegisterMessageType(Type messageType);
        
        /// <summary>
        /// Registers a message type with the registry and assigns a specific type code.
        /// </summary>
        /// <param name="messageType">The message type to register.</param>
        /// <param name="typeCode">The type code to assign to the message type.</param>
        void RegisterMessageType(Type messageType, ushort typeCode);
        
        /// <summary>
        /// Gets all registered message types.
        /// </summary>
        /// <returns>A dictionary of message types and their information.</returns>
        IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes();
        
        /// <summary>
        /// Gets all message categories.
        /// </summary>
        /// <returns>A list of message categories.</returns>
        IReadOnlyList<string> GetCategories();
        
        /// <summary>
        /// Gets all message types in a category.
        /// </summary>
        /// <param name="category">The category to get message types for.</param>
        /// <returns>A list of message types in the category.</returns>
        IReadOnlyList<Type> GetMessageTypesByCategory(string category);
        
        /// <summary>
        /// Gets information about a message type.
        /// </summary>
        /// <param name="messageType">The message type to get information for.</param>
        /// <returns>Information about the message type, or null if not registered.</returns>
        IMessageInfo GetMessageInfo(Type messageType);
        
        /// <summary>
        /// Gets information about a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type to get information for.</typeparam>
        /// <returns>Information about the message type, or null if not registered.</returns>
        IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage;
        
        /// <summary>
        /// Checks if a message type is registered.
        /// </summary>
        /// <param name="messageType">The message type to check.</param>
        /// <returns>True if the message type is registered; otherwise, false.</returns>
        bool IsRegistered(Type messageType);
        
        /// <summary>
        /// Checks if a message type is registered.
        /// </summary>
        /// <typeparam name="TMessage">The message type to check.</typeparam>
        /// <returns>True if the message type is registered; otherwise, false.</returns>
        bool IsRegistered<TMessage>() where TMessage : IMessage;
        
        /// <summary>
        /// Gets the type code for the specified message type.
        /// </summary>
        /// <param name="messageType">The message type to get the code for.</param>
        /// <returns>The type code for the message type, or 0 if not registered.</returns>
        ushort GetTypeCode(Type messageType);
        
        /// <summary>
        /// Gets the type code for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type to get the code for.</typeparam>
        /// <returns>The type code for the message type, or 0 if not registered.</returns>
        ushort GetTypeCode<TMessage>() where TMessage : IMessage;
        
        /// <summary>
        /// Gets the message type for the specified type code.
        /// </summary>
        /// <param name="typeCode">The type code to get the message type for.</param>
        /// <returns>The message type for the type code, or null if not found.</returns>
        Type GetMessageType(ushort typeCode);
        
        /// <summary>
        /// Gets all registered type codes and their corresponding message types.
        /// </summary>
        /// <returns>A dictionary of type codes and their corresponding message types.</returns>
        IReadOnlyDictionary<ushort, Type> GetAllTypeCodes();
        
        /// <summary>
        /// Clears all registered message types.
        /// </summary>
        void Clear();
    }
}