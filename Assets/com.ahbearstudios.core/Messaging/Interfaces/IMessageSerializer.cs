using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for serializing messages
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes an object to a string
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized string</returns>
        string Serialize(object obj);
    
        /// <summary>
        /// Serializes an object to a string
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized string</returns>
        string Serialize<T>(T obj);
    
        /// <summary>
        /// Deserializes a string to an object
        /// </summary>
        /// <param name="json">The serialized string</param>
        /// <param name="type">The type of object</param>
        /// <returns>The deserialized object</returns>
        object Deserialize(string json, Type type);
    
        /// <summary>
        /// Deserializes a string to an object
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="json">The serialized string</param>
        /// <returns>The deserialized object</returns>
        T Deserialize<T>(string json);
    }
}