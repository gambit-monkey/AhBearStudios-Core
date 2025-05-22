using System;
using System.Diagnostics;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Data;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MemoryPack;

namespace AhBearStudios.Core.Messaging.Serialization
{
    /// <summary>
    /// Message serializer implementation using MemoryPack for high-performance serialization.
    /// </summary>
    public sealed class MemoryPackMessageSerializer : IMessageSerializer, IDisposable
    {
        private readonly IBurstLogger _logger;
        private readonly IMessageRegistry _messageRegistry;
        private readonly ISerializerMetrics _metrics;
        
        /// <inheritdoc />
        public string Name => "MemoryPack";
        
        /// <inheritdoc />
        public bool IsNetworkCompatible => true;
        
        /// <inheritdoc />
        public bool IsBurstCompatible => false; // MemoryPack uses managed code
        
        /// <summary>
        /// Initializes a new instance of the MemoryPackMessageSerializer class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="messageRegistry">The message registry to use for type resolution.</param>
        public MemoryPackMessageSerializer(IBurstLogger logger, IMessageRegistry messageRegistry, ISerializerMetrics metrics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }
        
        /// <inheritdoc />
        public byte[] Serialize(IMessage message)
        {
            var result = SerializeWithResult(message);
            return result.IsSuccess ? result.Data : null;
        }
        
        /// <inheritdoc />
        public byte[] Serialize<TMessage>(TMessage message) where TMessage : IMessage
        {
            var result = SerializeWithResult(message);
            return result.IsSuccess ? result.Data : null;
        }
        
        /// <inheritdoc />
        public SerializationResult SerializeWithResult(IMessage message)
        {
            if (message == null)
            {
                return SerializationResult.Failure("Message cannot be null");
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // For managed types, use MemoryPack
                if (message is MessageBase managedMessage)
                {
                    var data = MemoryPackSerializer.Serialize(managedMessage);
                    
                    stopwatch.Stop();
                    _metrics.RecordSerialization(stopwatch.Elapsed, data.Length, true);
                    
                    return SerializationResult.Success(data);
                }
                
                // For blittable types, use binary serialization
                var blittableData = SerializeBlittable(message);
                
                stopwatch.Stop();
                _metrics.RecordSerialization(stopwatch.Elapsed, blittableData.Length, true);
                
                return SerializationResult.Success(blittableData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordSerialization(stopwatch.Elapsed, 0, false);
                
                var errorMessage = $"Failed to serialize message of type {message.GetType().Name}: {ex.Message}";
                _logger.Log(LogLevel.Error, errorMessage, "MessageSerializer");
                
                return SerializationResult.Failure(errorMessage, ex);
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
            if (data == null || data.Length < 2)
            {
                return DeserializationResult<TMessage>.Failure("Invalid message data: null or too short");
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Extract the type code
                ushort typeCode = (ushort)((data[1] << 8) | data[0]);
                
                // Get the message type
                var messageType = _messageRegistry.GetMessageType(typeCode);
                if (messageType == null)
                {
                    stopwatch.Stop();
                    _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, false);
                    
                    return DeserializationResult<TMessage>.Failure($"Unknown message type code: {typeCode}");
                }
                
                // Verify the type matches what was requested
                if (!typeof(TMessage).IsAssignableFrom(messageType))
                {
                    stopwatch.Stop();
                    _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, false);
                    
                    return DeserializationResult<TMessage>.Failure(
                        $"Message type {messageType.Name} is not assignable to {typeof(TMessage).Name}");
                }
                
                // For managed types, use MemoryPack
                if (typeof(MessageBase).IsAssignableFrom(messageType))
                {
                    var deserializedMessage = (TMessage)MemoryPackSerializer.Deserialize(messageType, data);
                    
                    stopwatch.Stop();
                    _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, true);
                    
                    return DeserializationResult<TMessage>.Success(deserializedMessage);
                }
                
                // For blittable types, use binary deserialization
                var blittableMessage = (TMessage)DeserializeBlittable(data, messageType);
                
                stopwatch.Stop();
                _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, true);
                
                return DeserializationResult<TMessage>.Success(blittableMessage);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, false);
                
