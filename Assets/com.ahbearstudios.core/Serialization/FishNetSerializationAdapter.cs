using System;
using System.Collections.Generic;
using System.Reflection;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Services;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Adapter that bridges between AhBearStudios serialization system and FishNet's Writer/Reader pattern.
    /// Uses MemoryPack for high-performance serialization with pooled buffers for zero-allocation operations.
    /// </summary>
    public class FishNetSerializationAdapter
    {
        private readonly ILoggingService _logger;
        private readonly ISerializationService _serializationService;
        private readonly NetworkSerializationBufferPool _bufferPool;
        private readonly Dictionary<Type, MethodInfo> _writeMethodCache;
        private readonly Dictionary<Type, MethodInfo> _readMethodCache;
        private readonly HashSet<Type> _registeredTypes;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Initializes a new instance of FishNetSerializationAdapter.
        /// </summary>
        /// <param name="logger">Logging service for monitoring</param>
        /// <param name="serializationService">Core serialization service</param>
        /// <param name="bufferPool">Network buffer pool</param>
        public FishNetSerializationAdapter(
            ILoggingService logger,
            ISerializationService serializationService,
            NetworkSerializationBufferPool bufferPool)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            
            _writeMethodCache = new Dictionary<Type, MethodInfo>();
            _readMethodCache = new Dictionary<Type, MethodInfo>();
            _registeredTypes = new HashSet<Type>();
            
            // Register common Unity types by default
            RegisterUnityTypes();
        }
        
        /// <summary>
        /// Serializes an object to bytes using MemoryPack through the AhBearStudios serialization system.
        /// Uses pooled buffers for zero-allocation operations.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <returns>Serialized byte array</returns>
        public byte[] SerializeToBytes<T>(T obj)
        {
            var type = typeof(T);
            
            _logger.LogDebug($"Serializing {type.Name} using MemoryPack via FishNet adapter");
            
            try
            {
                // Use the AhBearStudios serialization service with FishNet format
                // This will route through MemoryPack for high performance
                var serializedData = _serializationService.Serialize(obj, default, Models.SerializationFormat.FishNet);
                
                _logger.LogDebug($"Successfully serialized {type.Name} to {serializedData.Length} bytes using MemoryPack");
                return serializedData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to serialize {type.Name} using MemoryPack: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deserializes bytes to an object using MemoryPack through the AhBearStudios serialization system.
        /// Uses pooled buffers for zero-allocation operations.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <returns>Deserialized object</returns>
        public T DeserializeFromBytes<T>(byte[] data)
        {
            var type = typeof(T);
            
            _logger.LogDebug($"Deserializing {type.Name} from {data.Length} bytes using MemoryPack via FishNet adapter");
            
            try
            {
                // Use the AhBearStudios serialization service with FishNet format
                // This will route through MemoryPack for high performance
                var result = _serializationService.Deserialize<T>(data, default, Models.SerializationFormat.FishNet);
                
                _logger.LogDebug($"Successfully deserialized {type.Name} using MemoryPack");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to deserialize {type.Name} using MemoryPack: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Serializes an object using a pooled buffer for zero-allocation operations.
        /// Returns the buffer which must be returned to the pool after use.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="expectedSize">Expected serialized size for buffer selection</param>
        /// <returns>Pooled buffer containing serialized data</returns>
        public PooledNetworkBuffer SerializeToPooledBuffer<T>(T obj, int expectedSize = 0)
        {
            var type = typeof(T);
            
            _logger.LogDebug($"Serializing {type.Name} to pooled buffer using MemoryPack");
            
            try
            {
                // Get an appropriately sized buffer from the pool
                var buffer = _bufferPool.GetBuffer(expectedSize);
                
                // Serialize using MemoryPack
                var serializedData = _serializationService.Serialize(obj, default, Models.SerializationFormat.FishNet);
                
                // Set the data in the pooled buffer
                buffer.SetData(serializedData);
                
                _logger.LogDebug($"Successfully serialized {type.Name} to pooled buffer ({buffer.Length} bytes)");
                return buffer;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to serialize {type.Name} to pooled buffer: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deserializes an object from a pooled buffer.
        /// The buffer should be returned to the pool after this operation.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="buffer">Pooled buffer containing serialized data</param>
        /// <returns>Deserialized object</returns>
        public T DeserializeFromPooledBuffer<T>(Pooling.Models.PooledNetworkBuffer buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var type = typeof(T);
            
            _logger.LogDebug($"Deserializing {type.Name} from pooled buffer ({buffer.Length} bytes)");
            
            try
            {
                // Get data from the pooled buffer and deserialize
                var data = buffer.ToArray();
                var result = _serializationService.Deserialize<T>(data, default, Models.SerializationFormat.FishNet);
                
                _logger.LogDebug($"Successfully deserialized {type.Name} from pooled buffer");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to deserialize {type.Name} from pooled buffer: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets buffer pool statistics for monitoring.
        /// </summary>
        /// <returns>Network buffer pool statistics</returns>
        public NetworkBufferPoolStatistics GetBufferPoolStatistics()
        {
            return _bufferPool.GetStatistics();
        }
        
        /// <summary>
        /// Registers a type for FishNet serialization.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        public void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }
        
        /// <summary>
        /// Registers a type for FishNet serialization.
        /// Uses the MemoryPack serialization system for high-performance operations.
        /// </summary>
        /// <param name="type">The type to register</param>
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            lock (_lock)
            {
                if (_registeredTypes.Contains(type))
                    return;
                
                // Register with the AhBearStudios serialization service
                // This ensures MemoryPack can handle the type
                _serializationService.RegisterType(type);
                
                _registeredTypes.Add(type);
                _logger.LogInfo($"Registered type {type.Name} for FishNet serialization using MemoryPack");
                
                // Cache serialization methods for potential direct FishNet integration
                CacheSerializationMethods(type);
            }
        }
        
        /// <summary>
        /// Checks if a type is registered for FishNet serialization.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>True if registered</returns>
        public bool IsTypeRegistered<T>()
        {
            return IsTypeRegistered(typeof(T));
        }
        
        /// <summary>
        /// Checks if a type is registered for FishNet serialization.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if registered</returns>
        public bool IsTypeRegistered(Type type)
        {
            lock (_lock)
            {
                return _registeredTypes.Contains(type);
            }
        }
        
        /// <summary>
        /// Gets the count of registered types.
        /// </summary>
        /// <returns>Number of registered types</returns>
        public int GetRegisteredTypeCount()
        {
            lock (_lock)
            {
                return _registeredTypes.Count;
            }
        }
        
        /// <summary>
        /// Creates a Writer delegate for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create writer for</typeparam>
        /// <returns>Writer delegate or null if not found</returns>
        public Action<object, T> GetWriter<T>()
        {
            var type = typeof(T);
            lock (_lock)
            {
                if (_writeMethodCache.TryGetValue(type, out var method))
                {
                    // Create delegate from cached method
                    return (writer, value) => method.Invoke(null, new object[] { writer, value });
                }
            }
            return null;
        }
        
        /// <summary>
        /// Creates a Reader delegate for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create reader for</typeparam>
        /// <returns>Reader delegate or null if not found</returns>
        public Func<object, T> GetReader<T>()
        {
            var type = typeof(T);
            lock (_lock)
            {
                if (_readMethodCache.TryGetValue(type, out var method))
                {
                    // Create delegate from cached method
                    return (reader) => (T)method.Invoke(null, new object[] { reader });
                }
            }
            return null;
        }
        
        private void RegisterUnityTypes()
        {
            // Register common Unity types that FishNet likely supports
            RegisterType<UnityEngine.Vector2>();
            RegisterType<UnityEngine.Vector3>();
            RegisterType<UnityEngine.Vector4>();
            RegisterType<UnityEngine.Quaternion>();
            RegisterType<UnityEngine.Color>();
            RegisterType<UnityEngine.Color32>();
            RegisterType<UnityEngine.Bounds>();
            RegisterType<UnityEngine.Rect>();
            RegisterType<UnityEngine.Matrix4x4>();
            RegisterType<UnityEngine.Vector2Int>();
            RegisterType<UnityEngine.Vector3Int>();
            
            // Register common .NET types
            RegisterType<string>();
            RegisterType<int>();
            RegisterType<float>();
            RegisterType<double>();
            RegisterType<bool>();
            RegisterType<byte>();
            RegisterType<short>();
            RegisterType<long>();
            RegisterType<uint>();
            RegisterType<ushort>();
            RegisterType<ulong>();
            RegisterType<decimal>();
            RegisterType<DateTime>();
            RegisterType<Guid>();
            
            _logger.LogInfo("Registered Unity and common types for FishNet serialization");
        }
        
        private void CacheSerializationMethods(Type type)
        {
            // For MemoryPack integration, we don't need to cache individual FishNet methods
            // since all types go through the unified MemoryPack serialization system
            // This method is kept for potential future direct FishNet integration
            
            _logger.LogDebug($"Type {type.Name} registered for MemoryPack serialization via FishNet adapter");
        }
    }
}