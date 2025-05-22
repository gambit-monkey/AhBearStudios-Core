using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Registration
{
    /// <summary>
    /// High-performance message registry implementation that uses concurrent collections
    /// and caching for improved performance in multi-threaded scenarios.
    /// </summary>
    public sealed class CachedMessageRegistry : IMessageRegistry
    {
        private readonly IBurstLogger _logger;
        private readonly DefaultMessageRegistry _baseRegistry;
        private readonly ConcurrentDictionary<Type, IMessageInfo> _messageInfoCache = new ConcurrentDictionary<Type, IMessageInfo>();
        private readonly ConcurrentDictionary<Type, ushort> _typeCodeCache = new ConcurrentDictionary<Type, ushort>();
        private readonly ConcurrentDictionary<ushort, Type> _messageTypeCache = new ConcurrentDictionary<ushort, Type>();
        
        /// <summary>
        /// Initializes a new instance of the CachedMessageRegistry class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public CachedMessageRegistry(IBurstLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseRegistry = new DefaultMessageRegistry(logger);
        }
        
        /// <inheritdoc />
        public void DiscoverMessages()
        {
            _baseRegistry.DiscoverMessages();
            InvalidateCache();
        }
        
        /// <inheritdoc />
        public void RegisterMessageType(Type messageType)
        {
            _baseRegistry.RegisterMessageType(messageType);
            InvalidateCache();
        }
        
        /// <inheritdoc />
        public void RegisterMessageType(Type messageType, ushort typeCode)
        {
            _baseRegistry.RegisterMessageType(messageType, typeCode);
            InvalidateCache();
        }
        
        /// <inheritdoc />
        public IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes()
        {
            return _baseRegistry.GetAllMessageTypes();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetCategories()
        {
            return _baseRegistry.GetCategories();
        }
        
        /// <inheritdoc />
        public IReadOnlyList<Type> GetMessageTypesByCategory(string category)
        {
            return _baseRegistry.GetMessageTypesByCategory(category);
        }
        
        /// <inheritdoc />
        public IMessageInfo GetMessageInfo(Type messageType)
        {
            return _messageInfoCache.GetOrAdd(messageType, _baseRegistry.GetMessageInfo);
        }
        
        /// <inheritdoc />
        public IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage
        {
            return GetMessageInfo(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public bool IsRegistered(Type messageType)
        {
            return _baseRegistry.IsRegistered(messageType);
        }
        
        /// <inheritdoc />
        public bool IsRegistered<TMessage>() where TMessage : IMessage
        {
            return IsRegistered(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public ushort GetTypeCode(Type messageType)
        {
            return _typeCodeCache.GetOrAdd(messageType, _baseRegistry.GetTypeCode);
        }
        
        /// <inheritdoc />
        public ushort GetTypeCode<TMessage>() where TMessage : IMessage
        {
            return GetTypeCode(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public Type GetMessageType(ushort typeCode)
        {
            return _messageTypeCache.GetOrAdd(typeCode, _baseRegistry.GetMessageType);
        }
        
        /// <inheritdoc />
        public IReadOnlyDictionary<ushort, Type> GetAllTypeCodes()
        {
            return _baseRegistry.GetAllTypeCodes();
        }
        
        /// <inheritdoc />
        public void Clear()
        {
            _baseRegistry.Clear();
            InvalidateCache();
        }
        
        private void InvalidateCache()
        {
            _messageInfoCache.Clear();
            _typeCodeCache.Clear();
            _messageTypeCache.Clear();
            
            _logger.Log(LogLevel.Debug, "Message registry cache invalidated", "CachedMessageRegistry");
        }
    }
}