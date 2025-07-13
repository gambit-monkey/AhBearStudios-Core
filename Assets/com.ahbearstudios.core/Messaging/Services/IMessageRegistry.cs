using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
    /// Interface for message registry.
    /// </summary>
    public interface IMessageRegistry : IDisposable
    {
        /// <summary>
        /// Gets whether the registry is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the number of registered message types.
        /// </summary>
        int RegisteredTypeCount { get; }

        /// <summary>
        /// Registers a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        void RegisterMessageType<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Registers a message type.
        /// </summary>
        /// <param name="messageType">The message type</param>
        void RegisterMessageType(Type messageType);

        /// <summary>
        /// Checks if a message type is registered.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>True if registered, false otherwise</returns>
        bool IsRegistered<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Checks if a message type is registered.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <returns>True if registered, false otherwise</returns>
        bool IsRegistered(Type messageType);

        /// <summary>
        /// Gets message type information.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Message type information</returns>
        MessageTypeInfo GetMessageTypeInfo<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Gets message type information.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <returns>Message type information</returns>
        MessageTypeInfo GetMessageTypeInfo(Type messageType);

        /// <summary>
        /// Gets a message type by its type code.
        /// </summary>
        /// <param name="typeCode">The type code</param>
        /// <returns>The message type, or null if not found</returns>
        Type GetMessageType(ushort typeCode);

        /// <summary>
        /// Gets a message type by its name.
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The message type, or null if not found</returns>
        Type GetMessageType(string typeName);

        /// <summary>
        /// Gets all registered message types.
        /// </summary>
        /// <returns>Collection of message types</returns>
        IEnumerable<Type> GetMessageTypes();

        /// <summary>
        /// Gets message types by category.
        /// </summary>
        /// <param name="category">The category</param>
        /// <returns>Collection of message types in the category</returns>
        IEnumerable<Type> GetMessageTypesByCategory(string category);

        /// <summary>
        /// Gets derived types of a base type.
        /// </summary>
        /// <param name="baseType">The base type</param>
        /// <returns>Collection of derived types</returns>
        IEnumerable<Type> GetDerivedTypes(Type baseType);

        /// <summary>
        /// Unregisters a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        void UnregisterMessageType<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Unregisters a message type.
        /// </summary>
        /// <param name="messageType">The message type</param>
        void UnregisterMessageType(Type messageType);

        /// <summary>
        /// Clears the registry.
        /// </summary>
        void ClearRegistry();

        /// <summary>
        /// Gets registry statistics.
        /// </summary>
        /// <returns>Registry statistics</returns>
        MessageRegistryStatistics GetStatistics();

        /// <summary>
        /// Event raised when a message type is registered.
        /// </summary>
        event EventHandler<MessageTypeRegisteredEventArgs> MessageTypeRegistered;

        /// <summary>
        /// Event raised when a message type is unregistered.
        /// </summary>
        event EventHandler<MessageTypeUnregisteredEventArgs> MessageTypeUnregistered;

        /// <summary>
        /// Event raised when the registry is cleared.
        /// </summary>
        event EventHandler<RegistryClearedEventArgs> RegistryCleared;
    }