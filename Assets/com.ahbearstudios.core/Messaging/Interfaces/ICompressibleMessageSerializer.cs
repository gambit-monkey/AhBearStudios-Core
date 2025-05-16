using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a serializer that supports compression
    /// </summary>
    public interface ICompressibleMessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Serializes an object with compression
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <param name="compressionThreshold">The minimum size in bytes before compression is applied</param>
        /// <returns>The serialized string with compression metadata</returns>
        string SerializeCompressed(object obj, int compressionThreshold = 1024);
    
        /// <summary>
        /// Serializes an object with compression
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="compressionThreshold">The minimum size in bytes before compression is applied</param>
        /// <returns>The serialized string with compression metadata</returns>
        string SerializeCompressed<T>(T obj, int compressionThreshold = 1024);
    
        /// <summary>
        /// Deserializes a compressed string to an object
        /// </summary>
        /// <param name="data">The serialized string with compression metadata</param>
        /// <param name="type">The type of object</param>
        /// <returns>The deserialized object</returns>
        object DeserializeCompressed(string data, Type type);
    
        /// <summary>
        /// Deserializes a compressed string to an object
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="data">The serialized string with compression metadata</param>
        /// <returns>The deserialized object</returns>
        T DeserializeCompressed<T>(string data);
    
        /// <summary>
        /// Gets or sets the compressor to use
        /// </summary>
        IMessageCompressor Compressor { get; set; }
    }
}