using System;
using System.Diagnostics;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;
using AhBearStudios.Core.Profiling.Interfaces;
using MemoryPack;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.MessageBus.Serialization
{
    /// <summary>
    /// Message serializer implementation using MemoryPack for managed types.
    /// Provides high-performance serialization with automatic type handling.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
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
        public bool IsBurstCompatible => false;

        /// <summary>
        /// Initializes a new instance of the MemoryPackMessageSerializer class.
        /// </summary>
        /// <param name="logger">The logger to use for logging operations.</param>
        /// <param name="messageRegistry">The message registry for type resolution.</param>
        /// <param name="metrics">The metrics collector for performance tracking.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public MemoryPackMessageSerializer(IBurstLogger logger, IMessageRegistry messageRegistry, ISerializerMetrics metrics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
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
                // Get the type code for this message type
                var messageType = message.GetType();
                var typeCode = _messageRegistry.GetTypeCode(messageType);
                
                if (typeCode == 0)
                {
                    throw new InvalidOperationException($"Message type {messageType.Name} is not registered");
                }

                // Serialize the message using MemoryPack
                var messageData = MemoryPackSerializer.Serialize(message);
                
                // Create the final data with type code prefix
                var finalData = new byte[messageData.Length + sizeof(ushort)];
                
                // Write type code (little-endian)
                finalData[0] = (byte)(typeCode & 0xFF);
                finalData[1] = (byte)((typeCode >> 8) & 0xFF);
                
                // Copy message data
                Array.Copy(messageData, 0, finalData, sizeof(ushort), messageData.Length);

                stopwatch.Stop();
                _metrics.RecordSerialization(stopwatch.Elapsed, finalData.Length, true);
                
                return SerializationResult.Success(finalData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordSerialization(stopwatch.Elapsed, 0, false);
                
                _logger.Log(LogLevel.Error, 
                    $"Failed to serialize message of type {message.GetType().Name}: {ex.Message}", 
                    "MemoryPackSerializer");
                
                return SerializationResult.Failure($"Serialization failed: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public SerializationResult SerializeWithResult<TMessage>(TMessage message) where TMessage : IMessage
        {
            return SerializeWithResult((IMessage)message);
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
        public DeserializationResult<TMessage> DeserializeWithResult<TMessage>(byte[] data) where TMessage : IMessage
        {
            if (data == null || data.Length < sizeof(ushort))
            {
                return DeserializationResult<TMessage>.Failure("Data is null or too short to contain type code");
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Extract type code
                var typeCode = GetTypeCodeFromData(data);
                var messageType = _messageRegistry.GetMessageType(typeCode);
                
                if (messageType == null)
                {
                    throw new InvalidOperationException($"Unknown message type code: {typeCode}");
                }

                // Verify the type matches what we expect
                if (!typeof(TMessage).IsAssignableFrom(messageType))
                {
                    throw new InvalidOperationException(
                        $"Type mismatch: Expected {typeof(TMessage).Name} or compatible type, but data contains {messageType.Name}");
                }

                // Extract message data (skip type code)
                var messageData = new byte[data.Length - sizeof(ushort)];
                Array.Copy(data, sizeof(ushort), messageData, 0, messageData.Length);

                // Deserialize using MemoryPack
                var deserializedMessage = MemoryPackSerializer.Deserialize(messageType, messageData);
                
                if (deserializedMessage is TMessage typedMessage)
                {
                    stopwatch.Stop();
                    _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, true);
                    
                    return DeserializationResult<TMessage>.Success(typedMessage);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Deserialized message could not be cast to {typeof(TMessage).Name}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, false);
                
                _logger.Log(LogLevel.Error, 
                    $"Failed to deserialize message: {ex.Message}", 
                    "MemoryPackSerializer");
                
                return DeserializationResult<TMessage>.Failure($"Deserialization failed: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<TMessage>(byte[] data, out TMessage message) where TMessage : IMessage
        {
            var result = DeserializeWithResult<TMessage>(data);
            if (result.IsSuccess)
            {
                message = result.Message;
                return true;
            }

            message = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryDeserialize(byte[] data, out IMessage message)
        {
            message = null;
            
            if (data == null || data.Length < sizeof(ushort))
            {
                return false;
            }

            try
            {
                var typeCode = GetTypeCodeFromData(data);
                var messageType = _messageRegistry.GetMessageType(typeCode);
                
                if (messageType == null)
                {
                    return false;
                }

                // Extract message data (skip type code)
                var messageData = new byte[data.Length - sizeof(ushort)];
                Array.Copy(data, sizeof(ushort), messageData, 0, messageData.Length);

                // Deserialize using MemoryPack
                var deserializedMessage = MemoryPackSerializer.Deserialize(messageType, messageData);
                
                if (deserializedMessage is IMessage msg)
                {
                    message = msg;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, 
                    $"Failed to deserialize message: {ex.Message}", 
                    "MemoryPackSerializer");
                return false;
            }
        }

        /// <inheritdoc />
        public IMessage Deserialize(byte[] data)
        {
            if (TryDeserialize(data, out var message))
            {
                return message;
            }
            return null;
        }

        /// <inheritdoc />
        public TMessage Deserialize<TMessage>(byte[] data) where TMessage : IMessage
        {
            if (TryDeserialize(data, out TMessage message))
            {
                return message;
            }
            return default;
        }

        /// <inheritdoc />
        public ushort GetTypeCodeFromData(byte[] data)
        {
            if (data == null || data.Length < sizeof(ushort))
            {
                return 0;
            }

            // Read little-endian ushort
            return (ushort)(data[0] | (data[1] << 8));
        }

        /// <inheritdoc />
        public Type GetMessageTypeFromData(byte[] data)
        {
            var typeCode = GetTypeCodeFromData(data);
            return typeCode == 0 ? null : _messageRegistry.GetMessageType(typeCode);
        }

        /// <inheritdoc />
        public bool SupportsMessageType(Type messageType)
        {
            if (messageType == null)
            {
                return false;
            }

            // Support any type that implements IMessage and can be registered
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
            if (message == null)
            {
                return 0;
            }

            // Base size includes type code
            var baseSize = sizeof(ushort);
            
            try
            {
                // For accurate estimation, we serialize the message
                // This is more expensive but provides accurate sizing
                var messageData = MemoryPackSerializer.Serialize(message);
                return baseSize + messageData.Length;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning,
                    $"Failed to estimate serialized size for message type {message.GetType().Name}: {ex.Message}",
                    "MemoryPackSerializer");

                // Fallback to rough estimation
                return baseSize + EstimateMessageSize(message);
            }
        }

        /// <inheritdoc />
        public ISerializerMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Provides a rough estimation of message size for fallback scenarios.
        /// </summary>
        /// <param name="message">The message to estimate size for.</param>
        /// <returns>Estimated size in bytes.</returns>
        private int EstimateMessageSize(IMessage message)
        {
            const int BaseMessageSize = 32; // Base size for any message (ID, timestamp, etc.)
            const int PerFieldSize = 8; // Estimated size per field
            const int StringFieldSize = 16; // Estimated size for string fields

            var messageType = message.GetType();
            var fields = messageType.GetFields(System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic |
                                               System.Reflection.BindingFlags.Instance);

            var properties = messageType.GetProperties(System.Reflection.BindingFlags.Public |
                                                       System.Reflection.BindingFlags.NonPublic |
                                                       System.Reflection.BindingFlags.Instance);

            var totalFields = fields.Length + properties.Length;
            var stringFields = 0;

            // Count string properties for better estimation
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    stringFields++;
                }
            }

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    stringFields++;
                }
            }

            return BaseMessageSize + 
                   (totalFields * PerFieldSize) + 
                   (stringFields * StringFieldSize);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // MemoryPack doesn't require explicit disposal
            // No unmanaged resources to clean up
        }
    }
}