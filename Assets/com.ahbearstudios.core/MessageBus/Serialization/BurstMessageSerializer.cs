using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Data;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging.Serialization
{
    /// <summary>
    /// Burst-compatible message serializer for blittable message types.
    /// </summary>
    public sealed class BurstMessageSerializer : IMessageSerializer, IDisposable
    {
        private readonly IBurstLogger _logger;
        private readonly IMessageRegistry _messageRegistry;
        private readonly ISerializerMetrics _metrics;


        /// <inheritdoc />
        public string Name => "Burst";

        /// <inheritdoc />
        public bool IsNetworkCompatible => true;

        /// <inheritdoc />
        public bool IsBurstCompatible => true;

        /// <summary>
        /// Initializes a new instance of the BurstMessageSerializer class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="messageRegistry">The message registry to use for type resolution.</param>
        public BurstMessageSerializer(IBurstLogger logger, IMessageRegistry messageRegistry, ISerializerMetrics metrics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        /// <inheritdoc />
        public byte[] Serialize(IMessage message)
        {
            if (message == null)
            {
                _logger.Log(LogLevel.Error, "Cannot serialize null message", "BurstSerializer");
                return null;
            }

            if (!SupportsMessageType(message.GetType()))
            {
                _logger.Log(LogLevel.Error,
                    $"Message type {message.GetType().Name} is not supported by BurstMessageSerializer",
                    "BurstSerializer");
                return null;
            }

            // Handle the message based on its specific type
            if (message is BlittableMessageBase blittableMessage)
            {
                return SerializeBlittable(blittableMessage);
            }

            _logger.Log(LogLevel.Error,
                $"Message type {message.GetType().Name} is not a BlittableMessageBase",
                "BurstSerializer");
            return null;
        }

        public SerializationResult SerializeWithResult(IMessage message)
        {
            if (message == null)
            {
                return SerializationResult.Failure("Cannot serialize null message");
            }

            if (!SupportsMessageType(message.GetType()))
            {
                return SerializationResult.Failure(
                    $"Message type {message.GetType().Name} is not supported by BurstMessageSerializer");
            }

            try
            {
                // Check the actual runtime type of the message
                Type messageType = message.GetType();

                // Use reflection to call the generic method with the correct type
                var method = typeof(BurstMessageSerializer).GetMethod("SerializeBlittableGeneric",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var genericMethod = method.MakeGenericMethod(messageType);
                byte[] data = (byte[])genericMethod.Invoke(this, new object[] { message });

                return SerializationResult.Success(data);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"Failed to serialize message: {ex.Message}",
                    "BurstSerializer");
                return SerializationResult.Failure($"Failed to serialize message: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generic implementation of blittable message serialization for use with reflection.
        /// </summary>
        /// <typeparam name="T">The unmanaged message type</typeparam>
        /// <param name="message">The message to serialize</param>
        /// <returns>Serialized byte array</returns>
        [BurstCompile]
        private byte[] SerializeBlittableGeneric<T>(T message) where T : unmanaged, IMessage
        {
            var size = UnsafeUtility.SizeOf<T>();
            var buffer = new byte[size + 2]; // +2 for type code

            // Write the type code
            buffer[0] = (byte)(message.TypeCode & 0xFF);
            buffer[1] = (byte)((message.TypeCode >> 8) & 0xFF);

            // Copy the struct data using Burst-compatible operations
            unsafe
            {
                fixed (byte* bufferPtr = &buffer[2])
                {
                    UnsafeUtility.CopyStructureToPtr(ref message, bufferPtr);
                }
            }

            return buffer;
        }

        public SerializationResult SerializeWithResult<TMessage>(TMessage message) where TMessage : IMessage
        {
            return SerializeWithResult((IMessage)message);
        }

        public DeserializationResult<TMessage> DeserializeWithResult<TMessage>(byte[] data) where TMessage : IMessage
        {
            if (data == null || data.Length < 2)
            {
                return DeserializationResult<TMessage>.Failure("Invalid message data: null or too short");
            }

            // Extract the type code
            ushort typeCode = (ushort)((data[1] << 8) | data[0]);

            // Get the message type
            var messageType = _messageRegistry.GetMessageType(typeCode);
            if (messageType == null)
            {
                return DeserializationResult<TMessage>.Failure($"Unknown message type code: {typeCode}");
            }

            if (!SupportsMessageType(messageType))
            {
                return DeserializationResult<TMessage>.Failure(
                    $"Message type {messageType.Name} is not supported by BurstMessageSerializer");
            }

            try
            {
                var message = DeserializeBlittable(data, messageType);

                if (message == null)
                {
                    return DeserializationResult<TMessage>.Failure("Deserialization returned null message");
                }

                if (message is TMessage typedMessage)
                {
                    return DeserializationResult<TMessage>.Success(typedMessage);
                }
                else
                {
                    return DeserializationResult<TMessage>.Failure(
                        $"Deserialized message is not of type {typeof(TMessage).Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"Failed to deserialize message: {ex.Message}",
                    "BurstSerializer");
                return DeserializationResult<TMessage>.Failure($"Failed to deserialize message: {ex.Message}", ex);
            }
        }

        public int GetEstimatedSerializedSize(IMessage message)
        {
            if (message == null)
            {
                return 0;
            }

            if (!SupportsMessageType(message.GetType()))
            {
                return 0;
            }

            // Size of the message struct plus 2 bytes for the type code
            return UnsafeUtility.SizeOf(message.GetType()) + 2;
        }

        public ISerializerMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <inheritdoc />
        public byte[] Serialize<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null)
            {
                _logger.Log(LogLevel.Error, "Cannot serialize null message", "BurstSerializer");
                return null;
            }

            if (!SupportsMessageType(typeof(TMessage)))
            {
                _logger.Log(LogLevel.Error,
                    $"Message type {typeof(TMessage).Name} is not supported by BurstMessageSerializer",
                    "BurstSerializer");
                return null;
            }

            // This ensures we're working with a struct that can be used with Burst
            if (message is BlittableMessageBase blittableMessage)
            {
                return SerializeBlittable(blittableMessage);
            }

            _logger.Log(LogLevel.Error,
                $"Message type {typeof(TMessage).Name} is not a BlittableMessageBase",
                "BurstSerializer");
            return null;
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
        public bool TryDeserialize(byte[] data, out IMessage message)
        {
            message = null;

            if (data == null || data.Length < 2)
            {
                _logger.Log(LogLevel.Error, "Invalid message data: null or too short", "BurstSerializer");
                return false;
            }

            // Extract the type code
            ushort typeCode = (ushort)((data[1] << 8) | data[0]);

            // Get the message type
            var messageType = _messageRegistry.GetMessageType(typeCode);
            if (messageType == null)
            {
                _logger.Log(LogLevel.Error, $"Unknown message type code: {typeCode}", "BurstSerializer");
                return false;
            }

            if (!SupportsMessageType(messageType))
            {
                _logger.Log(LogLevel.Error,
                    $"Message type {messageType.Name} is not supported by BurstMessageSerializer",
                    "BurstSerializer");
                return false;
            }

            message = DeserializeBlittable(data, messageType);
            return message != null;
        }

        /// <inheritdoc />
        public bool TryDeserialize<TMessage>(byte[] data, out TMessage message) where TMessage : IMessage
        {
            message = default;

            if (!TryDeserialize(data, out var genericMessage))
            {
                return false;
            }

            if (genericMessage is TMessage typedMessage)
            {
                message = typedMessage;
                return true;
            }

            _logger.Log(LogLevel.Error,
                $"Deserialized message is not of type {typeof(TMessage).Name}",
                "BurstSerializer");
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

            // Only support blittable struct types that implement IMessage
            return messageType.IsValueType &&
                   typeof(IMessage).IsAssignableFrom(messageType) &&
                   typeof(BlittableMessageBase).IsAssignableFrom(messageType);
        }

        /// <inheritdoc />
        public bool SupportsMessageType<TMessage>() where TMessage : IMessage
        {
            return SupportsMessageType(typeof(TMessage));
        }

        /// <summary>
        /// Serializes a blittable message to a byte array using Burst-compatible operations.
        /// </summary>
        /// <typeparam name="T">The type of message to serialize.</typeparam>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized message as a byte array.</returns>
        [BurstCompile]
        private byte[] SerializeBlittable<T>(T message) where T : unmanaged, IMessage
        {
            var size = UnsafeUtility.SizeOf<T>();
            var buffer = new byte[size + 2]; // +2 for type code

            // Write the type code
            buffer[0] = (byte)(message.TypeCode & 0xFF);
            buffer[1] = (byte)((message.TypeCode >> 8) & 0xFF);

            // Copy the struct data using Burst-compatible operations
            unsafe
            {
                fixed (byte* bufferPtr = &buffer[2])
                {
                    UnsafeUtility.CopyStructureToPtr(ref message, bufferPtr);
                }
            }

            return buffer;
        }

        /// <summary>
        /// Deserializes a byte array to a blittable message using Burst-compatible operations.
        /// </summary>
        /// <param name="data">The serialized message data.</param>
        /// <param name="messageType">The type of message to deserialize to.</param>
        /// <returns>The deserialized message, or null if deserialization failed.</returns>
        private IMessage DeserializeBlittable(byte[] data, Type messageType)
        {
            var size = UnsafeUtility.SizeOf(messageType);

            if (data.Length != size + 2) // +2 for type code
            {
                _logger.Log(LogLevel.Error,
                    $"Invalid message data size: expected {size + 2}, got {data.Length}",
                    "BurstSerializer");
                return null;
            }

            try
            {
                // Use reflection to call the generic method with the correct type
                var method = typeof(BurstMessageSerializer).GetMethod("DeserializeBlittableGeneric",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
                var genericMethod = method.MakeGenericMethod(messageType);
                return (IMessage)genericMethod.Invoke(this, new object[] { data });
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"Failed to deserialize blittable message: {ex.Message}",
                    "BurstSerializer");
                return null;
            }
        }

        /// <summary>
        /// Generic implementation of blittable message deserialization for use with reflection.
        /// </summary>
        /// <typeparam name="T">The unmanaged message type</typeparam>
        /// <param name="data">The serialized message data</param>
        /// <returns>Deserialized message</returns>
        [BurstCompile]
        private T DeserializeBlittableGeneric<T>(byte[] data) where T : unmanaged, IMessage
        {
            T result = default;
    
            unsafe
            {
                fixed (byte* dataPtr = &data[2])
                {
                    UnsafeUtility.CopyPtrToStructure(dataPtr, out result);
                }
            }
    
            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to dispose for this implementation
        }
    }
}