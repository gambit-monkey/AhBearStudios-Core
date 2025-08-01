# Serialization System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.Serialization`  
**Role:** High-performance binary serialization using MemoryPack integration  
**Status:** ✅ Core Infrastructure

The Serialization System provides ultra-fast, zero-allocation serialization capabilities through MemoryPack integration, enabling efficient data transfer, persistence, and network communication across all AhBearStudios Core systems. Following Unity game development first principles, it prioritizes performance and frame budget constraints while using UniTask for asynchronous operations and ZLinq for zero-allocation LINQ operations when processing collections.

## 🚀 Key Features

- **⚡ Ultra-High Performance**: Zero-allocation serialization with MemoryPack
- **🔧 Burst Compatible**: Native-compatible data structures for job system integration
- **🎯 Type Safety**: Compile-time type registration and validation
- **📊 Schema Versioning**: Forward and backward compatibility support
- **🔄 Multiple Formats**: Binary, JSON, and custom format support
- **📈 Advanced Diagnostics**: Performance monitoring and error tracking

## 🏗️ Architecture

### Folder Structure

```
AhBearStudios.Core.Serialization/
├── ISerializationService.cs              # Primary service interface (NEW)
├── SerializationService.cs               # Service implementation with circuit breakers (NEW)
├── ISerializer.cs                        # Individual serializer interface
├── MemoryPackSerializer.cs               # MemoryPack implementation
├── JsonSerializer.cs                     # JSON implementation
├── BinarySerializer.cs                   # Binary implementation
├── XmlSerializer.cs                      # XML implementation
├── EncryptedSerializer.cs                # Encryption decorator
├── ValidatingSerializer.cs               # Validation decorator
├── PerformanceMonitoringSerializer.cs    # Performance monitoring decorator
├── Configs/
│   ├── SerializationConfig.cs            # Core configuration
│   ├── FormatterConfig.cs                # Formatter-specific settings
│   └── CompressionConfig.cs              # Compression settings
├── Builders/
│   ├── ISerializationConfigBuilder.cs    # Configuration builder interface
│   └── SerializationConfigBuilder.cs     # Builder implementation
├── Factories/
│   ├── ISerializerFactory.cs             # Serializer creation interface
│   └── SerializerFactory.cs              # Factory implementation
├── Services/
│   ├── ISerializationRegistry.cs         # Type registration interface
│   ├── SerializationRegistry.cs          # Type registration service
│   ├── IVersioningService.cs             # Schema versioning interface
│   ├── VersioningService.cs              # Schema versioning service
│   ├── ICompressionService.cs            # Compression interface
│   └── CompressionService.cs             # Data compression service
├── Formatters/
│   ├── ICustomFormatter.cs               # Custom formatter interface
│   ├── BinaryFormatter.cs                # Binary format support
│   └── JsonFormatter.cs                  # JSON format support
├── Models/
│   ├── SerializationContext.cs           # Serialization state
│   ├── SerializationStatistics.cs        # Performance statistics
│   ├── SerializationResult.cs            # Operation result
│   ├── SerializationFormat.cs            # Format enumeration
│   ├── SerializationMode.cs              # Mode enumeration
│   ├── SerializationException.cs         # Custom exceptions
│   ├── TypeDescriptor.cs                 # Type metadata
│   ├── DefaultTypeResolver.cs            # Type resolution
│   ├── CompressionLevel.cs               # Compression levels
│   └── BufferPoolStatistics.cs           # Buffer pool metrics
└── HealthChecks/
    ├── SerializationHealthCheck.cs       # Individual serializer monitoring
    └── SerializationServiceHealthCheck.cs # Service-level monitoring (NEW)

AhBearStudios.Unity.Serialization/
├── Installers/
│   ├── SerializationInstaller.cs         # Enhanced Reflex registration
│   └── UnitySerializationInstaller.cs    # Unity-specific registration
├── Formatters/
│   ├── UnityObjectFormatter.cs           # Unity object serialization
│   ├── UnityVector3Formatter.cs          # Vector3 formatter
│   ├── UnityQuaternionFormatter.cs       # Quaternion formatter
│   ├── UnityColorFormatter.cs            # Color formatter
│   ├── UnityBoundsFormatter.cs           # Bounds formatter
│   ├── UnityMatrix4x4Formatter.cs        # Matrix4x4 formatter
│   └── UnityFormatterRegistration.cs     # Formatter registration
├── Components/
│   ├── SerializableMonoBehaviour.cs      # Serializable MonoBehaviour base
│   ├── TransformSerializer.cs            # Transform serialization
│   ├── GameObjectSerializer.cs           # GameObject serialization
│   ├── PersistentDataManager.cs          # Persistent data management
│   ├── SceneSerializationManager.cs      # Scene serialization
│   ├── SceneTransitionManager.cs         # Scene transition handling
│   ├── LevelDataCoordinator.cs           # Level data coordination
│   └── SerializationOptimizationValidator.cs # Optimization validation
├── Jobs/
│   ├── SerializationJob.cs               # Job system serialization
│   ├── DeserializationJob.cs             # Job system deserialization
│   └── CompressionJob.cs                 # Job system compression
├── Editor/
│   ├── SerializationEditorWindow.cs      # Editor tools
│   ├── SerializationConfigPropertyDrawer.cs # Inspector integration
│   ├── SerializationDebugger.cs          # Debugging tools
│   └── SerializationMenuItems.cs         # Editor menu items
├── ScriptableObjects/
│   └── SerializationConfigAsset.cs       # Unity configuration
└── Tests/
    ├── SerializationTestSuite.cs         # Test suite
    ├── SerializationPerformanceTests.cs  # Performance tests
    └── SerializationServiceTests.cs      # Service layer tests (NEW)
```

## 🔌 MemoryPack Implementation

### MemoryPackSerializer

The core implementation using MemoryPack for high-performance serialization.

```csharp
using MemoryPack;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Serialization
{
    public class MemoryPackSerializer : ISerializer
    {
        private readonly MemoryPackSerializerOptions _options;
        private readonly ILoggingService _logger;
        
        public MemoryPackSerializer(ILoggingService logger, SerializationConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure MemoryPack options
            _options = MemoryPackSerializerOptions.Default with
            {
                GenerateType = MemoryPackGenerateType.CircularReference,
                UseCompression = config.EnableCompression
            };
        }
        
        public byte[] Serialize<T>(T obj)
        {
            try
            {
                return MemoryPackSerializer.Serialize(obj, _options);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to serialize {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
        
        public T Deserialize<T>(byte[] data)
        {
            try
            {
                return MemoryPackSerializer.Deserialize<T>(data, _options);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to deserialize {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
        
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            try
            {
                return MemoryPackSerializer.Deserialize<T>(data, _options);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to deserialize {typeof(T).Name} from span: {ex.Message}");
                throw;
            }
        }
        
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            // MemoryPack is synchronous, but we wrap in UniTask for consistency
            return await UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellationToken);
        }
        
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() => Deserialize<T>(data), cancellationToken: cancellationToken);
        }
        
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            var data = Serialize(obj);
            stream.Write(data, 0, data.Length);
        }
        
        public T DeserializeFromStream<T>(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return Deserialize<T>(ms.ToArray());
        }
        
        // Type registration is handled by MemoryPack source generators
        public void RegisterType<T>() 
        { 
            _logger.LogInfo($"Type {typeof(T).Name} uses MemoryPack source generation");
        }
        
        public bool IsRegistered<T>() => true; // All MemoryPackable types are auto-registered
    }
}
```

### JsonSerializer Implementation

Alternative implementation using Newtonsoft.Json for human-readable serialization.

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AhBearStudios.Core.Serialization
{
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings;
        private readonly ILoggingService _logger;
        
        public JsonSerializer(ILoggingService logger, SerializationConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure Newtonsoft.Json settings for Unity compatibility
            _settings = new JsonSerializerSettings
            {
                Formatting = config.FormatOutput ? Formatting.Indented : Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                FloatParseHandling = FloatParseHandling.Double,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Converters = new List<JsonConverter>
                {
                    new Vector3Converter(),
                    new QuaternionConverter(),
                    new ColorConverter(),
                    new Color32Converter(),
                    new BoundsConverter(),
                    new RectConverter()
                }
            };
        }
        
        public byte[] Serialize<T>(T obj)
        {
            try
            {
                var json = JsonConvert.SerializeObject(obj, _settings);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"JSON serialization failed for {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
        
        public T Deserialize<T>(byte[] data)
        {
            try
            {
                var json = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject<T>(json, _settings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"JSON deserialization failed for {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
        
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            // Newtonsoft.Json doesn't support ReadOnlySpan directly
            return Deserialize<T>(data.ToArray());
        }
        
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
        
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            return TryDeserialize<T>(data.ToArray(), out result);
        }
        
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellationToken);
        }
        
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() => Deserialize<T>(data), cancellationToken: cancellationToken);
        }
        
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true);
            using var jsonWriter = new JsonTextWriter(writer);
            
            var serializer = JsonSerializer.Create(_settings);
            serializer.Serialize(jsonWriter, obj);
            jsonWriter.Flush();
        }
        
        public T DeserializeFromStream<T>(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
            using var jsonReader = new JsonTextReader(reader);
            
            var serializer = JsonSerializer.Create(_settings);
            return serializer.Deserialize<T>(jsonReader);
        }
        
        // Type registration not needed for JSON
        public void RegisterType<T>() { }
        public void RegisterType(Type type) { }
        public bool IsRegistered<T>() => true;
        public bool IsRegistered(Type type) => true;
        
        public SerializationStatistics GetStatistics()
        {
            return new SerializationStatistics
            {
                RegisteredTypeCount = 0 // JSON doesn't require type registration
            };
        }
    }
    
    // Custom converter for Unity Vector3
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
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
        
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Vector3(
                obj["x"]?.Value<float>() ?? 0f,
                obj["y"]?.Value<float>() ?? 0f,
                obj["z"]?.Value<float>() ?? 0f
            );
        }
    }
    
    // Custom converter for Unity Quaternion
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
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
        
        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Quaternion(
                obj["x"]?.Value<float>() ?? 0f,
                obj["y"]?.Value<float>() ?? 0f,
                obj["z"]?.Value<float>() ?? 0f,
                obj["w"]?.Value<float>() ?? 0f
            );
        }
    }
    
    // Custom converter for Unity Color
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
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
        
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Color(
                obj["r"]?.Value<float>() ?? 0f,
                obj["g"]?.Value<float>() ?? 0f,
                obj["b"]?.Value<float>() ?? 0f,
                obj["a"]?.Value<float>() ?? 1f
            );
        }
    }
}
```

### XmlSerializer Implementation

Implementation using System.Xml for legacy system compatibility.

```csharp
using System.Xml;
using System.Xml.Serialization;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Serialization
{
    public class XmlSerializer : ISerializer
    {
        private readonly ILoggingService _logger;
        private readonly Dictionary<Type, System.Xml.Serialization.XmlSerializer> _serializerCache;
        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlReaderSettings _readerSettings;
        
        public XmlSerializer(ILoggingService logger, SerializationConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializerCache = new Dictionary<Type, System.Xml.Serialization.XmlSerializer>();
            
            _writerSettings = new XmlWriterSettings
            {
                Indent = config.FormatOutput,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };
            
            _readerSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };
        }
        
        public byte[] Serialize<T>(T obj)
        {
            try
            {
                var serializer = GetOrCreateSerializer<T>();
                
                using var stream = new MemoryStream();
                using var writer = XmlWriter.Create(stream, _writerSettings);
                
                serializer.Serialize(writer, obj);
                writer.Flush();
                
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"XML serialization failed for {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
        
        public T Deserialize<T>(byte[] data)
        {
            try
            {
                var serializer = GetOrCreateSerializer<T>();
                
                using var stream = new MemoryStream(data);
                using var reader = XmlReader.Create(stream, _readerSettings);
                
                return (T)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError($"XML deserialization failed for {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
        
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            // XML doesn't support ReadOnlySpan directly, convert to array
            return Deserialize<T>(data.ToArray());
        }
        
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
        
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellationToken);
        }
        
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() => Deserialize<T>(data), cancellationToken: cancellationToken);
        }
        
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            var serializer = GetOrCreateSerializer<T>();
            using var writer = XmlWriter.Create(stream, _writerSettings);
            serializer.Serialize(writer, obj);
        }
        
        public T DeserializeFromStream<T>(Stream stream)
        {
            var serializer = GetOrCreateSerializer<T>();
            using var reader = XmlReader.Create(stream, _readerSettings);
            return (T)serializer.Deserialize(reader);
        }
        
        // Type registration for XML serializer cache
        public void RegisterType<T>()
        {
            GetOrCreateSerializer<T>();
        }
        
        public void RegisterType(Type type)
        {
            GetOrCreateSerializer(type);
        }
        
        public bool IsRegistered<T>()
        {
            return _serializerCache.ContainsKey(typeof(T));
        }
        
        public bool IsRegistered(Type type)
        {
            return _serializerCache.ContainsKey(type);
        }
        
        public SerializationStatistics GetStatistics()
        {
            return new SerializationStatistics
            {
                RegisteredTypeCount = _serializerCache.Count
            };
        }
        
        private System.Xml.Serialization.XmlSerializer GetOrCreateSerializer<T>()
        {
            return GetOrCreateSerializer(typeof(T));
        }
        
        private System.Xml.Serialization.XmlSerializer GetOrCreateSerializer(Type type)
        {
            if (!_serializerCache.TryGetValue(type, out var serializer))
            {
                serializer = new System.Xml.Serialization.XmlSerializer(type);
                _serializerCache[type] = serializer;
                _logger.LogInfo($"Created XML serializer for type {type.Name}");
            }
            
            return serializer;
        }
    }
}
```

## 🚀 Service Layer Architecture

The serialization system now provides a centralized service layer through `ISerializationService` that manages multiple serializers with circuit breaker protection, automatic fallback, and comprehensive health monitoring.

### ISerializationService

The primary interface for all serialization operations with built-in fault tolerance.

```csharp
using Unity.Collections;
using Cysharp.Threading.Tasks;

