using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Services;
using Unity.Collections;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Core.Serialization.Factories
{
    /// <summary>
    /// Refactored factory for creating serializer instances following CLAUDE.md guidelines.
    /// Simple creation only - no lifecycle management, no caching, no IDisposable.
    /// Follows the Builder → Config → Factory → Service pattern.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public class SerializerFactory : ISerializerFactory
    {
        private readonly ILoggingService _logger;
        private readonly ISerializationRegistry _registry;
        private readonly IVersioningService _versioningService;
        private readonly ICompressionService _compressionService;

        /// <summary>
        /// Initializes a new instance of SerializerFactory.
        /// Factory is stateless and focused only on creation.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="registry">Type registration service</param>
        /// <param name="versioningService">Schema versioning service</param>
        /// <param name="compressionService">Compression service</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public SerializerFactory(
            ILoggingService logger,
            ISerializationRegistry registry = null,
            IVersioningService versioningService = null,
            ICompressionService compressionService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry;
            _versioningService = versioningService;
            _compressionService = compressionService;

            _logger.LogInfo("SerializerFactory initialized successfully (refactored)",
                GetCorrelationId(), nameof(SerializerFactory));
        }

        /// <inheritdoc />
        public ISerializer CreateSerializer(SerializationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.IsValid())
                throw new ArgumentException("Invalid serialization configuration", nameof(config));

            var correlationId = GetCorrelationId();
            _logger.LogDebug($"Creating serializer with format: {config.Format}",
                correlationId, nameof(SerializerFactory));

            try
            {
                return config.Format switch
                {
                    SerializationFormat.MemoryPack => CreateMemoryPackSerializer(config, correlationId),
                    SerializationFormat.Binary => CreateBinarySerializer(config, correlationId),
                    SerializationFormat.Json => CreateJsonSerializer(config, correlationId),
                    SerializationFormat.Xml => CreateXmlSerializer(config, correlationId),
                    SerializationFormat.MessagePack => CreateMessagePackSerializer(config, correlationId),
                    SerializationFormat.Protobuf => CreateProtobufSerializer(config, correlationId),
                    _ => throw new NotSupportedException($"Serialization format {config.Format} is not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create serializer for format {config.Format}: {ex.Message}",
                    correlationId, nameof(SerializerFactory));
                throw;
            }
        }

        /// <inheritdoc />
        public ISerializer CreateSerializer(SerializationFormat format)
        {
            var config = new SerializationConfig { Format = format };
            return CreateSerializer(config);
        }

        /// <inheritdoc />
        public bool CanCreateSerializer(SerializationConfig config)
        {
            if (config == null || !config.IsValid())
                return false;

            return config.Format switch
            {
                SerializationFormat.MemoryPack => true,
                SerializationFormat.Binary => true,
                SerializationFormat.Json => true,
                SerializationFormat.Xml => true,
                SerializationFormat.MessagePack => false, // Not implemented yet
                SerializationFormat.Protobuf => false, // Not implemented yet
                _ => false
            };
        }

        /// <inheritdoc />
        public SerializationFormat[] GetSupportedFormats()
        {
            return new[]
            {
                SerializationFormat.MemoryPack,
                SerializationFormat.Binary,
                SerializationFormat.Json,
                SerializationFormat.Xml
                // MessagePack and Protobuf not yet implemented
            };
        }

        #region Private Helper Methods

        private ISerializer CreateMemoryPackSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogDebug("Creating MemoryPack serializer", correlationId, nameof(SerializerFactory));

            // Create the basic serializer
            var serializer = new MemoryPackSerializer(config, _logger, _registry, _versioningService, _compressionService);

            // Apply decorators based on configuration
            return ApplyDecorators(serializer, config, correlationId);
        }

        private ISerializer CreateBinarySerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogDebug("Creating Binary serializer", correlationId, nameof(SerializerFactory));

            var serializer = new BinarySerializer(config, _logger, _registry, _versioningService, _compressionService);
            return ApplyDecorators(serializer, config, correlationId);
        }

        private ISerializer CreateJsonSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogDebug("Creating JSON serializer", correlationId, nameof(SerializerFactory));

            var serializer = new JsonSerializer(config, _logger, _registry, _versioningService, _compressionService);
            return ApplyDecorators(serializer, config, correlationId);
        }

        private ISerializer CreateXmlSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogDebug("Creating XML serializer", correlationId, nameof(SerializerFactory));

            var serializer = new XmlSerializer(config, _logger, _registry, _versioningService, _compressionService);
            return ApplyDecorators(serializer, config, correlationId);
        }

        private ISerializer CreateMessagePackSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogWarning("MessagePack serializer not yet implemented", correlationId, nameof(SerializerFactory));
            throw new NotImplementedException("MessagePack serializer is not yet implemented");
        }

        private ISerializer CreateProtobufSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogWarning("Protobuf serializer not yet implemented", correlationId, nameof(SerializerFactory));
            throw new NotImplementedException("Protobuf serializer is not yet implemented");
        }

        private ISerializer ApplyDecorators(ISerializer baseSerializer, SerializationConfig config,
            FixedString64Bytes correlationId)
        {
            ISerializer current = baseSerializer;

            // Apply validation decorator if enabled
            if (config.EnableTypeValidation)
            {
                _logger.LogDebug("Applying validation decorator", correlationId, nameof(SerializerFactory));
                current = new ValidatingSerializer(current, config, _logger);
            }

            // Apply compression decorator if enabled (CompressingSerializer not yet implemented)
            if (config.Compression != CompressionLevel.None && _compressionService != null)
            {
                _logger.LogWarning($"Compression level {config.Compression} requested but not yet supported",
                    correlationId, nameof(SerializerFactory));
            }

            // Apply encryption decorator if enabled
            if (config.EnableEncryption && !config.EncryptionKey.IsEmpty)
            {
                _logger.LogDebug("Applying encryption decorator", correlationId, nameof(SerializerFactory));
                current = new EncryptedSerializer(current, config.EncryptionKey, _logger);
            }

            // Apply versioning decorator if enabled (VersioningSerializer not yet implemented)
            if (config.EnableVersioning && _versioningService != null)
            {
                _logger.LogWarning("Schema versioning requested but not yet supported",
                    correlationId, nameof(SerializerFactory));
            }

            // Apply performance monitoring decorator if enabled
            if (config.EnablePerformanceMonitoring)
            {
                _logger.LogDebug("Applying performance monitoring decorator", correlationId, nameof(SerializerFactory));
                current = new PerformanceMonitoringSerializer(current, _logger);
            }

            return current;
        }

        private FixedString64Bytes GetCorrelationId()
        {
            // Generate a deterministic correlation ID for factory operations
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId(
                context: "SerializerFactory",
                operation: "CreateSerializer");
            return new FixedString64Bytes(correlationId.ToString("N")[..32]);
        }

        #endregion
    }
}