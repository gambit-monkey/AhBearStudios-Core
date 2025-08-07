using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Tests.Mocks;
using AhBearStudios.Core.Pooling.Factories;
using Moq;
using MemoryPack;

namespace AhBearStudios.Core.Tests.Serialization
{
    /// <summary>
    /// Comprehensive tests for FishNet + MemoryPack + Pooling integration.
    /// Tests the complete pipeline from MemoryPack serialization through pooled buffers to FishNet compatibility.
    /// </summary>
    [TestFixture]
    public class FishNetMemoryPackIntegrationTests
    {
        private ILoggingService _mockLoggingService;
        private ISerializationService _mockSerializationService;
        private NetworkSerializationBufferPool _bufferPool;
        private FishNetSerializationAdapter _fishNetAdapter;
        private IPoolingService _mockPoolingService;

        [SetUp]
        public void SetUp()
        {
            _mockLoggingService = new MockLoggingService();
            _mockSerializationService = new MockSerializationService();
            _mockPoolingService = new MockPoolingService();

            // Create network buffer pool with test configuration
            var networkConfig = NetworkPoolingConfig.CreateDefault();
            // Use builder pattern to create buffer pool with required factory dependencies
            var mockAdaptiveFactory = new Mock<IAdaptiveNetworkStrategyFactory>();
            var mockHighPerfFactory = new Mock<IHighPerformanceStrategyFactory>();
            var mockDynamicFactory = new Mock<IDynamicSizeStrategyFactory>();
            var mockPoolFactory = new Mock<INetworkBufferPoolFactory>();
            
            _bufferPool = new NetworkSerializationBufferPool(
                _mockLoggingService,
                networkConfig,
                mockPoolFactory.Object,
                _mockPoolingService);

            // Create FishNet adapter with all dependencies
            _fishNetAdapter = new FishNetSerializationAdapter(_mockLoggingService, _mockSerializationService, _bufferPool);
        }

        [TearDown]
        public void TearDown()
        {
            _bufferPool?.Dispose();
        }

        #region Basic Serialization Tests

        [Test]
        public void SerializeToBytes_Vector3_ReturnsValidData()
        {
            // Arrange
            var testVector = new Vector3(1.5f, 2.5f, 3.5f);

            // Act
            var result = _fishNetAdapter.SerializeToBytes(testVector);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Length, 0);
        }

        [Test]
        public void DeserializeFromBytes_Vector3_ReturnsOriginalValue()
        {
            // Arrange
            var originalVector = new Vector3(1.5f, 2.5f, 3.5f);
            var serializedData = _fishNetAdapter.SerializeToBytes(originalVector);

            // Act
            var deserializedVector = _fishNetAdapter.DeserializeFromBytes<Vector3>(serializedData);

            // Assert
            Assert.AreEqual(originalVector.x, deserializedVector.x, 0.001f);
            Assert.AreEqual(originalVector.y, deserializedVector.y, 0.001f);
            Assert.AreEqual(originalVector.z, deserializedVector.z, 0.001f);
        }