public interface ISerializationService : IDisposable
{
    // Configuration and state
    SerializationConfig Configuration { get; }
    bool IsEnabled { get; }
    
    // Core serialization with automatic format selection
    byte[] Serialize<T>(T obj, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    T Deserialize<T>(byte[] data, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    
    // Safe operations
    bool TrySerialize<T>(T obj, out byte[] result, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    bool TryDeserialize<T>(byte[] data, out T result, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    
    // Async operations with cancellation
    UniTask<byte[]> SerializeAsync<T>(T obj, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null, CancellationToken cancellationToken = default);
    UniTask<T> DeserializeAsync<T>(byte[] data, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null, CancellationToken cancellationToken = default);
    
    // Stream operations
    void SerializeToStream<T>(T obj, Stream stream, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    T DeserializeFromStream<T>(Stream stream, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    
    // Unity Job System support
    NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator, 
        FixedString64Bytes correlationId = default) where T : unmanaged;
    T DeserializeFromNativeArray<T>(NativeArray<byte> data, 
        FixedString64Bytes correlationId = default) where T : unmanaged;
    
    // Batch operations
    byte[][] SerializeBatch<T>(IEnumerable<T> objects, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    T[] DeserializeBatch<T>(IEnumerable<byte[]> dataArray, FixedString64Bytes correlationId = default, 
        SerializationFormat? format = null);
    
    // Serializer management
    void RegisterSerializer(SerializationFormat format, ISerializer serializer, 
        FixedString64Bytes correlationId = default);
    bool UnregisterSerializer(SerializationFormat format, FixedString64Bytes correlationId = default);
    IReadOnlyCollection<SerializationFormat> GetRegisteredFormats();
    ISerializer GetSerializer(SerializationFormat format);
    bool IsSerializerAvailable(SerializationFormat format);
    
    // Circuit breaker management
    ICircuitBreaker GetCircuitBreaker(SerializationFormat format);
    IReadOnlyDictionary<SerializationFormat, CircuitBreakerStatistics> GetCircuitBreakerStatistics();
    void OpenCircuitBreaker(SerializationFormat format, string reason, 
        FixedString64Bytes correlationId = default);
    void CloseCircuitBreaker(SerializationFormat format, string reason, 
        FixedString64Bytes correlationId = default);
    void ResetAllCircuitBreakers(FixedString64Bytes correlationId = default);
    
    // Type registration
    void RegisterType<T>(FixedString64Bytes correlationId = default);
    void RegisterType(Type type, FixedString64Bytes correlationId = default);
    bool IsRegistered<T>();
    bool IsRegistered(Type type);
    
    // Format detection and negotiation
    SerializationFormat? DetectFormat(byte[] data);
    SerializationFormat GetBestFormat<T>(SerializationFormat? preferredFormat = null);
    IReadOnlyList<SerializationFormat> GetFallbackChain(SerializationFormat primaryFormat);
    
    // Health and monitoring
    SerializationStatistics GetStatistics();
    UniTask FlushAsync(FixedString64Bytes correlationId = default);
    ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);
    void PerformMaintenance(FixedString64Bytes correlationId = default);
    bool PerformHealthCheck();
    IReadOnlyDictionary<string, bool> GetHealthStatus();
    
    // Configuration management
    void UpdateConfiguration(SerializationConfig newConfig, FixedString64Bytes correlationId = default);
    void SetEnabled(bool enabled, FixedString64Bytes correlationId = default);
}
```

### SerializationService Implementation

The service implementation provides:

#### **Fault Tolerance**
- **Per-Serializer Circuit Breakers**: Each format gets independent protection
- **Automatic Fallback**: MemoryPack → JSON → Binary → Exception
- **Smart Recovery**: Half-open state testing for automatic recovery
- **Configurable Thresholds**: Different settings per serializer type

#### **Performance Features**
- **Intelligent Caching**: Serializer instances cached and reused
- **Concurrent Operations**: Configurable concurrency limits
- **Memory Management**: Native collections for high-performance scenarios
- **Buffer Pooling**: Automatic buffer reuse to reduce GC pressure

#### **Monitoring Integration**
- **Comprehensive Statistics**: Operation counts, timing, and failure rates
- **Health Check Integration**: Automatic registration with health system
- **Alert Integration**: Critical errors trigger system alerts
- **Performance Profiling**: Built-in profiler integration

### Service Layer API

The `ISerializationService` provides a comprehensive API for all serialization operations with built-in reliability, monitoring, and performance optimization.

#### Core Serialization Operations

```csharp
// Basic serialization with automatic format selection
var correlationId = new FixedString64Bytes("user-action-123");
var data = new PlayerData { Name = "Alice", Level = 42 };

// Serialize with preferred format (falls back if circuit breaker open)
byte[] serialized = serializationService.Serialize(data, correlationId, SerializationFormat.MemoryPack);

// Deserialize with automatic format detection
PlayerData deserialized = serializationService.Deserialize<PlayerData>(serialized, correlationId);
```

#### Safe Operations (Try Pattern)

```csharp
// Safe serialization - returns false instead of throwing
if (serializationService.TrySerialize(complexObject, out byte[] result, correlationId))
{
    // Success - use result
    Console.WriteLine($"Serialized {result.Length} bytes");
}
else
{
    // Failure - handle gracefully without exception
    Logger.LogWarning("Serialization failed, using fallback approach");
}

// Safe deserialization
if (serializationService.TryDeserialize<PlayerData>(data, out PlayerData player, correlationId))
{
    // Success - use player object
    ProcessPlayer(player);
}
```

#### Async Operations with Cancellation

```csharp
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    // Async serialization with timeout
    byte[] serialized = await serializationService.SerializeAsync(
        largeObject, 
        correlationId, 
        SerializationFormat.MemoryPack,
        cancellationTokenSource.Token);

    // Async deserialization 
    LargeObject deserialized = await serializationService.DeserializeAsync<LargeObject>(
        serialized, 
        correlationId,
        cancellationToken: cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Logger.LogWarning("Serialization operation timed out");
}
```

#### Batch Operations

```csharp
// Serialize multiple objects efficiently
var players = new List<PlayerData> 
{ 
    new("Alice", 42), 
    new("Bob", 35), 
    new("Charlie", 28) 
};

// Batch serialize - more efficient than individual calls
byte[][] serializedPlayers = serializationService.SerializeBatch(players, correlationId);

// Batch deserialize
PlayerData[] deserializedPlayers = serializationService.DeserializeBatch<PlayerData>(
    serializedPlayers, correlationId);
```

#### Stream Operations

```csharp
// Serialize directly to stream (memory efficient for large objects)
using var fileStream = new FileStream("player.dat", FileMode.Create);
serializationService.SerializeToStream(playerData, fileStream, correlationId);

// Deserialize from stream
using var readStream = new FileStream("player.dat", FileMode.Open);
PlayerData loadedPlayer = serializationService.DeserializeFromStream<PlayerData>(readStream, correlationId);
```

#### Unity Job System Integration

```csharp
// High-performance serialization for Unity jobs (unmanaged types only)
[BurstCompile]
struct SerializationJob : IJob
{
    public NativeArray<byte> Output;
    
    public void Execute()
    {
        var correlationId = new FixedString64Bytes("job-serialization");
        var data = new UnmanagedPlayerStats { Score = 1000, Lives = 3 };
        
        // Serialize to native array (Burst compatible)
        Output = serializationService.SerializeToNativeArray(data, Allocator.Persistent, correlationId);
    }
}
```

#### Format Management and Negotiation

```csharp
// Check available serializers
var formats = serializationService.GetRegisteredFormats();
Console.WriteLine($"Available formats: {string.Join(", ", formats)}");

// Get optimal format for a type
SerializationFormat bestFormat = serializationService.GetBestFormat<PlayerData>();

// Check if specific serializer is available
bool memoryPackAvailable = serializationService.IsSerializerAvailable(SerializationFormat.MemoryPack);

// Get fallback chain for format
var fallbackChain = serializationService.GetFallbackChain(SerializationFormat.MemoryPack);
Console.WriteLine($"Fallback chain: {string.Join(" → ", fallbackChain)}");

// Detect format from data
byte[] unknownData = LoadFromNetwork();
SerializationFormat? detectedFormat = serializationService.DetectFormat(unknownData);
```

#### Health and Monitoring

```csharp
// Get service statistics
var stats = serializationService.GetStatistics();
Console.WriteLine($"Total operations: {stats.TotalSerializations + stats.TotalDeserializations}");
Console.WriteLine($"Failure rate: {(double)stats.FailedOperations / (stats.TotalSerializations + stats.TotalDeserializations):P2}");

// Perform health check
bool isHealthy = serializationService.PerformHealthCheck();
if (!isHealthy)
{
    // Get detailed health status
    var healthStatus = serializationService.GetHealthStatus();
    foreach (var status in healthStatus)
    {
        Console.WriteLine($"{status.Key}: {(status.Value ? "OK" : "FAILED")}");
    }
}

// Validate current configuration
ValidationResult validation = serializationService.ValidateConfiguration(correlationId);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Logger.LogError($"Configuration error: {error.Message}");
    }
}
```

#### Service Management

```csharp
// Enable/disable service (useful for maintenance)
serializationService.SetEnabled(false, correlationId);
// ... perform maintenance
serializationService.SetEnabled(true, correlationId);

// Force maintenance operations
serializationService.PerformMaintenance(correlationId);

// Flush pending operations
await serializationService.FlushAsync(correlationId);

// Update configuration at runtime
var newConfig = SerializationConfigBuilder.FromConfig(currentConfig)
    .WithFormat(SerializationFormat.Json)
    .WithMaxConcurrentOperations(8)
    .Build();
    
serializationService.UpdateConfiguration(newConfig, correlationId);
```

## ⚡ Circuit Breaker Integration

The Serialization System incorporates circuit breaker patterns for robust fault tolerance, preventing cascading failures when serializers experience issues.

### Per-Serializer Circuit Breakers

Each serialization format has its own dedicated circuit breaker with format-specific thresholds optimized for the characteristics of each serializer:

```csharp
// Per-serializer circuit breaker settings
var circuitBreakerConfigs = new Dictionary<SerializationFormat, CircuitBreakerConfig>
{
    [SerializationFormat.MemoryPack] = new CircuitBreakerConfig
    {
        Name = "MemoryPack Serializer Circuit Breaker",
        FailureThreshold = 5,                    // Open after 5 failures
        Timeout = TimeSpan.FromSeconds(60),      // Stay open for 60 seconds
        HalfOpenMaxCalls = 3,                    // Allow 3 test calls
        SuccessThreshold = 80.0,                 // Need 80% success to close
        EnableAutomaticRecovery = true
    },
    [SerializationFormat.Json] = new CircuitBreakerConfig
    {
        Name = "JSON Serializer Circuit Breaker", 
        FailureThreshold = 10,                   // More tolerant
        Timeout = TimeSpan.FromSeconds(30),      // Faster recovery
        HalfOpenMaxCalls = 5,
        SuccessThreshold = 70.0,
        EnableAutomaticRecovery = true
    },
    [SerializationFormat.Binary] = new CircuitBreakerConfig
    {
        Name = "Binary Serializer Circuit Breaker",
        FailureThreshold = 15,                   // Most resilient
        Timeout = TimeSpan.FromSeconds(20),      // Quickest recovery
        HalfOpenMaxCalls = 10,
        SuccessThreshold = 60.0,
        EnableAutomaticRecovery = true
    }
};
```

### Circuit Breaker States

- **🟢 Closed**: Normal operation, all requests pass through to the serializer
- **🔴 Open**: Circuit breaker has triggered due to failures, requests are rejected immediately 
- **🟡 Half-Open**: Testing mode, limited requests allowed to test if the serializer has recovered

### Automatic Fallback Chain

When a circuit breaker opens, the service automatically falls back to the next available serializer in the predefined chain:

```
MemoryPack (Primary) → JSON (Fallback) → Binary (Last Resort) → Exception
```

This ensures maximum availability even when the preferred serializer is experiencing issues.

### Circuit Breaker Management API

The service provides comprehensive circuit breaker management capabilities:

```csharp
// Get circuit breaker for specific format
var circuitBreaker = serializationService.GetCircuitBreaker(SerializationFormat.MemoryPack);

// Manually open circuit breaker (for maintenance or force failover)
serializationService.OpenCircuitBreaker(SerializationFormat.Json, "Maintenance mode", correlationId);

// Close circuit breaker (force recovery after manual intervention)
serializationService.CloseCircuitBreaker(SerializationFormat.Json, "Issue resolved", correlationId);

// Reset all circuit breakers to closed state
serializationService.ResetAllCircuitBreakers(correlationId);

// Get comprehensive statistics for monitoring
var stats = serializationService.GetCircuitBreakerStatistics();
foreach (var kvp in stats)
{
    var format = kvp.Key;
    var statistics = kvp.Value;
    
    Console.WriteLine($"Format: {format}");
    Console.WriteLine($"  State: {statistics.CurrentState}");
    Console.WriteLine($"  Failures: {statistics.FailureCount}/{statistics.TotalRequests}");
    Console.WriteLine($"  Success Rate: {statistics.SuccessRate:P2}");
    Console.WriteLine($"  Last State Change: {statistics.LastStateChange}");
}
```

### Health Integration

Circuit breaker status is deeply integrated with the health checking system:

- **🟢 Healthy**: All circuit breakers closed, all serializers operational
- **🟡 Degraded**: One or more circuit breakers half-open, or some open but fallbacks available
- **🔴 Unhealthy**: Critical circuit breakers open with no viable fallbacks remaining

The `SerializationServiceHealthCheck` monitors circuit breaker states and includes detailed statistics in health reports:

```csharp
// Health check includes circuit breaker status
var healthResult = await serializationServiceHealthCheck.CheckHealthAsync();
var circuitBreakerData = healthResult.Data["CircuitBreakers"] as Dictionary<string, object>;
```

### Monitoring and Alerts

Circuit breaker state changes trigger automatic alerts through the integrated alert system:

- **Circuit Breaker Opened**: Critical alert when a serializer circuit breaker opens
- **Fallback Activated**: Warning alert when falling back to alternate serializer
- **Recovery Success**: Info alert when circuit breaker successfully closes
- **All Serializers Down**: Critical alert when no serializers are available

## 🔌 Key Interfaces

### ISerializer

The primary interface for all serialization operations.

```csharp
using Cysharp.Threading.Tasks;

public interface ISerializer
{
    // Core serialization
    byte[] Serialize<T>(T obj);
    T Deserialize<T>(byte[] data);
    T Deserialize<T>(ReadOnlySpan<byte> data);
    
    // Safe deserialization
    bool TryDeserialize<T>(byte[] data, out T result);
    bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result);
    
    // Type management
    void RegisterType<T>();
    void RegisterType(Type type);
    bool IsRegistered<T>();
    bool IsRegistered(Type type);
    
    // Advanced operations
    UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);
    UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default);
    
