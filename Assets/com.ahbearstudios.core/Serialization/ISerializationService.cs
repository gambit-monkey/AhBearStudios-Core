using System.Collections.Generic;
using System.IO;
using System.Threading;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;
using Cysharp.Threading.Tasks;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Primary serialization service interface providing centralized serialization
    /// with circuit breaker protection, fault tolerance, and comprehensive system integration.
    /// Follows the AhBearStudios Core Architecture foundation system pattern.
    /// Designed for Unity game development with Job System and Burst compatibility.
    /// </summary>
    public interface ISerializationService : IDisposable
    {
        // Configuration and runtime state properties
        /// <summary>
        /// Gets the current configuration of the serialization service.
        /// </summary>
        SerializationConfig Configuration { get; }

        /// <summary>
        /// Gets whether the serialization service is enabled.
        /// </summary>
        bool IsEnabled { get; }

        // Core serialization methods with correlation tracking
        /// <summary>
        /// Serializes an object to a byte array with automatic format selection and circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format override</param>
        /// <returns>Serialized byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
        /// <exception cref="SerializationException">Thrown when serialization fails</exception>
        /// <exception cref="CircuitBreakerOpenException">Thrown when all serializers are unavailable</exception>
        byte[] Serialize<T>(T obj, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        /// <summary>
        /// Deserializes a byte array to an object with automatic format detection and circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format hint</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
        /// <exception cref="SerializationException">Thrown when deserialization fails</exception>
        /// <exception cref="CircuitBreakerOpenException">Thrown when all serializers are unavailable</exception>
        T Deserialize<T>(byte[] data, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        /// <summary>
        /// Attempts to serialize an object safely without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="result">The serialized data if successful</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format override</param>
        /// <returns>True if serialization succeeded, false otherwise</returns>
        bool TrySerialize<T>(T obj, out byte[] result, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        /// <summary>
        /// Attempts to deserialize a byte array safely without throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <param name="result">The deserialized object if successful</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format hint</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        bool TryDeserialize<T>(byte[] data, out T result, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        // Async serialization methods with cancellation support
        /// <summary>
        /// Asynchronously serializes an object with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format override</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing serialized byte array</returns>
        UniTask<byte[]> SerializeAsync<T>(T obj, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes a byte array with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format hint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing deserialized object</returns>
        UniTask<T> DeserializeAsync<T>(byte[] data, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null, CancellationToken cancellationToken = default);

        // Stream-based serialization methods
        /// <summary>
        /// Serializes an object directly to a stream with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="stream">The target stream</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format override</param>
        /// <exception cref="ArgumentNullException">Thrown when obj or stream is null</exception>
        void SerializeToStream<T>(T obj, Stream stream, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        /// <summary>
        /// Deserializes an object from a stream with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="stream">The source stream</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format hint</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
        T DeserializeFromStream<T>(Stream stream, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        // Burst-compatible serialization methods for Unity Job System
        /// <summary>
        /// Serializes to a NativeArray for Burst-compatible scenarios with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="allocator">Memory allocator for the NativeArray</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>NativeArray containing serialized data</returns>
        NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator, 
            FixedString64Bytes correlationId = default) where T : unmanaged;

        /// <summary>
        /// Deserializes from a NativeArray for Burst-compatible scenarios with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data in NativeArray</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Deserialized object</returns>
        T DeserializeFromNativeArray<T>(NativeArray<byte> data, 
            FixedString64Bytes correlationId = default) where T : unmanaged;

        // Batch serialization operations for performance
        /// <summary>
        /// Serializes multiple objects in a batch operation with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize</typeparam>
        /// <param name="objects">The objects to serialize</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format override</param>
        /// <returns>Array of serialized byte arrays</returns>
        byte[][] SerializeBatch<T>(IEnumerable<T> objects, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        /// <summary>
        /// Deserializes multiple byte arrays in a batch operation with circuit breaker protection.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="dataArray">The serialized data arrays</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="format">Optional format hint</param>
        /// <returns>Array of deserialized objects</returns>
        T[] DeserializeBatch<T>(IEnumerable<byte[]> dataArray, FixedString64Bytes correlationId = default, 
            SerializationFormat? format = null);

        // Serializer management methods
        /// <summary>
        /// Registers a serializer with the service and creates a circuit breaker for it.
        /// </summary>
        /// <param name="format">The serialization format this serializer handles</param>
        /// <param name="serializer">The serializer instance</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RegisterSerializer(SerializationFormat format, ISerializer serializer, 
            FixedString64Bytes correlationId = default);

        /// <summary>
        /// Unregisters a serializer from the service and disposes its circuit breaker.
        /// </summary>
        /// <param name="format">The serialization format to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if serializer was unregistered</returns>
        bool UnregisterSerializer(SerializationFormat format, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets all registered serialization formats.
        /// </summary>
        /// <returns>Collection of registered formats</returns>
        IReadOnlyCollection<SerializationFormat> GetRegisteredFormats();

        /// <summary>
        /// Gets the serializer for a specific format if available and healthy.
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <returns>Serializer instance or null if not available</returns>
        ISerializer GetSerializer(SerializationFormat format);

        /// <summary>
        /// Checks if a serializer for the specified format is available and healthy.
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <returns>True if serializer is available and circuit breaker is closed</returns>
        bool IsSerializerAvailable(SerializationFormat format);

        // Circuit breaker management
        /// <summary>
        /// Gets the circuit breaker for a specific serialization format.
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <returns>Circuit breaker instance or null if not found</returns>
        ICircuitBreaker GetCircuitBreaker(SerializationFormat format);

        /// <summary>
        /// Gets circuit breaker statistics for all serializers.
        /// </summary>
        /// <returns>Dictionary of format to circuit breaker statistics</returns>
        IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetCircuitBreakerStatistics();

        /// <summary>
        /// Manually opens a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <param name="reason">Reason for opening the circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void OpenCircuitBreaker(SerializationFormat format, string reason, 
            FixedString64Bytes correlationId = default);

        /// <summary>
        /// Manually closes a circuit breaker for a specific format.
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <param name="reason">Reason for closing the circuit breaker</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void CloseCircuitBreaker(SerializationFormat format, string reason, 
            FixedString64Bytes correlationId = default);

        /// <summary>
        /// Resets all circuit breakers to their default state.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetAllCircuitBreakers(FixedString64Bytes correlationId = default);

        // Type registration methods
        /// <summary>
        /// Registers a type for serialization optimization across all available serializers.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RegisterType<T>(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Registers a type for serialization optimization across all available serializers.
        /// </summary>
        /// <param name="type">The type to register</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        void RegisterType(Type type, FixedString64Bytes correlationId = default);

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

        // Format detection and negotiation
        /// <summary>
        /// Detects the serialization format of a byte array.
        /// </summary>
        /// <param name="data">The serialized data</param>
        /// <returns>Detected format or null if unable to detect</returns>
        SerializationFormat? DetectFormat(byte[] data);

        /// <summary>
        /// Gets the best available serialization format based on type and configuration.
        /// </summary>
        /// <typeparam name="T">The type to serialize</typeparam>
        /// <param name="preferredFormat">Preferred format if available</param>
        /// <returns>Best available format</returns>
        SerializationFormat GetBestFormat<T>(SerializationFormat? preferredFormat = null);

        /// <summary>
        /// Gets the fallback chain for a specific format.
        /// </summary>
        /// <param name="primaryFormat">The primary format</param>
        /// <returns>Ordered list of fallback formats</returns>
        IReadOnlyList<SerializationFormat> GetFallbackChain(SerializationFormat primaryFormat);

        // Performance and monitoring methods
        /// <summary>
        /// Gets comprehensive performance and usage statistics for the serialization service.
        /// </summary>
        /// <returns>Serialization statistics including circuit breaker metrics</returns>
        SerializationStatistics GetStatistics();

        /// <summary>
        /// Flushes all buffered operations and performs maintenance on all serializers.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Task representing the flush operation</returns>
        UniTask FlushAsync(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Validates serialization service configuration and all registered serializers.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Validation result</returns>
        Common.Models.ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Clears internal caches and performs maintenance on all components.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void PerformMaintenance(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Performs a health check on the serialization service and all registered serializers.
        /// </summary>
        /// <returns>True if the service and all serializers are healthy, false otherwise</returns>
        bool PerformHealthCheck();

        /// <summary>
        /// Gets detailed health status for all components including circuit breakers.
        /// </summary>
        /// <returns>Dictionary mapping component names to health status</returns>
        IReadOnlyDictionary<string, bool> GetHealthStatus();

        // Configuration management
        /// <summary>
        /// Updates the service configuration at runtime.
        /// </summary>
        /// <param name="newConfig">The new configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <exception cref="ArgumentNullException">Thrown when newConfig is null</exception>
        void UpdateConfiguration(SerializationConfig newConfig, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Sets the enabled state of the serialization service.
        /// </summary>
        /// <param name="enabled">Whether the service should be enabled</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void SetEnabled(bool enabled, FixedString64Bytes correlationId = default);
    }
}