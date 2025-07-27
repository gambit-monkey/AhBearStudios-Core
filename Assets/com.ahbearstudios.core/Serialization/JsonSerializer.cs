using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Models;
using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;
using SerializationException = AhBearStudios.Core.Serialization.Models.SerializationException;
using AhBearStudios.Core.Serialization.Services;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// High-performance JSON serializer implementation using Newtonsoft.Json.
    /// Provides human-readable JSON serialization with Unity type support and comprehensive monitoring.
    /// </summary>
    public class JsonSerializer : ISerializer, IDisposable
    {
        private readonly SerializationConfig _config;
        private readonly ILoggingService _logger;
        private readonly ISerializationRegistry _registry;
        private readonly IVersioningService _versioningService;
        private readonly ICompressionService _compressionService;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly ConcurrentDictionary<Type, bool> _registeredTypes;
        private readonly SerializationStatisticsCollector _statistics;
        private readonly JsonSerializerSettings _jsonSettings;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of JsonSerializer.
        /// </summary>
        /// <param name="config">Serialization configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="registry">Type registration service</param>
        /// <param name="versioningService">Schema versioning service</param>
        /// <param name="compressionService">Compression service</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public JsonSerializer(
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
            _statistics = new SerializationStatisticsCollector();

            // Configure Newtonsoft.Json settings
            _jsonSettings = CreateJsonSettings();

            var correlationId = GetCorrelationId();
            _logger.LogInfo("JsonSerializer initialized with Newtonsoft.Json", correlationId, sourceContext: null, properties: null);
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
                _logger.LogInfo($"Starting JSON serialization of type {typeof(T).Name}", correlationId, sourceContext: null, properties: null);

                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                var jsonString = JsonConvert.SerializeObject(obj, _jsonSettings);
                var serializedData = Encoding.UTF8.GetBytes(jsonString);

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

                _logger.LogException($"Failed to serialize type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"JSON serialization failed for type {typeof(T).Name}", typeof(T), "Serialize", ex);
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
                _logger.LogInfo($"Starting JSON deserialization of type {typeof(T).Name} from {data.Length} bytes", correlationId, sourceContext: null, properties: null);

                EnsureTypeRegistered<T>();
                ValidateTypeIfEnabled<T>();

                var decompressedData = _config.Compression != CompressionLevel.None
                    ? _compressionService.Decompress(data)
                    : data.ToArray();

                var jsonString = Encoding.UTF8.GetString(decompressedData);
                var result = JsonConvert.DeserializeObject<T>(jsonString, _jsonSettings);

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

                _logger.LogException($"Failed to deserialize type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"JSON deserialization failed for type {typeof(T).Name}", typeof(T), "Deserialize", ex);
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
                _logger.LogInfo($"Registered type {type.FullName} for JSON serialization", correlationId, sourceContext: null, properties: null);
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

                if (_config.Compression != CompressionLevel.None)
                {
                    // For compressed streams, serialize to memory first
                    var data = Serialize(obj);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    // Direct stream serialization for better performance
                    using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
                    using var jsonWriter = new JsonTextWriter(writer)
                    {
                        Formatting = _jsonSettings.Formatting,
                        Culture = _jsonSettings.Culture,
                        DateFormatHandling = _jsonSettings.DateFormatHandling,
                        DateFormatString = _jsonSettings.DateFormatString,
                        DateTimeZoneHandling = _jsonSettings.DateTimeZoneHandling,
                        FloatFormatHandling = _jsonSettings.FloatFormatHandling,
                        StringEscapeHandling = _jsonSettings.StringEscapeHandling
                    };
                    
                    var newtonsoftSerializer = Newtonsoft.Json.JsonSerializer.Create(_jsonSettings);
                    newtonsoftSerializer.Serialize(jsonWriter, obj);
                    jsonWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to serialize {typeof(T).Name} to stream", ex, correlationId);
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
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
                    using var jsonReader = new JsonTextReader(reader)
                    {
                        Culture = _jsonSettings.Culture,
                        DateFormatString = _jsonSettings.DateFormatString,
                        DateParseHandling = _jsonSettings.DateParseHandling,
                        DateTimeZoneHandling = _jsonSettings.DateTimeZoneHandling,
                        FloatParseHandling = _jsonSettings.FloatParseHandling,
                        MaxDepth = _jsonSettings.MaxDepth
                    };
                    
                    var newtonsoftSerializer = Newtonsoft.Json.JsonSerializer.Create(_jsonSettings);
                    result = newtonsoftSerializer.Deserialize<T>(jsonReader);
                }

                if (_config.EnableVersioning)
                {
                    result = _versioningService.MigrateToLatest(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to deserialize {typeof(T).Name} from stream", ex, correlationId);
                throw;
            }
        }

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            ThrowIfDisposed();

            var data = Serialize(obj);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            
            // Use NativeArray.CopyFrom for better performance
            nativeArray.CopyFrom(data);

            return nativeArray;
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            if (!data.IsCreated)
                throw new ArgumentException("NativeArray is not created", nameof(data));

            ThrowIfDisposed();

            // Use ToArray() for better performance than manual copying
            var managedArray = data.ToArray();
            return Deserialize<T>(managedArray);
        }

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            ThrowIfDisposed();
            return _statistics.GetStatistics(_registeredTypes.Count);
        }

        /// <summary>
        /// Gets the JSON serializer settings being used.
        /// </summary>
        /// <returns>Current JSON serializer settings</returns>
        public JsonSerializerSettings GetJsonSettings()
        {
            return _jsonSettings;
        }

        private JsonSerializerSettings CreateJsonSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // Basic formatting and structure
                Formatting = _config.Mode == SerializationMode.Debug || _config.Mode == SerializationMode.Development 
                    ? Formatting.Indented 
                    : Formatting.None,
                
                // Buffer size optimization
                CheckAdditionalContent = false,
                
                // Naming and case handling
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                
                // Null handling
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                
                // Type handling for better compatibility
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                
                // Date and time handling
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                
                // Numeric handling
                FloatFormatHandling = FloatFormatHandling.String,
                FloatParseHandling = FloatParseHandling.Double,
                
                // Security and depth  
                MaxDepth = 64,
                
                // Performance optimizations
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                
                // Error handling
                Error = (sender, args) =>
                {
                    var correlationId = GetCorrelationId();
                    _logger.LogError($"JSON serialization error: {args.ErrorContext.Error?.Message}", correlationId, sourceContext: null, properties: null);
                    
                    // Mark error as handled to continue processing when possible
                    args.ErrorContext.Handled = true;
                },
                
                // Culture
                Culture = System.Globalization.CultureInfo.InvariantCulture
            };

            // Add Unity type converters
            settings.Converters.Add(new Vector3JsonConverter());
            settings.Converters.Add(new Vector2JsonConverter());
            settings.Converters.Add(new QuaternionJsonConverter());
            settings.Converters.Add(new ColorJsonConverter());
            settings.Converters.Add(new Color32JsonConverter());
            settings.Converters.Add(new BoundsJsonConverter());
            settings.Converters.Add(new RectJsonConverter());
            settings.Converters.Add(new Matrix4x4JsonConverter());

            return settings;
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

            // Check whitelist if configured - optimized pattern matching
            if (_config.TypeWhitelist.Count > 0)
            {
                var isWhitelisted = false;
                foreach (var pattern in _config.TypeWhitelist)
                {
                    // Use more efficient pattern matching
                    if (IsTypeNameMatch(typeName, pattern))
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

            // Check blacklist - optimized pattern matching
            foreach (var pattern in _config.TypeBlacklist)
            {
                if (IsTypeNameMatch(typeName, pattern))
                {
                    throw new SerializationException($"Type {typeName} is blacklisted from serialization", type, "ValidateType");
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonSerializer));
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        private static bool IsTypeNameMatch(string typeName, string pattern)
        {
            // Support both exact matches and wildcard patterns
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern[..^1];
                return typeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }
            return typeName.Equals(pattern, StringComparison.OrdinalIgnoreCase) || 
                   typeName.Contains(pattern, StringComparison.OrdinalIgnoreCase);
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
                _disposed = true;

                var correlationId = GetCorrelationId();
                _logger.LogInfo("JsonSerializer disposed", correlationId, sourceContext: null, properties: null);
            }
        }
    }

    // Unity Type Converters for Newtonsoft.Json

    /// <summary>
    /// JSON converter for Unity Vector3 type using Newtonsoft.Json.
    /// </summary>
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Vector3.zero;
                
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    if (!reader.Read()) break; // Safety check

                    if (reader.Value != null)
                    {
                        switch (propertyName)
                        {
                            case "x":
                                if (float.TryParse(reader.Value.ToString(), out var xVal))
                                    x = xVal;
                                break;
                            case "y":
                                if (float.TryParse(reader.Value.ToString(), out var yVal))
                                    y = yVal;
                                break;
                            case "z":
                                if (float.TryParse(reader.Value.ToString(), out var zVal))
                                    z = zVal;
                                break;
                        }
                    }
                }
            }

            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// JSON converter for Unity Vector2 type using Newtonsoft.Json.
    /// </summary>
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected StartObject token");

            float x = 0, y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = Convert.ToSingle(reader.Value);
                            break;
                        case "y":
                            y = Convert.ToSingle(reader.Value);
                            break;
                    }
                }
            }

            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// JSON converter for Unity Quaternion type using Newtonsoft.Json.
    /// </summary>
    public class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Quaternion.identity;
                
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

            float x = 0, y = 0, z = 0, w = 1;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    if (!reader.Read()) break; // Safety check

                    if (reader.Value != null)
                    {
                        switch (propertyName)
                        {
                            case "x":
                                if (float.TryParse(reader.Value.ToString(), out var xVal))
                                    x = xVal;
                                break;
                            case "y":
                                if (float.TryParse(reader.Value.ToString(), out var yVal))
                                    y = yVal;
                                break;
                            case "z":
                                if (float.TryParse(reader.Value.ToString(), out var zVal))
                                    z = zVal;
                                break;
                            case "w":
                                if (float.TryParse(reader.Value.ToString(), out var wVal))
                                    w = wVal;
                                break;
                        }
                    }
                }
            }

            return new Quaternion(x, y, z, w);
        }
    }

    /// <summary>
    /// JSON converter for Unity Color type using Newtonsoft.Json.
    /// </summary>
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected StartObject token");

            float r = 0, g = 0, b = 0, a = 1;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "r":
                            r = Convert.ToSingle(reader.Value);
                            break;
                        case "g":
                            g = Convert.ToSingle(reader.Value);
                            break;
                        case "b":
                            b = Convert.ToSingle(reader.Value);
                            break;
                        case "a":
                            a = Convert.ToSingle(reader.Value);
                            break;
                    }
                }
            }

            return new Color(r, g, b, a);
        }
    }

    /// <summary>
    /// JSON converter for Unity Color32 type using Newtonsoft.Json.
    /// </summary>
    public class Color32JsonConverter : JsonConverter<Color32>
    {
        public override void WriteJson(JsonWriter writer, Color32 value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color32 ReadJson(JsonReader reader, Type objectType, Color32 existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected StartObject token");

            byte r = 0, g = 0, b = 0, a = 255;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "r":
                            r = Convert.ToByte(reader.Value);
                            break;
                        case "g":
                            g = Convert.ToByte(reader.Value);
                            break;
                        case "b":
                            b = Convert.ToByte(reader.Value);
                            break;
                        case "a":
                            a = Convert.ToByte(reader.Value);
                            break;
                    }
                }
            }

            return new Color32(r, g, b, a);
        }
    }

    /// <summary>
    /// JSON converter for Unity Bounds type using Newtonsoft.Json.
    /// </summary>
    public class BoundsJsonConverter : JsonConverter<Bounds>
    {
        public override void WriteJson(JsonWriter writer, Bounds value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("center");
            serializer.Serialize(writer, value.center);
            writer.WritePropertyName("size");
            serializer.Serialize(writer, value.size);
            writer.WriteEndObject();
        }

        public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected StartObject token");

            Vector3 center = Vector3.zero;
            Vector3 size = Vector3.zero;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "center":
                            center = serializer.Deserialize<Vector3>(reader);
                            break;
                        case "size":
                            size = serializer.Deserialize<Vector3>(reader);
                            break;
                    }
                }
            }

            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// JSON converter for Unity Rect type using Newtonsoft.Json.
    /// </summary>
    public class RectJsonConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("width");
            writer.WriteValue(value.width);
            writer.WritePropertyName("height");
            writer.WriteValue(value.height);
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected StartObject token");

            float x = 0, y = 0, width = 0, height = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value?.ToString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = Convert.ToSingle(reader.Value);
                            break;
                        case "y":
                            y = Convert.ToSingle(reader.Value);
                            break;
                        case "width":
                            width = Convert.ToSingle(reader.Value);
                            break;
                        case "height":
                            height = Convert.ToSingle(reader.Value);
                            break;
                    }
                }
            }

            return new Rect(x, y, width, height);
        }
    }

    /// <summary>
    /// JSON converter for Unity Matrix4x4 type using Newtonsoft.Json.
    /// </summary>
    public class Matrix4x4JsonConverter : JsonConverter<Matrix4x4>
    {
        public override void WriteJson(JsonWriter writer, Matrix4x4 value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.m00); writer.WriteValue(value.m01); writer.WriteValue(value.m02); writer.WriteValue(value.m03);
            writer.WriteValue(value.m10); writer.WriteValue(value.m11); writer.WriteValue(value.m12); writer.WriteValue(value.m13);
            writer.WriteValue(value.m20); writer.WriteValue(value.m21); writer.WriteValue(value.m22); writer.WriteValue(value.m23);
            writer.WriteValue(value.m30); writer.WriteValue(value.m31); writer.WriteValue(value.m32); writer.WriteValue(value.m33);
            writer.WriteEndArray();
        }

        public override Matrix4x4 ReadJson(JsonReader reader, Type objectType, Matrix4x4 existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonException("Expected StartArray token for Matrix4x4");

            var values = new float[16];
            int index = 0;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (index >= 16)
                    throw new JsonException("Too many values for Matrix4x4");

                values[index++] = Convert.ToSingle(reader.Value);
            }

            if (index != 16)
                throw new JsonException("Expected exactly 16 values for Matrix4x4");

            var matrix = new Matrix4x4();
            matrix.m00 = values[0]; matrix.m01 = values[1]; matrix.m02 = values[2]; matrix.m03 = values[3];
            matrix.m10 = values[4]; matrix.m11 = values[5]; matrix.m12 = values[6]; matrix.m13 = values[7];
            matrix.m20 = values[8]; matrix.m21 = values[9]; matrix.m22 = values[10]; matrix.m23 = values[11];
            matrix.m30 = values[12]; matrix.m31 = values[13]; matrix.m32 = values[14]; matrix.m33 = values[15];

            return matrix;
        }
    }
}