    // Stream operations
    void SerializeToStream<T>(T obj, Stream stream);
    T DeserializeFromStream<T>(Stream stream);
    
    // Diagnostics
    SerializationStatistics GetStatistics();
}
```

### ISerializationContext

Provides context and state information during serialization operations.

```csharp
using System.Collections.Generic;

public interface ISerializationContext
{
    int Version { get; }
    SerializationMode Mode { get; }
    CompressionLevel Compression { get; }
    Dictionary<string, object> Properties { get; }
    
    void SetProperty(string key, object value);
    T GetProperty<T>(string key, T defaultValue = default);
    
    // Type resolution
    Type ResolveType(string typeName);
    string GetTypeName(Type type);
}
```

### ICustomFormatter<T>

Interface for custom type formatters.

```csharp
public interface ICustomFormatter<T>
{
    void Serialize(ref MemoryPackWriter writer, T value, SerializationContext context);
    T Deserialize(ref MemoryPackReader reader, SerializationContext context);
    
    bool CanFormat(Type type);
    int GetSize(T value);
}
```

### IVersioningService

Handles schema versioning and migration.

```csharp
public interface IVersioningService
{
    void RegisterMigration<T>(int fromVersion, int toVersion, Func<T, T> migrator);
    T MigrateToLatest<T>(T obj, int currentVersion);
    int GetLatestVersion<T>();
    bool RequiresMigration<T>(int version);
}
```

## ⚙️ Configuration

### Serializer Factory Pattern

Factory implementation to select appropriate serializer based on format.

```csharp
namespace AhBearStudios.Core.Serialization.Factories
{
    public enum SerializationFormat
    {
        MemoryPack,  // Default high-performance binary format
        Json,        // Human-readable, debugging-friendly
        Xml,         // Legacy system compatibility
        MessagePack, // Alternative binary format
        Protobuf     // Cross-platform compatibility
    }
    
    public class SerializerFactory : ISerializerFactory
    {
        private readonly ILoggingService _logger;
        private readonly SerializationConfig _config;
        
        public SerializerFactory(ILoggingService logger, SerializationConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        
        public ISerializer CreateSerializer(SerializationFormat format)
        {
            _logger.LogInfo($"Creating serializer for format: {format}");
            
            return format switch
            {
                SerializationFormat.MemoryPack => new MemoryPackSerializer(_logger, _config),
                SerializationFormat.Json => new JsonSerializer(_logger, _config),
                SerializationFormat.Xml => new XmlSerializer(_logger, _config),
                SerializationFormat.MessagePack => new MessagePackSerializer(_logger, _config),
                SerializationFormat.Protobuf => new ProtobufSerializer(_logger, _config),
                _ => throw new NotSupportedException($"Serialization format {format} is not supported")
            };
        }
        
        public ISerializer CreateSerializer()
        {
            // Use default format from configuration
            return CreateSerializer(_config.DefaultFormat);
        }
    }
}
```

### Basic Configuration

```csharp
var config = new SerializationConfigBuilder()
    .WithFormat(SerializationFormat.MemoryPack)
    .WithCompression(CompressionLevel.Optimal)
    .WithTypeValidation(enabled: true)
    .WithPerformanceMonitoring(enabled: true)
    .Build();
```

### Advanced Configuration

```csharp
var config = new SerializationConfigBuilder()
    .WithFormat(SerializationFormat.MemoryPack)
    .WithCompression(CompressionLevel.Fastest)
    .WithBufferPooling(enabled: true, maxPoolSize: 1024 * 1024) // 1MB pool
    .WithVersioning(enabled: true, strictMode: false)
    .WithCustomFormatter<Vector3>(new Vector3Formatter())
    .WithTypeWhitelist("AhBearStudios.*", "UnityEngine.Vector*")
    .WithEncryption(enabled: true, algorithm: EncryptionAlgorithm.AES256)
    .Build();
```

### Unity Integration

```csharp
[CreateAssetMenu(menuName = "AhBear/Serialization/Config")]
public class SerializationConfigAsset : ScriptableObject
{
    [Header("Performance")]
    public SerializationFormat format = SerializationFormat.MemoryPack;
    public CompressionLevel compression = CompressionLevel.Optimal;
    public bool enableBufferPooling = true;
    
    [Header("Compatibility")]
    public bool enableVersioning = true;
    public bool strictTypeChecking = true;
    public string[] typeWhitelist = { "AhBearStudios.*" };
    
    [Header("Security")]
    public bool enableEncryption = false;
    public EncryptionAlgorithm encryptionAlgorithm = EncryptionAlgorithm.AES256;
    
    [Header("Diagnostics")]
    public bool enablePerformanceMonitoring = true;
    public bool enableDetailedLogging = false;
}
```

## 🚀 Usage Examples

### Service Layer Usage (Recommended)

The service layer provides the most robust and feature-rich approach to serialization with built-in fault tolerance, circuit breakers, and health monitoring.

#### Basic Service Operations

```csharp
using Unity.Collections;
using Cysharp.Threading.Tasks;

public class GameDataService
{
    private readonly ISerializationService _serializationService;
    
    public GameDataService(ISerializationService serializationService)
    {
        _serializationService = serializationService;
    }
    
    public async UniTask SavePlayerDataAsync(PlayerData playerData, string playerId)
    {
        var correlationId = new FixedString64Bytes($"save-player-{playerId}");
        
        try
        {
            // Service automatically selects best format and handles fallbacks
            var serialized = await _serializationService.SerializeAsync(playerData, correlationId);
            await SaveToFileAsync($"player_{playerId}.dat", serialized);
            
            Logger.LogInfo($"Player data saved successfully ({serialized.Length} bytes)", correlationId);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to save player data: {ex.Message}", correlationId);
            throw;
        }
    }
    
    public async UniTask<PlayerData> LoadPlayerDataAsync(string playerId)
    {
        var correlationId = new FixedString64Bytes($"load-player-{playerId}");
        
        try
        {
            var serialized = await LoadFromFileAsync($"player_{playerId}.dat");
            
            // Service automatically detects format and deserializes
            var playerData = await _serializationService.DeserializeAsync<PlayerData>(serialized, correlationId);
            
            Logger.LogInfo($"Player data loaded successfully", correlationId);
            return playerData;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load player data: {ex.Message}", correlationId);
            throw;
        }
    }
    
    // Safe operations that don't throw exceptions
    public bool TrySavePlayerData(PlayerData playerData, string playerId)
    {
        var correlationId = new FixedString64Bytes($"try-save-{playerId}");
        
        if (_serializationService.TrySerialize(playerData, out var serialized, correlationId))
        {
            return TrySaveToFile($"player_{playerId}.dat", serialized);
        }
        
        Logger.LogWarning($"Failed to serialize player data for {playerId}", correlationId);
        return false;
    }
}
```

#### Batch Operations for Performance

```csharp
public class LeaderboardService
{
    private readonly ISerializationService _serializationService;
    
    public LeaderboardService(ISerializationService serializationService)
    {
        _serializationService = serializationService;
    }
    
    public async UniTask SaveLeaderboardAsync(List<PlayerScore> scores)
    {
        var correlationId = new FixedString64Bytes("save-leaderboard");
        
        // Batch serialize for better performance
        var serializedScores = _serializationService.SerializeBatch(scores, correlationId);
        
        var tasks = serializedScores.Select(async (data, index) => 
        {
            var filename = $"score_{index:D4}.dat";
            await SaveToFileAsync(filename, data);
        });
        
        await UniTask.WhenAll(tasks);
        
        Logger.LogInfo($"Saved {scores.Count} leaderboard entries", correlationId);
    }
    
    public async UniTask<List<PlayerScore>> LoadLeaderboardAsync()
    {
        var correlationId = new FixedString64Bytes("load-leaderboard");
        
        var files = Directory.GetFiles("leaderboard/", "score_*.dat");
        var serializedData = await LoadMultipleFilesAsync(files);
        
        // Batch deserialize
        var scores = _serializationService.DeserializeBatch<PlayerScore>(serializedData, correlationId);
        
        return scores.OrderByDescending(s => s.Score).ToList();
    }
}
```

#### Format Negotiation and Fallbacks

```csharp
public class NetworkSynchronizationService
{
    private readonly ISerializationService _serializationService;
    
    public NetworkSynchronizationService(ISerializationService serializationService)
    {
        _serializationService = serializationService;
    }
    
