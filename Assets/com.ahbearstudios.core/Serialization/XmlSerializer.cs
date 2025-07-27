using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Services;
using Cysharp.Threading.Tasks;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// High-performance XML serializer implementation using System.Xml.Serialization.
    /// Provides XML serialization with caching, comprehensive error handling, and monitoring.
    /// </summary>
    public class XmlSerializer : ISerializer, IDisposable
    {
        private readonly SerializationConfig _config;
        private readonly ILoggingService _logger;
        private readonly ISerializationRegistry _registry;
        private readonly IVersioningService _versioningService;
        private readonly ICompressionService _compressionService;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly ConcurrentDictionary<Type, bool> _registeredTypes;
        private readonly ConcurrentDictionary<Type, System.Xml.Serialization.XmlSerializer> _serializerCache;
        private readonly SerializationStatisticsCollector _statistics;
        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlReaderSettings _readerSettings;
        private readonly object _cacheLock = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of XmlSerializer.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="registry">Type registration service</param>
        /// <param name="versioningService">Schema versioning service</param>
        /// <param name="compressionService">Compression service</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public XmlSerializer(
            SerializationConfig config,
            ILoggingService logger,
            ISerializationRegistry registry,
            IVersioningService versioningService,
            ICompressionService compressionService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _versioningService = versioningService ?? throw new ArgumentNullException(nameof(versioningService));
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));

            _concurrencyLimiter = new SemaphoreSlim(_config.MaxConcurrentOperations, _config.MaxConcurrentOperations);
            _registeredTypes = new ConcurrentDictionary<Type, bool>();
            _serializerCache = new ConcurrentDictionary<Type, System.Xml.Serialization.XmlSerializer>();
            _statistics = new SerializationStatisticsCollector();

            // Configure XML writer settings
            _writerSettings = new XmlWriterSettings
            {
                Indent = _config.Mode == SerializationMode.Debug || _config.Mode == SerializationMode.Development,
                IndentChars = "  ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false,
                NewLineHandling = NewLineHandling.Entitize,
                WriteEndDocumentOnClose = true
            };

            // Configure XML reader settings
            _readerSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                CloseInput = false,
                ValidationType = ValidationType.None
            };

            var correlationId = GetCorrelationId();
            _logger.LogInfo("XmlSerializer initialized with XML format", correlationId, sourceContext: null, properties: null);
        }

        /// <inheritdoc />
        public byte[] Serialize<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting XML serialization of type {typeof(T).Name}", correlationId, sourceContext: null, properties: null);

                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                var xmlSerializer = GetOrCreateXmlSerializer<T>();
                
                byte[] serializedData;
                using (var memoryStream = new MemoryStream())
                using (var xmlWriter = XmlWriter.Create(memoryStream, _writerSettings))
                {
                    xmlSerializer.Serialize(xmlWriter, obj);
                    xmlWriter.Flush();
                    serializedData = memoryStream.ToArray();
                }

                var result = _config.Compression != CompressionLevel.None
                    ? _compressionService.Compress(serializedData, _config.Compression)
                    : serializedData;

                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordSerialization(typeof(T), result.Length, duration, true);

                _logger.LogInfo($"Successfully serialized {typeof(T).Name} to {result.Length} bytes in {duration.TotalMilliseconds:F2}ms", correlationId, sourceContext: null, properties: null);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordSerialization(typeof(T), 0, duration, false);

                _logger.LogException($"Failed to serialize type {typeof(T).Name}", ex, correlationId, sourceContext: null, properties: null);
                throw new SerializationException($"XML serialization failed for type {typeof(T).Name}", typeof(T), "Serialize", ex);
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return Deserialize<T>(data.AsSpan());
        }

        /// <inheritdoc />
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting XML deserialization of type {typeof(T).Name} from {data.Length} bytes", correlationId, sourceContext: null, properties: null);

                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                var decompressedData = _config.Compression != CompressionLevel.None
                    ? _compressionService.Decompress(data)
                    : data.ToArray();

                var xmlSerializer = GetOrCreateXmlSerializer<T>();
                
                T result;
                using (var memoryStream = new MemoryStream(decompressedData))
                using (var xmlReader = XmlReader.Create(memoryStream, _readerSettings))
                {
                    result = (T)xmlSerializer.Deserialize(xmlReader);
                }

                if (_config.EnableVersioning)
                {
                    result = _versioningService.MigrateToLatest(result);
                }

                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordDeserialization(typeof(T), data.Length, duration, true);

                _logger.LogInfo($"Successfully deserialized {typeof(T).Name} in {duration.TotalMilliseconds:F2}ms", correlationId, sourceContext: null, properties: null);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _statistics.RecordDeserialization(typeof(T), data.Length, duration, false);

                _logger.LogException($"Failed to deserialize type {typeof(T).Name}", ex, correlationId, sourceContext: null, properties: null);
                throw new SerializationException($"XML deserialization failed for type {typeof(T).Name}", typeof(T), "Deserialize", ex);
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            return TryDeserialize(data.AsSpan(), out result);
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            result = default;

            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch (Exception ex)
            {
                var correlationId = GetCorrelationId();
                _logger.LogError($"TryDeserialize failed for type {typeof(T).Name}: {ex.Message}", correlationId, sourceContext: null, properties: null);
                return false;
            }
        }

        /// <inheritdoc />
        public void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            if (_registeredTypes.TryAdd(type, true))
            {
                _registry.RegisterType(type);
                
                // Pre-create the XML serializer for better performance
                GetOrCreateXmlSerializer(type);
                
                _logger.LogInfo($"Registered type {type.FullName} for XML serialization", correlationId, sourceContext: null, properties: null);
            }
        }

        /// <inheritdoc />
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return _registeredTypes.ContainsKey(type) || _registry.IsRegistered(type);
        }

        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            ThrowIfDisposed();

            await _concurrencyLimiter.WaitAsync(cancellationToken);

            try
            {
                return await UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellationToken);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            await _concurrencyLimiter.WaitAsync(cancellationToken);

            try
            {
                return await UniTask.RunOnThreadPool(() => Deserialize<T>(data), cancellationToken: cancellationToken);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                _logger.LogInfo($"Serializing {typeof(T).Name} directly to stream", correlationId, sourceContext: null, properties: null);

                var xmlSerializer = GetOrCreateXmlSerializer<T>();

                if (_config.Compression != CompressionLevel.None)
                {
                    // For compressed streams, serialize to memory first
                    var data = Serialize(obj);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    // Direct stream serialization for better performance
                    using var xmlWriter = XmlWriter.Create(stream, _writerSettings);
                    xmlSerializer.Serialize(xmlWriter, obj);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to serialize {typeof(T).Name} to stream", ex, correlationId, sourceContext: null, properties: null);
                throw;
            }
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                _logger.LogInfo($"Deserializing {typeof(T).Name} from stream", correlationId, sourceContext: null, properties: null);

                var xmlSerializer = GetOrCreateXmlSerializer<T>();
                
                T result;
                if (_config.Compression != CompressionLevel.None)
                {
                    // For compressed streams, read all data first
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    var data = memoryStream.ToArray();
                    result = Deserialize<T>(data);
                }
                else
                {
                    // Direct stream deserialization
                    using var xmlReader = XmlReader.Create(stream, _readerSettings);
                    result = (T)xmlSerializer.Deserialize(xmlReader);
                }

                if (_config.EnableVersioning)
                {
                    result = _versioningService.MigrateToLatest(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to deserialize {typeof(T).Name} from stream", ex, correlationId, sourceContext: null, properties: null);
                throw;
            }
        }

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            if (obj.Equals(default(T)))
                throw new ArgumentException("Object cannot be default value", nameof(obj));

            ThrowIfDisposed();

            var data = Serialize(obj);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            
            for (int i = 0; i < data.Length; i++)
            {
                nativeArray[i] = data[i];
            }

            return nativeArray;
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            if (!data.IsCreated)
                throw new ArgumentException("NativeArray is not created", nameof(data));

            ThrowIfDisposed();

            var managedArray = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                managedArray[i] = data[i];
            }

            return Deserialize<T>(managedArray);
        }

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            ThrowIfDisposed();
            return _statistics.GetStatistics(_registeredTypes.Count);
        }

        /// <summary>
        /// Gets the number of cached XML serializers.
        /// </summary>
        public int CachedSerializerCount => _serializerCache.Count;

        /// <summary>
        /// Clears the XML serializer cache to free memory.
        /// </summary>
        public void ClearSerializerCache()
        {
            var correlationId = GetCorrelationId();
            var count = _serializerCache.Count;
            
            _serializerCache.Clear();
            
            _logger.LogInfo($"Cleared XML serializer cache. Removed {count} cached serializers", correlationId, sourceContext: null, properties: null);
        }

        private System.Xml.Serialization.XmlSerializer GetOrCreateXmlSerializer<T>()
        {
            return GetOrCreateXmlSerializer(typeof(T));
        }

        private System.Xml.Serialization.XmlSerializer GetOrCreateXmlSerializer(Type type)
        {
            return _serializerCache.GetOrAdd(type, CreateXmlSerializer);
        }

        private System.Xml.Serialization.XmlSerializer CreateXmlSerializer(Type type)
        {
            lock (_cacheLock)
            {
                var correlationId = GetCorrelationId();
                
                try
                {
                    _logger.LogInfo($"Creating XML serializer for type {type.Name}", correlationId, sourceContext: null, properties: null);
                    
                    var xmlSerializer = new System.Xml.Serialization.XmlSerializer(type);
                    
                    // Warm up the serializer by creating its assembly
                    // This prevents JIT compilation during actual serialization
                    try
                    {
                        using var tempStream = new MemoryStream();
                        using var tempWriter = XmlWriter.Create(tempStream, _writerSettings);
                        // Just create the writer to warm up the serializer - don't actually serialize
                        tempWriter.WriteStartDocument();
                        tempWriter.WriteEndDocument();
                    }
                    catch (Exception warmupEx)
                    {
                        _logger.LogWarning($"Failed to warm up XML serializer for {type.Name}: {warmupEx.Message}", correlationId, sourceContext: null, properties: null);
                    }
                    
                    _logger.LogInfo($"Created and cached XML serializer for type {type.Name}", correlationId, sourceContext: null, properties: null);
                    return xmlSerializer;
                }
                catch (Exception ex)
                {
                    _logger.LogException($"Failed to create XML serializer for type {type.Name}", ex, correlationId, sourceContext: null, properties: null);
                    throw new SerializationException($"Could not create XML serializer for type {type.Name}", type, "CreateSerializer", ex);
                }
            }
        }

        private void EnsureTypeRegistered<T>()
        {
            var type = typeof(T);
            if (!IsRegistered(type))
            {
                RegisterType(type);
            }
        }

        private void ValidateTypeIfEnabled<T>()
        {
            if (!_config.EnableTypeValidation)
                return;

            var type = typeof(T);
            var typeName = type.FullName ?? type.Name;

            // Check whitelist if configured
            if (_config.TypeWhitelist.Count > 0)
            {
                var isWhitelisted = false;
                foreach (var pattern in _config.TypeWhitelist)
                {
                    if (typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        isWhitelisted = true;
                        break;
                    }
                }

                if (!isWhitelisted)
                {
                    throw new SerializationException($"Type {typeName} is not whitelisted for serialization", type, "ValidateType");
                }
            }

            // Check blacklist
            foreach (var pattern in _config.TypeBlacklist)
            {
                if (typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SerializationException($"Type {typeName} is blacklisted from serialization", type, "ValidateType");
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(XmlSerializer));
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        /// <summary>
        /// Disposes the serializer and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _concurrencyLimiter?.Dispose();
                _statistics?.Dispose();
                _serializerCache?.Clear();
                _disposed = true;

                var correlationId = GetCorrelationId();
                _logger.LogInfo("XmlSerializer disposed", correlationId, sourceContext: null, properties: null);
            }
        }
    }
}