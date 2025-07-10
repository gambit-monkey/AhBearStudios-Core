using System;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for message serializers that can serialize and deserialize messages.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes a message to a byte array.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized message as a byte array.</returns>
        byte[] Serialize(IMessage message);
        
        /// <summary>
        /// Serializes a message to a byte array with type information.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to serialize.</typeparam>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized message as a byte array.</returns>
        byte[] Serialize<TMessage>(TMessage message) where TMessage : IMessage;
        
        /// <summary>
        /// Deserializes a byte array to a message.
        /// </summary>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The deserialized message, or null if deserialization failed.</returns>
        IMessage Deserialize(byte[] data);
        
        /// <summary>
        /// Deserializes a byte array to a message of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to deserialize to.</typeparam>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The deserialized message, or default if deserialization failed.</returns>
        TMessage Deserialize<TMessage>(byte[] data) where TMessage : IMessage;
        
        /// <summary>
        /// Tries to deserialize a byte array to a message.
        /// </summary>
        /// <param name="data">The serialized message data.</param>
        /// <param name="message">The deserialized message if successful.</param>
        /// <returns>True if deserialization was successful; otherwise, false.</returns>
        bool TryDeserialize(byte[] data, out IMessage message);
        
        /// <summary>
        /// Tries to deserialize a byte array to a message of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to deserialize to.</typeparam>
        /// <param name="data">The serialized message data.</param>
        /// <param name="message">The deserialized message if successful.</param>
        /// <returns>True if deserialization was successful; otherwise, false.</returns>
        bool TryDeserialize<TMessage>(byte[] data, out TMessage message) where TMessage : IMessage;
        
        /// <summary>
        /// Gets the type code from serialized data without full deserialization.
        /// </summary>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The type code of the message, or 0 if extraction failed.</returns>
        ushort GetTypeCodeFromData(byte[] data);
        
        /// <summary>
        /// Gets the message type from serialized data without full deserialization.
        /// </summary>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The message type, or null if extraction failed.</returns>
        Type GetMessageTypeFromData(byte[] data);
        
        /// <summary>
        /// Gets whether this serializer supports the specified message type.
        /// </summary>
        /// <param name="messageType">The message type to check.</param>
        /// <returns>True if the message type is supported; otherwise, false.</returns>
        bool SupportsMessageType(Type messageType);
        
        /// <summary>
        /// Gets whether this serializer supports the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type to check.</typeparam>
        /// <returns>True if the message type is supported; otherwise, false.</returns>
        bool SupportsMessageType<TMessage>() where TMessage : IMessage;
        
        /// <summary>
        /// Gets the name of this serializer implementation.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets whether this serializer is suitable for network transmission.
        /// </summary>
        bool IsNetworkCompatible { get; }
        
        /// <summary>
        /// Gets whether this serializer supports Burst compilation.
        /// </summary>
        bool IsBurstCompatible { get; }
        
        /// <summary>
        /// Serializes a message to a byte array with detailed result information.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialization result.</returns>
        SerializationResult SerializeWithResult(IMessage message);
        
        /// <summary>
        /// Serializes a message to a byte array with detailed result information.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to serialize.</typeparam>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialization result.</returns>
        SerializationResult SerializeWithResult<TMessage>(TMessage message) where TMessage : IMessage;
        
        /// <summary>
        /// Deserializes a byte array to a message with detailed result information.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to deserialize to.</typeparam>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The deserialization result.</returns>
        DeserializationResult<TMessage> DeserializeWithResult<TMessage>(byte[] data) where TMessage : IMessage;
        
        /// <summary>
        /// Gets the estimated size of a message when serialized.
        /// </summary>
        /// <param name="message">The message to estimate the size for.</param>
        /// <returns>The estimated serialized size in bytes.</returns>
        int GetEstimatedSerializedSize(IMessage message);
        
        /// <summary>
        /// Gets performance metrics for this serializer.
        /// </summary>
        /// <returns>Performance metrics for the serializer.</returns>
        ISerializerMetrics GetMetrics();
    }
}