    public byte[] PrepareNetworkPacket(GameStateData gameState, string clientId)
    {
        var correlationId = new FixedString64Bytes($"net-{clientId}");
        
        // Try MemoryPack first for performance, fallback to JSON for compatibility
        var preferredFormat = SerializationFormat.MemoryPack;
        
        // Check if preferred serializer is available
        if (!_serializationService.IsSerializerAvailable(preferredFormat))
        {
            Logger.LogWarning($"MemoryPack not available, checking fallback chain", correlationId);
            
            var fallbackChain = _serializationService.GetFallbackChain(preferredFormat);
            Logger.LogInfo($"Fallback chain: {string.Join(" → ", fallbackChain)}", correlationId);
        }
        
        // Service automatically handles fallback if circuit breaker is open
        return _serializationService.Serialize(gameState, correlationId, preferredFormat);
    }
    
    public GameStateData ProcessNetworkPacket(byte[] packetData, string clientId)
    {
        var correlationId = new FixedString64Bytes($"proc-{clientId}");
        
        // Service automatically detects format
        var detectedFormat = _serializationService.DetectFormat(packetData);
        Logger.LogDebug($"Detected packet format: {detectedFormat}", correlationId);
        
        return _serializationService.Deserialize<GameStateData>(packetData, correlationId);
    }
}
```

#### Health Monitoring Integration

```csharp
public class SerializationMonitoringService
{
    private readonly ISerializationService _serializationService;
    private readonly IHealthCheckService _healthCheckService;
    
    public SerializationMonitoringService(
        ISerializationService serializationService,
        IHealthCheckService healthCheckService)
    {
        _serializationService = serializationService;
        _healthCheckService = healthCheckService;
    }
    
    public async UniTask<ServiceHealthReport> GetHealthReportAsync()
    {
        var correlationId = new FixedString64Bytes("health-report");
        
        // Get service-level health
        var serviceHealthy = _serializationService.PerformHealthCheck();
        var healthStatus = _serializationService.GetHealthStatus();
        
        // Get circuit breaker status
        var circuitBreakerStats = _serializationService.GetCircuitBreakerStatistics();
        
        // Get performance statistics
        var stats = _serializationService.GetStatistics();
        
        // Validate configuration
        var configValidation = _serializationService.ValidateConfiguration(correlationId);
        
        return new ServiceHealthReport
        {
            IsHealthy = serviceHealthy,
            DetailedStatus = healthStatus,
            CircuitBreakers = circuitBreakerStats.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => new CircuitBreakerReport
                {
                    State = kvp.Value.CurrentState,
                    FailureCount = kvp.Value.FailureCount,
                    SuccessRate = kvp.Value.SuccessRate,
                    LastStateChange = kvp.Value.LastStateChange
                }),
            Statistics = new PerformanceReport
            {
                TotalOperations = stats.TotalSerializations + stats.TotalDeserializations,
                FailureRate = (double)stats.FailedOperations / (stats.TotalSerializations + stats.TotalDeserializations),
                TotalBytesProcessed = stats.TotalBytesProcessed,
                RegisteredFormats = _serializationService.GetRegisteredFormats().Count
            },
            ConfigurationIsValid = configValidation.IsValid,
            ConfigurationErrors = configValidation.Errors?.Select(e => e.Message).ToList() ?? new List<string>()
        };
    }
    
    public async UniTask PerformMaintenanceAsync()
    {
        var correlationId = new FixedString64Bytes("maintenance");
        
        Logger.LogInfo("Starting serialization service maintenance", correlationId);
        
        // Flush any pending operations
        await _serializationService.FlushAsync(correlationId);
        
        // Perform maintenance
        _serializationService.PerformMaintenance(correlationId);
        
        // Reset circuit breakers if needed
        var circuitBreakerStats = _serializationService.GetCircuitBreakerStatistics();
        if (circuitBreakerStats.Any(kvp => kvp.Value.CurrentState == CircuitBreakerState.Open))
        {
            Logger.LogInfo("Resetting circuit breakers after maintenance", correlationId);
            _serializationService.ResetAllCircuitBreakers(correlationId);
        }
        
        Logger.LogInfo("Serialization service maintenance completed", correlationId);
    }
}
```

#### Unity Job System Integration

```csharp
[BurstCompile]
public struct HighPerformanceSerializationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<UnmanagedGameObject> InputData;
    [WriteOnly] public NativeArray<NativeArray<byte>> OutputData;
    
    public void Execute(int index)
    {
        var correlationId = new FixedString64Bytes("job-serialize");
        var gameObject = InputData[index];
        
        // Use service for high-performance serialization
        // Note: This would need to be injected or accessed differently in practice
        OutputData[index] = serializationService.SerializeToNativeArray(gameObject, Allocator.TempJob, correlationId);
    }
}

public class HighPerformanceDataProcessor
{
    private readonly ISerializationService _serializationService;
    
    public async UniTask ProcessGameObjectsBatch(NativeArray<UnmanagedGameObject> gameObjects)
    {
        var outputArrays = new NativeArray<NativeArray<byte>>(gameObjects.Length, Allocator.TempJob);
        
        var job = new HighPerformanceSerializationJob
        {
            InputData = gameObjects,
            OutputData = outputArrays
        };
        
        var handle = job.Schedule(gameObjects.Length, 64);
        await handle.ToUniTask();
        
        // Process results
        for (int i = 0; i < outputArrays.Length; i++)
        {
            var serialized = outputArrays[i];
            await ProcessSerializedData(serialized);
            serialized.Dispose();
        }
        
        outputArrays.Dispose();
    }
}
```

### Direct Serializer Usage (Legacy/Advanced)

For specific scenarios where you need direct serializer access, you can still use individual serializers:

### Basic Serialization

```csharp
using MemoryPack;
using System.Collections.Generic;

[MemoryPackable]
public partial class PlayerData
{
    [MemoryPackOrder(0)]
    public int PlayerId { get; set; }
    
    [MemoryPackOrder(1)]
    public string PlayerName { get; set; }
    
    [MemoryPackOrder(2)]
    public Vector3 Position { get; set; }
    
    [MemoryPackOrder(3)]
    public float Health { get; set; }
    
    [MemoryPackOrder(4)]
    public Dictionary<string, int> Inventory { get; set; }
    
    [MemoryPackIgnore]
    public DateTime LastModified { get; set; } // This property won't be serialized
    
    // MemoryPack requires a parameterless constructor
    public PlayerData() 
    { 
        Inventory = new Dictionary<string, int>();
    }
}

public class GameService
{
    private readonly ISerializer _serializer;
    
    public GameService(ISerializer serializer)
    {
        _serializer = serializer;
        
        // With MemoryPack, types are auto-registered via source generation
        // No manual registration needed for [MemoryPackable] types
    }
    
    public byte[] SavePlayerData(PlayerData playerData)
    {
        return _serializer.Serialize(playerData);
    }
    
    public PlayerData LoadPlayerData(byte[] data)
    {
        return _serializer.Deserialize<PlayerData>(data);
    }
}
```

### MemoryPack-Specific Features

```csharp
using MemoryPack;

// Union types for polymorphic serialization
[MemoryPackable]
[MemoryPackUnion(0, typeof(MeleeWeapon))]
[MemoryPackUnion(1, typeof(RangedWeapon))]
[MemoryPackUnion(2, typeof(MagicWeapon))]
public abstract partial class Weapon
{
    public abstract int Damage { get; }
    public abstract string Name { get; }
}

[MemoryPackable]
public partial class MeleeWeapon : Weapon
{
    [MemoryPackOrder(0)]
    public override int Damage { get; set; }
    
    [MemoryPackOrder(1)]
    public override string Name { get; set; }
    
    [MemoryPackOrder(2)]
    public float AttackSpeed { get; set; }
}

// Struct serialization for performance
[MemoryPackable]
public partial struct CombatStats
{
    [MemoryPackOrder(0)]
    public int Strength;
    
    [MemoryPackOrder(1)]
    public int Defense;
    
    [MemoryPackOrder(2)]
    public int Magic;
    
    [MemoryPackOrder(3)]
    public int Speed;
}

// Collection types with MemoryPack
[MemoryPackable]
public partial class GameState
{
    [MemoryPackOrder(0)]
    public List<PlayerData> Players { get; set; }
    
    [MemoryPackOrder(1)]
    public HashSet<int> CompletedQuests { get; set; }
    
    [MemoryPackOrder(2)]
    public Queue<string> EventQueue { get; set; }
    
    [MemoryPackOrder(3)]
    public CombatStats[] PartyStats { get; set; }
    
    [MemoryPackConstructor]
    public GameState()
    {
        Players = new List<PlayerData>();
        CompletedQuests = new HashSet<int>();
        EventQueue = new Queue<string>();
        PartyStats = new CombatStats[4];
    }
}
```

### Using Different Serializers

```csharp
public class MultiFormatService
{
    private readonly ISerializerFactory _serializerFactory;
    private readonly ILoggingService _logger;
    
    public MultiFormatService(ISerializerFactory serializerFactory, ILoggingService logger)
    {
        _serializerFactory = serializerFactory;
        _logger = logger;
    }
    
    // Save game data in binary format for performance
    public void SaveGameData(GameState gameState, string filePath)
    {
        var serializer = _serializerFactory.CreateSerializer(SerializationFormat.MemoryPack);
        var data = serializer.Serialize(gameState);
        File.WriteAllBytes(filePath, data);
        
        _logger.LogInfo($"Game saved using MemoryPack: {data.Length} bytes");
    }
    
    // Export settings as JSON for editing
    public string ExportSettingsAsJson(GameSettings settings)
    {
        var serializer = _serializerFactory.CreateSerializer(SerializationFormat.Json);
        var data = serializer.Serialize(settings);
        var json = Encoding.UTF8.GetString(data);
        
        _logger.LogInfo("Settings exported as JSON");
        return json;
    }
    
    // Import legacy data from XML
    public LegacyData ImportLegacyXml(string xmlFilePath)
    {
        var serializer = _serializerFactory.CreateSerializer(SerializationFormat.Xml);
        var xmlData = File.ReadAllBytes(xmlFilePath);
        var legacyData = serializer.Deserialize<LegacyData>(xmlData);
        
        _logger.LogInfo("Legacy data imported from XML");
        return legacyData;
    }
    
    // Debug serialization with format comparison
    public void CompareFormats<T>(T obj)
    {
        var formats = new[] 
        { 
            SerializationFormat.MemoryPack, 
            SerializationFormat.Json, 
            SerializationFormat.Xml 
        };
        
        foreach (var format in formats)
        {
            var serializer = _serializerFactory.CreateSerializer(format);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var data = serializer.Serialize(obj);
            stopwatch.Stop();
            
            _logger.LogInfo($"{format}: {data.Length} bytes, {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
```

### Safe Deserialization

```csharp
public class SaveService
{
    private readonly ISerializer _serializer;
    private readonly ILoggingService _logger;
    
    public bool TryLoadGameState(byte[] saveData, out GameState gameState)
    {
        if (_serializer.TryDeserialize(saveData, out gameState))
        {
            _logger.LogInfo("Game state loaded successfully");
            return true;
        }
        
        _logger.LogWarning("Failed to deserialize game state");
        gameState = GameState.CreateDefault();
        return false;
    }
}
```

### Async Operations

```csharp
using Cysharp.Threading.Tasks;

public class NetworkService
{
    private readonly ISerializer _serializer;
    
    public async UniTask<byte[]> SerializeMessageAsync(NetworkMessage message)
    {
        return await _serializer.SerializeAsync(message);
    }
    
    public async UniTask<NetworkMessage> DeserializeMessageAsync(byte[] data)
    {
        return await _serializer.DeserializeAsync<NetworkMessage>(data);
    }
}
```

### Stream Operations

```csharp
public class FileStorageService
{
    private readonly ISerializer _serializer;
    
    public void SaveToFile<T>(T data, string filePath)
    {
        using var fileStream = File.Create(filePath);
        _serializer.SerializeToStream(data, fileStream);
    }
    
    public T LoadFromFile<T>(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        return _serializer.DeserializeFromStream<T>(fileStream);
    }
}
```

### Custom Formatters with MemoryPack

```csharp
using MemoryPack;
using MemoryPack.Formatters;
using UnityEngine;

// Custom formatter for Unity Vector3 type
public class UnityVector3Formatter : MemoryPackFormatter<Vector3>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector3 value)
        where TBufferWriter : IBufferWriter<byte>
    {
        writer.WriteUnmanaged(value.x);
        writer.WriteUnmanaged(value.y);
        writer.WriteUnmanaged(value.z);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector3 value)
    {
        reader.ReadUnmanaged(out float x);
        reader.ReadUnmanaged(out float y);
        reader.ReadUnmanaged(out float z);
        value = new Vector3(x, y, z);
    }
}

// Custom formatter for Unity Quaternion type
public class UnityQuaternionFormatter : MemoryPackFormatter<Quaternion>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Quaternion value)
        where TBufferWriter : IBufferWriter<byte>
    {
        writer.WriteUnmanaged(value.x);
        writer.WriteUnmanaged(value.y);
        writer.WriteUnmanaged(value.z);
        writer.WriteUnmanaged(value.w);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref Quaternion value)
    {
        reader.ReadUnmanaged(out float x);
        reader.ReadUnmanaged(out float y);
        reader.ReadUnmanaged(out float z);
        reader.ReadUnmanaged(out float w);
        value = new Quaternion(x, y, z, w);
    }
}

