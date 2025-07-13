using System.Collections.Concurrent;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Factories
{
    /// <summary>
    /// Factory for creating and managing serializer instances.
    /// Provides caching and lifecycle management for optimal performance.
    /// </summary>
    public class SerializerFactory : ISerializerFactory
    {
        private readonly ILoggingService _logger;
        private readonly ISerializationRegistry _registry;
        private readonly IVersioningService _versioningService;
        private readonly ICompressionService _compressionService;
        private readonly ConcurrentDictionary<string, ISerializer> _serializerCache;
        private readonly object _creationLock = new();

        /// <summary>
        /// Initializes a new instance of SerializerFactory.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="registry">Type registration service</param>
        /// <param name="versioningService">Schema versioning service</param>
        /// <param name="compressionService">Compression service</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public SerializerFactory(
            ILoggingService logger,
            ISerializationRegistry registry,
            IVersioningService versioningService,
            ICompressionService compressionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _versioningService = versioningService ?? throw new ArgumentNullException(nameof(versioningService));
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
            
            _serializerCache = new ConcurrentDictionary<string, ISerializer>();

            _logger.LogInfo("SerializerFactory initialized successfully", GetCorrelationId());
        }

        /// <inheritdoc />
        public ISerializer CreateSerializer(SerializationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.IsValid())
                throw new ArgumentException("Invalid serialization configuration", nameof(config));

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"Creating serializer with format: {config.Format}", correlationId);

            try
            {
                return config.Format switch
                {
                    SerializationFormat.MemoryPack => CreateMemoryPackSerializer(config, correlationId),
                    SerializationFormat.Binary => CreateBinarySerializer(config, correlationId),
                    SerializationFormat.Json => CreateJsonSerializer(config, correlationId),
                    _ => throw new NotSupportedException($"Serialization format {config.Format} is not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to create serializer for format {config.Format}", ex, correlationId);
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
        public ISerializer GetOrCreateSerializer(SerializationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var cacheKey = GenerateCacheKey(config);
            
            return _serializerCache.GetOrAdd(cacheKey, _ =>
            {
                lock (_creationLock)
                {
                    // Double-check pattern for thread safety
                    if (_serializerCache.TryGetValue(cacheKey, out var existingSerializer))
                        return existingSerializer;

                    var correlationId = GetCorrelationId();
                    _logger.LogInfo($"Creating new cached serializer with key: {cacheKey}", correlationId);
                    
                    return CreateSerializer(config);
                }
            });
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
                SerializationFormat.Json
            };
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            var correlationId = GetCorrelationId();
            var cacheCount = _serializerCache.Count;
            
            _serializerCache.Clear();
            
            _logger.LogInfo($"Cleared serializer cache. Removed {cacheCount} cached instances", correlationId);
        }

        private ISerializer CreateMemoryPackSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogInfo("Creating MemoryPack serializer", correlationId);
            
            var serializer = new MemoryPackSerializer(
                config,
                _logger,
                _registry,
                _versioningService,
                _compressionService);

            return WrapWithDecorators(serializer, config, correlationId);
        }

        private ISerializer CreateBinarySerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogInfo("Creating Binary serializer", correlationId);
            
            var serializer = new BinarySerializer(
                config,
                _logger,
                _registry,
                _versioningService,
                _compressionService);

            return WrapWithDecorators(serializer, config, correlationId);
        }

        private ISerializer CreateJsonSerializer(SerializationConfig config, FixedString64Bytes correlationId)
        {
            _logger.LogInfo("Creating JSON serializer", correlationId);
            
            var serializer = new JsonSerializer(
                config,
                _logger,
                _registry,
                _versioningService,
                _compressionService);

            return WrapWithDecorators(serializer, config, correlationId);
        }

        private ISerializer WrapWithDecorators(ISerializer baseSerializer, SerializationConfig config, FixedString64Bytes correlationId)
        {
            var serializer = baseSerializer;

            // Add performance monitoring if enabled
            if (config.EnablePerformanceMonitoring)
            {
                _logger.LogInfo("Adding performance monitoring decorator", correlationId);
                serializer = new PerformanceMonitoringSerializer(serializer, _logger);
            }

            // Add encryption if enabled
            if (config.EnableEncryption)
            {
                _logger.LogInfo("Adding encryption decorator", correlationId);
                serializer = new EncryptedSerializer(serializer, config.EncryptionKey, _logger);
            }

            // Add buffer pooling if enabled
            if (config.EnableBufferPooling)
            {
                _logger.LogInfo("Adding buffer pooling decorator", correlationId);
                serializer = new PooledSerializer(serializer, config.MaxBufferPoolSize, _logger);
            }

            // Add type validation if enabled
            if (config.EnableTypeValidation)
            {
                _logger.LogInfo("Adding type validation decorator", correlationId);
                serializer = new ValidatingSerializer(serializer, config, _logger);
            }

            return serializer;
        }

        private string GenerateCacheKey(SerializationConfig config)
        {
            // Generate a unique cache key based on configuration properties
            var keyComponents = new[]
            {
                config.Format.ToString(),
                config.Compression.ToString(),
                config.Mode.ToString(),
                config.EnableTypeValidation.ToString(),
                config.EnablePerformanceMonitoring.ToString(),
                config.EnableBufferPooling.ToString(),
                config.MaxBufferPoolSize.ToString(),
                config.EnableVersioning.ToString(),
                config.StrictVersioning.ToString(),
                config.EnableEncryption.ToString(),
                string.Join(",", config.TypeWhitelist),
                string.Join(",", config.TypeBlacklist)
            };

            return string.Join("|", keyComponents);
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }
}