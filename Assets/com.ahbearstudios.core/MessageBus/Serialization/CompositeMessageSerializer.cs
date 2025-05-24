using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Metrics.Serialization;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.MessageBus.Serialization
{
    /// <summary>
    /// Composite message serializer that delegates to different serializers based on message type.
    /// </summary>
    public sealed class CompositeMessageSerializer : IMessageSerializer, IDisposable
    {
        private readonly IBurstLogger _logger;
        private readonly List<IMessageSerializer> _serializers;
        private readonly Dictionary<Type, IMessageSerializer> _serializerCache;
        private readonly object _cacheLock = new object();
        
        /// <inheritdoc />
        public string Name => "Composite";
        
        /// <inheritdoc />
        public bool IsNetworkCompatible => _serializers.All(s => s.IsNetworkCompatible);
        
        /// <inheritdoc />
        public bool IsBurstCompatible => _serializers.Any(s => s.IsBurstCompatible);
        
        /// <summary>
        /// Initializes a new instance of the CompositeMessageSerializer class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="serializers">The serializers to use in order of preference.</param>
        public CompositeMessageSerializer(IBurstLogger logger, params IMessageSerializer[] serializers)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializers = serializers?.ToList() ?? throw new ArgumentNullException(nameof(serializers));
            _serializerCache = new Dictionary<Type, IMessageSerializer>();
            
            if (_serializers.Count == 0)
            {
                throw new ArgumentException("At least one serializer must be provided", nameof(serializers));
            }
            
            _logger.Log(LogLevel.Info, 
                $"CompositeMessageSerializer initialized with {_serializers.Count} serializers: {string.Join(", ", _serializers.Select(s => s.Name))}",
                "CompositeSerializer");
        }
        
        /// <inheritdoc />
        public byte[] Serialize(IMessage message)
        {
            var serializer = GetSerializerForType(message?.GetType());
            if (serializer == null)
            {
                _logger.Log(LogLevel.Error, 
                    $"No suitable serializer found for message type {message?.GetType()?.Name ?? "null"}",
                    "CompositeSerializer");
                return null;
            }
            
            return serializer.Serialize(message);
        }
        
        /// <inheritdoc />
        public byte[] Serialize<TMessage>(TMessage message) where TMessage : IMessage
        {
            return Serialize((IMessage)message);
        }
        
        /// <inheritdoc />
        public SerializationResult SerializeWithResult(IMessage message)
        {
            if (message == null)
            {
                return SerializationResult.Failure("Message cannot be null");
            }
            
            var serializer = GetSerializerForType(message.GetType());
            if (serializer == null)
            {
                return SerializationResult.Failure($"No suitable serializer found for message type {message.GetType().Name}");
            }
            
            // If the serializer supports extended interface, use it
            if (serializer is IMessageSerializer extendedSerializer)
            {
                return extendedSerializer.SerializeWithResult(message);
            }
            
            // Fallback to basic serialization
            try
            {
                var data = serializer.Serialize(message);
                return data != null 
                    ? SerializationResult.Success(data) 
                    : SerializationResult.Failure("Serialization returned null");
            }
            catch (Exception ex)
            {
                return SerializationResult.Failure($"Serialization failed: {ex.Message}", ex);
            }
        }
        
        /// <inheritdoc />
        public SerializationResult SerializeWithResult<TMessage>(TMessage message) where TMessage : IMessage
        {
            return SerializeWithResult((IMessage)message);
        }
        
        /// <inheritdoc />
        public IMessage Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var message))
            {
                return null;
            }
            
            return message;
        }
        
        /// <inheritdoc />
        public TMessage Deserialize<TMessage>(byte[] data) where TMessage : IMessage
        {
            if (!TryDeserialize<TMessage>(data, out var message))
            {
                return default;
            }
            
            return message;
        }
        
        /// <inheritdoc />
        public DeserializationResult<TMessage> DeserializeWithResult<TMessage>(byte[] data) where TMessage : IMessage
        {
            if (data == null || data.Length == 0)
            {
                return DeserializationResult<TMessage>.Failure("Data cannot be null or empty");
            }
            
            // Try to determine the message type from the data
            var messageType = GetMessageTypeFromData(data);
            if (messageType == null)
            {
                return DeserializationResult<TMessage>.Failure("Unable to determine message type from data");
            }
            
            var serializer = GetSerializerForType(messageType);
            if (serializer == null)
            {
                return DeserializationResult<TMessage>.Failure($"No suitable serializer found for message type {messageType.Name}");
            }
            
            // If the serializer supports extended interface, use it
            if (serializer is IMessageSerializer extendedSerializer)
            {
                return extendedSerializer.DeserializeWithResult<TMessage>(data);
            }
            
            // Fallback to basic deserialization
            try
            {
                var message = serializer.Deserialize<TMessage>(data);
                return !EqualityComparer<TMessage>.Default.Equals(message, default) 
                    ? DeserializationResult<TMessage>.Success(message) 
                    : DeserializationResult<TMessage>.Failure("Deserialization returned default value");
            }
            catch (Exception ex)
            {
                return DeserializationResult<TMessage>.Failure($"Deserialization failed: {ex.Message}", ex);
            }
        }
        
        /// <inheritdoc />
        public bool TryDeserialize(byte[] data, out IMessage message)
        {
            message = null;
            
            if (data == null || data.Length == 0)
            {
                return false;
            }
            
            // Try each serializer until one succeeds
            foreach (var serializer in _serializers)
            {
                if (serializer.TryDeserialize(data, out message))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public bool TryDeserialize<TMessage>(byte[] data, out TMessage message) where TMessage : IMessage
        {
            message = default;
            
            if (data == null || data.Length == 0)
            {
                return false;
            }
            
            // Try to determine the message type from the data first
            var messageType = GetMessageTypeFromData(data);
            if (messageType != null)
            {
                var serializer = GetSerializerForType(messageType);
                if (serializer != null && serializer.TryDeserialize<TMessage>(data, out message))
                {
                    return true;
                }
            }
            
            // Fallback: try each serializer until one succeeds
            foreach (var serializer in _serializers)
            {
                if (serializer.TryDeserialize<TMessage>(data, out message))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public ushort GetTypeCodeFromData(byte[] data)
        {
            if (data == null || data.Length < 2)
            {
                return 0;
            }
            
            // Try each serializer
            foreach (var serializer in _serializers)
            {
                var typeCode = serializer.GetTypeCodeFromData(data);
                if (typeCode != 0)
                {
                    return typeCode;
                }
            }
            
            return 0;
        }
        
        /// <inheritdoc />
        public Type GetMessageTypeFromData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }
            
            // Try each serializer
            foreach (var serializer in _serializers)
            {
                var messageType = serializer.GetMessageTypeFromData(data);
                if (messageType != null)
                {
                    return messageType;
                }
            }
            
            return null;
        }
        
        /// <inheritdoc />
        public bool SupportsMessageType(Type messageType)
        {
            return _serializers.Any(s => s.SupportsMessageType(messageType));
        }
        
        /// <inheritdoc />
        public bool SupportsMessageType<TMessage>() where TMessage : IMessage
        {
            return SupportsMessageType(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public int GetEstimatedSerializedSize(IMessage message)
        {
            var serializer = GetSerializerForType(message?.GetType());
            if (serializer is IMessageSerializer extendedSerializer)
            {
                return extendedSerializer.GetEstimatedSerializedSize(message);
            }
            
            // Fallback estimation
            return message?.GetType().IsValueType == true 
                ? UnsafeUtility.SizeOf(message.GetType()) + 2
                : 256; // Rough estimate for managed types
        }
        
        /// <inheritdoc />
        public ISerializerMetrics GetMetrics()
        {
            // Return combined metrics from all serializers that support it
            var metricsSerializers = _serializers.OfType<IMessageSerializer>().ToList();
            if (metricsSerializers.Count == 0)
            {
                return new NullSerializerMetrics();
            }
            
            return new CompositeSerializerMetrics(metricsSerializers.Select(s => s.GetMetrics()).ToList());
        }
        
        /// <summary>
        /// Gets the most suitable serializer for the specified message type.
        /// </summary>
        /// <param name="messageType">The message type to get a serializer for.</param>
        /// <returns>The most suitable serializer, or null if none found.</returns>
        private IMessageSerializer GetSerializerForType(Type messageType)
        {
            if (messageType == null) return null;
            
            lock (_cacheLock)
            {
                if (_serializerCache.TryGetValue(messageType, out var cachedSerializer))
                {
                    return cachedSerializer;
                }
                
                // Find the first serializer that supports this type
                var serializer = _serializers.FirstOrDefault(s => s.SupportsMessageType(messageType));
                if (serializer != null)
                {
                    _serializerCache[messageType] = serializer;
                    _logger.Log(LogLevel.Debug, 
                        $"Selected serializer {serializer.Name} for message type {messageType.Name}",
                        "CompositeSerializer");
                }
                
                return serializer;
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var serializer in _serializers.OfType<IDisposable>())
            {
                serializer.Dispose();
            }
            
            _serializerCache.Clear();
        }
    }
}