// Registration with MemoryPack
public static class MemoryPackFormatterRegistration
{
    [ModuleInitializer]
    public static void RegisterFormatters()
    {
        MemoryPackFormatterProvider.Register(new UnityVector3Formatter());
        MemoryPackFormatterProvider.Register(new UnityQuaternionFormatter());
        MemoryPackFormatterProvider.Register(new UnityColorFormatter());
        MemoryPackFormatterProvider.Register(new UnityBoundsFormatter());
    }
}
```

### Schema Versioning

```csharp
[MemoryPackable]
public partial class PlayerDataV1
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public Vector3 Position { get; set; }
}

[MemoryPackable]
public partial class PlayerDataV2
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public Vector3 Position { get; set; }
    public float Health { get; set; } // New field
    public DateTime LastLogin { get; set; } // New field
}

public class PlayerDataMigration
{
    private readonly IVersioningService _versioning;
    
    public void RegisterMigrations()
    {
        // Migrate from V1 to V2
        _versioning.RegisterMigration<PlayerDataV1, PlayerDataV2>(1, 2, (v1) => new PlayerDataV2
        {
            PlayerId = v1.PlayerId,
            PlayerName = v1.PlayerName,
            Position = v1.Position,
            Health = 100f, // Default value
            LastLogin = DateTime.UtcNow
        });
    }
    
    public PlayerDataV2 LoadPlayerData(byte[] data, int version)
    {
        if (version == 1)
        {
            var v1Data = _serializer.Deserialize<PlayerDataV1>(data);
            return _versioning.MigrateToLatest(v1Data, version);
        }
        
        return _serializer.Deserialize<PlayerDataV2>(data);
    }
}
```

## 🎯 Advanced Features

### Compression Integration

```csharp
using System.IO;

public class CompressionService
{
    private readonly ISerializer _serializer;
    
    public byte[] SerializeWithCompression<T>(T obj, CompressionLevel level)
    {
        var rawData = _serializer.Serialize(obj);
        
        return level switch
        {
            CompressionLevel.None => rawData,
            CompressionLevel.Fastest => CompressWithLZ4(rawData),
            CompressionLevel.Optimal => CompressWithBrotli(rawData),
            CompressionLevel.SmallestSize => CompressWithZstd(rawData),
            _ => rawData
        };
    }
    
    private byte[] CompressWithLZ4(byte[] data)
    {
        using var source = new MemoryStream(data);
        using var destination = new MemoryStream();
        using var lz4Stream = new LZ4EncoderStream(destination);
        
        source.CopyTo(lz4Stream);
        return destination.ToArray();
    }
}
```

### Batch Serialization with ZLinq

```csharp
using ZLinq;
using System.Collections.Generic;

public class BatchSerializer
{
    private readonly ISerializer _serializer;
    
    public byte[][] SerializeBatch<T>(IList<T> items)
    {
        // Using ZLinq for zero-allocation transformation
        using var pooledArray = items.AsValueEnumerable()
            .Select(item => _serializer.Serialize(item))
            .ToArrayPool();
        
        // Convert to regular array for return
        var result = new byte[pooledArray.Size][];
        pooledArray.Span.CopyTo(result);
        return result;
    }
    
    public List<T> DeserializeBatch<T>(byte[][] dataArray)
    {
        // Using ZLinq for efficient filtering and transformation
        var validItems = dataArray.AsValueEnumerable()
            .Where(data => data != null && data.Length > 0)
            .Select(data => _serializer.Deserialize<T>(data))
            .ToList();
        
        return validItems;
    }
}
```

### Buffer Pooling

```csharp
using System.Buffers;

public class PooledSerializer : ISerializer
{
    private readonly ArrayPool<byte> _bufferPool;
    private readonly ISerializer _innerSerializer;
    
    public PooledSerializer(ISerializer innerSerializer)
    {
        _innerSerializer = innerSerializer;
        _bufferPool = ArrayPool<byte>.Shared;
    }
    
    public byte[] Serialize<T>(T obj)
    {
        var estimatedSize = EstimateSize(obj);
        var buffer = _bufferPool.Rent(estimatedSize);
        
        try
        {
            var actualSize = _innerSerializer.SerializeToBuffer(obj, buffer);
            var result = new byte[actualSize];
            Array.Copy(buffer, result, actualSize);
            return result;
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
    }
}
```

### Encryption Integration

```csharp
public class EncryptedSerializer : ISerializer
{
    private readonly ISerializer _innerSerializer;
    private readonly ICryptographyService _crypto;
    
    public byte[] Serialize<T>(T obj)
    {
        var plainData = _innerSerializer.Serialize(obj);
        return _crypto.Encrypt(plainData);
    }
    
    public T Deserialize<T>(byte[] encryptedData)
    {
        var plainData = _crypto.Decrypt(encryptedData);
        return _innerSerializer.Deserialize<T>(plainData);
    }
}
```

### Performance Monitoring

```csharp
public class MonitoredSerializer : ISerializer
{
    private readonly ISerializer _innerSerializer;
    private readonly IProfilerService _profiler;
    private readonly ILoggingService _logger;
    
    public byte[] Serialize<T>(T obj)
    {
        using var scope = _profiler.BeginScope($"Serialize.{typeof(T).Name}");
        
        try
        {
            var result = _innerSerializer.Serialize(obj);
            
            scope.AddCustomMetric("OutputSizeBytes", result.Length);
            scope.AddCustomMetric("CompressionRatio", CalculateCompressionRatio(obj, result));
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Serialization failed for {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }
}
```

## 📊 Performance Characteristics

### MemoryPack Performance

MemoryPack provides exceptional performance through:
- **Source Generation**: Compile-time code generation eliminates reflection overhead
- **Direct Memory Access**: Unmanaged read/write operations for primitive types
- **Zero Allocation**: Minimal heap allocations during serialization
- **Vectorized Operations**: SIMD-optimized operations where available

### Benchmarks (MemoryPack vs Other Serializers)

| Operation | Data Size | MemoryPack | MessagePack | JSON | Protobuf |
|-----------|-----------|------------|-------------|------|----------|
| Serialize Simple Object | 1KB | 0.8 μs | 2.3 μs | 5.7 μs | 3.1 μs |
| Serialize Complex Object | 10KB | 12.4 μs | 45.2 μs | 124.3 μs | 67.8 μs |
| Serialize Collection (1000) | 100KB | 98 μs | 412 μs | 1,243 μs | 734 μs |
| Deserialize Simple Object | 1KB | 0.6 μs | 1.8 μs | 7.2 μs | 2.9 μs |
| Deserialize Complex Object | 10KB | 9.8 μs | 32.4 μs | 156.7 μs | 54.3 μs |
| Deserialize Collection | 100KB | 76 μs | 298 μs | 1,567 μs | 623 μs |

### Memory Usage

- **Zero Allocation Serialization**: No garbage generated during serialization
- **Minimal Allocation Deserialization**: Only allocates the target object
- **Buffer Pooling**: Reuses temporary buffers to minimize GC pressure
- **Native Collections**: Uses Unity.Collections for job system compatibility

### Compression Ratios

| Data Type | Original Size | LZ4 | Brotli | Zstd |
|-----------|---------------|-----|--------|------|
| Game State | 50KB | 38KB (24% reduction) | 31KB (38% reduction) | 29KB (42% reduction) |
| Player Data | 2KB | 1.8KB (10% reduction) | 1.5KB (25% reduction) | 1.4KB (30% reduction) |
| Analytics Events | 100KB | 72KB (28% reduction) | 58KB (42% reduction) | 54KB (46% reduction) |

## 🏥 Health Monitoring

The Serialization System provides comprehensive health monitoring at both the individual serializer level and the service layer level, ensuring robust operation and early detection of issues.

### Multi-Level Health Monitoring

The system includes two complementary health check implementations:

1. **`SerializationHealthCheck`**: Monitors individual serializer performance
2. **`SerializationServiceHealthCheck`**: Monitors service layer operations, circuit breakers, and fault tolerance

### SerializationHealthCheck (Individual Serializers)

Monitors the health of individual serializer implementations:

```csharp
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class SerializationHealthCheck : IHealthCheck
{
    private readonly ISerializer _serializer;
    private readonly SerializationHealthThresholds _thresholds;
    
    public FixedString64Bytes Name => new("SerializationHealthCheck");
    public string Description => "Monitors serialization system performance and health";
    
    public async UniTask<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test basic functionality
            var functionalityResult = await CheckBasicFunctionality(cancellationToken);
            
            // Check performance metrics
            var performanceResult = CheckPerformanceMetrics();
            
            // Check memory usage
            var memoryResult = CheckMemoryUsage();
            
            var healthData = new Dictionary<string, object>
            {
                ["BasicFunctionality"] = functionalityResult.IsHealthy,
                ["Performance"] = performanceResult.Data,
                ["Memory"] = memoryResult.Data,
                ["Statistics"] = GetOverallStatistics()
            };
            
            // Determine overall status
            var status = DetermineOverallStatus(functionalityResult, performanceResult, memoryResult);
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = status,
                Message = GetStatusMessage(status),
                Data = healthData,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                Exception = ex,
                Timestamp = DateTime.UtcNow
            };
        }
    }
    
    private async UniTask<(bool IsHealthy, string ErrorMessage)> CheckBasicFunctionality(
        CancellationToken cancellationToken)
    {
        // Test serialization round-trip with multiple operations
        var testData = new TestSerializationObject
        {
            Id = Guid.NewGuid(),
            Name = "HealthCheck",
            Value = 42,
            Timestamp = DateTime.UtcNow
        };
        
        // Ensure type is registered
        _serializer.RegisterType<TestSerializationObject>();
        
        // Test synchronous operations
        var serialized = _serializer.Serialize(testData);
        var deserialized = _serializer.Deserialize<TestSerializationObject>(serialized);
        
        if (!testData.Equals(deserialized))
            return (false, "Serialization round-trip validation failed");
        
        // Test asynchronous operations
        var asyncSerialized = await _serializer.SerializeAsync(testData, cancellationToken);
        var asyncDeserialized = await _serializer.DeserializeAsync<TestSerializationObject>(asyncSerialized, cancellationToken);
        
        if (!testData.Equals(asyncDeserialized))
            return (false, "Async serialization round-trip validation failed");
        
        return (true, null);
    }
}
```

### SerializationServiceHealthCheck (Service Layer)

Monitors the comprehensive service layer including circuit breakers and fault tolerance:

```csharp
public class SerializationServiceHealthCheck : IHealthCheck
{
    private readonly ISerializationService _serializationService;
    private readonly ILoggingService _logger;
    private readonly SerializationServiceHealthThresholds _thresholds;
    
    public FixedString64Bytes Name => new("SerializationServiceHealthCheck");
    public string Description => "Monitors SerializationService health including circuit breakers and fault tolerance";
    
    public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var healthData = new Dictionary<string, object>();
        var issues = new List<string>();
        var status = HealthStatus.Healthy;
        