                var errorMessage = $"Failed to deserialize message: {ex.Message}";
                _logger.Log(LogLevel.Error, errorMessage, "MessageSerializer");
                
                return DeserializationResult<TMessage>.Failure(errorMessage, ex);
            }
        }
        
        /// <inheritdoc />
        public bool TryDeserialize(byte[] data, out IMessage message)
        {
            message = null;
            
            if (data == null || data.Length < 2)
            {
                return false;
            }
            
            try
            {
                // Extract the type code
                ushort typeCode = (ushort)((data[1] << 8) | data[0]);
                
                // Get the message type
                var messageType = _messageRegistry.GetMessageType(typeCode);
                if (messageType == null)
                {
                    return false;
                }
                
                // For managed types, use MemoryPack
                if (typeof(MessageBase).IsAssignableFrom(messageType))
                {
                    message = (IMessage)MemoryPackSerializer.Deserialize(messageType, data);
                    return true;
                }
                
                // For blittable types, use binary deserialization
                message = DeserializeBlittable(data, messageType);
                return message != null;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"Failed to deserialize message: {ex.Message}", "MessageSerializer");
                return false;
            }
        }
        
        /// <inheritdoc />
        public bool TryDeserialize<TMessage>(byte[] data, out TMessage message) where TMessage : IMessage
        {
            message = default;
            
            var result = DeserializeWithResult<TMessage>(data);
            if (result.IsSuccess)
            {
                message = result.Message;
                return true;
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
            
            return (ushort)((data[1] << 8) | data[0]);
        }
        
        /// <inheritdoc />
        public Type GetMessageTypeFromData(byte[] data)
        {
            var typeCode = GetTypeCodeFromData(data);
            if (typeCode == 0)
            {
                return null;
            }
            
            return _messageRegistry.GetMessageType(typeCode);
        }
        
        /// <inheritdoc />
        public bool SupportsMessageType(Type messageType)
        {
            if (messageType == null) return false;
            
            // Support all types that implement IMessage
            return typeof(IMessage).IsAssignableFrom(messageType);
        }
        
        /// <inheritdoc />
        public bool SupportsMessageType<TMessage>() where TMessage : IMessage
        {
            return SupportsMessageType(typeof(TMessage));
        }
        
        /// <inheritdoc />
        public int GetEstimatedSerializedSize(IMessage message)
        {
            if (message == null) return 0;
            
            // For managed types, we can use MemoryPack's measure functionality
            if (message is MessageBase managedMessage)
            {
                return MemoryPackSerializer.Serialize(managedMessage).Length;
            }
            
            // For blittable types, use the struct size + type code
            var messageType = message.GetType();
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(messageType) + 2;
        }
        
        /// <inheritdoc />
        public ISerializerMetrics GetMetrics()
        {
            return _metrics;
        }
        
        private byte[] SerializeBlittable(IMessage message)
        {
            var type = message.GetType();
            var size = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(type);
            var buffer = new byte[size + 2]; // +2 for type code
            
            // Write the type code
            buffer[0] = (byte)(message.TypeCode & 0xFF);
            buffer[1] = (byte)((message.TypeCode >> 8) & 0xFF);
            
            // Copy the struct data
            unsafe
            {
                fixed (byte* bufferPtr = &buffer[2])
                {
                    Unity.Collections.LowLevel.Unsafe.UnsafeUtility.CopyStructureToPtr(ref message, bufferPtr);
                }
            }
            
            return buffer;
        }
        
        private IMessage DeserializeBlittable(byte[] data, Type messageType)
        {
            var size = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(messageType);
            
            if (data.Length != size + 2) // +2 for type code
            {
                _logger.Log(LogLevel.Error, 
                    $"Invalid message data size: expected {size + 2}, got {data.Length}",
                    "MessageSerializer");
                return null;
            }
            
            try
            {
                var message = Activator.CreateInstance(messageType);
                
                unsafe
                {
                    fixed (byte* dataPtr = &data[2])
                    {
                        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.CopyPtrToStructure(dataPtr, message);
                    }
                }
                
                return (IMessage)message;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, 
                    $"Failed to deserialize blittable message: {ex.Message}",
                    "MessageSerializer");
                return null;
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            // Clean up any resources if needed
        }
    }
}