using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Attributes;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;

namespace AhBearStudios.Core.MessageBus.Registration
{
    /// <summary>
    /// Default implementation of IMessageRegistry that provides discovery and cataloging of messages.
    /// </summary>
    public sealed class DefaultMessageRegistry : IMessageRegistry
    {
        private readonly ILoggingService _logger;
        private readonly Dictionary<Type, IMessageInfo> _messageTypes = new Dictionary<Type, IMessageInfo>();
        private readonly Dictionary<string, List<Type>> _messagesByCategory = new Dictionary<string, List<Type>>();
        private readonly Dictionary<Type, ushort> _typeToCode = new Dictionary<Type, ushort>();
        private readonly Dictionary<ushort, Type> _codeToType = new Dictionary<ushort, Type>();
        
        private ushort _nextTypeCode = 1; // 0 is reserved for unknown
        private readonly object _registryLock = new object();
        
        /// <summary>
        /// Initializes a new instance of the DefaultMessageRegistry class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public DefaultMessageRegistry(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc />
        public void DiscoverMessages()
        {
            lock (_registryLock)
            {
                _logger.Log(LogLevel.Info, "Discovering message types...", "MessageRegistry");
                
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var messageInterfaceType = typeof(IMessage);
                int discoveredCount = 0;
                
                foreach (var assembly in assemblies)
                {
                    // Skip system assemblies
                    if (IsSystemAssembly(assembly))
                    {
                        continue;
                    }
                    
                    try
                    {
                        // Find all types that implement IMessage
                        var messageTypes = assembly.GetTypes()
                            .Where(t => messageInterfaceType.IsAssignableFrom(t) && 
                                       !t.IsInterface && 
                                       !t.IsAbstract &&
                                       !IsBaseMessageType(t))
                            .ToList();
                        
                        foreach (var messageType in messageTypes)
                        {
                            RegisterMessageTypeInternal(messageType);
                            discoveredCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Warning, 
                            $"Error discovering messages in assembly {assembly.FullName}: {ex.Message}", 
                            "MessageRegistry");
                    }
                }
                
                _logger.Log(LogLevel.Info, 
                    $"Discovered {discoveredCount} message types in {_messagesByCategory.Count} categories", 
                    "MessageRegistry");
            }
        }
        
        /// <inheritdoc />
        public void RegisterMessageType(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            
            lock (_registryLock)
            {
                RegisterMessageTypeInternal(messageType);
            }
        }
        