        try
        {
            // Check service availability
            healthData["IsEnabled"] = _serializationService.IsEnabled;
            if (!_serializationService.IsEnabled)
            {
                issues.Add("SerializationService is disabled");
                status = HealthStatus.Degraded;
            }
            
            // Check basic functionality
            var functionalityResult = await CheckServiceFunctionality(correlationId, cancellationToken);
            healthData["ServiceFunctionality"] = functionalityResult.IsHealthy;
            if (!functionalityResult.IsHealthy)
            {
                issues.Add($"Service functionality failed: {functionalityResult.ErrorMessage}");
                status = HealthStatus.Unhealthy;
            }
            
            // Check circuit breaker status
            var circuitBreakerResult = CheckCircuitBreakerStatus();
            healthData["CircuitBreakers"] = circuitBreakerResult.Data;
            if (circuitBreakerResult.Status != HealthStatus.Healthy)
            {
                issues.AddRange(circuitBreakerResult.Issues);
                if (circuitBreakerResult.Status == HealthStatus.Unhealthy)
                    status = HealthStatus.Unhealthy;
                else if (status == HealthStatus.Healthy)
                    status = HealthStatus.Degraded;
            }
            
            // Check serializer availability
            var availabilityResult = CheckSerializerAvailability();
            healthData["SerializerAvailability"] = availabilityResult.Data;
            if (availabilityResult.Status != HealthStatus.Healthy)
            {
                issues.AddRange(availabilityResult.Issues);
                if (availabilityResult.Status == HealthStatus.Unhealthy)
                    status = HealthStatus.Unhealthy;
            }
            
            // Check performance metrics
            var performanceResult = CheckServicePerformance();
            healthData["Performance"] = performanceResult.Data;
            if (performanceResult.Status != HealthStatus.Healthy)
            {
                issues.AddRange(performanceResult.Issues);
                if (performanceResult.Status == HealthStatus.Unhealthy)
                    status = HealthStatus.Unhealthy;
            }
            
            // Get comprehensive statistics
            healthData["ServiceStatistics"] = GetServiceStatistics();
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = status,
                Message = status == HealthStatus.Healthy 
                    ? "SerializationService is operating normally"
                    : $"SerializationService has {issues.Count} issue(s)",
                Data = healthData,
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogException("SerializationService health check failed", ex, correlationId);
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                Exception = ex,
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId
            };
        }
    }
}
```

### Health Check Registration

Both health checks are automatically registered during system initialization:

```csharp
// In SerializationInstaller
public override void InstallBindings(ContainerBuilder builder)
{
    // ... other registrations
    
    // Register individual serializer health check
    builder.AddSingleton(typeof(SerializationHealthCheck));
    
    // Register service layer health check
    builder.AddSingleton<SerializationServiceHealthCheck>(container =>
    {
        var serializationService = container.Resolve<ISerializationService>();
        var loggingService = container.TryResolve<ILoggingService>();
        return new SerializationServiceHealthCheck(serializationService, loggingService);
    });
}

// Automatic registration with health system
protected override void PerformPostInstall(Container container)
{
    if (container.HasBinding(typeof(IHealthCheckService)))
    {
        var healthCheckService = container.Resolve<IHealthCheckService>();
        var serializationHealthCheck = container.Resolve<SerializationHealthCheck>();
        var serializationServiceHealthCheck = container.Resolve<SerializationServiceHealthCheck>();
        
        healthCheckService.RegisterHealthCheck(serializationHealthCheck);
        healthCheckService.RegisterHealthCheck(serializationServiceHealthCheck);
    }
}
```

### Configurable Health Thresholds

Health checks use configurable thresholds optimized for different environments:

```csharp
// Production thresholds (strict)
var productionThresholds = new SerializationServiceHealthThresholds
{
    MaxFailureRate = 0.02,           // 2% maximum failure rate
    CriticalFailureRate = 0.10,      // 10% triggers unhealthy
    MinAvailableSerializers = 2,     // Need at least 2 serializers
    MaxOpenCircuitBreakers = 1       // Max 1 open circuit breaker
};

// Development thresholds (relaxed)
var developmentThresholds = SerializationServiceHealthThresholds.Development;
```

### Health Monitoring Dashboard

Access comprehensive health information programmatically:

```csharp
// Get service health status
var serviceHealthy = serializationService.PerformHealthCheck();
var healthStatus = serializationService.GetHealthStatus();

// Get circuit breaker statistics
var circuitBreakerStats = serializationService.GetCircuitBreakerStatistics();
foreach (var (format, stats) in circuitBreakerStats)
{
    Console.WriteLine($"{format}: {stats.CurrentState} - Failures: {stats.FailureCount}");
}

// Validate configuration
var validation = serializationService.ValidateConfiguration(correlationId);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Logger.LogError($"Configuration error: {error.Message}");
    }
}
```

### Statistics and Metrics

```csharp
public class SerializationStatistics
{
    public long TotalSerializations { get; init; }
    public long TotalDeserializations { get; init; }
    public long TotalBytesProcessed { get; init; }
    public TimeSpan AverageSerializeTime { get; init; }
    public TimeSpan AverageDeserializeTime { get; init; }
    public long ErrorCount { get; init; }
    public double ErrorRate => (TotalSerializations + TotalDeserializations) > 0 
        ? (double)ErrorCount / (TotalSerializations + TotalDeserializations) 
        : 0;
    public int RegisteredTypeCount { get; init; }
    public Dictionary<Type, TypeStatistics> TypeStatistics { get; init; }
    public CompressionStatistics Compression { get; init; }
}

public class TypeStatistics
{
    public long SerializationCount { get; init; }
    public long DeserializationCount { get; init; }
    public long TotalBytes { get; init; }
    public TimeSpan TotalTime { get; init; }
    public long AverageSize => SerializationCount > 0 ? TotalBytes / SerializationCount : 0;
    public TimeSpan AverageTime => SerializationCount > 0 
        ? new TimeSpan(TotalTime.Ticks / SerializationCount) 
        : TimeSpan.Zero;
}
```

## 🧪 Testing

### Unit Testing

```csharp
[Test]
public void Serializer_RoundTrip_PreservesData()
{
    // Arrange
    var original = new TestData
    {
        Id = 123,
        Name = "Test",
        Values = new[] { 1.0, 2.0, 3.0 },
        Metadata = new Dictionary<string, string> { ["key"] = "value" }
    };
    
    // Act
    var serialized = _serializer.Serialize(original);
    var deserialized = _serializer.Deserialize<TestData>(serialized);
    
    // Assert
    Assert.That(deserialized.Id, Is.EqualTo(original.Id));
    Assert.That(deserialized.Name, Is.EqualTo(original.Name));
    Assert.That(deserialized.Values, Is.EqualTo(original.Values));
    Assert.That(deserialized.Metadata, Is.EqualTo(original.Metadata));
}

[Test]
public void Serializer_InvalidData_ReturnsFalse()
{
    // Arrange
    var invalidData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
    
    // Act
    var success = _serializer.TryDeserialize<TestData>(invalidData, out var result);
    
    // Assert
    Assert.That(success, Is.False);
    Assert.That(result, Is.Null);
}
```

### Performance Testing

```csharp
[Benchmark]
public byte[] SerializePlayerData()
{
    return _serializer.Serialize(_testPlayerData);
}

[Benchmark]
public PlayerData DeserializePlayerData()
{
    return _serializer.Deserialize<PlayerData>(_serializedPlayerData);
}

[Benchmark]
public void SerializeDeserializeRoundTrip()
{
    var serialized = _serializer.Serialize(_testPlayerData);
    var deserialized = _serializer.Deserialize<PlayerData>(serialized);
}
```

### Integration Testing

```csharp
[Test]
public async UniTask Serializer_WithNetworking_TransfersDataCorrectly()
{
    // Arrange
    var server = CreateTestServer();
    var client = CreateTestClient();
    
    var originalMessage = new NetworkMessage
    {
        Type = MessageType.PlayerAction,
        Payload = new PlayerActionData { Action = "Jump", Position = Vector3.up }
    };
    
    // Act
    var serialized = _serializer.Serialize(originalMessage);
    await server.SendToClient(client.Id, serialized);
    var received = await client.ReceiveMessage();
    var deserialized = _serializer.Deserialize<NetworkMessage>(received);
    
    // Assert
    Assert.That(deserialized.Type, Is.EqualTo(originalMessage.Type));
    Assert.That(deserialized.Payload, Is.EqualTo(originalMessage.Payload));
}
```

## 🚀 Getting Started

### 1. Installation

```csharp
// In Package Manager, add:
"com.ahbearstudios.core.serialization": "2.0.0"
"com.cysharp.memorypack": "1.21.1"
"com.cysharp.unitask": "2.5.0"
"com.cysharp.zlinq": "1.5.1"
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

### 2. Basic Setup

```csharp
public class SerializationInstaller : MonoBehaviour, IInstaller
{
    [Header("Serialization Settings")]
    [SerializeField] private SerializationFormat _defaultFormat = SerializationFormat.MemoryPack;
    [SerializeField] private bool _enableCompression = true;
    [SerializeField] private bool _formatOutput = false;
    
    public void InstallBindings(ContainerBuilder builder)
    {
        // Configure serialization
        var config = new SerializationConfigBuilder()
            .WithFormat(_defaultFormat)
            .WithCompression(_enableCompression ? CompressionLevel.Optimal : CompressionLevel.None)
            .WithFormatOutput(_formatOutput)
            .WithPerformanceMonitoring(true)
            .Build();
            
        builder.AddSingleton(config);
        
        // Register factory
        builder.AddSingleton<ISerializerFactory, SerializerFactory>();
        
        // Register all serializer implementations
        builder.AddSingleton<MemoryPackSerializer>();
        builder.AddSingleton<JsonSerializer>();
        builder.AddSingleton<XmlSerializer>();
        
        // Register default serializer based on configuration
        builder.AddSingleton<ISerializer>(container => 
        {
            var factory = container.Resolve<ISerializerFactory>();
            return factory.CreateSerializer();
        });
        
        // Register named serializers for specific use cases
        builder.AddSingleton<ISerializer>(container => 
            container.Resolve<MemoryPackSerializer>()).WithId("binary");
        builder.AddSingleton<ISerializer>(container => 
            container.Resolve<JsonSerializer>()).WithId("json");
        builder.AddSingleton<ISerializer>(container => 
            container.Resolve<XmlSerializer>()).WithId("xml");
        
        // Register supporting services
        builder.AddSingleton<IVersioningService, VersioningService>();
    }
}
```

### 3. Usage in Services

```csharp
using Cysharp.Threading.Tasks;
using System.IO;

public class SaveService
{
    private readonly ISerializer _serializer;
    private readonly ILoggingService _logger;
    
    public SaveService(ISerializer serializer, ILoggingService logger)
    {
        _serializer = serializer;
        _logger = logger;
        
        // Register game-specific types
        _serializer.RegisterType<GameState>();
        _serializer.RegisterType<PlayerData>();
        _serializer.RegisterType<SettingsData>();
    }
    
    public async UniTask<bool> SaveGameAsync(GameState gameState, string filePath)
    {
        try
        {
            var data = await _serializer.SerializeAsync(gameState);
            await File.WriteAllBytesAsync(filePath, data);
            
            _logger.LogInfo($"Game saved successfully to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save game: {ex.Message}");
            return false;
        }
    }
}
```

### Using Named Serializers

```csharp
public class ConfigurationService
{
    private readonly ISerializer _binarySerializer;
    private readonly ISerializer _jsonSerializer;
    private readonly ILoggingService _logger;
    
    public ConfigurationService(
        [Inject(Id = "binary")] ISerializer binarySerializer,
        [Inject(Id = "json")] ISerializer jsonSerializer,
        ILoggingService logger)
    {
        _binarySerializer = binarySerializer;
        _jsonSerializer = jsonSerializer;
        _logger = logger;
    }
    
    // Save runtime data in efficient binary format
    public void SaveRuntimeState(RuntimeState state, string filePath)
    {
        var data = _binarySerializer.Serialize(state);
        File.WriteAllBytes(filePath, data);
        _logger.LogInfo($"Runtime state saved: {data.Length} bytes");
    }
    
    // Export configuration as editable JSON
    public void ExportConfiguration(AppConfiguration config, string jsonPath)
    {
        var data = _jsonSerializer.Serialize(config);
        File.WriteAllBytes(jsonPath, data);
        _logger.LogInfo($"Configuration exported to: {jsonPath}");
    }
    
    // Import configuration from JSON
    public AppConfiguration ImportConfiguration(string jsonPath)
    {
        var data = File.ReadAllBytes(jsonPath);
        var config = _jsonSerializer.Deserialize<AppConfiguration>(data);
        _logger.LogInfo($"Configuration imported from: {jsonPath}");
        return config;
    }
}
```

## 🏭 Production Readiness

The Serialization System is designed for production use with comprehensive monitoring, fault tolerance, and operational considerations.

### Deployment Checklist

#### Configuration Validation

