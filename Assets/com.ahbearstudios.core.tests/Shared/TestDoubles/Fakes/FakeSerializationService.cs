using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.HealthChecking;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes
{
    /// <summary>
    /// Fake implementation of ISerializationService for TDD testing.
    /// Uses simple in-memory storage without actual serialization libraries.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class FakeSerializationService : ISerializationService
    {
        private readonly Dictionary<string, object> _dataStore = new();
        private readonly object _lockObject = new();
        private bool _isDisposed;

        #region Test Configuration Properties

        /// <summary>
        /// Gets or sets whether the service is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets the service configuration.
        /// </summary>
        public SerializationConfig Configuration { get; private set; }

        /// <summary>
        /// Gets the count of serialization operations performed.
        /// </summary>
        public int SerializationCount { get; private set; }

        /// <summary>
        /// Gets the count of deserialization operations performed.
        /// </summary>
        public int DeserializationCount { get; private set; }

        /// <summary>
        /// Gets the count of items currently stored in the fake data store.
        /// </summary>
        public int StoredItemCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _dataStore.Count;
                }
            }
        }

        /// <summary>
        /// Clears all stored data and resets counters.
        /// </summary>
        public void ClearData()
        {
            lock (_lockObject)
            {
                _dataStore.Clear();
                SerializationCount = 0;
                DeserializationCount = 0;
            }
        }

        /// <summary>
        /// Checks if data is stored for the given key.
        /// </summary>
        public bool HasStoredData(string key)
        {
            lock (_lockObject)
            {
                return _dataStore.ContainsKey(key);
            }
        }

        /// <summary>
        /// Gets all stored keys for verification.
        /// </summary>
        public IReadOnlyList<string> StoredKeys
        {
            get
            {
                lock (_lockObject)
                {
                    return _dataStore.Keys.ToList();
                }
            }
        }

        #endregion

        public FakeSerializationService(SerializationConfig config = null)
        {
            Configuration = config ?? new SerializationConfig();
        }

        #region ISerializationService Implementation - Fake Behavior

        // Core serialization methods - use simple in-memory storage
        public byte[] Serialize<T>(T obj, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializationService));

            if (!IsEnabled)
                throw new InvalidOperationException("Serialization service is disabled");

            SerializationCount++;

            if (obj == null)
                return Array.Empty<byte>();

            // Generate a unique key for this object
            var key = GenerateKey(obj, correlationId);

            lock (_lockObject)
            {
                // Store the object directly (no actual serialization)
                _dataStore[key] = obj;
            }

            // Return the key as bytes (this represents the "serialized" data)
            return Encoding.UTF8.GetBytes(key);
        }

        public async UniTask<byte[]> SerializeAsync<T>(T obj, FixedString64Bytes correlationId = default, SerializationFormat? format = null, CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return Serialize(obj, correlationId, format);
        }

        public T Deserialize<T>(byte[] data, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializationService));

            if (!IsEnabled)
                throw new InvalidOperationException("Serialization service is disabled");

            DeserializationCount++;

            if (data == null || data.Length == 0)
                return default(T);

            // Extract the key from the data
            var key = Encoding.UTF8.GetString(data);

            lock (_lockObject)
            {
                if (_dataStore.TryGetValue(key, out var storedObj))
                {
                    if (storedObj is T typedObj)
                    {
                        return typedObj;
                    }

                    // Try to convert if types don't match exactly
                    if (storedObj != null)
                    {
                        try
                        {
                            return (T)storedObj;
                        }
                        catch (InvalidCastException)
                        {
                            throw new InvalidOperationException($"Cannot deserialize stored object of type {storedObj.GetType()} as {typeof(T)}");
                        }
                    }
                }
            }

            // If not found in store, return default
            return default(T);
        }

        public async UniTask<T> DeserializeAsync<T>(byte[] data, FixedString64Bytes correlationId = default, SerializationFormat? format = null, CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return Deserialize<T>(data, correlationId, format);
        }

        // Stream-based operations - Unity Test Runner compatible
        public async UniTask SerializeToStreamAsync<T>(T obj, Stream stream, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            var data = Serialize(obj, correlationId, format);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public async UniTask<T> DeserializeFromStreamAsync<T>(Stream stream, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();

            return Deserialize<T>(data, correlationId, format);
        }

        public void SerializeToStream<T>(T obj, Stream stream, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var data = Serialize(obj, correlationId, format);
            stream.Write(data, 0, data.Length);
        }

        public T DeserializeFromStream<T>(Stream stream, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var data = memoryStream.ToArray();

            return Deserialize<T>(data, correlationId, format);
        }

        // NativeArray operations
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var data = Serialize(obj, correlationId);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            nativeArray.CopyFrom(data);
            return nativeArray;
        }

        public T DeserializeFromNativeArray<T>(NativeArray<byte> data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            var bytes = data.ToArray();
            return Deserialize<T>(bytes, correlationId);
        }

        // Batch operations - simple iteration
        public byte[][] SerializeBatch<T>(T[] objects, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (objects == null)
                return Array.Empty<byte[]>();

            var results = new byte[objects.Length][];
            for (int i = 0; i < objects.Length; i++)
            {
                results[i] = Serialize(objects[i], correlationId, format);
            }
            return results;
        }

        public byte[][] SerializeBatch<T>(IEnumerable<T> objects, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (objects == null)
                return Array.Empty<byte[]>();

            return objects.Select(obj => Serialize(obj, correlationId, format)).ToArray();
        }

        public T[] DeserializeBatch<T>(byte[][] dataArray, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (dataArray == null)
                return Array.Empty<T>();

            var results = new T[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++)
            {
                results[i] = Deserialize<T>(dataArray[i], correlationId, format);
            }
            return results;
        }

        public T[] DeserializeBatch<T>(IEnumerable<byte[]> dataArray, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            if (dataArray == null)
                return Array.Empty<T>();

            return dataArray.Select(data => Deserialize<T>(data, correlationId, format)).ToArray();
        }

        public async UniTask<byte[][]> SerializeBatchAsync<T>(T[] objects, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return SerializeBatch(objects, correlationId, format);
        }

        public async UniTask<T[]> DeserializeBatchAsync<T>(byte[][] dataArray, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return DeserializeBatch<T>(dataArray, correlationId, format);
        }

        // Format detection and validation - simple implementations
        public SerializationFormat? DetectFormat(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            // Fake always uses "binary" format (since we store keys as UTF8 bytes)
            return SerializationFormat.Binary;
        }

        public bool IsFormatSupported(SerializationFormat format)
        {
            // Fake supports all formats (but treats them the same)
            return true;
        }

        public SerializationFormat GetBestFormat<T>()
        {
            // Fake always returns binary as "best" format
            return SerializationFormat.Binary;
        }

        public SerializationFormat GetBestFormat<T>(SerializationFormat? preferredFormat = null)
        {
            // If preferred format is specified and supported, use it
            if (preferredFormat.HasValue && IsFormatSupported(preferredFormat.Value))
                return preferredFormat.Value;

            // Otherwise return binary as default
            return SerializationFormat.Binary;
        }

        public SerializationFormat[] GetSupportedFormats()
        {
            return (SerializationFormat[])Enum.GetValues(typeof(SerializationFormat));
        }

        public IReadOnlyCollection<SerializationFormat> GetRegisteredFormats()
        {
            return GetSupportedFormats();
        }

        public IReadOnlyList<SerializationFormat> GetFallbackChain(SerializationFormat format)
        {
            // Fake returns a simple fallback chain
            return new[] { format, SerializationFormat.Binary };
        }

        // Try methods for safe serialization/deserialization
        public bool TrySerialize<T>(T obj, out byte[] result, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            try
            {
                result = Serialize(obj, correlationId, format);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public bool TryDeserialize<T>(byte[] data, out T result, FixedString64Bytes correlationId = default, SerializationFormat? format = null)
        {
            try
            {
                result = Deserialize<T>(data, correlationId, format);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        // Serializer management - no-op implementations
        public void RegisterSerializer(SerializationFormat format, ISerializer serializer, string source)
        {
            // No-op: fake doesn't use actual serializers
        }

        public void RegisterSerializer(SerializationFormat format, ISerializer serializer, FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't use actual serializers
        }

        public bool UnregisterSerializer(SerializationFormat format, string source)
        {
            return true; // Always returns success
        }

        public bool UnregisterSerializer(SerializationFormat format, FixedString64Bytes correlationId = default)
        {
            return true; // Always returns success
        }

        public bool IsSerializerAvailable(SerializationFormat format)
        {
            return true; // Fake always has serializers available
        }

        public ISerializer GetSerializer(SerializationFormat format)
        {
            return new FakeSerializer(); // Return a dummy serializer
        }

        // Type registration - no-op implementations
        public void RegisterType<T>(string typeName = null)
        {
            // No-op: fake doesn't require type registration
        }

        public void RegisterType<T>(FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't require type registration
        }

        public void RegisterType(Type type, FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't require type registration
        }

        public void RegisterTypes(Dictionary<Type, string> typeMapping)
        {
            // No-op: fake doesn't require type registration
        }

        public bool IsTypeRegistered<T>()
        {
            return true; // Fake always considers types registered
        }

        public bool IsTypeRegistered(Type type)
        {
            return true; // Fake always considers types registered
        }

        public bool IsRegistered<T>()
        {
            return true; // Fake always considers types registered
        }

        public bool IsRegistered(Type type)
        {
            return true; // Fake always considers types registered
        }

        // Circuit breaker operations - fake implementations
        public ICircuitBreaker GetCircuitBreaker(SerializationFormat format)
        {
            return new FakeCircuitBreaker(); // Return a dummy circuit breaker
        }

        public void OpenCircuitBreaker(SerializationFormat format, string reason, FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't implement actual circuit breaking
        }

        public void CloseCircuitBreaker(SerializationFormat format, string reason, FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't implement actual circuit breaking
        }

        public void ResetAllCircuitBreakers(FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't implement actual circuit breaking
        }

        public IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetCircuitBreakerStatistics()
        {
            return new Dictionary<SerializationFormat, CircuitBreakerStatistics>();
        }

        // Service management methods
        public void SetEnabled(bool enabled, FixedString64Bytes correlationId = default)
        {
            IsEnabled = enabled;
        }

        public async UniTask FlushAsync(FixedString64Bytes correlationId = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            // No-op: fake doesn't have buffers to flush
        }

        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            // No-op: fake doesn't require maintenance
        }

        // Configuration and validation
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            return ValidationResult.Success("FakeSerializationService");
        }

        public void UpdateConfiguration(SerializationConfig config, FixedString64Bytes correlationId = default)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Statistics - return fake data
        public SerializationStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                return new SerializationStatistics
                {
                    TotalSerializations = SerializationCount,
                    TotalDeserializations = DeserializationCount,
                    FailedOperations = 0,
                    TotalBytesProcessed = _dataStore.Count * 100, // Fake byte count
                    AverageSerializationTimeMs = 1.0,
                    AverageDeserializationTimeMs = 1.0,
                    PeakMemoryUsage = _dataStore.Count * 150, // Fake peak memory
                    RegisteredTypeCount = _dataStore.Count,
                    LastResetTime = DateTime.UtcNow,
                    ValidationEnabled = false,
                    EncryptionEnabled = false,
                    EncryptionAlgorithm = "None",
                    EncryptionKeySize = 0,
                    TotalOperationTime = TimeSpan.FromMilliseconds(SerializationCount + DeserializationCount),
                    AverageOperationTime = TimeSpan.FromMilliseconds(1.0)
                };
            }
        }

        // Health and monitoring
        public IReadOnlyDictionary<string, bool> GetHealthStatus()
        {
            return new Dictionary<string, bool>
            {
                { "SerializationService", IsEnabled },
                { "BinarySerializer", IsEnabled },
                { "JsonSerializer", IsEnabled },
                { "MemoryPackSerializer", IsEnabled },
                { "CircuitBreakers", IsEnabled }
            };
        }

        public bool PerformHealthCheck()
        {
            return IsEnabled;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_isDisposed)
            {
                ClearData();
                _isDisposed = true;
            }
        }

        #endregion

        #region Private Helper Methods

        private string GenerateKey<T>(T obj, FixedString64Bytes correlationId)
        {
            var type = typeof(T).Name;
            var objHash = obj?.GetHashCode() ?? 0;
            var correlation = correlationId.IsEmpty ? "default" : correlationId.ToString();
            var timestamp = DateTime.UtcNow.Ticks;

            return $"{type}_{objHash}_{correlation}_{timestamp}";
        }

        #endregion
    }
}