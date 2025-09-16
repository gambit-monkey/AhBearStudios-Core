using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Serialization.Models;

namespace AhBearStudios.Core.Serialization.Services
{
    /// <summary>
    /// Service interface for coordinating serialization operations with fallback support and circuit breaker integration.
    /// Extracts complex operation logic from SerializationService for better separation of concerns.
    /// Handles format selection, fallback chains, and error recovery for serialization operations.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface ISerializationOperationCoordinator : IDisposable
    {
        #region Synchronous Operations

        /// <summary>
        /// Coordinates serialization of an object with automatic format selection and fallback support.
        /// Handles circuit breaker integration and error recovery.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="preferredFormat">Preferred serialization format</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <returns>Serialized byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
        /// <exception cref="SerializationException">Thrown when all serializers fail</exception>
        byte[] CoordinateSerialize<T>(T obj, SerializationFormat? preferredFormat = null,
            Guid correlationId = default);

        /// <summary>
        /// Coordinates deserialization with format detection and fallback support.
        /// Handles circuit breaker integration and error recovery.
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="data">Serialized data</param>
        /// <param name="preferredFormat">Preferred serialization format</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
        /// <exception cref="SerializationException">Thrown when all deserializers fail</exception>
        T CoordinateDeserialize<T>(byte[] data, SerializationFormat? preferredFormat = null,
            Guid correlationId = default);

        #endregion

        #region Asynchronous Operations

        /// <summary>
        /// Coordinates async serialization with timeout and cancellation support.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="preferredFormat">Preferred serialization format</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing serialized byte array</returns>
        UniTask<byte[]> CoordinateSerializeAsync<T>(T obj, SerializationFormat? preferredFormat = null,
            Guid correlationId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates async deserialization with timeout and cancellation support.
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="data">Serialized data</param>
        /// <param name="preferredFormat">Preferred serialization format</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing deserialized object</returns>
        UniTask<T> CoordinateDeserializeAsync<T>(byte[] data, SerializationFormat? preferredFormat = null,
            Guid correlationId = default, CancellationToken cancellationToken = default);

        #endregion

        #region Burst-Compatible Operations

        /// <summary>
        /// Coordinates serialization to NativeArray for Unity Job System compatibility.
        /// </summary>
        /// <typeparam name="T">Type of object to serialize (must be unmanaged)</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="allocator">Memory allocator for NativeArray</param>
        /// <param name="preferredFormat">Preferred serialization format</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <returns>NativeArray containing serialized data</returns>
        NativeArray<byte> CoordinateSerializeToNativeArray<T>(T obj, Allocator allocator,
            SerializationFormat? preferredFormat = null, Guid correlationId = default) where T : unmanaged;

        /// <summary>
        /// Coordinates deserialization from NativeArray for Unity Job System compatibility.
        /// </summary>
        /// <typeparam name="T">Type to deserialize to (must be unmanaged)</typeparam>
        /// <param name="data">Serialized data in NativeArray</param>
        /// <param name="preferredFormat">Preferred serialization format</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <returns>Deserialized object</returns>
        T CoordinateDeserializeFromNativeArray<T>(NativeArray<byte> data,
            SerializationFormat? preferredFormat = null, Guid correlationId = default) where T : unmanaged;

        #endregion

        #region Format Management

        /// <summary>
        /// Determines the best serialization format for a given type.
        /// </summary>
        /// <typeparam name="T">Type to evaluate</typeparam>
        /// <param name="preferredFormat">Preferred format if available</param>
        /// <returns>Best available serialization format</returns>
        SerializationFormat DetermineBestFormat<T>(SerializationFormat? preferredFormat = null);

        /// <summary>
        /// Detects the serialization format of byte array data.
        /// </summary>
        /// <param name="data">Data to analyze</param>
        /// <returns>Detected format or null if unable to detect</returns>
        SerializationFormat? DetectFormat(byte[] data);

        /// <summary>
        /// Gets the fallback chain for a specific format.
        /// </summary>
        /// <param name="primaryFormat">Primary format to get fallbacks for</param>
        /// <returns>Ordered list of fallback formats</returns>
        SerializationFormat[] GetFallbackChain(SerializationFormat primaryFormat);

        #endregion

        #region Serializer Management

        /// <summary>
        /// Registers a serializer for a specific format with circuit breaker integration.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="serializer">Serializer instance</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RegisterSerializer(SerializationFormat format, ISerializer serializer, Guid correlationId = default);

        /// <summary>
        /// Unregisters a serializer for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if serializer was found and removed</returns>
        bool UnregisterSerializer(SerializationFormat format, Guid correlationId = default);

        /// <summary>
        /// Gets the serializer for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>Serializer instance or null if not available</returns>
        ISerializer GetSerializer(SerializationFormat format);

        /// <summary>
        /// Gets all registered serialization formats.
        /// </summary>
        /// <returns>Collection of registered formats</returns>
        System.Collections.Generic.IReadOnlyCollection<SerializationFormat> GetRegisteredFormats();

        /// <summary>
        /// Initializes serializers using the provided factory and configuration.
        /// This method follows the Factory → Service pattern from CLAUDE.md.
        /// </summary>
        /// <param name="factory">Serializer factory for creating instances</param>
        /// <param name="config">Serialization configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void InitializeSerializers(Factories.ISerializerFactory factory, Configs.SerializationConfig config, Guid correlationId = default);

        #endregion

        #region Circuit Breaker Management

        /// <summary>
        /// Gets the circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <returns>Circuit breaker instance or null if not found</returns>
        AhBearStudios.Core.HealthChecking.CircuitBreaker GetCircuitBreaker(SerializationFormat format);

        /// <summary>
        /// Opens a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="reason">Reason for opening the circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void OpenCircuitBreaker(SerializationFormat format, string reason, Guid correlationId = default);

        /// <summary>
        /// Closes a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">Serialization format</param>
        /// <param name="reason">Reason for closing the circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void CloseCircuitBreaker(SerializationFormat format, string reason, Guid correlationId = default);

        /// <summary>
        /// Resets all circuit breakers.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetAllCircuitBreakers(Guid correlationId = default);

        /// <summary>
        /// Gets circuit breaker statistics for all formats.
        /// </summary>
        /// <returns>Dictionary mapping formats to their circuit breaker statistics</returns>
        System.Collections.Generic.IReadOnlyDictionary<SerializationFormat, AhBearStudios.Core.HealthChecking.Models.CircuitBreakerStatistics> GetCircuitBreakerStatistics();

        #endregion

        #region Health and Status

        /// <summary>
        /// Checks if a specific serialization format is available and healthy.
        /// </summary>
        /// <param name="format">Format to check</param>
        /// <returns>True if format is available and circuit breaker allows requests</returns>
        bool IsFormatAvailable(SerializationFormat format);

        /// <summary>
        /// Gets the health status of all registered formats.
        /// </summary>
        /// <returns>Dictionary mapping formats to their availability status</returns>
        System.Collections.Generic.IReadOnlyDictionary<SerializationFormat, bool> GetFormatHealth();

        #endregion
    }
}