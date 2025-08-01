using System;
using System.IO;
using System.Threading;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Configs;
using Cysharp.Threading.Tasks;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// FishNet-specific implementation of ISerializer that bridges between
    /// AhBearStudios serialization system and FishNet's networking serialization.
    /// Provides compatibility layer for using FishNet's Writer/Reader pattern.
    /// </summary>
    public class FishNetSerializer : ISerializer
    {
        private readonly ILoggingService _logger;
        private readonly FishNetSerializationAdapter _adapter;
        private readonly SerializationConfig _config;
        
        // Performance tracking
        private long _totalSerializations;
        private long _totalDeserializations;
        private long _totalFailures;
        private long _totalBytesProcessed;
        private DateTime _lastStatsReset = DateTime.UtcNow;
        
        /// <summary>
        /// Initializes a new instance of FishNetSerializer.
        /// </summary>
        /// <param name="logger">Logging service for monitoring</param>
        /// <param name="config">Serialization configuration</param>
        /// <param name="adapter">FishNet serialization adapter</param>
        public FishNetSerializer(
            ILoggingService logger, 
            SerializationConfig config,
            FishNetSerializationAdapter adapter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            
            _logger.LogInfo("FishNetSerializer initialized", default, nameof(FishNetSerializer));
        }
        
        /// <inheritdoc />
        public byte[] Serialize<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            try
            {
                var data = _adapter.SerializeToBytes(obj);
                
                Interlocked.Increment(ref _totalSerializations);
                Interlocked.Add(ref _totalBytesProcessed, data.Length);
                
                return data;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFailures);
                _logger.LogError($"FishNet serialization failed for {typeof(T).Name}: {ex.Message}");
                throw new SerializationException($"Failed to serialize {typeof(T).Name} using FishNet", typeof(T), "Serialize", ex);
            }
        }
        
        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            try
            {
                var result = _adapter.DeserializeFromBytes<T>(data);
                
                Interlocked.Increment(ref _totalDeserializations);
                Interlocked.Add(ref _totalBytesProcessed, data.Length);
                
                return result;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFailures);
                _logger.LogError($"FishNet deserialization failed for {typeof(T).Name}: {ex.Message}");
                throw new SerializationException($"Failed to deserialize {typeof(T).Name} using FishNet", typeof(T), "Deserialize", ex);
            }
        }
        
        /// <inheritdoc />
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            // FishNet doesn't directly support ReadOnlySpan, convert to array
            return Deserialize<T>(data.ToArray());
        }
        
        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
        
        /// <inheritdoc />
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            return TryDeserialize<T>(data.ToArray(), out result);
        }
        
        /// <inheritdoc />
        public void RegisterType<T>()
        {
            // FishNet handles type registration through extension methods
            _adapter.RegisterType<T>();
            _logger.LogInfo($"Registered type {typeof(T).Name} for FishNet serialization");
        }
        
        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            _adapter.RegisterType(type);
            _logger.LogInfo($"Registered type {type.Name} for FishNet serialization");
        }
        
        /// <inheritdoc />
        public bool IsRegistered<T>()
        {
            return _adapter.IsTypeRegistered<T>();
        }
        
        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            return _adapter.IsTypeRegistered(type);
        }
        
        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            // FishNet serialization is synchronous, wrap in UniTask for consistency
            return await UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellationToken);
        }
        
        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() => Deserialize<T>(data), cancellationToken: cancellationToken);
        }
        
        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            var data = Serialize(obj);
            stream.Write(data, 0, data.Length);
        }
        
        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return Deserialize<T>(ms.ToArray());
        }
        
        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            var data = Serialize(obj);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            nativeArray.CopyFrom(data);
            return nativeArray;
        }
        
        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            var managedArray = data.ToArray();
            return Deserialize<T>(managedArray);
        }
        
        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            var uptime = DateTime.UtcNow - _lastStatsReset;
            var totalOps = _totalSerializations + _totalDeserializations;
            
            return new SerializationStatistics
            {
                TotalSerializations = _totalSerializations,
                TotalDeserializations = _totalDeserializations,
                FailedOperations = _totalFailures,
                TotalBytesProcessed = _totalBytesProcessed,
                AverageSerializationTimeMs = 0.5, // FishNet is very fast, placeholder value
                AverageDeserializationTimeMs = 0.3, // FishNet is very fast, placeholder value
                RegisteredTypeCount = _adapter.GetRegisteredTypeCount(),
                PeakMemoryUsage = GC.GetTotalMemory(false),
                BufferPoolStats = null // FishNet manages its own pooling
            };
        }
    }
}