using System.Collections.Generic;
using AhBearStudios.Core.Serialization.Models;
using Unity.Collections;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Core.Serialization.Configs;

/// <summary>
    /// Comprehensive configuration for the serialization system.
    /// Immutable record for thread-safety and performance.
    /// </summary>
    public record SerializationConfig
    {
        /// <summary>
        /// The primary serialization format to use.
        /// </summary>
        public SerializationFormat Format { get; init; } = SerializationFormat.MemoryPack;

        /// <summary>
        /// Compression level for serialized data.
        /// </summary>
        public CompressionLevel Compression { get; init; } = CompressionLevel.Optimal;

        /// <summary>
        /// Current serialization mode.
        /// </summary>
        public SerializationMode Mode { get; init; } = SerializationMode.Production;

        /// <summary>
        /// Whether to enable type validation during serialization.
        /// </summary>
        public bool EnableTypeValidation { get; init; } = true;

        /// <summary>
        /// Whether to enable performance monitoring.
        /// </summary>
        public bool EnablePerformanceMonitoring { get; init; } = true;

        /// <summary>
        /// Whether to enable buffer pooling for memory optimization.
        /// </summary>
        public bool EnableBufferPooling { get; init; } = true;

        /// <summary>
        /// Maximum size of the buffer pool in bytes.
        /// </summary>
        public int MaxBufferPoolSize { get; init; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Whether to enable schema versioning support.
        /// </summary>
        public bool EnableVersioning { get; init; } = true;

        /// <summary>
        /// Whether versioning should be strict (fail on version mismatch).
        /// </summary>
        public bool StrictVersioning { get; init; } = false;

        /// <summary>
        /// Maximum number of concurrent serialization operations.
        /// </summary>
        public int MaxConcurrentOperations { get; init; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Timeout for async serialization operations in milliseconds.
        /// </summary>
        public int AsyncTimeoutMs { get; init; } = 30000; // 30 seconds

        /// <summary>
        /// Whether to enable encryption for sensitive data.
        /// </summary>
        public bool EnableEncryption { get; init; } = false;

        /// <summary>
        /// Encryption key for sensitive data (if encryption enabled).
        /// </summary>
        public FixedString128Bytes EncryptionKey { get; init; }

        /// <summary>
        /// List of type name patterns to whitelist for serialization.
        /// Empty list means all types are allowed.
        /// </summary>
        public IReadOnlyList<string> TypeWhitelist { get; init; } = Array.Empty<string>();

        /// <summary>
        /// List of type name patterns to blacklist from serialization.
        /// </summary>
        public IReadOnlyList<string> TypeBlacklist { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Custom properties for extension scenarios.
        /// </summary>
        public IReadOnlyDictionary<string, object> CustomProperties { get; init; } = 
            new Dictionary<string, object>();

        /// <summary>
        /// Validates the configuration for consistency and correctness.
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool IsValid()
        {
            return MaxBufferPoolSize > 0 &&
                   MaxConcurrentOperations > 0 &&
                   AsyncTimeoutMs > 0 &&
                   (!EnableEncryption || !EncryptionKey.IsEmpty);
        }

        /// <summary>
        /// Creates a copy of the configuration with validation.
        /// </summary>
        /// <returns>Validated configuration copy</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public SerializationConfig Validate()
        {
            if (!IsValid())
            {
                throw new InvalidOperationException("Invalid serialization configuration");
            }
            return this;
        }
    }