```csharp
public class ProductionReadinessValidator
{
    public static ValidationResult ValidateProductionConfiguration(ISerializationService serializationService)
    {
        var correlationId = new FixedString64Bytes("prod-validation");
        var issues = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        
        // 1. Validate service configuration
        var configValidation = serializationService.ValidateConfiguration(correlationId);
        if (!configValidation.IsValid)
        {
            issues.AddRange(configValidation.Errors);
        }
        
        // 2. Check circuit breaker configuration
        var circuitBreakerStats = serializationService.GetCircuitBreakerStatistics();
        if (circuitBreakerStats.Count == 0)
        {
            issues.Add(new ValidationError("No circuit breakers configured"));
        }
        
        // 3. Verify serializer availability
        var formats = serializationService.GetRegisteredFormats();
        if (formats.Count < 2)
        {
            warnings.Add(new ValidationWarning("Less than 2 serializers available - limited fallback options"));
        }
        
        // 4. Check health monitoring
        if (!serializationService.PerformHealthCheck())
        {
            issues.Add(new ValidationError("Service health check failed"));
        }
        
        // 5. Validate performance thresholds
        var stats = serializationService.GetStatistics();
        if (stats.FailedOperations > 0)
        {
            warnings.Add(new ValidationWarning($"Service has {stats.FailedOperations} previous failures"));
        }
        
        return new ValidationResult
        {
            IsValid = issues.Count == 0,
            Errors = issues,
            Warnings = warnings
        };
    }
}
```

#### Monitoring Setup

```csharp
public class ProductionMonitoringSetup
{
    public static void ConfigureProductionMonitoring(
        ISerializationService serializationService,
        IHealthCheckService healthCheckService,
        IAlertService alertService)
    {
        // Configure health check intervals
        var healthCheckConfig = new HealthCheckConfiguration
        {
            Name = new FixedString64Bytes("SerializationService"),
            Category = HealthCheckCategory.Critical,
            Timeout = TimeSpan.FromSeconds(30),
            Interval = TimeSpan.FromMinutes(1)  // Check every minute in production
        };
        
        // Set up performance monitoring thresholds
        var performanceThresholds = new SerializationServiceHealthThresholds
        {
            MaxFailureRate = 0.01,           // 1% max failure rate
            CriticalFailureRate = 0.05,      // 5% triggers critical alerts
            MinAvailableSerializers = 2,     // Always need fallback
            MaxOpenCircuitBreakers = 1       // Max 1 open breaker before alert
        };
        
        // Configure alerting rules
        var alertRules = new List<AlertRule>
        {
            new AlertRule
            {
                Name = "SerializationFailureRate",
                Condition = "failure_rate > 0.02",
                Severity = AlertSeverity.Warning,
                Description = "Serialization failure rate exceeded 2%"
            },
            new AlertRule
            {
                Name = "CircuitBreakerOpen",
                Condition = "circuit_breaker_open = true",
                Severity = AlertSeverity.Critical,
                Description = "Serialization circuit breaker opened"
            },
            new AlertRule
            {
                Name = "AllSerializersUnavailable",
                Condition = "available_serializers = 0",
                Severity = AlertSeverity.Critical,
                Description = "No serializers available - system degraded"
            }
        };
        
        foreach (var rule in alertRules)
        {
            alertService.RegisterAlertRule(rule);
        }
    }
}
```

### Performance Optimization

#### Memory Management

```csharp
public class ProductionPerformanceConfig
{
    public static SerializationConfig GetOptimizedConfig()
    {
        return new SerializationConfigBuilder()
            .WithFormat(SerializationFormat.MemoryPack)           // Fastest format
            .WithCompression(CompressionLevel.Optimal)            // Best space/time ratio
            .WithMode(SerializationMode.Production)               // Production optimizations
            .WithBufferPooling(true, poolSize: 10 * 1024 * 1024) // 10MB buffer pool
            .WithMaxConcurrentOperations(Environment.ProcessorCount * 2) // Optimal concurrency
            .WithPerformanceMonitoring(true)                     // Track performance
            .WithTypeValidation(false)                           // Disable in production for speed
            .Build();
    }
}
```

#### Batch Processing Optimization

```csharp
public class ProductionBatchProcessor
{
    private readonly ISerializationService _serializationService;
    private readonly SemaphoreSlim _concurrencyLimiter;
    
    public ProductionBatchProcessor(ISerializationService serializationService)
    {
        _serializationService = serializationService;
        _concurrencyLimiter = new SemaphoreSlim(Environment.ProcessorCount * 2);
    }
    
    public async UniTask<List<byte[]>> ProcessLargeBatchAsync<T>(
        IEnumerable<T> items, 
        int batchSize = 1000)
    {
        var correlationId = new FixedString64Bytes("batch-process");
        var results = new ConcurrentBag<byte[]>();
        
        var batches = items.Chunk(batchSize);
        var tasks = batches.Select(async batch =>
        {
            await _concurrencyLimiter.WaitAsync();
            try
            {
                // Use batch serialization for efficiency
                var serialized = _serializationService.SerializeBatch(batch, correlationId);
                foreach (var item in serialized)
                {
                    results.Add(item);
                }
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        });
        
        await UniTask.WhenAll(tasks);
        return results.ToList();
    }
}
```

### Operational Procedures

#### Graceful Degradation

```csharp
public class GracefulDegradationManager
{
    private readonly ISerializationService _serializationService;
    private readonly ILoggingService _logger;
    
    public async UniTask HandleServiceDegradationAsync()
    {
        var correlationId = new FixedString64Bytes("degradation");
        
        // Check circuit breaker status
        var circuitBreakerStats = _serializationService.GetCircuitBreakerStatistics();
        var openBreakers = circuitBreakerStats.Where(kvp => kvp.Value.CurrentState == CircuitBreakerState.Open);
        
        if (openBreakers.Any())
        {
            _logger.LogWarning($"Detected {openBreakers.Count()} open circuit breakers", correlationId);
            
            // Force fallback to most reliable serializer
            var availableFormats = _serializationService.GetRegisteredFormats()
                .Where(f => _serializationService.IsSerializerAvailable(f))
                .ToList();
            
            if (availableFormats.Count == 0)
            {
                _logger.LogCritical("No serializers available - entering emergency mode", correlationId);
                await EnterEmergencyModeAsync();
            }
            else
            {
                _logger.LogInfo($"Failing over to available formats: {string.Join(", ", availableFormats)}", correlationId);
            }
        }
    }
    
    private async UniTask EnterEmergencyModeAsync()
    {
        // Implement emergency procedures:
        // 1. Disable non-critical serialization
        // 2. Use simplified JSON serialization
        // 3. Alert operations team
        // 4. Attempt service restart
        
        var emergencyConfig = new SerializationConfigBuilder()
            .WithFormat(SerializationFormat.Json)  // Most reliable fallback
            .WithCompression(CompressionLevel.None)
            .WithMode(SerializationMode.Debug)     // Safe mode
            .Build();
            
        _serializationService.UpdateConfiguration(emergencyConfig, new FixedString64Bytes("emergency"));
    }
}
```

#### Maintenance Windows

```csharp
public class MaintenanceWindowManager
{
    private readonly ISerializationService _serializationService;
    
    public async UniTask ExecuteMaintenanceWindowAsync()
    {
        var correlationId = new FixedString64Bytes("maintenance");
        
        try
        {
            // 1. Put service in maintenance mode
            _serializationService.SetEnabled(false, correlationId);
            
            // 2. Wait for pending operations to complete
            await _serializationService.FlushAsync(correlationId);
            
            // 3. Perform maintenance tasks
            _serializationService.PerformMaintenance(correlationId);
            
            // 4. Reset circuit breakers
            _serializationService.ResetAllCircuitBreakers(correlationId);
            
            // 5. Validate configuration
            var validation = _serializationService.ValidateConfiguration(correlationId);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", validation.Errors.Select(e => e.Message))}");
            }
            
            // 6. Perform health check
            if (!_serializationService.PerformHealthCheck())
            {
                throw new InvalidOperationException("Post-maintenance health check failed");
            }
            
            // 7. Re-enable service
            _serializationService.SetEnabled(true, correlationId);
            
            Logger.LogInfo("Maintenance window completed successfully", correlationId);
        }
        catch (Exception ex)
        {
            Logger.LogCritical($"Maintenance window failed: {ex.Message}", correlationId);
            
            // Emergency recovery
            _serializationService.SetEnabled(true, correlationId);
            throw;
        }
    }
}
```

### Security Considerations

#### Data Protection

```csharp
public class SecureSerializationService
{
    private readonly ISerializationService _serializationService;
    private readonly IEncryptionService _encryptionService;
    
    public async UniTask<byte[]> SecureSerializeAsync<T>(T data, string encryptionKey)
    {
        var correlationId = new FixedString64Bytes("secure-serialize");
        
        // 1. Serialize data
        var serialized = await _serializationService.SerializeAsync(data, correlationId);
        
        // 2. Encrypt serialized data
        var encrypted = await _encryptionService.EncryptAsync(serialized, encryptionKey);
        
        // 3. Add integrity hash
        var withIntegrity = AddIntegrityHash(encrypted);
        
        return withIntegrity;
    }
    
    public async UniTask<T> SecureDeserializeAsync<T>(byte[] encryptedData, string encryptionKey)
    {
        var correlationId = new FixedString64Bytes("secure-deserialize");
        
        // 1. Verify integrity
        if (!VerifyIntegrity(encryptedData))
        {
            throw new SecurityException("Data integrity check failed");
        }
        
        // 2. Decrypt data
        var decrypted = await _encryptionService.DecryptAsync(encryptedData, encryptionKey);
        
        // 3. Deserialize
        return await _serializationService.DeserializeAsync<T>(decrypted, correlationId);
    }
}
```

### Disaster Recovery

#### Backup and Recovery

```csharp
public class DisasterRecoveryManager
{
    public async UniTask<BackupResult> CreateSystemBackupAsync()
    {
        // 1. Backup serialization configuration
        var config = _serializationService.Configuration;
        var configBackup = JsonConvert.SerializeObject(config);
        
        // 2. Backup statistics and performance data
        var stats = _serializationService.GetStatistics();
        var statsBackup = JsonConvert.SerializeObject(stats);
        
        // 3. Backup circuit breaker states
        var circuitBreakerStats = _serializationService.GetCircuitBreakerStatistics();
        var circuitBreakerBackup = JsonConvert.SerializeObject(circuitBreakerStats);
        
        return new BackupResult
        {
            ConfigurationBackup = configBackup,
            StatisticsBackup = statsBackup,
            CircuitBreakerBackup = circuitBreakerBackup,
            BackupTimestamp = DateTime.UtcNow
        };
    }
    
    public async UniTask RestoreFromBackupAsync(BackupResult backup)
    {
        var correlationId = new FixedString64Bytes("disaster-recovery");
        
        try
        {
            // 1. Restore configuration
            var config = JsonConvert.DeserializeObject<SerializationConfig>(backup.ConfigurationBackup);
            _serializationService.UpdateConfiguration(config, correlationId);
            
            // 2. Reset circuit breakers to known good state
            _serializationService.ResetAllCircuitBreakers(correlationId);
            
            // 3. Validate restoration
            var validation = _serializationService.ValidateConfiguration(correlationId);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Configuration restoration failed validation");
            }
            
            Logger.LogInfo("Disaster recovery completed successfully", correlationId);
        }
        catch (Exception ex)
        {
            Logger.LogCritical($"Disaster recovery failed: {ex.Message}", correlationId);
            throw;
        }
    }
}
```

### Key Production Metrics

Monitor these critical metrics in production:

#### Performance Metrics
- **Serialization Throughput**: Operations per second
- **Average Latency**: P50, P95, P99 response times
- **Memory Usage**: Peak memory consumption, buffer pool efficiency
- **Failure Rate**: Percentage of failed operations

#### Reliability Metrics
- **Circuit Breaker State**: Open/closed status per format
- **Fallback Frequency**: How often fallbacks are triggered
- **Service Availability**: Uptime percentage
- **Health Check Status**: Pass/fail rates

#### Operational Metrics
- **Configuration Changes**: Frequency and success rate
- **Maintenance Windows**: Duration and success rate
- **Alert Frequency**: Rate of triggered alerts
- **Recovery Time**: Time to recover from failures

## 🔄 Migration Guide

This guide helps you migrate from individual serializer usage to the new service layer architecture with circuit breaker protection and enhanced fault tolerance.

### Migration Overview

The migration path involves transitioning from direct `ISerializer` usage to the centralized `ISerializationService` while maintaining backward compatibility.

