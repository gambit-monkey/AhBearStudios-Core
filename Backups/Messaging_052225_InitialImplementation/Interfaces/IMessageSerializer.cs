using System;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Interface for converting messages to and from a storage format.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to serialize.</typeparam>
    public interface IMessageSerializer<TMessage> : IDisposable where TMessage : IMessage
    {
        /// <summary>
        /// Converts a message to a string format for storage.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized message as a string.</returns>
        string Serialize(TMessage message);
        
        /// <summary>
        /// Converts a serialized string back to a message.
        /// </summary>
        /// <param name="serializedMessage">The serialized message string.</param>
        /// <returns>The deserialized message.</returns>
        TMessage Deserialize(string serializedMessage);
        
        /// <summary>
        /// Extracts just the timestamp from a serialized message without fully deserializing it.
        /// </summary>
        /// <param name="serializedMessage">The serialized message string.</param>
        /// <returns>The message timestamp.</returns>
        long GetTimestamp(string serializedMessage);
    }
}