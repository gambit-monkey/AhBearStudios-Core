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
    /// Message serializer implementation using MemoryPack for managed types and direct memory operations for blittable types.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed unsafe class MemoryPackMessageSerializer : IMessageSerializer, IDisposable
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
        public MemoryPackMessageSerializer(IBurstLogger logger, IMessageRegistry messageRegistry,
            ISerializerMetrics metrics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        /// <inheritdoc />
        public SerializationResult SerializeWithResult(IMessage message)
        {
            if (message == null) return SerializationResult.Failure("Message cannot be null");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                byte[] data;
                if (IsBlittableMessage(message))
                {
                    data = SerializeBlittableMessage(message);
                }
                else
                {
                    data = MemoryPackSerializer.Serialize(message);
                }

                stopwatch.Stop();
                _metrics.RecordSerialization(stopwatch.Elapsed, data.Length, true);
                return SerializationResult.Success(data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordSerialization(stopwatch.Elapsed, 0, false);
                return SerializationResult.Failure(ex.Message, ex);
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
        /// <inheritdoc />
        public DeserializationResult<TMessage> DeserializeWithResult<TMessage>(byte[] data) where TMessage : IMessage
        {
            if (data == null || data.Length == 0)
                return DeserializationResult<TMessage>.Failure("Data is null or empty");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                TMessage result;
                var type = typeof(TMessage);

                // For blittable value types, use direct memory copying
                if (type.IsValueType && UnsafeUtility.IsBlittable(type))
                {
                    // Get type code from data
                    var typeCode = GetTypeCodeFromData(data);
                    var expectedType = _messageRegistry.GetMessageType(typeCode);

                    // Verify the type matches what we expect
                    if (expectedType != type)
                    {
                        throw new InvalidOperationException(
                            $"Type mismatch: Expected {type.Name} but data contains {expectedType?.Name ?? "unknown"}");
                    }

                    // Make sure we have enough data
                    var expectedSize = UnsafeUtility.SizeOf(type) + sizeof(ushort);
                    if (data.Length != expectedSize)
                    {
                        throw new ArgumentException(
                            $"Invalid data length for type {type.Name}: expected {expectedSize}, got {data.Length}");
                    }

                    // Handle blittable types using type-specific method
                    if (type == typeof(BlittableMessage))
                    {
                        var blittableResult = DeserializeBlittableInternal<BlittableMessage>(data);
                        result = (TMessage)(object)blittableResult;
                    }
                    else
                    {
                        // Use MemoryPack for all other cases
                        result = MemoryPackSerializer.Deserialize<TMessage>(data);
                    }
                }
                else
                {
                    // Use MemoryPack for reference types and non-blittable types
                    result = MemoryPackSerializer.Deserialize<TMessage>(data);
                }

                stopwatch.Stop();
                _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, true);
                return DeserializationResult<TMessage>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordDeserialization(stopwatch.Elapsed, data.Length, false);
                return DeserializationResult<TMessage>.Failure(ex.Message, ex);
            }
        }

// Helper method for deserializing blittable types
        private T DeserializeBlittableInternal<T>(byte[] data) where T : struct
        {
            var result = default(T);
            fixed (byte* src = &data[sizeof(ushort)])
            {
                UnsafeUtility.CopyPtrToStructure(src, UnsafeUtility.AddressOf(ref result));
            }

            return result;
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
            if (data == null || data.Length < 2) return false;

            try
            {
                var typeCode = GetTypeCodeFromData(data);
                var messageType = _messageRegistry.GetMessageType(typeCode);
                if (messageType == null) return false;

                var result = MemoryPackSerializer.Deserialize(messageType, data);
                message = result as IMessage;
                return message != null;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Deserialization failed: {ex.Message}", "MemoryPackSerializer");
                return false;
            }
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
            if (!TryDeserialize(data, out TMessage message))
            {
                return default;
            }

            return message;
        }

        /// <inheritdoc />
        public ushort GetTypeCodeFromData(byte[] data)
        {
            if (data == null || data.Length < 2) return 0;
            return (ushort)((data[1] << 8) | data[0]);
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
            if (messageType == null) return false;
            return typeof(IMessage).IsAssignableFrom(messageType);
        }

        /// <inheritdoc />
        public bool SupportsMessageType<TMessage>() where TMessage : IMessage
        {
            return true;
        }

        private bool IsBlittableMessage(IMessage message)
        {
            var type = message.GetType();
            return type.IsValueType && UnsafeUtility.IsBlittable(type);
        }

        private bool IsBlittableType<T>() where T : IMessage
        {
            return typeof(T).IsValueType && UnsafeUtility.IsBlittable<T>();
        }

        private byte[] SerializeBlittableMessage(IMessage message)
        {
            var type = message.GetType();
            var size = UnsafeUtility.SizeOf(type);
            var data = new byte[size + sizeof(ushort)];

            var typeCode = _messageRegistry.GetTypeCode(type);
            data[0] = (byte)(typeCode & 0xFF);
            data[1] = (byte)(typeCode >> 8);

            fixed (void* dest = &data[sizeof(ushort)])
            {
                void* src = UnsafeUtility.AddressOf(ref message);
                UnsafeUtility.MemCpy(dest, src, size);
            }

            return data;
        }

        private T DeserializeBlittable<T>(byte[] data) where T : struct, IMessage
        {
            var size = UnsafeUtility.SizeOf<T>();
            if (data.Length != size + sizeof(ushort))
            {
                throw new ArgumentException($"Invalid data length for type {typeof(T).Name}");
            }

            T result = default;
            fixed (byte* src = &data[sizeof(ushort)])
            {
                UnsafeUtility.CopyPtrToStructure(src, UnsafeUtility.AddressOf(ref result));
            }

            return result;
        }

        /// <inheritdoc />
        public int GetEstimatedSerializedSize(IMessage message)
        {
            if (message == null)
                return 0;

            // Add size for type code
            int estimatedSize = sizeof(ushort);

            if (IsBlittableMessage(message))
            {
                // For blittable types, we can get the exact size
                estimatedSize += UnsafeUtility.SizeOf(message.GetType());
            }
            else
            {
                // For non-blittable types, we'll serialize to get the actual size
                // This is more expensive but accurate
                try
                {
                    var serialized = MemoryPackSerializer.Serialize(message);
                    estimatedSize += serialized.Length;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warning,
                        $"Failed to estimate serialized size for message type {message.GetType().Name}: {ex.Message}",
                        "MemoryPackSerializer");

                    // Fallback to a rough estimate based on the object's type
                    estimatedSize += EstimateObjectSize(message);
                }
            }

            return estimatedSize;
        }

        /// <inheritdoc />
        public ISerializerMetrics GetMetrics()
        {
            return _metrics;
        }

        private int EstimateObjectSize(IMessage message)
        {
            // This is a rough estimation for non-blittable types
            // You might want to adjust these values based on your specific message types
            const int BaseSize = 32; // Base size for any message
            const int PerFieldSize = 8; // Estimated size per field

            var type = message.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Instance);

            var properties = type.GetProperties(System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.NonPublic |
                                                System.Reflection.BindingFlags.Instance);

            return BaseSize + ((fields.Length + properties.Length) * PerFieldSize);
        }

        public void Dispose()
        {
            // No unmanaged resources to dispose
        }
    }
}