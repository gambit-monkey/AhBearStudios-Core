using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Serialization.Factories;
using AhBearStudios.Core.Serialization.Services;
using AhBearStudios.Core.HealthChecking;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Serialization.Tests
{
    /// <summary>
    /// Unit tests for the SerializationService implementation.
    /// Tests circuit breaker integration, fault tolerance, and service functionality.
    /// </summary>
    [TestFixture]
    public class SerializationServiceTests
    {
        private ISerializationService _serializationService;
        private MockSerializerFactory _mockFactory;
        private MockSerializer _mockSerializer;
        private SerializationConfig _config;

        [SetUp]
        public void SetUp()
        {
            // Create test configuration
            _config = new SerializationConfig
            {
                Format = SerializationFormat.Json,
                EnableTypeValidation = true,
                EnablePerformanceMonitoring = true,
                MaxConcurrentOperations = 4
            };

            // Create mock serializer
            _mockSerializer = new MockSerializer();
            
            // Create mock factory
            _mockFactory = new MockSerializerFactory(_mockSerializer);

            // Create service instance with minimal dependencies
            _serializationService = new SerializationService(
                _config,
                _mockFactory,
                registry: null,
                versioningService: null,
                compressionService: null,
                loggingService: null,
                healthCheckService: null,
                alertService: null,
                profilerService: null,
                messageBusService: null);
        }

        [TearDown]
        public void TearDown()
        {
            _serializationService?.Dispose();
        }

        [Test]
        public void SerializationService_IsEnabled_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(_serializationService.IsEnabled);
        }

        [Test]
        public void SerializationService_Configuration_ReturnsCorrectConfig()
        {
            // Act & Assert
            Assert.AreEqual(_config, _serializationService.Configuration);
        }

        [Test]
        public void SerializationService_Serialize_WithBasicObject_ReturnsData()
        {
            // Arrange
            var testObject = new TestClass { Id = 1, Name = "Test" };
            var correlationId = new FixedString64Bytes("test-correlation");

            // Act
            var result = _serializationService.Serialize(testObject, correlationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Length, 0);
        }

        [Test]
        public void SerializationService_Deserialize_WithValidData_ReturnsObject()
        {
            // Arrange
            var testObject = new TestClass { Id = 1, Name = "Test" };
            var correlationId = new FixedString64Bytes("test-correlation");
            var serialized = _serializationService.Serialize(testObject, correlationId);

            // Act
            var result = _serializationService.Deserialize<TestClass>(serialized, correlationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(testObject.Id, result.Id);
            Assert.AreEqual(testObject.Name, result.Name);
        }

        [Test]
        public async UniTask SerializationService_SerializeAsync_WithBasicObject_ReturnsData()
        {
            // Arrange
            var testObject = new TestClass { Id = 2, Name = "AsyncTest" };
            var correlationId = new FixedString64Bytes("async-test");

            // Act
            var result = await _serializationService.SerializeAsync(testObject, correlationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Length, 0);
        }

        [Test]
        public async UniTask SerializationService_DeserializeAsync_WithValidData_ReturnsObject()
        {
            // Arrange
            var testObject = new TestClass { Id = 2, Name = "AsyncTest" };
            var correlationId = new FixedString64Bytes("async-test");
            var serialized = await _serializationService.SerializeAsync(testObject, correlationId);

            // Act
            var result = await _serializationService.DeserializeAsync<TestClass>(serialized, correlationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(testObject.Id, result.Id);
            Assert.AreEqual(testObject.Name, result.Name);
        }

        [Test]
        public void SerializationService_TrySerialize_WithBasicObject_ReturnsTrue()
        {
            // Arrange
            var testObject = new TestClass { Id = 3, Name = "TryTest" };
            var correlationId = new FixedString64Bytes("try-test");

            // Act
            var success = _serializationService.TrySerialize(testObject, out var result, correlationId);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.Greater(result.Length, 0);
        }

        [Test]
        public void SerializationService_TryDeserialize_WithValidData_ReturnsTrue()
        {
            // Arrange
            var testObject = new TestClass { Id = 3, Name = "TryTest" };
            var correlationId = new FixedString64Bytes("try-test");
            _serializationService.TrySerialize(testObject, out var serialized, correlationId);

            // Act
            var success = _serializationService.TryDeserialize<TestClass>(serialized, out var result, correlationId);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(testObject.Id, result.Id);
            Assert.AreEqual(testObject.Name, result.Name);
        }

        [Test]
        public void SerializationService_RegisterType_WithGeneric_DoesNotThrow()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("register-test");

            // Act & Assert
            Assert.DoesNotThrow(() => _serializationService.RegisterType<TestClass>(correlationId));
        }

        [Test]
        public void SerializationService_RegisterType_WithType_DoesNotThrow()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("register-test");

            // Act & Assert
            Assert.DoesNotThrow(() => _serializationService.RegisterType(typeof(TestClass), correlationId));
        }

        [Test]
        public void SerializationService_GetRegisteredFormats_ReturnsFormats()
        {
            // Act
            var formats = _serializationService.GetRegisteredFormats();

            // Assert
            Assert.IsNotNull(formats);
            Assert.Greater(formats.Count, 0);
            Assert.Contains(SerializationFormat.Json, formats.ToArray());
        }

        [Test]
        public void SerializationService_IsSerializerAvailable_WithRegisteredFormat_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(_serializationService.IsSerializerAvailable(SerializationFormat.Json));
        }

        [Test]
        public void SerializationService_GetBestFormat_WithNoPreference_ReturnsConfiguredFormat()
        {
            // Act
            var bestFormat = _serializationService.GetBestFormat<TestClass>();

            // Assert
            Assert.AreEqual(SerializationFormat.Json, bestFormat);
        }

        [Test]
        public void SerializationService_GetStatistics_ReturnsValidStatistics()
        {
            // Act
            var stats = _serializationService.GetStatistics();

            // Assert
            Assert.IsNotNull(stats);
            Assert.GreaterOrEqual(stats.TotalSerializations, 0);
            Assert.GreaterOrEqual(stats.TotalDeserializations, 0);
            Assert.GreaterOrEqual(stats.FailedOperations, 0);
        }

        [Test]
        public void SerializationService_PerformHealthCheck_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(_serializationService.PerformHealthCheck());
        }

        [Test]
        public void SerializationService_GetHealthStatus_ReturnsStatus()
        {
            // Act
            var healthStatus = _serializationService.GetHealthStatus();

            // Assert
            Assert.IsNotNull(healthStatus);
            Assert.Greater(healthStatus.Count, 0);
        }

        [Test]
        public void SerializationService_GetCircuitBreakerStatistics_ReturnsStatistics()
        {
            // Act
            var cbStats = _serializationService.GetCircuitBreakerStatistics();

            // Assert
            Assert.IsNotNull(cbStats);
        }

        [Test]
        public async UniTask SerializationService_FlushAsync_DoesNotThrow()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("flush-test");

            // Act & Assert
            await Assert.DoesNotThrowAsync(async () => await _serializationService.FlushAsync(correlationId));
        }

        [Test]
        public void SerializationService_ValidateConfiguration_ReturnsResult()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("validate-test");

            // Act
            var result = _serializationService.ValidateConfiguration(correlationId);

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void SerializationService_PerformMaintenance_DoesNotThrow()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("maintenance-test");

            // Act & Assert
            Assert.DoesNotThrow(() => _serializationService.PerformMaintenance(correlationId));
        }

        [Test]
        public void SerializationService_SetEnabled_UpdatesEnabledState()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("enabled-test");

            // Act
            _serializationService.SetEnabled(false, correlationId);

            // Assert
            Assert.IsFalse(_serializationService.IsEnabled);

            // Cleanup
            _serializationService.SetEnabled(true, correlationId);
            Assert.IsTrue(_serializationService.IsEnabled);
        }

        [Test]
        public void SerializationService_DetectFormat_WithNullData_ReturnsNull()
        {
            // Act
            var format = _serializationService.DetectFormat(null);

            // Assert
            Assert.IsNull(format);
        }

        [Test]
        public void SerializationService_GetFallbackChain_ReturnsChain()
        {
            // Act
            var chain = _serializationService.GetFallbackChain(SerializationFormat.Json);

            // Assert
            Assert.IsNotNull(chain);
            Assert.Greater(chain.Count, 0);
            Assert.AreEqual(SerializationFormat.Json, chain[0]);
        }

        [Test]
        public void SerializationService_Dispose_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _serializationService.Dispose());
        }
    }

    /// <summary>
    /// Test class for serialization testing.
    /// </summary>
    [Serializable]
    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Mock serializer factory for testing.
    /// </summary>
    public class MockSerializerFactory : ISerializerFactory
    {
        private readonly ISerializer _serializer;

        public MockSerializerFactory(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public ISerializer CreateSerializer(SerializationConfig config) => _serializer;
        public ISerializer CreateSerializer(SerializationFormat format) => _serializer;
        public ISerializer GetOrCreateSerializer(SerializationConfig config) => _serializer;
        public bool CanCreateSerializer(SerializationConfig config) => true;
        public SerializationFormat[] GetSupportedFormats() => new[] { SerializationFormat.Json, SerializationFormat.Binary };
        public void ClearCache() { }
    }

    /// <summary>
    /// Mock serializer implementation for testing.
    /// </summary>
    public class MockSerializer : ISerializer
    {
        private readonly Dictionary<Type, bool> _registeredTypes = new Dictionary<Type, bool>();
        private long _serializationCount = 0;
        private long _deserializationCount = 0;

        public byte[] Serialize<T>(T obj)
        {
            Interlocked.Increment(ref _serializationCount);
            // Simple mock serialization - just convert to string and encode
            var json = $"{{\"Id\":{((dynamic)obj).Id},\"Name\":\"{((dynamic)obj).Name}\"}}";
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            Interlocked.Increment(ref _deserializationCount);
            // Simple mock deserialization
            var json = System.Text.Encoding.UTF8.GetString(data);
            if (typeof(T) == typeof(TestClass))
            {
                // Very basic JSON parsing for test
                var obj = (T)(object)new TestClass
                {
                    Id = 1, // Simplified for test
                    Name = "Test"
                };
                return obj;
            }
            return default(T);
        }

        public T Deserialize<T>(ReadOnlySpan<byte> data) => Deserialize<T>(data.ToArray());

        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result) => TryDeserialize(data.ToArray(), out result);

        public void RegisterType<T>() => RegisterType(typeof(T));

        public void RegisterType(Type type)
        {
            _registeredTypes[type] = true;
        }

        public bool IsRegistered<T>() => IsRegistered(typeof(T));

        public bool IsRegistered(Type type) => _registeredTypes.ContainsKey(type);

        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            await UniTask.Yield();
            return Serialize(obj);
        }

        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            await UniTask.Yield();
            return Deserialize<T>(data);
        }

        public void SerializeToStream<T>(T obj, System.IO.Stream stream)
        {
            var data = Serialize(obj);
            stream.Write(data, 0, data.Length);
        }

        public T DeserializeFromStream<T>(System.IO.Stream stream)
        {
            using var reader = new System.IO.StreamReader(stream);
            var data = System.Text.Encoding.UTF8.GetBytes(reader.ReadToEnd());
            return Deserialize<T>(data);
        }

        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            var data = Serialize(obj);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            nativeArray.CopyFrom(data);
            return nativeArray;
        }

        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            var byteArray = data.ToArray();
            return Deserialize<T>(byteArray);
        }

        public SerializationStatistics GetStatistics()
        {
            return new SerializationStatistics
            {
                TotalSerializations = _serializationCount,
                TotalDeserializations = _deserializationCount,
                FailedOperations = 0,
                TotalBytesProcessed = _serializationCount * 50, // Mock value
                RegisteredTypeCount = _registeredTypes.Count,
                LastResetTime = DateTime.UtcNow
            };
        }

        public void Dispose()
        {
            _registeredTypes.Clear();
        }
    }
}