        [Test]
        public void SerializeToBytes_Quaternion_ReturnsValidData()
        {
            // Arrange
            var testQuaternion = Quaternion.Euler(45f, 90f, 180f);

            // Act
            var result = _fishNetAdapter.SerializeToBytes(testQuaternion);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Length, 0);
        }

        [Test]
        public void DeserializeFromBytes_Quaternion_ReturnsOriginalValue()
        {
            // Arrange
            var originalQuaternion = Quaternion.Euler(45f, 90f, 180f);
            var serializedData = _fishNetAdapter.SerializeToBytes(originalQuaternion);

            // Act
            var deserializedQuaternion = _fishNetAdapter.DeserializeFromBytes<Quaternion>(serializedData);

            // Assert
            Assert.AreEqual(originalQuaternion.x, deserializedQuaternion.x, 0.001f);
            Assert.AreEqual(originalQuaternion.y, deserializedQuaternion.y, 0.001f);
            Assert.AreEqual(originalQuaternion.z, deserializedQuaternion.z, 0.001f);
            Assert.AreEqual(originalQuaternion.w, deserializedQuaternion.w, 0.001f);
        }

        #endregion

        #region Pooled Buffer Tests

        [Test]
        public void SerializeToPooledBuffer_Vector3_ReturnsValidBuffer()
        {
            // Arrange
            var testVector = new Vector3(1.0f, 2.0f, 3.0f);

            // Act
            var buffer = _fishNetAdapter.SerializeToPooledBuffer(testVector);

            try
            {
                // Assert
                Assert.IsNotNull(buffer);
                Assert.Greater(buffer.Length, 0);
                Assert.IsTrue(buffer.IsValid());
            }
            finally
            {
                _bufferPool.ReturnBuffer(buffer);
            }
        }

        [Test]
        public void DeserializeFromPooledBuffer_Vector3_ReturnsOriginalValue()
        {
            // Arrange
            var originalVector = new Vector3(1.0f, 2.0f, 3.0f);
            var buffer = _fishNetAdapter.SerializeToPooledBuffer(originalVector);

            try
            {
                // Act
                var deserializedVector = _fishNetAdapter.DeserializeFromPooledBuffer<Vector3>(buffer);

                // Assert
                Assert.AreEqual(originalVector.x, deserializedVector.x, 0.001f);
                Assert.AreEqual(originalVector.y, deserializedVector.y, 0.001f);
                Assert.AreEqual(originalVector.z, deserializedVector.z, 0.001f);
            }
            finally
            {
                _bufferPool.ReturnBuffer(buffer);
            }
        }

        [Test]
        public void BufferPool_GetReturnCycle_MaintainsPoolIntegrity()
        {
            // Arrange
            var initialStats = _bufferPool.GetStatistics();

            // Act - Get and return buffers multiple times
            var buffers = new List<PooledNetworkBuffer>();
            for (int i = 0; i < 10; i++)
            {
                buffers.Add(_bufferPool.GetSmallBuffer());
            }

            foreach (var buffer in buffers)
            {
                _bufferPool.ReturnBuffer(buffer);
            }

            var finalStats = _bufferPool.GetStatistics();

            // Assert
            Assert.AreEqual(10, finalStats.SmallBufferGets - initialStats.SmallBufferGets);
            Assert.AreEqual(10, finalStats.TotalBufferReturns - initialStats.TotalBufferReturns);
            Assert.GreaterOrEqual(finalStats.BufferReturnRate, 0.9); // 90%+ return rate
        }

        #endregion

        #region Performance Tests

        [Test]
        public void SerializationPerformance_Vector3_CompletesInReasonableTime()
        {
            // Arrange
            var testVector = new Vector3(1.0f, 2.0f, 3.0f);
            const int iterations = 1000;

            // Act
            var startTime = DateTime.UtcNow;
            
            for (int i = 0; i < iterations; i++)
            {
                var data = _fishNetAdapter.SerializeToBytes(testVector);
                var result = _fishNetAdapter.DeserializeFromBytes<Vector3>(data);
            }

            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.Less(elapsed.TotalMilliseconds, 1000); // Should complete in under 1 second
            
            // Log performance metrics
            var avgTimePerOp = elapsed.TotalMicroseconds / iterations;
            Debug.Log($"Average serialization time: {avgTimePerOp:F2} microseconds per operation");
        }

        [Test]
        public void PooledBufferPerformance_ZeroAllocation_CompletesEfficiently()
        {
            // Arrange
            var testVector = new Vector3(1.0f, 2.0f, 3.0f);
            const int iterations = 100;

            // Act
            var startTime = DateTime.UtcNow;
            
            for (int i = 0; i < iterations; i++)
            {
                var buffer = _fishNetAdapter.SerializeToPooledBuffer(testVector);
                try
                {
                    var result = _fishNetAdapter.DeserializeFromPooledBuffer<Vector3>(buffer);
                }
                finally
                {
                    _bufferPool.ReturnBuffer(buffer);
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            var stats = _bufferPool.GetStatistics();

            // Assert
            Assert.Less(elapsed.TotalMilliseconds, 500); // Should be faster due to pooling
            Assert.GreaterOrEqual(stats.BufferReturnRate, 0.99); // 99%+ return rate
            
            Debug.Log($"Pooled buffer performance: {elapsed.TotalMilliseconds:F2}ms for {iterations} operations");
        }

        #endregion

        #region Complex Type Tests

        [Test]
        public void ComplexTypeSerialization_CustomStruct_WorksCorrectly()
        {
            // Arrange
            var testData = new TestNetworkData
            {
                PlayerId = 12345,
                Position = new Vector3(10f, 20f, 30f),
                Rotation = Quaternion.Euler(45f, 90f, 180f),
                Health = 85.5f,
                Name = "TestPlayer"
            };

            // Act
            var serializedData = _fishNetAdapter.SerializeToBytes(testData);
            var deserializedData = _fishNetAdapter.DeserializeFromBytes<TestNetworkData>(serializedData);

            // Assert
            Assert.AreEqual(testData.PlayerId, deserializedData.PlayerId);
            Assert.AreEqual(testData.Position.x, deserializedData.Position.x, 0.001f);
            Assert.AreEqual(testData.Health, deserializedData.Health, 0.001f);
            Assert.AreEqual(testData.Name, deserializedData.Name);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void DeserializeFromBytes_InvalidData_ThrowsException()
        {
            // Arrange
            var invalidData = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };

            // Act & Assert
            Assert.Throws<Exception>(() => _fishNetAdapter.DeserializeFromBytes<Vector3>(invalidData));
        }

        [Test]
        public void DeserializeFromPooledBuffer_NullBuffer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _fishNetAdapter.DeserializeFromPooledBuffer<Vector3>(null));
        }

        #endregion

        #region Type Registration Tests

        [Test]
        public void RegisterType_Vector3_RegistersSuccessfully()
        {
            // Act
            _fishNetAdapter.RegisterType<Vector3>();

            // Assert
            Assert.IsTrue(_fishNetAdapter.IsTypeRegistered<Vector3>());
        }

        [Test]
        public void RegisterType_CustomType_RegistersWithMemoryPack()
        {
            // Act
            _fishNetAdapter.RegisterType<TestNetworkData>();

            // Assert
            Assert.IsTrue(_fishNetAdapter.IsTypeRegistered<TestNetworkData>());
        }

        #endregion

        #region Stress Tests

        [Test]
        public void StressTest_ConcurrentSerialization_HandlesLoad()
        {
            // Arrange
            const int concurrentOperations = 50;
            var tasks = new List<Task>();
            var testVector = new Vector3(1.0f, 2.0f, 3.0f);

            // Act
            for (int i = 0; i < concurrentOperations; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var buffer = _fishNetAdapter.SerializeToPooledBuffer(testVector);
                    try
                    {
                        var result = _fishNetAdapter.DeserializeFromPooledBuffer<Vector3>(buffer);
                        Assert.AreEqual(testVector.x, result.x, 0.001f);
                    }
                    finally
                    {
                        _bufferPool.ReturnBuffer(buffer);
                    }
                }));
            }

            // Assert
            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
            
            var stats = _bufferPool.GetStatistics();
            Assert.GreaterOrEqual(stats.BufferReturnRate, 0.95); // 95%+ return rate under stress
        }

        #endregion
    }

    /// <summary>
    /// Test data structure for complex serialization testing.
    /// </summary>
    [MemoryPackable]
    public partial struct TestNetworkData
    {
        public int PlayerId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Health { get; set; }
        public string Name { get; set; }
    }
}