        /// <inheritdoc />
        public void RegisterMessageType(Type messageType, ushort typeCode)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            
            lock (_registryLock)
            {
                // Validate type code is not already assigned
                if (_codeToType.ContainsKey(typeCode))
                {
                    var existingType = _codeToType[typeCode];
                    if (existingType != messageType)
                    {
                        throw new InvalidOperationException(
                            $"Type code {typeCode} is already assigned to {existingType.FullName}. Cannot assign to {messageType.FullName}.");
                    }
                    
                    // Already registered with this type code
                    return;
                }
                
                RegisterMessageTypeInternal(messageType, typeCode);
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes()
        {
            lock (_registryLock)
            {
                return new Dictionary<Type, IMessageInfo>(_messageTypes);
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetCategories()
        {
            lock (_registryLock)
            {
                return _messagesByCategory.Keys.ToList();
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyList<Type> GetMessageTypesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) throw new ArgumentException("Category cannot be null or empty.", nameof(category));
            
            lock (_registryLock)
            {
                if (!_messagesByCategory.TryGetValue(category, out var categoryMessages))
                {
                    return new List<Type>();
                }
                
                return new List<Type>(categoryMessages);
            }
        }
        
        /// <inheritdoc />
        public IMessageInfo GetMessageInfo(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            
            lock (_registryLock)
            {
                _messageTypes.TryGetValue(messageType, out var messageInfo);
                return messageInfo;
            }
        }
        
        /// <inheritdoc />
        public IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage
        {
            return GetMessageInfo(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public bool IsRegistered(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            
            lock (_registryLock)
            {
                return _messageTypes.ContainsKey(messageType);
            }
        }
        
        /// <inheritdoc />
        public bool IsRegistered<TMessage>() where TMessage : IMessage
        {
            return IsRegistered(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public ushort GetTypeCode(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            
            lock (_registryLock)
            {
                _typeToCode.TryGetValue(messageType, out var typeCode);
                return typeCode; // Returns 0 if not found
            }
        }
        
        /// <inheritdoc />
        public ushort GetTypeCode<TMessage>() where TMessage : IMessage
        {
            return GetTypeCode(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public Type GetMessageType(ushort typeCode)
        {
            lock (_registryLock)
            {
                _codeToType.TryGetValue(typeCode, out var messageType);
                return messageType; // Returns null if not found
            }
        }
        
        /// <inheritdoc />
        public IReadOnlyDictionary<ushort, Type> GetAllTypeCodes()
        {
            lock (_registryLock)
            {
                return new Dictionary<ushort, Type>(_codeToType);
            }
        }
        
        /// <inheritdoc />
        public void Clear()
        {
            lock (_registryLock)
            {
                _messageTypes.Clear();
                _messagesByCategory.Clear();
                _typeToCode.Clear();
                _codeToType.Clear();
                _nextTypeCode = 1;
                
                _logger.Log(LogLevel.Info, "Message registry cleared", "MessageRegistry");
            }
        }
        
        private void RegisterMessageTypeInternal(Type messageType, ushort? explicitTypeCode = null)
        {
            if (_messageTypes.ContainsKey(messageType))
            {
                _logger.Log(LogLevel.Debug, 
                    $"Message type {messageType.FullName} is already registered", 
                    "MessageRegistry");
                return;
            }
            
            // Get the MessageAttribute if present, or create a default one
            var attribute = messageType.GetCustomAttribute<MessageAttribute>() ?? 
                           new MessageAttribute("Uncategorized", $"Auto-discovered message: {messageType.Name}");
            
            // Determine type code
            ushort typeCode;
            if (explicitTypeCode.HasValue)
            {
                typeCode = explicitTypeCode.Value;
            }
            else
            {
                // Check if there's an explicit type code attribute
                var typeCodeAttr = messageType.GetCustomAttribute<MessageTypeCodeAttribute>();
                if (typeCodeAttr != null)
                {
                    typeCode = typeCodeAttr.TypeCode;
                    
                    // Validate type code is not already assigned
                    if (_codeToType.ContainsKey(typeCode))
                    {
                        var existingType = _codeToType[typeCode];
                        _logger.Log(LogLevel.Error, 
                            $"Type code conflict: {typeCode} is assigned to both {existingType.FullName} and {messageType.FullName}. Using automatic assignment instead.",
                            "MessageRegistry");
                        
                        typeCode = _nextTypeCode++;
                    }
                }
                else
                {
                    // Auto-assign type code
                    typeCode = _nextTypeCode++;
                }
            }
            
            var messageInfo = new MessageInfo(messageType, attribute, typeCode);
            _messageTypes[messageType] = messageInfo;
            _typeToCode[messageType] = typeCode;
            _codeToType[typeCode] = messageType;
            
            // Add to category index
            if (!_messagesByCategory.TryGetValue(attribute.Category, out var categoryMessages))
            {
                categoryMessages = new List<Type>();
                _messagesByCategory[attribute.Category] = categoryMessages;
            }
            
            categoryMessages.Add(messageType);
            
            _logger.Log(LogLevel.Debug, 
                $"Registered message type {messageType.FullName} in category {attribute.Category} with type code {typeCode}",
                "MessageRegistry");
        }
        
        private static bool IsSystemAssembly(Assembly assembly)
        {
            return assembly.FullName?.StartsWith("System.") == true ||
                   assembly.FullName?.StartsWith("Microsoft.") == true ||
                   assembly.FullName?.StartsWith("Unity.") == true ||
                   assembly.FullName?.StartsWith("UnityEngine.") == true ||
                   assembly.FullName?.StartsWith("UnityEditor.") == true ||
                   assembly.FullName?.StartsWith("mscorlib") == true ||
                   assembly.FullName?.StartsWith("netstandard") == true;
        }
        
        private static bool IsBaseMessageType(Type type)
        {
            return type == typeof(MessageBase) ||
                   type == typeof(ReliableMessageBase) ||
                   type == typeof(BlittableMessageBase);
        }
    }
}