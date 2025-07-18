﻿using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// High-performance serializer implementation using MemoryPack.
    /// Provides zero-allocation serialization with comprehensive error handling and monitoring.
    /// </summary>
    public class MemoryPackSerializer : ISerializer, IDisposable
    {
        private readonly SerializationConfig _config;
        private readonly ILoggingService _logger;
        private readonly ISerializationRegistry _registry;
        private readonly IVersioningService _versioningService;
        private readonly ICompressionService _compressionService;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly ConcurrentDictionary<Type, bool> _registeredTypes;
        private readonly SerializationStatisticsCollector _statistics;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of MemoryPackSerializer.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="registry">Type registration service</param>
        /// <param name="versioningService">Schema versioning service</param>
        /// <param name="compressionService">Compression service</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public MemoryPackSerializer(
            SerializationConfig config,
            ILoggingService logger,
            ISerializationRegistry registry,
            IVersioningService versioningService,
            ICompressionService compressionService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _versioningService = versioningService ?? throw new ArgumentNullException(nameof(versioningService));
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));

            _concurrencyLimiter = new SemaphoreSlim(_config.MaxConcurrentOperations, _config.MaxConcurrentOperations);
            _registeredTypes = new ConcurrentDictionary<Type, bool>();
            _statistics = new SerializationStatisticsCollector();

            var correlationId = GetCorrelationId();
            _logger.LogInfo("MemoryPackSerializer initialized with MemoryPack format", correlationId);
        }

        /// <inheritdoc />
        public byte[] Serialize<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting serialization of type {typeof(T).Name}", correlationId);

                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                var serializedData = MemoryPackSerializer.Serialize(obj);

                var result = _config.Compression != CompressionLevel.None
                    ? _compressionService.Compress(serializedData, _config.Compression)
                    : serializedData;

                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordSerialization(typeof(T), result.Length, duration, true);

                _logger.LogInfo($"Successfully serialized {typeof(T).Name} to {result.Length} bytes in {duration.TotalMilliseconds:F2}ms", correlationId);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordSerialization(typeof(T), 0, duration, false);

                _logger.LogException($"Failed to serialize type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"Serialization failed for type {typeof(T).Name}", typeof(T), "Serialize", ex);
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return Deserialize<T>(data.AsSpan());
        }

        /// <inheritdoc />
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting deserialization of type {typeof(T).Name} from {data.Length} bytes", correlationId);

                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                var decompressedData = _config.Compression != CompressionLevel.None
                    ? _compressionService.Decompress(data)
                    : data.ToArray();

                var result = MemoryPackSerializer.Deserialize<T>(decompressedData);

                if (_config.EnableVersioning)
                {
                    result = _versioningService.MigrateToLatest(result);
                }

                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordDeserialization(typeof(T), data.Length, duration, true);

                _logger.LogInfo($"Successfully deserialized {typeof(T).Name} in {duration.TotalMilliseconds:F2}ms", correlationId);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordDeserialization(typeof(T), data.Length, duration, false);

                _logger.LogException($"Failed to deserialize type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"Deserialization failed for type {typeof(T).Name}", typeof(T), "Deserialize", ex);
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            return TryDeserialize(data.AsSpan(), out result);
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            result = default;

            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch (Exception ex)
            {
                var correlationId = GetCorrelationId();
                _logger.LogError($"TryDeserialize failed for type {typeof(T).Name}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <inheritdoc />
        public void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            if (_registeredTypes.TryAdd(type, true))
            {
                _registry.RegisterType(type);
                _logger.LogInfo($"Registered type {type.FullName} for serialization", correlationId);
            }
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

            return _registeredTypes.ContainsKey(type) || _registry.IsRegistered(type);
        }

        /// <inheritdoc />
        public async Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ThrowIfDisposed();

            await _concurrencyLimiter.WaitAsync(cancellationToken);

            try
            {
                return await Task.Run(() => Serialize(obj), cancellationToken);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        /// <inheritdoc />
        public async Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            await _concurrencyLimiter.WaitAsync(cancellationToken);

            try
            {
                return await Task.Run(() => Deserialize<T>(data), cancellationToken);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ThrowIfDisposed();

            var data = Serialize(obj);
            stream.Write(data, 0, data.Length);
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ThrowIfDisposed();

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var data = memoryStream.ToArray();

            return Deserialize<T>(data);
        }

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            if (obj.Equals(default(T)))
                throw new ArgumentException("Object cannot be default value", nameof(obj));

            ThrowIfDisposed();

            var data = Serialize(obj);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            
            for (int i = 0; i < data.Length; i++)
            {
                nativeArray[i] = data[i];
            }

            return nativeArray;
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            if (!data.IsCreated)
                throw new ArgumentException("NativeArray is not created", nameof(data));

            ThrowIfDisposed();

            var managedArray = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                managedArray[i] = data[i];
            }

            return Deserialize<T>(managedArray);
        }

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            ThrowIfDisposed();
            return _statistics.GetStatistics(_registeredTypes.Count);
        }

        private void EnsureTypeRegistered<T>()
        {
            var type = typeof(T);
            if (!IsRegistered(type))
            {
                RegisterType(type);
            }
        }

        private void ValidateTypeIfEnabled<T>()
        {
            if (!_config.EnableTypeValidation)
                return;

            var type = typeof(T);
            var typeName = type.FullName ?? type.Name;

            // Check whitelist if configured
            if (_config.TypeWhitelist.Count > 0)
            {
                var isWhitelisted = false;
                foreach (var pattern in _config.TypeWhitelist)
                {
                    if (typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        isWhitelisted = true;
                        break;
                    }
                }

                if (!isWhitelisted)
                {
                    throw new SerializationException($"Type {typeName} is not whitelisted for serialization", type, "ValidateType");
                }
            }

            // Check blacklist
            foreach (var pattern in _config.TypeBlacklist)
            {
                if (typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SerializationException($"Type {typeName} is blacklisted from serialization", type, "ValidateType");
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoryPackSerializer));
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        /// <summary>
        /// Disposes the serializer and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _concurrencyLimiter?.Dispose();
                _statistics?.Dispose();
                _disposed = true;

                var correlationId = GetCorrelationId();
                _logger.LogInfo("MemoryPackSerializer disposed", correlationId);
            }
        }
    }

    /// <summary>
    /// Internal statistics collector for serialization operations.
    /// Thread-safe implementation for tracking performance metrics.
    /// </summary>
    internal class SerializationStatisticsCollector : IDisposable
    {
        private long _totalSerializations;
        private long _totalDeserializations;
        private long _failedOperations;
        private long _totalBytesProcessed;
        private double _totalSerializationTimeMs;
        private double _totalDeserializationTimeMs;
        private long _peakMemoryUsage;
        private readonly ConcurrentDictionary<string, long> _typeUsageStats;
        private readonly DateTime _creationTime;
        private readonly object _statsLock = new();
        private bool _disposed;

        public SerializationStatisticsCollector()
        {
            _typeUsageStats = new ConcurrentDictionary<string, long>();
            _creationTime = DateTime.UtcNow;
        }

        public void RecordSerialization(Type type, int dataSize, TimeSpan duration, bool success)
        {
            if (_disposed) return;

            lock (_statsLock)
            {
                Interlocked.Increment(ref _totalSerializations);
                
                if (success)
                {
                    Interlocked.Add(ref _totalBytesProcessed, dataSize);
                    _totalSerializationTimeMs += duration.TotalMilliseconds;
                    
                    var typeName = type.FullName ?? type.Name;
                    _typeUsageStats.AddOrUpdate(typeName, 1, (_, count) => count + 1);
                }
                else
                {
                    Interlocked.Increment(ref _failedOperations);
                }

                // Update peak memory usage
                var currentMemory = GC.GetTotalMemory(false);
                if (currentMemory > _peakMemoryUsage)
                {
                    Interlocked.Exchange(ref _peakMemoryUsage, currentMemory);
                }
            }
        }

        public void RecordDeserialization(Type type, int dataSize, TimeSpan duration, bool success)
        {
            if (_disposed) return;

            lock (_statsLock)
            {
                Interlocked.Increment(ref _totalDeserializations);
                
                if (success)
                {
                    Interlocked.Add(ref _totalBytesProcessed, dataSize);
                    _totalDeserializationTimeMs += duration.TotalMilliseconds;
                    
                    var typeName = type.FullName ?? type.Name;
                    _typeUsageStats.AddOrUpdate(typeName, 1, (_, count) => count + 1);
                }
                else
                {
                    Interlocked.Increment(ref _failedOperations);
                }

                // Update peak memory usage
                var currentMemory = GC.GetTotalMemory(false);
                if (currentMemory > _peakMemoryUsage)
                {
                    Interlocked.Exchange(ref _peakMemoryUsage, currentMemory);
                }
            }
        }

        public SerializationStatistics GetStatistics(int registeredTypeCount)
        {
            if (_disposed)
                return new SerializationStatistics();

            lock (_statsLock)
            {
                var avgSerializationTime = _totalSerializations > 0 
                    ? _totalSerializationTimeMs / _totalSerializations 
                    : 0.0;
                    
                var avgDeserializationTime = _totalDeserializations > 0 
                    ? _totalDeserializationTimeMs / _totalDeserializations 
                    : 0.0;

                return new SerializationStatistics
                {
                    TotalSerializations = _totalSerializations,
                    TotalDeserializations = _totalDeserializations,
                    FailedOperations = _failedOperations,
                    TotalBytesProcessed = _totalBytesProcessed,
                    AverageSerializationTimeMs = avgSerializationTime,
                    AverageDeserializationTimeMs = avgDeserializationTime,
                    PeakMemoryUsage = _peakMemoryUsage,
                    RegisteredTypeCount = registeredTypeCount,
                    TypeUsageStats = _typeUsageStats.ToImmutableDictionary(),
                    LastResetTime = _creationTime
                };
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _typeUsageStats?.Clear();
        }
    }
}