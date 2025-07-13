using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Core interface for high-performance object serialization with MemoryPack integration.
    /// Provides zero-allocation serialization capabilities with comprehensive type safety.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <returns>Serialized byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
        /// <exception cref="SerializationException">Thrown when serialization fails</exception>
        byte[] Serialize<T>(T obj);

        /// <summary>
        /// Deserializes a byte array to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
        /// <exception cref="SerializationException">Thrown when deserialization fails</exception>
        T Deserialize<T>(byte[] data);

        /// <summary>
        /// Deserializes a ReadOnlySpan of bytes to an object of type T.
        /// Zero-allocation variant for performance-critical scenarios.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data span</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="SerializationException">Thrown when deserialization fails</exception>
        T Deserialize<T>(ReadOnlySpan<byte> data);

        /// <summary>
        /// Attempts to deserialize a byte array safely without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <param name="result">The deserialized object if successful</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        bool TryDeserialize<T>(byte[] data, out T result);

        /// <summary>
        /// Attempts to deserialize a ReadOnlySpan safely without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data span</param>
        /// <param name="result">The deserialized object if successful</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result);

        /// <summary>
        /// Registers a type for serialization optimization.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        void RegisterType<T>();

        /// <summary>
        /// Registers a type for serialization optimization.
        /// </summary>
        /// <param name="type">The type to register</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        void RegisterType(Type type);

        /// <summary>
        /// Checks if a type is registered for serialization.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>True if the type is registered</returns>
        bool IsRegistered<T>();

        /// <summary>
        /// Checks if a type is registered for serialization.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is registered</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        bool IsRegistered(Type type);

        /// <summary>
        /// Asynchronously serializes an object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing serialized byte array</returns>
        Task<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes a byte array to an object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing deserialized object</returns>
        Task<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes an object directly to a stream.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="stream">The target stream</param>
        /// <exception cref="ArgumentNullException">Thrown when obj or stream is null</exception>
        void SerializeToStream<T>(T obj, Stream stream);

        /// <summary>
        /// Deserializes an object from a stream.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="stream">The source stream</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
        T DeserializeFromStream<T>(Stream stream);

        /// <summary>
        /// Serializes to a NativeArray for Burst-compatible scenarios.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="allocator">Memory allocator for the NativeArray</param>
        /// <returns>NativeArray containing serialized data</returns>
        NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged;

        /// <summary>
        /// Deserializes from a NativeArray for Burst-compatible scenarios.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data in NativeArray</param>
        /// <returns>Deserialized object</returns>
        T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged;

        /// <summary>
        /// Gets performance and usage statistics for the serializer.
        /// </summary>
        /// <returns>Serialization statistics</returns>
        SerializationStatistics GetStatistics();
    }
}