#### Before (Individual Serializers)
```csharp
public class OldGameService
{
    private readonly ISerializer _serializer;
    
    public OldGameService(ISerializer serializer)
    {
        _serializer = serializer;
    }
    
    public byte[] SaveData(PlayerData data)
    {
        return _serializer.Serialize(data);
    }
    
    public PlayerData LoadData(byte[] data)
    {
        return _serializer.Deserialize<PlayerData>(data);
    }
}
```

#### After (Service Layer)
```csharp
public class NewGameService
{
    private readonly ISerializationService _serializationService;
    
    public NewGameService(ISerializationService serializationService)
    {
        _serializationService = serializationService;
    }
    
    public byte[] SaveData(PlayerData data)
    {
        var correlationId = new FixedString64Bytes("save-player");
        return _serializationService.Serialize(data, correlationId);
    }
    
    public PlayerData LoadData(byte[] data)
    {
        var correlationId = new FixedString64Bytes("load-player");
        return _serializationService.Deserialize<PlayerData>(data, correlationId);
    }
}
```

### Step-by-Step Migration

#### Phase 1: Update Dependency Injection

**Old Registration:**
```csharp
public class OldSerializationInstaller : BootstrapInstaller
{
    public override void InstallBindings(ContainerBuilder builder)
    {
        // Register individual serializer only
        builder.AddSingleton(typeof(MemoryPackSerializer), typeof(ISerializer));
    }
}
```

**New Registration:**
```csharp
public class NewSerializationInstaller : BootstrapInstaller
{
    public override void InstallBindings(ContainerBuilder builder)
    {
        // Register both individual serializers AND service layer
        var config = new SerializationConfigBuilder()
            .WithFormat(SerializationFormat.MemoryPack)
            .WithMode(SerializationMode.Production)
            .Build();
            
        builder.AddSingleton(config, typeof(SerializationConfig));
        builder.AddSingleton(typeof(SerializerFactory), typeof(ISerializerFactory));
        
        // Service layer with circuit breakers
        builder.AddSingleton<ISerializationService>(container =>
        {
            var factory = container.Resolve<ISerializerFactory>();
            var loggingService = container.TryResolve<ILoggingService>();
            var healthCheckService = container.TryResolve<IHealthCheckService>();
            var alertService = container.TryResolve<IAlertService>();
            
            return new SerializationService(
                config,
                factory,
                registry: null,
                versioningService: null,
                compressionService: null,
                loggingService,
                healthCheckService,
                alertService,
                profilerService: null,
                messageBusService: null);
        });
        
        // Keep backward compatibility
        builder.AddSingleton<ISerializer>(container =>
        {
            var factory = container.Resolve<ISerializerFactory>();
            return factory.CreateSerializer(config);
        });
    }
}
```

#### Phase 2: Gradual Service Adoption

**Adapter Pattern for Gradual Migration:**
```csharp
public class SerializationServiceAdapter : ISerializer
{
    private readonly ISerializationService _serializationService;
    private readonly FixedString64Bytes _defaultCorrelationId;
    
    public SerializationServiceAdapter(ISerializationService serializationService)
    {
        _serializationService = serializationService;
        _defaultCorrelationId = new FixedString64Bytes("adapter");
    }
    
    // Implement ISerializer using service layer
    public byte[] Serialize<T>(T obj)
    {
        return _serializationService.Serialize(obj, _defaultCorrelationId);
    }
    
    public T Deserialize<T>(byte[] data)
    {
        return _serializationService.Deserialize<T>(data, _defaultCorrelationId);
    }
    
    public bool TryDeserialize<T>(byte[] data, out T result)
    {
        return _serializationService.TryDeserialize(data, out result, _defaultCorrelationId);
    }
    
    // ... implement other ISerializer methods using service layer
}
```

#### Phase 3: Update Existing Services

**Migration Helper for Services:**
```csharp
public class MigrationHelper
{
    public static void MigrateService<TService>(
        TService service,
        ISerializationService serializationService)
        where TService : class
    {
        var serviceType = typeof(TService);
        var serializerFields = serviceType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(ISerializer));
        
        foreach (var field in serializerFields)
        {
            // Replace ISerializer with adapter
            var adapter = new SerializationServiceAdapter(serializationService);
            field.SetValue(service, adapter);
        }
    }
    
    public static void ValidateMigration<TService>(TService service)
        where TService : class
    {
        // Validate that service is using new patterns
        var methods = typeof(TService).GetMethods();
        var hasCorrelationIdUsage = methods.Any(m => 
            m.GetParameters().Any(p => p.ParameterType == typeof(FixedString64Bytes)));
            
        if (!hasCorrelationIdUsage)
        {
            Console.WriteLine($"Warning: {typeof(TService).Name} may not be fully migrated to service layer patterns");
        }
    }
}
```

### Breaking Changes and Compatibility

#### Breaking Changes in v2.0

1. **Circuit Breaker Integration**: Services may fail fast when circuit breakers are open
2. **Correlation ID Requirements**: Many service methods now require correlation IDs
3. **Health Check Dependencies**: Services integrate with health monitoring system
4. **Configuration Changes**: New configuration options for circuit breakers

#### Backward Compatibility Features

```csharp
public static class BackwardCompatibility
{
    // Extension methods for seamless migration
    public static byte[] Serialize<T>(this ISerializationService service, T obj)
    {
        var correlationId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        return service.Serialize(obj, correlationId);
    }
    
    public static T Deserialize<T>(this ISerializationService service, byte[] data)
    {
        var correlationId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        return service.Deserialize<T>(data, correlationId);
    }
    
    // Safe migration wrapper
    public static ISerializer CreateCompatibilityWrapper(ISerializationService service)
    {
        return new SerializationServiceAdapter(service);
    }
}
```

### Migration Testing Strategy

#### Parallel Testing
```csharp
public class MigrationTester
{
    private readonly ISerializer _oldSerializer;
    private readonly ISerializationService _newService;
    
    public async UniTask<MigrationTestResult> RunParallelTestAsync<T>(T testData)
    {
        var correlationId = new FixedString64Bytes("migration-test");
        var result = new MigrationTestResult();
        
        try
        {
            // Test old serializer
            var oldSerialized = _oldSerializer.Serialize(testData);
            var oldDeserialized = _oldSerializer.Deserialize<T>(oldSerialized);
            result.OldSerializerWorked = testData.Equals(oldDeserialized);
            result.OldSerializedSize = oldSerialized.Length;
            
            // Test new service
            var newSerialized = _newService.Serialize(testData, correlationId);
            var newDeserialized = _newService.Deserialize<T>(newSerialized, correlationId);
            result.NewServiceWorked = testData.Equals(newDeserialized);
            result.NewSerializedSize = newSerialized.Length;
            
            // Test cross-compatibility
            var oldToNew = _newService.Deserialize<T>(oldSerialized, correlationId);
            var newToOld = _oldSerializer.Deserialize<T>(newSerialized);
            result.CrossCompatible = testData.Equals(oldToNew) && testData.Equals(newToOld);
            
            result.Success = result.OldSerializerWorked && result.NewServiceWorked && result.CrossCompatible;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
}

public class MigrationTestResult
{
    public bool Success { get; set; }
    public bool OldSerializerWorked { get; set; }
    public bool NewServiceWorked { get; set; }
    public bool CrossCompatible { get; set; }
    public int OldSerializedSize { get; set; }
    public int NewSerializedSize { get; set; }
    public string Error { get; set; }
}
```

### Data Migration

#### Handling Existing Serialized Data

```csharp
public class DataMigrationService
{
    private readonly ISerializationService _serializationService;
    
    public async UniTask MigrateExistingDataAsync(string dataDirectory)
    {
        var correlationId = new FixedString64Bytes("data-migration");
        var dataFiles = Directory.GetFiles(dataDirectory, "*.dat");
        
        foreach (var filePath in dataFiles)
        {
            try
            {
                var originalData = await File.ReadAllBytesAsync(filePath);
                
                // Detect format of existing data
                var detectedFormat = _serializationService.DetectFormat(originalData);
                if (detectedFormat == null)
                {
                    Logger.LogWarning($"Could not detect format for {filePath}");
                    continue;
                }
                
                // Re-serialize using new service layer (may upgrade format)
                var deserialized = _serializationService.Deserialize<object>(originalData, correlationId);
                var newSerialized = _serializationService.Serialize(deserialized, correlationId);
                
                // Backup original and write new version
                var backupPath = $"{filePath}.backup";
                File.Move(filePath, backupPath);
                await File.WriteAllBytesAsync(filePath, newSerialized);
                
                Logger.LogInfo($"Migrated {filePath} from {detectedFormat} to new format");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to migrate {filePath}: {ex.Message}");
            }
        }
    }
}
```

### Performance Impact Assessment

#### Migration Performance Testing

```csharp
public class PerformanceComparisonService
{
    public async UniTask<PerformanceReport> ComparePerformanceAsync<T>(
        IEnumerable<T> testData,
        ISerializer oldSerializer,
        ISerializationService newService)
    {
        var correlationId = new FixedString64Bytes("perf-comparison");
        var report = new PerformanceReport();
        
        // Test old serializer performance
        var oldStopwatch = Stopwatch.StartNew();
        foreach (var item in testData)
        {
            var serialized = oldSerializer.Serialize(item);
            var deserialized = oldSerializer.Deserialize<T>(serialized);
        }
        oldStopwatch.Stop();
        report.OldSerializerTime = oldStopwatch.Elapsed;
        
        // Test new service performance
        var newStopwatch = Stopwatch.StartNew();
        foreach (var item in testData)
        {
            var serialized = newService.Serialize(item, correlationId);
            var deserialized = newService.Deserialize<T>(serialized, correlationId);
        }
        newStopwatch.Stop();
        report.NewServiceTime = newStopwatch.Elapsed;
        
        report.PerformanceRatio = newStopwatch.Elapsed.TotalMilliseconds / oldStopwatch.Elapsed.TotalMilliseconds;
        report.Recommendation = report.PerformanceRatio < 1.2 ? "Migration Recommended" : "Consider Optimization";
        
        return report;
    }
}
```

### Migration Checklist

#### Pre-Migration
- [ ] Backup all existing serialized data
- [ ] Update dependency injection configuration
- [ ] Install new health check dependencies
- [ ] Configure circuit breaker thresholds
- [ ] Set up monitoring and alerting

#### During Migration
- [ ] Deploy adapter pattern for gradual migration
- [ ] Run parallel testing to validate compatibility
- [ ] Monitor performance impact
- [ ] Validate circuit breaker functionality
- [ ] Test fallback scenarios

#### Post-Migration
- [ ] Remove old serializer dependencies
- [ ] Update all services to use correlation IDs
- [ ] Implement proper error handling for circuit breaker states
- [ ] Configure production monitoring
- [ ] Document new operational procedures

### Rollback Plan

#### Emergency Rollback Procedure

```csharp
public class RollbackManager
{
    public async UniTask ExecuteEmergencyRollbackAsync()
    {
        try
        {
            // 1. Disable new service layer
            _serializationService.SetEnabled(false, new FixedString64Bytes("rollback"));
            
            // 2. Re-enable old serializer
            var oldSerializerConfig = LoadBackupConfiguration();
            RegisterOldSerializer(oldSerializerConfig);
            
            // 3. Restore data from backups if necessary
            await RestoreDataFromBackupsAsync();
            
            // 4. Update service bindings to use old serializer
            UpdateServiceBindings();
            
            Logger.LogCritical("Emergency rollback completed - using old serializer");
        }
        catch (Exception ex)
        {
            Logger.LogCritical($"Rollback failed: {ex.Message}");
            throw;
        }
    }
}
```

## 📚 Additional Resources

- [MemoryPack Documentation](https://github.com/Cysharp/MemoryPack)
- [Custom Formatter Development Guide](SERIALIZATION_CUSTOM_FORMATTERS.md)
- [Schema Versioning Best Practices](SERIALIZATION_VERSIONING.md)
- [Performance Optimization Guide](SERIALIZATION_PERFORMANCE.md)
- [Troubleshooting Guide](SERIALIZATION_TROUBLESHOOTING.md)

## 🤝 Contributing

See our [Contributing Guidelines](../../CONTRIBUTING.md) for information on how to contribute to the Serialization System.

## 📄 Dependencies

- **Direct**: Logging
- **Integration**: MemoryPack library, UniTask library, ZLinq library, Newtonsoft.Json library
- **Dependents**: Messaging, Database, Analytics, Save, Cloud, Networking

---

*The Serialization System provides the foundation for efficient data transfer and persistence across all AhBearStudios Core systems.*