using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Registration
{
    /// <summary>
    /// Static registry of message types with their corresponding type codes.
    /// Acts as a facade over the underlying IMessageRegistry implementation.
    /// </summary>
    public static class MessageTypeRegistry
    {
        private static IMessageRegistry _registry;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Initializes the message type registry with the specified implementation.
        /// </summary>
        /// <param name="registry">The message registry implementation to use.</param>
        /// <param name="logger">The logger to use for logging.</param>
        public static void Initialize(IMessageRegistry registry, IBurstLogger logger)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
            lock (_lock)
            {
                if (_isInitialized) return;
                
                _registry = registry;
                _registry.DiscoverMessages();
                _isInitialized = true;
                
                logger.Log(LogLevel.Info, 
                    $"MessageTypeRegistry initialized with {registry.GetType().Name}",
                    "MessageTypeRegistry");
            }
        }
        
        /// <summary>
        /// Initializes the message type registry with the default implementation.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public static void Initialize(IBurstLogger logger)
        {
            Initialize(new DefaultMessageRegistry(logger), logger);
        }
        
        /// <summary>
        /// Gets the type code for the specified message type.
        /// </summary>
        /// <param name="messageType">The message type to get the code for.</param>
        /// <returns>The type code for the message type.</returns>
        public static ushort GetTypeCode(Type messageType)
        {
            EnsureInitialized();
            return _registry.GetTypeCode(messageType);
        }
        
        /// <summary>
        /// Gets the type code for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type to get the code for.</typeparam>
        /// <returns>The type code for the message type.</returns>
        public static ushort GetTypeCode<TMessage>() where TMessage : IMessage
        {
            EnsureInitialized();
            return _registry.GetTypeCode<TMessage>();
        }
        
        /// <summary>
        /// Gets the message type for the specified type code.
        /// </summary>
        /// <param name="typeCode">The type code to get the message type for.</param>
        /// <returns>The message type for the type code, or null if not found.</returns>
        public static Type GetMessageType(ushort typeCode)
        {
            EnsureInitialized();
            return _registry.GetMessageType(typeCode);
        }
        
        /// <summary>
        /// Gets all registered message types.
        /// </summary>
        /// <returns>A dictionary of message types and their corresponding type codes.</returns>
        public static IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes()
        {
            EnsureInitialized();
            return _registry.GetAllMessageTypes();
        }
        
        /// <summary>
        /// Gets all registered type codes and their corresponding message types.
        /// </summary>
        /// <returns>A dictionary of type codes and their corresponding message types.</returns>
        public static IReadOnlyDictionary<ushort, Type> GetAllTypeCodes()
        {
            EnsureInitialized();
            return _registry.GetAllTypeCodes();
        }
        
        /// <summary>
        /// Registers a message type with the registry.
        /// </summary>
        /// <param name="messageType">The message type to register.</param>
        public static void RegisterMessageType(Type messageType)
        {
            EnsureInitialized();
            _registry.RegisterMessageType(messageType);
        }
        
        /// <summary>
        /// Registers a message type with the registry and assigns a specific type code.
        /// </summary>
        /// <param name="messageType">The message type to register.</param>
        /// <param name="typeCode">The type code to assign to the message type.</param>
        public static void RegisterMessageType(Type messageType, ushort typeCode)
        {
            EnsureInitialized();
            _registry.RegisterMessageType(messageType, typeCode);
        }
        
        /// <summary>
        /// Gets the underlying message registry implementation.
        /// </summary>
        /// <returns>The message registry implementation.</returns>
        public static IMessageRegistry GetRegistry()
        {
            EnsureInitialized();
            return _registry;
        }
        
        /// <summary>
        /// Checks if the message type registry is initialized.
        /// </summary>
        /// <returns>True if initialized; otherwise, false.</returns>
        public static bool IsInitialized()
        {
            lock (_lock)
            {
                return _isInitialized;
            }
        }
        
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("MessageTypeRegistry must be initialized before use. Call Initialize() first.");
            }
        }
    }
}