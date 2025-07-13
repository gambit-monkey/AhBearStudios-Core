using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Production-ready implementation of message type registry.
    /// Provides centralized registration, discovery, and management of message types in the system.
    /// Follows AhBearStudios Core Development Guidelines with full core systems integration.
    /// </summary>
    public sealed class MessageRegistry : IMessageRegistry
    {
        #region Private Fields

        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;

        // Message type registry
        private readonly ConcurrentDictionary<Type, MessageTypeInfo> _messageTypes;
        private readonly ConcurrentDictionary<ushort, Type> _typeCodeMap;
        private readonly ConcurrentDictionary<FixedString64Bytes, Type> _nameMap;
        private readonly ConcurrentDictionary<string, HashSet<Type>> _categoryMap;

        // Type hierarchy mapping
        private readonly ConcurrentDictionary<Type, HashSet<Type>> _derivedTypesMap;
        private readonly ConcurrentDictionary<Type, Type[]> _inheritanceChainMap;

        // Performance optimization
        private readonly ConcurrentDictionary<Type, MessageTypeInfo> _typeInfoCache;
        private readonly ReaderWriterLockSlim _registryLock;

        // State management
        private volatile bool _disposed;
        private volatile bool _initialized;

        // Statistics tracking
        private long _totalRegistrations;
        private long _totalLookups;
        private long _cacheHits;
        private long _cacheMisses;

        // Performance monitoring
        private readonly Timer _statisticsTimer;
        private readonly Timer _cacheCleanupTimer;
        private DateTime _lastStatsReset;

        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        // Type code assignment
        private ushort _nextTypeCode = 1000; // Start from 1000 to avoid conflicts with system codes

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageRegistry class.
        /// </summary>
        /// <param name="logger">The logging service</param>
        /// <param name="alertService">The alert service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageRegistry(
            ILoggingService logger,
            IAlertService alertService,
            IProfilerService profilerService,
            IPoolingService poolingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));

            // Generate correlation ID for tracking
            _correlationId = $"MessageRegistry_{Guid.NewGuid():N}"[..32];

            try
            {
                using var initScope = _profilerService.BeginScope("MessageRegistry_Initialize");

                _logger.LogInfo($"[{_correlationId}] Initializing MessageRegistry");

                // Initialize collections
                _messageTypes = new ConcurrentDictionary<Type, MessageTypeInfo>();
                _typeCodeMap = new ConcurrentDictionary<ushort, Type>();
                _nameMap = new ConcurrentDictionary<FixedString64Bytes, Type>();
                _categoryMap = new ConcurrentDictionary<string, HashSet<Type>>();
                _derivedTypesMap = new ConcurrentDictionary<Type, HashSet<Type>>();
                _inheritanceChainMap = new ConcurrentDictionary<Type, Type[]>();
                _typeInfoCache = new ConcurrentDictionary<Type, MessageTypeInfo>();

                // Initialize synchronization
                _registryLock = new ReaderWriterLockSlim();

                // Initialize timers
                _lastStatsReset = DateTime.UtcNow;
                _statisticsTimer = new Timer(UpdateStatistics, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                _cacheCleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

                // Auto-discover message types
                DiscoverMessageTypes();

                _initialized = true;

                _logger.LogInfo($"[{_correlationId}] MessageRegistry initialized with {_messageTypes.Count} message types");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to initialize MessageRegistry");

                if (_alertService != null)
                {
                    _alertService.RaiseAlert(
                        $"MessageRegistry initialization failed: {ex.Message}",
                        AlertSeverity.Critical,
                        "MessageRegistry",
                        "Initialization");
                }

                throw;
            }
        }

        #endregion

        #region IMessageRegistry Implementation

        /// <inheritdoc />
        public bool IsInitialized => _initialized && !_disposed;

        /// <inheritdoc />
        public int RegisteredTypeCount => _messageTypes.Count;

        /// <inheritdoc />
        public void RegisterMessageType<TMessage>() where TMessage : IMessage
        {
            RegisterMessageType(typeof(TMessage));
        }

        /// <inheritdoc />
        public void RegisterMessageType(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            ThrowIfDisposed();

            var registrationCorrelationId = $"{_correlationId}_{messageType.Name}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"MessageRegistry_Register_{messageType.Name}");

                _logger.LogInfo($"[{registrationCorrelationId}] Registering message type {messageType.Name}");

                // Validate message type
                ValidateMessageType(messageType, registrationCorrelationId);

                _registryLock.EnterWriteLock();
                try
                {
                    // Check if already registered
                    if (_messageTypes.ContainsKey(messageType))
                    {
                        _logger.LogWarning($"[{registrationCorrelationId}] Message type {messageType.Name} is already registered");
                        return;
                    }

                    // Assign type code
                    var typeCode = AssignTypeCode(messageType);

                    // Create message type info
                    var typeInfo = CreateMessageTypeInfo(messageType, typeCode);

                    // Register in all maps
                    _messageTypes[messageType] = typeInfo;
                    _typeCodeMap[typeCode] = messageType;
                    _nameMap[messageType.Name] = messageType;

                    // Register in category map
                    RegisterInCategoryMap(messageType, typeInfo);

                    // Update inheritance hierarchy
                    UpdateInheritanceHierarchy(messageType);

                    // Update cache
                    _typeInfoCache[messageType] = typeInfo;

                    Interlocked.Increment(ref _totalRegistrations);

                    _logger.LogInfo($"[{registrationCorrelationId}] Successfully registered message type {messageType.Name} with type code {typeCode}");

                    // Raise event
                    MessageTypeRegistered?.Invoke(this, new MessageTypeRegisteredEventArgs(messageType, typeInfo));
                }
                finally
                {
                    _registryLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{registrationCorrelationId}] Failed to register message type {messageType.Name}");

                if (_alertService != null)
                {
                    _alertService.RaiseAlert(
                        $"Message type registration failed: {ex.Message}",
                        AlertSeverity.High,
                        "MessageRegistry",
                        "Registration");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public bool IsRegistered<TMessage>() where TMessage : IMessage
        {
            return IsRegistered(typeof(TMessage));
        }

        /// <inheritdoc />
        public bool IsRegistered(Type messageType)
        {
            if (messageType == null)
                return false;

            ThrowIfDisposed();

            Interlocked.Increment(ref _totalLookups);

            _registryLock.EnterReadLock();
            try
            {
                return _messageTypes.ContainsKey(messageType);
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public MessageTypeInfo GetMessageTypeInfo<TMessage>() where TMessage : IMessage
        {
            return GetMessageTypeInfo(typeof(TMessage));
        }

        /// <inheritdoc />
        public MessageTypeInfo GetMessageTypeInfo(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            ThrowIfDisposed();

            Interlocked.Increment(ref _totalLookups);

            // Check cache first
            if (_typeInfoCache.TryGetValue(messageType, out var cachedInfo))
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedInfo;
            }

            Interlocked.Increment(ref _cacheMisses);

            _registryLock.EnterReadLock();
            try
            {
                if (_messageTypes.TryGetValue(messageType, out var typeInfo))
                {
                    // Update cache
                    _typeInfoCache[messageType] = typeInfo;
                    return typeInfo;
                }

                throw new InvalidOperationException($"Message type {messageType.Name} is not registered");
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public Type GetMessageType(ushort typeCode)
        {
            ThrowIfDisposed();

            Interlocked.Increment(ref _totalLookups);

            _registryLock.EnterReadLock();
            try
            {
                if (_typeCodeMap.TryGetValue(typeCode, out var messageType))
                {
                    Interlocked.Increment(ref _cacheHits);
                    return messageType;
                }

                Interlocked.Increment(ref _cacheMisses);
                return null;
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public Type GetMessageType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

            ThrowIfDisposed();

            Interlocked.Increment(ref _totalLookups);

            FixedString64Bytes fixedTypeName = typeName;

            _registryLock.EnterReadLock();
            try
            {
                if (_nameMap.TryGetValue(fixedTypeName, out var messageType))
                {
                    Interlocked.Increment(ref _cacheHits);
                    return messageType;
                }

                Interlocked.Increment(ref _cacheMisses);
                return null;
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetMessageTypes()
        {
            ThrowIfDisposed();

            _registryLock.EnterReadLock();
            try
            {
                return _messageTypes.Keys.ToArray();
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetMessageTypesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentException("Category cannot be null or empty", nameof(category));

            ThrowIfDisposed();

            _registryLock.EnterReadLock();
            try
            {
                if (_categoryMap.TryGetValue(category, out var types))
                {
                    return types.ToArray();
                }

                return Enumerable.Empty<Type>();
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetDerivedTypes(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            ThrowIfDisposed();

            _registryLock.EnterReadLock();
            try
            {
                if (_derivedTypesMap.TryGetValue(baseType, out var derivedTypes))
                {
                    return derivedTypes.ToArray();
                }

                return Enumerable.Empty<Type>();
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public void UnregisterMessageType<TMessage>() where TMessage : IMessage
        {
            UnregisterMessageType(typeof(TMessage));
        }

        /// <inheritdoc />
        public void UnregisterMessageType(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            ThrowIfDisposed();

            var unregistrationCorrelationId = $"{_correlationId}_{messageType.Name}"[..32];

            try
            {
                using var profilerScope = _profilerService?.BeginScope($"MessageRegistry_Unregister_{messageType.Name}");

                _logger.LogInfo($"[{unregistrationCorrelationId}] Unregistering message type {messageType.Name}");

                _registryLock.EnterWriteLock();
                try
                {
                    if (!_messageTypes.TryRemove(messageType, out var typeInfo))
                    {
                        _logger.LogWarning($"[{unregistrationCorrelationId}] Message type {messageType.Name} was not registered");
                        return;
                    }

                    // Remove from all maps
                    _typeCodeMap.TryRemove(typeInfo.TypeCode, out _);
                    _nameMap.TryRemove(messageType.Name, out _);
                    _typeInfoCache.TryRemove(messageType, out _);

                    // Remove from category map
                    if (!string.IsNullOrEmpty(typeInfo.Category) && _categoryMap.TryGetValue(typeInfo.Category, out var categoryTypes))
                    {
                        categoryTypes.Remove(messageType);
                        if (categoryTypes.Count == 0)
                        {
                            _categoryMap.TryRemove(typeInfo.Category, out _);
                        }
                    }

                    // Update inheritance hierarchy
                    RemoveFromInheritanceHierarchy(messageType);

                    _logger.LogInfo($"[{unregistrationCorrelationId}] Successfully unregistered message type {messageType.Name}");

                    // Raise event
                    MessageTypeUnregistered?.Invoke(this, new MessageTypeUnregisteredEventArgs(messageType, typeInfo));
                }
                finally
                {
                    _registryLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{unregistrationCorrelationId}] Failed to unregister message type {messageType.Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public void ClearRegistry()
        {
            ThrowIfDisposed();

            try
            {
                using var profilerScope = _profilerService?.BeginScope("MessageRegistry_Clear");

                _logger.LogInfo($"[{_correlationId}] Clearing message registry");

                _registryLock.EnterWriteLock();
                try
                {
                    var typesToRemove = _messageTypes.Keys.ToArray();

                    _messageTypes.Clear();
                    _typeCodeMap.Clear();
                    _nameMap.Clear();
                    _categoryMap.Clear();
                    _derivedTypesMap.Clear();
                    _inheritanceChainMap.Clear();
                    _typeInfoCache.Clear();

                    // Reset type code assignment
                    _nextTypeCode = 1000;

                    _logger.LogInfo($"[{_correlationId}] Cleared {typesToRemove.Length} message types from registry");

                    // Raise event
                    RegistryCleared?.Invoke(this, new RegistryClearedEventArgs(typesToRemove));
                }
                finally
                {
                    _registryLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to clear message registry");
                throw;
            }
        }

        /// <inheritdoc />
        public MessageRegistryStatistics GetStatistics()
        {
            try
            {
                var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
                var lookupsPerSecond = timeSinceReset.TotalSeconds > 0 
                    ? _totalLookups / timeSinceReset.TotalSeconds 
                    : 0;

                var cacheHitRate = _totalLookups > 0 
                    ? (double)_cacheHits / _totalLookups 
                    : 0;

                return new MessageRegistryStatistics(
                    _totalRegistrations,
                    _totalLookups,
                    _cacheHits,
                    _cacheMisses,
                    cacheHitRate,
                    lookupsPerSecond,
                    RegisteredTypeCount,
                    _typeInfoCache.Count,
                    DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to get registry statistics");
                return MessageRegistryStatistics.Empty;
            }
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<MessageTypeRegisteredEventArgs> MessageTypeRegistered;

        /// <inheritdoc />
        public event EventHandler<MessageTypeUnregisteredEventArgs> MessageTypeUnregistered;

        /// <inheritdoc />
        public event EventHandler<RegistryClearedEventArgs> RegistryCleared;

        #endregion

        #region Private Methods

        /// <summary>
        /// Discovers and registers message types from loaded assemblies.
        /// </summary>
        private void DiscoverMessageTypes()
        {
            try
            {
                using var profilerScope = _profilerService?.BeginScope("MessageRegistry_Discover");

                _logger.LogInfo($"[{_correlationId}] Discovering message types in loaded assemblies");

                var discoveredTypes = new List<Type>();

                // Get all loaded assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !a.GlobalAssemblyCache)
                    .ToArray();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var messageTypes = assembly.GetTypes()
                            .Where(t => typeof(IMessage).IsAssignableFrom(t) && 
                                       !t.IsInterface && 
                                       !t.IsAbstract &&
                                       t.IsClass)
                            .ToArray();

                        discoveredTypes.AddRange(messageTypes);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        _logger.LogWarning($"[{_correlationId}] Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                        
                        // Try to register the types that did load
                        var loadedTypes = ex.Types?.Where(t => t != null && typeof(IMessage).IsAssignableFrom(t));
                        if (loadedTypes != null)
                        {
                            discoveredTypes.AddRange(loadedTypes);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex, $"[{_correlationId}] Error processing assembly {assembly.FullName}");
                    }
                }

                // Register discovered types
                foreach (var messageType in discoveredTypes)
                {
                    try
                    {
                        RegisterMessageType(messageType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex, $"[{_correlationId}] Failed to auto-register message type {messageType.Name}");
                    }
                }

                _logger.LogInfo($"[{_correlationId}] Discovered and registered {discoveredTypes.Count} message types");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to discover message types");
            }
        }

        /// <summary>
        /// Validates that a type is a valid message type.
        /// </summary>
        /// <param name="messageType">The type to validate</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <exception cref="ArgumentException">Thrown when type is invalid</exception>
        private void ValidateMessageType(Type messageType, FixedString128Bytes correlationId)
        {
            if (!typeof(IMessage).IsAssignableFrom(messageType))
            {
                var errorMessage = $"Type {messageType.Name} does not implement IMessage";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage, nameof(messageType));
            }

            if (messageType.IsInterface)
            {
                var errorMessage = $"Type {messageType.Name} cannot be an interface";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage, nameof(messageType));
            }

            if (messageType.IsAbstract)
            {
                var errorMessage = $"Type {messageType.Name} cannot be abstract";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage, nameof(messageType));
            }

            if (!messageType.IsClass)
            {
                var errorMessage = $"Type {messageType.Name} must be a class";
                _logger.LogError($"[{correlationId}] {errorMessage}");
                throw new ArgumentException(errorMessage, nameof(messageType));
            }
        }

        /// <summary>
        /// Assigns a unique type code to a message type.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <returns>Assigned type code</returns>
        private ushort AssignTypeCode(Type messageType)
        {
            // Check if type has a TypeCodeAttribute
            var typeCodeAttr = messageType.GetCustomAttribute<MessageTypeCodeAttribute>();
            if (typeCodeAttr != null)
            {
                if (_typeCodeMap.ContainsKey(typeCodeAttr.TypeCode))
                {
                    throw new InvalidOperationException($"Type code {typeCodeAttr.TypeCode} is already assigned to another type");
                }
                return typeCodeAttr.TypeCode;
            }

            // Auto-assign type code
            ushort typeCode;
            do
            {
                typeCode = _nextTypeCode++;
                if (_nextTypeCode == ushort.MaxValue)
                {
                    throw new InvalidOperationException("Exhausted available type codes");
                }
            } while (_typeCodeMap.ContainsKey(typeCode));

            return typeCode;
        }

        /// <summary>
        /// Creates message type information for a given type.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="typeCode">The assigned type code</param>
        /// <returns>Message type information</returns>
        private MessageTypeInfo CreateMessageTypeInfo(Type messageType, ushort typeCode)
        {
            var categoryAttr = messageType.GetCustomAttribute<MessageCategoryAttribute>();
            var category = categoryAttr?.Category ?? "General";

            var descriptionAttr = messageType.GetCustomAttribute<MessageDescriptionAttribute>();
            var description = descriptionAttr?.Description ?? string.Empty;

            var priorityAttr = messageType.GetCustomAttribute<MessagePriorityAttribute>();
            var defaultPriority = priorityAttr?.Priority ?? MessagePriority.Normal;

            var serializableAttr = messageType.GetCustomAttribute<MessageSerializableAttribute>();
            var isSerializable = serializableAttr?.IsSerializable ?? true;

            return new MessageTypeInfo(
                messageType,
                typeCode,
                messageType.Name,
                messageType.FullName,
                category,
                description,
                defaultPriority,
                isSerializable,
                DateTime.UtcNow);
        }

        /// <summary>
        /// Registers a message type in the category mapping.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="typeInfo">The type information</param>
        private void RegisterInCategoryMap(Type messageType, MessageTypeInfo typeInfo)
        {
            if (!string.IsNullOrEmpty(typeInfo.Category))
            {
                _categoryMap.AddOrUpdate(typeInfo.Category,
                    new HashSet<Type> { messageType },
                    (_, existing) =>
                    {
                        existing.Add(messageType);
                        return existing;
                    });
            }
        }

        /// <summary>
        /// Updates the inheritance hierarchy mappings.
        /// </summary>
        /// <param name="messageType">The message type to add</param>
        private void UpdateInheritanceHierarchy(Type messageType)
        {
            // Build inheritance chain
            var inheritanceChain = new List<Type>();
            var currentType = messageType.BaseType;
            while (currentType != null && currentType != typeof(object))
            {
                inheritanceChain.Add(currentType);
                currentType = currentType.BaseType;
            }

            _inheritanceChainMap[messageType] = inheritanceChain.ToArray();

            // Update derived types mapping
            foreach (var baseType in inheritanceChain)
            {
                _derivedTypesMap.AddOrUpdate(baseType,
                    new HashSet<Type> { messageType },
                    (_, existing) =>
                    {
                        existing.Add(messageType);
                        return existing;
                    });
            }
        }

        /// <summary>
        /// Removes a message type from the inheritance hierarchy mappings.
        /// </summary>
        /// <param name="messageType">The message type to remove</param>
        private void RemoveFromInheritanceHierarchy(Type messageType)
        {
            if (_inheritanceChainMap.TryRemove(messageType, out var inheritanceChain))
            {
                foreach (var baseType in inheritanceChain)
                {
                    if (_derivedTypesMap.TryGetValue(baseType, out var derivedTypes))
                    {
                        derivedTypes.Remove(messageType);
                        if (derivedTypes.Count == 0)
                        {
                            _derivedTypesMap.TryRemove(baseType, out _);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates statistics periodically (timer callback).
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void UpdateStatistics(object state)
        {
            if (_disposed) return;

            try
            {
                // Statistics are updated in real-time by other methods
                // This timer could be used for periodic cleanup or aggregation
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to update registry statistics");
            }
        }

        /// <summary>
        /// Cleans up the cache periodically (timer callback).
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void CleanupCache(object state)
        {
            if (_disposed) return;

            try
            {
                // If cache is getting too large, trim it
                if (_typeInfoCache.Count > 1000)
                {
                    _logger.LogInfo($"[{_correlationId}] Cleaning up type info cache (current size: {_typeInfoCache.Count})");

                    // Clear cache - it will be rebuilt on demand
                    _typeInfoCache.Clear();

                    _logger.LogInfo($"[{_correlationId}] Type info cache cleaned up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to cleanup cache");
            }
        }

        /// <summary>
        /// Throws an exception if the registry has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when registry is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageRegistry));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message registry and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"[{_correlationId}] Disposing MessageRegistry");

            try
            {
                _disposed = true;

                // Dispose timers
                _statisticsTimer?.Dispose();
                _cacheCleanupTimer?.Dispose();

                // Clear all collections
                _registryLock?.EnterWriteLock();
                try
                {
                    _messageTypes.Clear();
                    _typeCodeMap.Clear();
                    _nameMap.Clear();
                    _categoryMap.Clear();
                    _derivedTypesMap.Clear();
                    _inheritanceChainMap.Clear();
                    _typeInfoCache.Clear();
                }
                finally
                {
                    _registryLock?.ExitWriteLock();
                }

                // Dispose synchronization objects
                _registryLock?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessageRegistry disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Error during MessageRegistry disposal");
            }
        }

        #endregion
    }
}