using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Factories;
using AhBearStudios.Core.Serialization.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Enhanced high-performance serialization service implementation with circuit breaker protection.
    /// Provides centralized serialization with health monitoring, alerting, and fault tolerance.
    /// Follows AhBearStudios Core Architecture patterns with complete dependency integration.
    /// </summary>
    public sealed class SerializationService : ISerializationService
    {
        private readonly SerializationConfig _config;
        private readonly ConcurrentDictionary<SerializationFormat, ISerializer> _serializers;
        private readonly ConcurrentDictionary<SerializationFormat, ICircuitBreaker> _circuitBreakers;
        private readonly ISerializerFactory _serializerFactory;
        private readonly ISerializationRegistry _registry;
        private readonly IVersioningService _versioningService;
        private readonly ICompressionService _compressionService;
        private readonly ILoggingService _loggingService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IMessageBusService _messageBusService;
        
        private readonly object _lock = new object();
        private readonly SemaphoreSlim _concurrencyLimiter;
        private volatile bool _disposed;
        private volatile bool _isEnabled;
        
        // Performance tracking
        private long _totalSerializations;
        private long _totalDeserializations;
        private long _totalFailures;
        private long _totalBytesProcessed;
        private DateTime _lastHealthCheck;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        
        // Circuit breaker configuration per format
        private readonly Dictionary<SerializationFormat, CircuitBreakerConfig> _circuitBreakerConfigs;

        /// <summary>
        /// Initializes a new instance of the SerializationService with full system integration.
        /// </summary>
        /// <param name="config">The serialization configuration</param>
        /// <param name="serializerFactory">Factory for creating serializer instances</param>
        /// <param name="registry">Type registration service</param>
        /// <param name="versioningService">Schema versioning service</param>
        /// <param name="compressionService">Compression service</param>
        /// <param name="loggingService">Logging service for monitoring</param>
        /// <param name="healthCheckService">Health check service for monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="messageBusService">Message bus service for loose coupling through events</param>
        public SerializationService(
            SerializationConfig config,
            ISerializerFactory serializerFactory,
            ISerializationRegistry registry = null,
            IVersioningService versioningService = null,
            ICompressionService compressionService = null,
            ILoggingService loggingService = null,
            IHealthCheckService healthCheckService = null,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IMessageBusService messageBusService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
            _registry = registry;
            _versioningService = versioningService;
            _compressionService = compressionService;
            _loggingService = loggingService;
            _healthCheckService = healthCheckService;
            _alertService = alertService;
            _profilerService = profilerService;
            _messageBusService = messageBusService;
            
            _serializers = new ConcurrentDictionary<SerializationFormat, ISerializer>();
            _circuitBreakers = new ConcurrentDictionary<SerializationFormat, ICircuitBreaker>();
            _concurrencyLimiter = new SemaphoreSlim(config.MaxConcurrentOperations, config.MaxConcurrentOperations);
            
            _isEnabled = true;
            _lastHealthCheck = DateTime.UtcNow;
            
            // Initialize circuit breaker configurations
            _circuitBreakerConfigs = CreateCircuitBreakerConfigs();
            
            // Initialize default serializers
            InitializeDefaultSerializers();
            
            _loggingService?.LogInfo("SerializationService initialized successfully", 
                new FixedString64Bytes("Serialization.Bootstrap"), "SerializationService");
        }
        
        /// <summary>
        /// Gets the current configuration of the serialization service.
        /// </summary>
        public SerializationConfig Configuration => _config;
        
        /// <summary>
        /// Gets whether the serialization service is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_disposed;

        #region Core Serialization Methods

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Serialize<T>(T obj, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            ThrowIfDisposed();
            
            var selectedFormat = format ?? GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.Serialize");
            
            try
            {
                var result = ExecuteWithFallback(selectedFormat, correlationIdStr, 
                    serializer => serializer.Serialize(obj));
                
                Interlocked.Increment(ref _totalSerializations);
                Interlocked.Add(ref _totalBytesProcessed, result.Length);
                
                _loggingService?.LogDebug($"Serialized {typeof(T).Name} to {result.Length} bytes using {selectedFormat}",
                    correlationId, "SerializationService");
                
                return result;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFailures);
                _loggingService?.LogError($"Serialization failed for {typeof(T).Name}: {ex.Message}", 
                    correlationId, "SerializationService");
                TriggerErrorAlert($"Serialization failed for {typeof(T).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Deserialize<T>(byte[] data, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            ThrowIfDisposed();
            
            var selectedFormat = format ?? DetectFormat(data) ?? GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.Deserialize");
            
            try
            {
                var result = ExecuteWithFallback(selectedFormat, correlationIdStr, 
                    serializer => serializer.Deserialize<T>(data));
                
                Interlocked.Increment(ref _totalDeserializations);
                Interlocked.Add(ref _totalBytesProcessed, data.Length);
                
                _loggingService?.LogDebug($"Deserialized {data.Length} bytes to {typeof(T).Name} using {selectedFormat}",
                    correlationId, "SerializationService");
                
                return result;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFailures);
                _loggingService?.LogError($"Deserialization failed for {typeof(T).Name}: {ex.Message}", 
                    correlationId, "SerializationService");
                TriggerErrorAlert($"Deserialization failed for {typeof(T).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public bool TrySerialize<T>(T obj, out byte[] result, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            result = null;
            
            try
            {
                result = Serialize(obj, correlationId, format);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"TrySerialize failed for {typeof(T).Name}: {ex.Message}", 
                    correlationId, "SerializationService");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            result = default(T);
            
            try
            {
                result = Deserialize<T>(data, correlationId, format);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"TryDeserialize failed for {typeof(T).Name}: {ex.Message}", 
                    correlationId, "SerializationService");
                return false;
            }
        }

        #endregion

        #region Async Serialization Methods

        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null, CancellationToken cancellationToken = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            ThrowIfDisposed();
            
            await _concurrencyLimiter.WaitAsync(cancellationToken);
            
            try
            {
                var selectedFormat = format ?? GetBestFormat<T>();
                var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
                
                using var scope = _profilerService?.BeginScope("SerializationService.SerializeAsync");
                
                var result = await ExecuteWithFallbackAsync(selectedFormat, correlationIdStr, cancellationToken,
                    async serializer => await serializer.SerializeAsync(obj, cancellationToken));
                
                Interlocked.Increment(ref _totalSerializations);
                Interlocked.Add(ref _totalBytesProcessed, result.Length);
                
                return result;
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null, CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            ThrowIfDisposed();
            
            await _concurrencyLimiter.WaitAsync(cancellationToken);
            
            try
            {
                var selectedFormat = format ?? DetectFormat(data) ?? GetBestFormat<T>();
                var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
                
                using var scope = _profilerService?.BeginScope("SerializationService.DeserializeAsync");
                
                var result = await ExecuteWithFallbackAsync(selectedFormat, correlationIdStr, cancellationToken,
                    async serializer => await serializer.DeserializeAsync<T>(data, cancellationToken));
                
                Interlocked.Increment(ref _totalDeserializations);
                Interlocked.Add(ref _totalBytesProcessed, data.Length);
                
                return result;
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        #endregion

        #region Stream-based Serialization Methods

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            ThrowIfDisposed();
            
            var selectedFormat = format ?? GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.SerializeToStream");
            
            ExecuteWithFallback(selectedFormat, correlationIdStr, 
                serializer => { serializer.SerializeToStream(obj, stream); return true; });
            
            Interlocked.Increment(ref _totalSerializations);
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            ThrowIfDisposed();
            
            var selectedFormat = format ?? GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.DeserializeFromStream");
            
            var result = ExecuteWithFallback(selectedFormat, correlationIdStr, 
                serializer => serializer.DeserializeFromStream<T>(stream));
            
            Interlocked.Increment(ref _totalDeserializations);
            
            return result;
        }

        #endregion

        #region Burst-compatible Methods

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator, 
            FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (obj.Equals(default(T)))
                throw new ArgumentException("Object cannot be default value", nameof(obj));
            
            ThrowIfDisposed();
            
            var selectedFormat = GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.SerializeToNativeArray");
            
            return ExecuteWithFallback(selectedFormat, correlationIdStr, 
                serializer => serializer.SerializeToNativeArray(obj, allocator));
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data, 
            FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (!data.IsCreated)
                throw new ArgumentException("NativeArray is not created", nameof(data));
            
            ThrowIfDisposed();
            
            var selectedFormat = GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.DeserializeFromNativeArray");
            
            return ExecuteWithFallback(selectedFormat, correlationIdStr, 
                serializer => serializer.DeserializeFromNativeArray<T>(data));
        }

        #endregion

        #region Batch Operations

        /// <inheritdoc />
        public byte[][] SerializeBatch<T>(IEnumerable<T> objects, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));
            
            ThrowIfDisposed();
            
            var objectList = objects.AsValueEnumerable().ToList();
            var results = new byte[objectList.Count][];
            var selectedFormat = format ?? GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.SerializeBatch");
            
            for (int i = 0; i < objectList.Count; i++)
            {
                results[i] = ExecuteWithFallback(selectedFormat, correlationIdStr, 
                    serializer => serializer.Serialize(objectList[i]));
            }
            
            Interlocked.Add(ref _totalSerializations, objectList.Count);
            
            return results;
        }

        /// <inheritdoc />
        public T[] DeserializeBatch<T>(IEnumerable<byte[]> dataArray, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null)
        {
            if (dataArray == null)
                throw new ArgumentNullException(nameof(dataArray));
            
            ThrowIfDisposed();
            
            var dataList = dataArray.AsValueEnumerable().ToList();
            var results = new T[dataList.Count];
            var selectedFormat = format ?? GetBestFormat<T>();
            var correlationIdStr = correlationId.IsEmpty ? Guid.NewGuid().ToString("N") : correlationId.ToString();
            
            using var scope = _profilerService?.BeginScope("SerializationService.DeserializeBatch");
            
            for (int i = 0; i < dataList.Count; i++)
            {
                results[i] = ExecuteWithFallback(selectedFormat, correlationIdStr, 
                    serializer => serializer.Deserialize<T>(dataList[i]));
            }
            
            Interlocked.Add(ref _totalDeserializations, dataList.Count);
            
            return results;
        }

        #endregion

        #region Serializer Management

        /// <inheritdoc />
        public void RegisterSerializer(SerializationFormat format, ISerializer serializer, 
            FixedString64Bytes correlationId = default)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            
            ThrowIfDisposed();
            
            ICircuitBreaker existingCircuitBreaker = null;
            ISerializer existingSerializer = null;
            
            lock (_lock)
            {
                // Check if we're replacing an existing serializer
                if (_serializers.TryGetValue(format, out existingSerializer))
                {
                    _circuitBreakers.TryGetValue(format, out existingCircuitBreaker);
                }
                
                if (_serializers.TryAdd(format, serializer) || _serializers.TryUpdate(format, serializer, existingSerializer))
                {
                    // Create circuit breaker for this serializer
                    var circuitBreakerConfig = _circuitBreakerConfigs.GetValueOrDefault(format) ?? 
                        CreateDefaultCircuitBreakerConfig(format);
                    
                    var circuitBreaker = new CircuitBreaker(
                        new FixedString64Bytes($"Serializer_{format}"),
                        circuitBreakerConfig,
                        _loggingService);
                    
                    _circuitBreakers.AddOrUpdate(format, circuitBreaker, (key, oldValue) => circuitBreaker);
                    
                    _loggingService?.LogInfo($"Registered serializer for format: {format}", 
                        correlationId, "SerializationService");
                }
                else
                {
                    _loggingService?.LogWarning($"Failed to register serializer for format '{format}'", 
                        correlationId, "SerializationService");
                    return;
                }
            }
            
            // Dispose replaced components outside the lock
            if (existingCircuitBreaker != null)
            {
                try
                {
                    existingCircuitBreaker.Dispose();
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Error disposing replaced circuit breaker for format '{format}': {ex.Message}", 
                        correlationId, "SerializationService");
                }
            }
            
            if (existingSerializer != null && existingSerializer is IDisposable disposableSerializer)
            {
                try
                {
                    disposableSerializer.Dispose();
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Error disposing replaced serializer for format '{format}': {ex.Message}", 
                        correlationId, "SerializationService");
                }
            }
        }

        /// <inheritdoc />
        public bool UnregisterSerializer(SerializationFormat format, FixedString64Bytes correlationId = default)
        {
            ThrowIfDisposed();
            
            ISerializer serializerToDispose = null;
            ICircuitBreaker circuitBreakerToDispose = null;
            bool removed;
            
            // Remove from collections first while holding the lock
            lock (_lock)
            {
                removed = _serializers.TryRemove(format, out serializerToDispose);
                if (removed)
                {
                    _circuitBreakers.TryRemove(format, out circuitBreakerToDispose);
                }
            }
            
            // Dispose outside the lock to avoid potential deadlocks
            if (removed)
            {
                // Dispose circuit breaker first (it may depend on the serializer)
                if (circuitBreakerToDispose != null)
                {
                    try
                    {
                        circuitBreakerToDispose.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError($"Error disposing circuit breaker for format '{format}': {ex.Message}", 
                            correlationId, "SerializationService");
                    }
                }
                
                // Dispose serializer
                if (serializerToDispose != null)
                {
                    try
                    {
                        if (serializerToDispose is IDisposable disposableSerializer)
                        {
                            disposableSerializer.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError($"Error disposing serializer for format '{format}': {ex.Message}", 
                            correlationId, "SerializationService");
                    }
                }
                
                _loggingService?.LogInfo($"Unregistered serializer for format: {format}", 
                    correlationId, "SerializationService");
            }
            
            return removed;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<SerializationFormat> GetRegisteredFormats()
        {
            return _serializers.Keys.AsValueEnumerable().ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public ISerializer GetSerializer(SerializationFormat format)
        {
            if (_serializers.TryGetValue(format, out var serializer) && IsSerializerAvailable(format))
            {
                return serializer;
            }
            return null;
        }

        /// <inheritdoc />
        public bool IsSerializerAvailable(SerializationFormat format)
        {
            return _serializers.ContainsKey(format) && 
                   _circuitBreakers.TryGetValue(format, out var breaker) && 
                   breaker.AllowsRequests();
        }

        #endregion

        #region Circuit Breaker Management

        /// <inheritdoc />
        public ICircuitBreaker GetCircuitBreaker(SerializationFormat format)
        {
            _circuitBreakers.TryGetValue(format, out var breaker);
            return breaker;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetCircuitBreakerStatistics()
        {
            var stats = new Dictionary<SerializationFormat, CircuitBreakerStatistics>();
            
            foreach (var kvp in _circuitBreakers)
            {
                try
                {
                    stats[kvp.Key] = kvp.Value.GetStatistics();
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to get circuit breaker statistics for {kvp.Key}: {ex.Message}", 
                        default, "SerializationService");
                }
            }
            
            return stats;
        }

        /// <inheritdoc />
        public void OpenCircuitBreaker(SerializationFormat format, string reason, 
            FixedString64Bytes correlationId = default)
        {
            if (_circuitBreakers.TryGetValue(format, out var breaker))
            {
                breaker.Open(reason);
                _loggingService?.LogWarning($"Opened circuit breaker for {format}: {reason}", 
                    correlationId, "SerializationService");
                TriggerCircuitBreakerAlert(format, CircuitBreakerState.Open, reason);
            }
        }

        /// <inheritdoc />
        public void CloseCircuitBreaker(SerializationFormat format, string reason, 
            FixedString64Bytes correlationId = default)
        {
            if (_circuitBreakers.TryGetValue(format, out var breaker))
            {
                breaker.Close(reason);
                _loggingService?.LogInfo($"Closed circuit breaker for {format}: {reason}", 
                    correlationId, "SerializationService");
            }
        }

        /// <inheritdoc />
        public void ResetAllCircuitBreakers(FixedString64Bytes correlationId = default)
        {
            foreach (var kvp in _circuitBreakers)
            {
                try
                {
                    kvp.Value.Reset("Manual reset");
                    _loggingService?.LogInfo($"Reset circuit breaker for {kvp.Key}", 
                        correlationId, "SerializationService");
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError($"Failed to reset circuit breaker for {kvp.Key}: {ex.Message}", 
                        correlationId, "SerializationService");
                }
            }
        }

        #endregion

        #region Type Registration

        /// <inheritdoc />
        public void RegisterType<T>(FixedString64Bytes correlationId = default)
        {
            RegisterType(typeof(T), correlationId);
        }

        /// <inheritdoc />
        public void RegisterType(Type type, FixedString64Bytes correlationId = default)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            ThrowIfDisposed();
            
            // Register with global registry
            _registry?.RegisterType(type);
            
            // Register with all available serializers
            foreach (var serializer in _serializers.Values)
            {
                try
                {
                    serializer.RegisterType(type);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to register type {type.Name} with serializer: {ex.Message}", 
                        correlationId, "SerializationService");
                }
            }
            
            _loggingService?.LogDebug($"Registered type: {type.Name}", correlationId, "SerializationService");
        }

        /// <inheritdoc />
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            return _registry?.IsRegistered(type) ?? false;
        }

        #endregion

        #region Format Detection and Negotiation

        /// <inheritdoc />
        public SerializationFormat? DetectFormat(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;
            
            // Try to detect format based on data characteristics
            if (data.Length >= 1)
            {
                // JSON typically starts with '{' or '['
                if (data[0] == '{' || data[0] == '[')
                    return SerializationFormat.Json;
                
                // MemoryPack has specific header patterns
                if (data.Length >= 4)
                {
                    // Add MemoryPack detection logic here
                    // This is a simplified check - actual implementation would be more sophisticated
                    if (data[0] == 0x9A || data[0] == 0x9B) // Example MemoryPack markers
                        return SerializationFormat.MemoryPack;
                }
                
                // Binary serialization fallback
                return SerializationFormat.Binary;
            }
            
            return null;
        }

        /// <inheritdoc />
        public SerializationFormat GetBestFormat<T>(SerializationFormat? preferredFormat = null)
        {
            // If preferred format is specified and available, use it
            if (preferredFormat.HasValue && IsSerializerAvailable(preferredFormat.Value))
            {
                return preferredFormat.Value;
            }
            
            // Use configuration default if available
            if (IsSerializerAvailable(_config.Format))
            {
                return _config.Format;
            }
            
            // Fallback chain
            var fallbackChain = GetFallbackChain(_config.Format);
            foreach (var format in fallbackChain)
            {
                if (IsSerializerAvailable(format))
                {
                    return format;
                }
            }
            
            throw new SerializationException("No available serializers for the requested operation");
        }

        /// <inheritdoc />
        public IReadOnlyList<SerializationFormat> GetFallbackChain(SerializationFormat primaryFormat)
        {
            var chain = new List<SerializationFormat> { primaryFormat };
            
            // Define fallback hierarchy
            switch (primaryFormat)
            {
                case SerializationFormat.MemoryPack:
                    chain.Add(SerializationFormat.Binary);
                    chain.Add(SerializationFormat.Json);
                    break;
                case SerializationFormat.Binary:
                    chain.Add(SerializationFormat.Json);
                    chain.Add(SerializationFormat.MemoryPack);
                    break;
                case SerializationFormat.Json:
                    chain.Add(SerializationFormat.Binary);
                    chain.Add(SerializationFormat.MemoryPack);
                    break;
                default:
                    chain.Add(SerializationFormat.Json);
                    chain.Add(SerializationFormat.Binary);
                    break;
            }
            
            return chain.AsReadOnly();
        }

        #endregion

        #region Performance and Monitoring

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            var circuitBreakerStats = GetCircuitBreakerStatistics();
            
            return new SerializationStatistics
            {
                TotalSerializations = _totalSerializations,
                TotalDeserializations = _totalDeserializations,
                FailedOperations = _totalFailures,
                TotalBytesProcessed = _totalBytesProcessed,
                RegisteredTypeCount = _registry?.GetRegisteredTypes().Count ?? 0,
                LastResetTime = DateTime.UtcNow
            };
        }

        /// <inheritdoc />
        public async UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService?.BeginScope("SerializationService.FlushAsync");
            
            var flushTasks = new List<UniTask>();
            
            foreach (var serializer in _serializers.Values)
            {
                flushTasks.Add(UniTask.RunOnThreadPool(() =>
                {
                    try
                    {
                        // Note: ISerializer doesn't have FlushAsync, so we would need to add it
                        // For now, assume synchronous flush exists
                        var stats = serializer.GetStatistics();
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogWarning($"Failed to flush serializer: {ex.Message}", 
                            correlationId, "SerializationService");
                    }
                }));
            }
            
            await UniTask.WhenAll(flushTasks);
        }

        /// <inheritdoc />
        public Common.Models.ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            var errors = new List<Common.Models.ValidationError>();
            var warnings = new List<Common.Models.ValidationWarning>();
            
            // Validate configuration
            if (!_config.IsValid())
            {
                errors.Add(new ValidationError("Invalid serialization configuration", "Configuration"));
            }
            
            // Validate serializers
            if (_serializers.Count == 0)
            {
                warnings.Add(new ValidationWarning("No serializers registered", "Serializers"));
            }
            
            foreach (var kvp in _serializers)
            {
                if (!IsSerializerAvailable(kvp.Key))
                {
                    warnings.Add(new ValidationWarning($"Serializer '{kvp.Key}' is not available", $"Serializer.{kvp.Key}"));
                }
            }
            
            return errors.Count == 0
                ? Common.Models.ValidationResult.Success("SerializationService", warnings)
                : Common.Models.ValidationResult.Failure(errors, "SerializationService", warnings);
        }

        /// <inheritdoc />
        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            try
            {
                // Perform health checks on all components
                foreach (var kvp in _serializers)
                {
                    try
                    {
                        var stats = kvp.Value.GetStatistics();
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogWarning($"Maintenance check failed for {kvp.Key}: {ex.Message}", 
                            correlationId, "SerializationService");
                    }
                }
                
                // Check circuit breaker health
                foreach (var kvp in _circuitBreakers)
                {
                    try
                    {
                        var stats = kvp.Value.GetStatistics();
                        if (stats.FailureCount > 0)
                        {
                            _loggingService?.LogInfo($"Circuit breaker {kvp.Key} has {stats.FailureCount} failures", 
                                correlationId, "SerializationService");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogWarning($"Circuit breaker maintenance failed for {kvp.Key}: {ex.Message}", 
                            correlationId, "SerializationService");
                    }
                }
                
                _loggingService?.LogInfo("Serialization service maintenance completed", 
                    correlationId, "SerializationService");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Maintenance operation failed: {ex.Message}", 
                    correlationId, "SerializationService");
            }
        }

        /// <inheritdoc />
        public bool PerformHealthCheck()
        {
            if (_disposed) return false;
            
            _lastHealthCheck = DateTime.UtcNow;
            
            foreach (var kvp in _serializers)
            {
                if (!IsSerializerAvailable(kvp.Key))
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, bool> GetHealthStatus()
        {
            var status = new Dictionary<string, bool>();
            
            foreach (var kvp in _serializers)
            {
                status[$"Serializer_{kvp.Key}"] = IsSerializerAvailable(kvp.Key);
            }
            
            foreach (var kvp in _circuitBreakers)
            {
                status[$"CircuitBreaker_{kvp.Key}"] = kvp.Value.AllowsRequests();
            }
            
            return status;
        }

        #endregion

        #region Configuration Management

        /// <inheritdoc />
        public void UpdateConfiguration(SerializationConfig newConfig, FixedString64Bytes correlationId = default)
        {
            if (newConfig == null)
                throw new ArgumentNullException(nameof(newConfig));
            
            ThrowIfDisposed();
            
            // Note: Since _config is readonly, we would need to make it mutable or recreate the service
            // For now, log the intention
            _loggingService?.LogInfo("Configuration update requested - requires service restart", 
                correlationId, "SerializationService");
        }

        /// <inheritdoc />
        public void SetEnabled(bool enabled, FixedString64Bytes correlationId = default)
        {
            _isEnabled = enabled;
            
            _loggingService?.LogInfo($"SerializationService enabled state set to {enabled}", 
                correlationId, "SerializationService");
        }

        #endregion

        #region Private Helper Methods

        private Dictionary<SerializationFormat, CircuitBreakerConfig> CreateCircuitBreakerConfigs()
        {
            return new Dictionary<SerializationFormat, CircuitBreakerConfig>
            {
                [SerializationFormat.MemoryPack] = new CircuitBreakerConfig
                {
                    Name = "MemoryPack Serializer Circuit Breaker",
                    FailureThreshold = 5,
                    Timeout = TimeSpan.FromSeconds(60),
                    SamplingDuration = TimeSpan.FromMinutes(2),
                    MinimumThroughput = 10,
                    SuccessThreshold = 80.0,
                    HalfOpenMaxCalls = 3,
                    UseSlidingWindow = true,
                    SlidingWindowType = SlidingWindowType.CountBased,
                    SlidingWindowSize = 50,
                    EnableAutomaticRecovery = true,
                    MaxRecoveryAttempts = 3,
                    TimeoutMultiplier = 1.5,
                    MaxTimeout = TimeSpan.FromMinutes(5),
                    EnableMetrics = true,
                    EnableEvents = true
                },
                [SerializationFormat.Json] = new CircuitBreakerConfig
                {
                    Name = "JSON Serializer Circuit Breaker",
                    FailureThreshold = 10,
                    Timeout = TimeSpan.FromSeconds(30),
                    SamplingDuration = TimeSpan.FromMinutes(1),
                    MinimumThroughput = 15,
                    SuccessThreshold = 70.0,
                    HalfOpenMaxCalls = 5,
                    UseSlidingWindow = true,
                    SlidingWindowType = SlidingWindowType.CountBased,
                    SlidingWindowSize = 100,
                    EnableAutomaticRecovery = true,
                    MaxRecoveryAttempts = 5,
                    TimeoutMultiplier = 1.2,
                    MaxTimeout = TimeSpan.FromMinutes(3),
                    EnableMetrics = true,
                    EnableEvents = true
                },
                [SerializationFormat.Binary] = new CircuitBreakerConfig
                {
                    Name = "Binary Serializer Circuit Breaker",
                    FailureThreshold = 15,
                    Timeout = TimeSpan.FromSeconds(20),
                    SamplingDuration = TimeSpan.FromSeconds(90),
                    MinimumThroughput = 20,
                    SuccessThreshold = 60.0,
                    HalfOpenMaxCalls = 10,
                    UseSlidingWindow = true,
                    SlidingWindowType = SlidingWindowType.CountBased,
                    SlidingWindowSize = 150,
                    EnableAutomaticRecovery = true,
                    MaxRecoveryAttempts = 10,
                    TimeoutMultiplier = 1.1,
                    MaxTimeout = TimeSpan.FromMinutes(2),
                    EnableMetrics = true,
                    EnableEvents = true
                }
            };
        }

        private CircuitBreakerConfig CreateDefaultCircuitBreakerConfig(SerializationFormat format)
        {
            return new CircuitBreakerConfig
            {
                Name = $"{format} Serializer Circuit Breaker",
                FailureThreshold = 10,
                Timeout = TimeSpan.FromSeconds(30),
                SamplingDuration = TimeSpan.FromMinutes(1),
                MinimumThroughput = 10,
                SuccessThreshold = 60.0,
                HalfOpenMaxCalls = 5,
                UseSlidingWindow = true,
                SlidingWindowType = SlidingWindowType.CountBased,
                SlidingWindowSize = 75,
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 5,
                TimeoutMultiplier = 1.5,
                MaxTimeout = TimeSpan.FromMinutes(3),
                EnableMetrics = true,
                EnableEvents = true
            };
        }

        private void InitializeDefaultSerializers()
        {
            try
            {
                // Create default serializer for the configured format
                var defaultSerializer = _serializerFactory.CreateSerializer(_config);
                RegisterSerializer(_config.Format, defaultSerializer);
                
                // Try to create additional serializers for fallback
                var supportedFormats = _serializerFactory.GetSupportedFormats();
                foreach (var format in supportedFormats)
                {
                    if (format != _config.Format && !_serializers.ContainsKey(format))
                    {
                        try
                        {
                            var fallbackSerializer = _serializerFactory.CreateSerializer(format);
                            RegisterSerializer(format, fallbackSerializer);
                        }
                        catch (Exception ex)
                        {
                            _loggingService?.LogWarning($"Failed to create fallback serializer for {format}: {ex.Message}", 
                                default, "SerializationService");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Failed to initialize default serializers: {ex.Message}", 
                    default, "SerializationService");
                throw;
            }
        }

        private T ExecuteWithFallback<T>(SerializationFormat primaryFormat, string correlationId, 
            Func<ISerializer, T> operation)
        {
            var fallbackChain = GetFallbackChain(primaryFormat);
            Exception lastException = null;
            
            foreach (var format in fallbackChain)
            {
                if (!_serializers.TryGetValue(format, out var serializer))
                    continue;
                
                if (!_circuitBreakers.TryGetValue(format, out var circuitBreaker))
                    continue;
                
                try
                {
                    return circuitBreaker.ExecuteAsync(async _ =>
                    {
                        return await UniTask.FromResult(operation(serializer));
                    }).GetAwaiter().GetResult();
                }
                catch (CircuitBreakerOpenException)
                {
                    _loggingService?.LogWarning($"Circuit breaker open for {format}, trying next fallback", 
                        new FixedString64Bytes(correlationId), "SerializationService");
                    continue;
                }
                catch (System.Exception ex)
                {
                    lastException = ex;
                    circuitBreaker.RecordFailure(ex);
                    _loggingService?.LogWarning($"Operation failed with {format}: {ex.Message}", 
                        new FixedString64Bytes(correlationId), "SerializationService");
                }
            }
            
            // All fallbacks failed
            var finalException = new SerializationException(
                $"All serializers failed. Last error: {lastException?.Message}", 
                typeof(T), 
                "ExecuteWithFallback", 
                lastException);
            
            TriggerCriticalAlert("All serialization fallbacks failed", finalException);
            throw finalException;
        }

        private async UniTask<T> ExecuteWithFallbackAsync<T>(SerializationFormat primaryFormat, string correlationId, 
            CancellationToken cancellationToken, Func<ISerializer, UniTask<T>> operation)
        {
            var fallbackChain = GetFallbackChain(primaryFormat);
            Exception lastException = null;
            
            foreach (var format in fallbackChain)
            {
                if (!_serializers.TryGetValue(format, out var serializer))
                    continue;
                
                if (!_circuitBreakers.TryGetValue(format, out var circuitBreaker))
                    continue;
                
                try
                {
                    return await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        return await operation(serializer);
                    }, cancellationToken);
                }
                catch (CircuitBreakerOpenException)
                {
                    _loggingService?.LogWarning($"Circuit breaker open for {format}, trying next fallback", 
                        new FixedString64Bytes(correlationId), "SerializationService");
                    continue;
                }
                catch (System.Exception ex)
                {
                    lastException = ex;
                    circuitBreaker.RecordFailure(ex);
                    _loggingService?.LogWarning($"Async operation failed with {format}: {ex.Message}", 
                        new FixedString64Bytes(correlationId), "SerializationService");
                }
            }
            
            // All fallbacks failed
            var finalException = new SerializationException(
                $"All serializers failed. Last error: {lastException?.Message}", 
                typeof(T), 
                "ExecuteWithFallbackAsync", 
                lastException);
            
            TriggerCriticalAlert("All async serialization fallbacks failed", finalException);
            throw finalException;
        }

        private void TriggerErrorAlert(string message, Exception exception)
        {
            if (_alertService == null) return;
            
            try
            {
                var alertMessage = exception != null ? $"{message} - {exception.Message}" : message;
                // Truncate message to fit in FixedString512Bytes (511 chars max)
                if (alertMessage.Length > 511)
                    alertMessage = alertMessage.Substring(0, 511);
                    
                _alertService.RaiseAlert(
                    new FixedString512Bytes(alertMessage),
                    AlertSeverity.High,
                    new FixedString64Bytes("SerializationService"),
                    new FixedString32Bytes("Error"));
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"Failed to raise error alert: {ex.Message}", 
                    default, "SerializationService");
            }
        }

        private void TriggerCriticalAlert(string message, Exception exception)
        {
            if (_alertService == null) return;
            
            try
            {
                var alertMessage = exception != null ? $"CRITICAL: {message} - {exception.Message}" : $"CRITICAL: {message}";
                // Truncate message to fit in FixedString512Bytes (511 chars max)
                if (alertMessage.Length > 511)
                    alertMessage = alertMessage.Substring(0, 511);
                    
                _alertService.RaiseAlert(
                    new FixedString512Bytes(alertMessage),
                    AlertSeverity.Critical,
                    new FixedString64Bytes("SerializationService"),
                    new FixedString32Bytes("Critical"));
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"Failed to raise critical alert: {ex.Message}", 
                    default, "SerializationService");
            }
        }

        private void TriggerCircuitBreakerAlert(SerializationFormat format, CircuitBreakerState state, string reason)
        {
            if (_alertService == null) return;
            
            try
            {
                var severity = state == CircuitBreakerState.Open ? AlertSeverity.High : AlertSeverity.Medium;
                var message = $"Circuit breaker {state} for {format}: {reason}";
                // Truncate message to fit in FixedString512Bytes (511 chars max)
                if (message.Length > 511)
                    message = message.Substring(0, 511);
                
                _alertService.RaiseAlert(
                    new FixedString512Bytes(message),
                    severity,
                    new FixedString64Bytes("SerializationService"),
                    new FixedString32Bytes("CircuitBreaker"));
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"Failed to raise circuit breaker alert: {ex.Message}", 
                    default, "SerializationService");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SerializationService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Finalizer to ensure disposal happens even if Dispose() is not called explicitly.
        /// </summary>
        ~SerializationService()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the serialization service and all registered serializers and circuit breakers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method that handles both finalizer and explicit disposal.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            // Mark as disposed first to prevent new operations
            _disposed = true;
            
            if (disposing)
            {
                try
                {
                    // Disable the service to prevent new operations
                    _isEnabled = false;
                    
                    // Wait for any pending operations to complete (with timeout)
                    var waitStart = DateTime.UtcNow;
                    const int maxWaitMs = 5000; // 5 second timeout
                    
                    while (_concurrencyLimiter?.CurrentCount < _config.MaxConcurrentOperations && 
                           (DateTime.UtcNow - waitStart).TotalMilliseconds < maxWaitMs)
                    {
                        Thread.Sleep(10);
                    }
                    
                    // Collect all disposable objects while holding the lock
                    var serializersToDispose = new List<ISerializer>();
                    var circuitBreakersToDispose = new List<ICircuitBreaker>();
                    
                    lock (_lock)
                    {
                        serializersToDispose.AddRange(_serializers.Values);
                        circuitBreakersToDispose.AddRange(_circuitBreakers.Values);
                        
                        // Clear collections while holding the lock
                        _serializers.Clear();
                        _circuitBreakers.Clear();
                    }
                    
                    // Dispose circuit breakers first (they may depend on serializers)
                    foreach (var circuitBreaker in circuitBreakersToDispose)
                    {
                        try
                        {
                            circuitBreaker?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _loggingService?.LogWarning($"Error disposing circuit breaker: {ex.Message}", 
                                default, "SerializationService");
                        }
                    }
                    
                    // Dispose serializers
                    foreach (var serializer in serializersToDispose)
                    {
                        try
                        {
                            if (serializer is IDisposable disposableSerializer)
                            {
                                disposableSerializer.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService?.LogWarning($"Error disposing serializer: {ex.Message}", 
                                default, "SerializationService");
                        }
                    }
                    
                    // Dispose concurrency limiter
                    try
                    {
                        _concurrencyLimiter?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogWarning($"Error disposing concurrency limiter: {ex.Message}", 
                            default, "SerializationService");
                    }
                    
                    _loggingService?.LogInfo("SerializationService disposed successfully", 
                        default, "SerializationService");
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError($"Error during SerializationService disposal: {ex.Message}", 
                        default, "SerializationService");
                }
            }
            else
            {
                // Called from finalizer - only dispose unmanaged resources
                // In this case, we should be more conservative and not call managed code
                try
                {
                    _disposed = true;
                    _isEnabled = false;
                    
                    // Clear collections without logging (finalizer context)
                    _serializers?.Clear();
                    _circuitBreakers?.Clear();
                }
                catch
                {
                    // Suppress exceptions in finalizer
                }
            }
        }

        #endregion
    }
}