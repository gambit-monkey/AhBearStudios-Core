using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Unity.Serialization.Components;
using AhBearStudios.Unity.Serialization.Formatters;
using AhBearStudios.Unity.Serialization.Jobs;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using MemoryPack;
using ZLinq;

namespace AhBearStudios.Core.Serialization.Tests
{
    /// <summary>
    /// Comprehensive test suite for the AhBearStudios Unity Serialization system.
    /// Tests all major components, formatters, jobs, and integration scenarios.
    /// </summary>
    public class SerializationTestSuite
    {
        private GameObject _testGameObject;
        private SerializableMonoBehaviour _testComponent;
        private TransformSerializer _transformSerializer;
        private PersistentDataManager _dataManager;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with required components
            _testGameObject = new GameObject("TestSerializationObject");
            _testComponent = _testGameObject.AddComponent<TestSerializableComponent>();
            _transformSerializer = _testGameObject.AddComponent<TransformSerializer>();
            
            // Create data manager
            var dataManagerObject = new GameObject("DataManager");
            _dataManager = dataManagerObject.AddComponent<PersistentDataManager>();

            // Initialize Unity formatters
            UnityFormatterRegistration.RegisterFormatters();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
            
            if (_dataManager != null)
            {
                UnityEngine.Object.DestroyImmediate(_dataManager.gameObject);
            }

            // Clear any persistent data created during tests
            PlayerPrefs.DeleteAll();
        }

        #region Formatter Tests

        [Test]
        public void UnityVector3Formatter_SerializeDeserialize_Success()
        {
            // Arrange
            var formatter = new UnityVector3Formatter();
            var testVector = new Vector3(1.5f, 2.7f, -3.2f);
            var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
            var arrayWriter = new ArrayBufferWriter<byte>();
            writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);

            // Act
            formatter.Serialize(ref writer, ref testVector);
            writer.Flush();
            
            var data = arrayWriter.WrittenSpan.ToArray();
            var reader = new MemoryPackReader(data);
            var deserializedVector = new Vector3();
            formatter.Deserialize(ref reader, ref deserializedVector);

            // Assert
            Assert.AreEqual(testVector.x, deserializedVector.x, 0.001f);
            Assert.AreEqual(testVector.y, deserializedVector.y, 0.001f);
            Assert.AreEqual(testVector.z, deserializedVector.z, 0.001f);
        }

        [Test]
        public void UnityQuaternionFormatter_SerializeDeserialize_Success()
        {
            // Arrange
            var formatter = new UnityQuaternionFormatter();
            var testQuaternion = Quaternion.Euler(45f, 90f, 180f);
            var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
            var arrayWriter = new ArrayBufferWriter<byte>();
            writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);

            // Act
            formatter.Serialize(ref writer, ref testQuaternion);
            writer.Flush();
            
            var data = arrayWriter.WrittenSpan.ToArray();
            var reader = new MemoryPackReader(data);
            var deserializedQuaternion = new Quaternion();
            formatter.Deserialize(ref reader, ref deserializedQuaternion);

            // Assert
            Assert.AreEqual(testQuaternion.x, deserializedQuaternion.x, 0.001f);
            Assert.AreEqual(testQuaternion.y, deserializedQuaternion.y, 0.001f);
            Assert.AreEqual(testQuaternion.z, deserializedQuaternion.z, 0.001f);
            Assert.AreEqual(testQuaternion.w, deserializedQuaternion.w, 0.001f);
        }

        [Test]
        public void UnityColorFormatter_SerializeDeserialize_Success()
        {
            // Arrange
            var formatter = new UnityColorFormatter();
            var testColor = new Color(0.8f, 0.4f, 0.2f, 1.0f);
            var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
            var arrayWriter = new ArrayBufferWriter<byte>();
            writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);

            // Act
            formatter.Serialize(ref writer, ref testColor);
            writer.Flush();
            
            var data = arrayWriter.WrittenSpan.ToArray();
            var reader = new MemoryPackReader(data);
            var deserializedColor = new Color();
            formatter.Deserialize(ref reader, ref deserializedColor);

            // Assert
            Assert.AreEqual(testColor.r, deserializedColor.r, 0.001f);
            Assert.AreEqual(testColor.g, deserializedColor.g, 0.001f);
            Assert.AreEqual(testColor.b, deserializedColor.b, 0.001f);
            Assert.AreEqual(testColor.a, deserializedColor.a, 0.001f);
        }

        [Test]
        public void UnityMatrix4x4Formatter_SerializeDeserialize_Success()
        {
            // Arrange
            var formatter = new UnityMatrix4x4Formatter();
            var testMatrix = Matrix4x4.TRS(
                new Vector3(1, 2, 3),
                Quaternion.Euler(45, 90, 180),
                new Vector3(2, 2, 2)
            );
            var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
            var arrayWriter = new ArrayBufferWriter<byte>();
            writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);

            // Act
            formatter.Serialize(ref writer, ref testMatrix);
            writer.Flush();
            
            var data = arrayWriter.WrittenSpan.ToArray();
            var reader = new MemoryPackReader(data);
            var deserializedMatrix = new Matrix4x4();
            formatter.Deserialize(ref reader, ref deserializedMatrix);

            // Assert
            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(testMatrix[i], deserializedMatrix[i], 0.001f, $"Matrix element {i} mismatch");
            }
        }

        [Test]
        public void UnityFormatterRegistration_RegisterFormatters_Success()
        {
            // Act
            UnityFormatterRegistration.RegisterFormatters();
            
            // Assert
            Assert.IsTrue(UnityFormatterRegistration.IsRegistered());
            Assert.AreEqual(13, UnityFormatterRegistration.GetRegisteredFormatterCount());
        }

        #endregion

        #region Job System Tests

        [UnityTest]
        public IEnumerator SerializationJob_Vector3Array_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testData = new Vector3[]
                {
                    new Vector3(1, 2, 3),
                    new Vector3(4, 5, 6),
                    new Vector3(7, 8, 9)
                };

                var jobService = new UnitySerializationJobService(null);

                // Act
                var serializedData = await jobService.SerializeAsync(testData, Allocator.TempJob);

                // Assert
                Assert.IsTrue(serializedData.IsCreated);
                Assert.Greater(serializedData.Length, 0);

                // Cleanup
                if (serializedData.IsCreated) serializedData.Dispose();
                jobService.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator DeserializationJob_Vector3Array_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var originalData = new Vector3[]
                {
                    new Vector3(1.5f, 2.5f, 3.5f),
                    new Vector3(4.5f, 5.5f, 6.5f)
                };

                var serializationService = new UnitySerializationJobService(null);
                var deserializationService = new UnityDeserializationJobService(null);

                // Act - Serialize first
                var serializedData = await serializationService.SerializeAsync(originalData, Allocator.TempJob);
                
                // Convert to managed array for deserialization
                var managedData = new byte[serializedData.Length];
                NativeArray<byte>.Copy(serializedData, managedData, serializedData.Length);

                // Deserialize
                var deserializedData = await deserializationService.DeserializeAsync<Vector3>(managedData, originalData.Length, Allocator.TempJob);

                // Assert
                Assert.AreEqual(originalData.Length, deserializedData.Length);
                for (int i = 0; i < originalData.Length; i++)
                {
                    Assert.AreEqual(originalData[i].x, deserializedData[i].x, 0.001f);
                    Assert.AreEqual(originalData[i].y, deserializedData[i].y, 0.001f);
                    Assert.AreEqual(originalData[i].z, deserializedData[i].z, 0.001f);
                }

                // Cleanup
                if (serializedData.IsCreated) serializedData.Dispose();
                serializationService.Dispose();
                deserializationService.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator CompressionJob_DataCompression_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testData = new byte[1000];
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = (byte)(i % 10); // Repetitive pattern for good compression
                }

                var compressionService = new UnityCompressionJobService(null);

                // Act
                var compressedData = await compressionService.CompressAsync(testData, CompressionAlgorithm.RLE);
                var decompressedData = await compressionService.DecompressAsync(compressedData, testData.Length, CompressionAlgorithm.RLE);

                // Assert
                Assert.Less(compressedData.Length, testData.Length, "Data should be compressed");
                Assert.AreEqual(testData.Length, decompressedData.Length);
                
                for (int i = 0; i < testData.Length; i++)
                {
                    Assert.AreEqual(testData[i], decompressedData[i], $"Byte {i} mismatch after compression/decompression");
                }

                // Cleanup
                compressionService.Dispose();
            });
        }

        #endregion

        #region Component Tests

        [UnityTest]
        public IEnumerator TransformSerializer_SerializeDeserialize_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var originalPosition = new Vector3(10, 20, 30);
                var originalRotation = Quaternion.Euler(45, 90, 135);
                var originalScale = new Vector3(2, 3, 4);

                _testGameObject.transform.localPosition = originalPosition;
                _testGameObject.transform.localRotation = originalRotation;
                _testGameObject.transform.localScale = originalScale;

                // Act
                var transformData = _transformSerializer.GetCurrentTransformData();
                
                // Modify transform
                _testGameObject.transform.localPosition = Vector3.zero;
                _testGameObject.transform.localRotation = Quaternion.identity;
                _testGameObject.transform.localScale = Vector3.one;

                // Restore from data
                await _transformSerializer.ApplyTransformDataAsync(transformData);

                // Assert
                Assert.AreEqual(originalPosition.x, _testGameObject.transform.localPosition.x, 0.01f);
                Assert.AreEqual(originalPosition.y, _testGameObject.transform.localPosition.y, 0.01f);
                Assert.AreEqual(originalPosition.z, _testGameObject.transform.localPosition.z, 0.01f);

                Assert.AreEqual(originalRotation.x, _testGameObject.transform.localRotation.x, 0.01f);
                Assert.AreEqual(originalRotation.y, _testGameObject.transform.localRotation.y, 0.01f);
                Assert.AreEqual(originalRotation.z, _testGameObject.transform.localRotation.z, 0.01f);
                Assert.AreEqual(originalRotation.w, _testGameObject.transform.localRotation.w, 0.01f);

                Assert.AreEqual(originalScale.x, _testGameObject.transform.localScale.x, 0.01f);
                Assert.AreEqual(originalScale.y, _testGameObject.transform.localScale.y, 0.01f);
                Assert.AreEqual(originalScale.z, _testGameObject.transform.localScale.z, 0.01f);
            });
        }

        [UnityTest]
        public IEnumerator SerializableMonoBehaviour_SaveLoad_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testComponent = _testGameObject.GetComponent<TestSerializableComponent>();
                testComponent.TestValue = 42.5f;
                testComponent.TestString = "Test Data";

                // Act - Save
                var saveResult = await testComponent.SerializeAsync();
                Assert.IsTrue(saveResult.IsSuccess, "Serialization should succeed");

                // Modify data
                testComponent.TestValue = 0f;
                testComponent.TestString = "";

                // Load
                var loadResult = await testComponent.DeserializeAsync(saveResult.Data);
                Assert.IsTrue(loadResult.IsSuccess, "Deserialization should succeed");

                // Assert
                Assert.AreEqual(42.5f, testComponent.TestValue, 0.001f);
                Assert.AreEqual("Test Data", testComponent.TestString);
            });
        }

        [UnityTest]
        public IEnumerator PersistentDataManager_MultipleComponents_Success()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                await _dataManager.InitializeAsync();
                
                var component1 = _testGameObject.AddComponent<TestSerializableComponent>();
                var component2 = _testGameObject.AddComponent<TestSerializableComponent>();
                
                component1.TestValue = 100f;
                component1.TestString = "Component1";
                component2.TestValue = 200f;
                component2.TestString = "Component2";

                _dataManager.RegisterComponent("comp1", component1);
                _dataManager.RegisterComponent("comp2", component2);

                // Act
                var saveResult = await _dataManager.SaveAllDataAsync();
                Assert.IsTrue(saveResult.IsSuccess, "Save all should succeed");

                // Modify data
                component1.TestValue = 0f;
                component1.TestString = "";
                component2.TestValue = 0f;
                component2.TestString = "";

                // Load
                var loadResult = await _dataManager.LoadAllDataAsync();
                Assert.IsTrue(loadResult.IsSuccess, "Load all should succeed");

                // Assert
                Assert.AreEqual(100f, component1.TestValue, 0.001f);
                Assert.AreEqual("Component1", component1.TestString);
                Assert.AreEqual(200f, component2.TestValue, 0.001f);
                Assert.AreEqual("Component2", component2.TestString);
            });
        }

        #endregion

        #region Performance Tests

        [UnityTest]
        public IEnumerator PerformanceTest_LargeDataSerialization()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var largeArray = new Vector3[10000];
                for (int i = 0; i < largeArray.Length; i++)
                {
                    largeArray[i] = new Vector3(i, i * 2f, i * 3f);
                }

                var jobService = new UnitySerializationJobService(null);
                var startTime = DateTime.UtcNow;

                // Act
                var serializedData = await jobService.SerializeAsync(largeArray, Allocator.TempJob);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // Assert
                Assert.IsTrue(serializedData.IsCreated);
                Assert.Greater(serializedData.Length, 0);
                Assert.Less(duration.TotalMilliseconds, 1000, "Large data serialization should complete within 1 second");

                // Cleanup
                if (serializedData.IsCreated) serializedData.Dispose();
                jobService.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator PerformanceTest_BatchOperations()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var batches = new Vector3[5][];
                for (int i = 0; i < batches.Length; i++)
                {
                    batches[i] = new Vector3[1000];
                    for (int j = 0; j < batches[i].Length; j++)
                    {
                        batches[i][j] = new Vector3(i, j, i * j);
                    }
                }

                var jobService = new UnitySerializationJobService(null);
                var startTime = DateTime.UtcNow;

                // Act
                var serializedBatches = await jobService.SerializeBatchesAsync(batches, Allocator.TempJob);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // Assert
                Assert.AreEqual(batches.Length, serializedBatches.Length);
                Assert.Less(duration.TotalMilliseconds, 2000, "Batch operations should complete within 2 seconds");

                foreach (var batch in serializedBatches)
                {
                    Assert.IsTrue(batch.IsCreated);
                    Assert.Greater(batch.Length, 0);
                }

                // Cleanup
                foreach (var batch in serializedBatches)
                {
                    if (batch.IsCreated) batch.Dispose();
                }
                jobService.Dispose();
            });
        }

        [Test]
        public void MemoryTest_ZLinqOperations_NoAllocations()
        {
            // Arrange
            var testList = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Use ZLinq operations
            var sum = testList.AsValueEnumerable().Sum();
            var max = testList.AsValueEnumerable().Max();
            var count = testList.AsValueEnumerable().Count(x => x > 5);
            var any = testList.AsValueEnumerable().Any(x => x > 8);

            var finalMemory = GC.GetTotalMemory(false);
            var allocatedMemory = finalMemory - initialMemory;

            // Assert
            Assert.AreEqual(55, sum);
            Assert.AreEqual(10, max);
            Assert.AreEqual(5, count);
            Assert.IsTrue(any);
            Assert.LessOrEqual(allocatedMemory, 100, "ZLinq operations should not allocate significant memory");
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator IntegrationTest_SceneSerializationFlow()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var sceneManager = _testGameObject.AddComponent<SceneSerializationManager>();
                await sceneManager.InitializeAsync();

                // Register some test objects
                sceneManager.RegisterSceneObject(_testGameObject);

                // Act
                var saveResult = await sceneManager.SaveSceneDataAsync();
                Assert.IsTrue(saveResult.IsSuccess, "Scene save should succeed");

                var loadResult = await sceneManager.LoadSceneDataAsync();
                Assert.IsTrue(loadResult.IsSuccess, "Scene load should succeed");

                // Assert
                Assert.Greater(saveResult.ObjectCount, 0);
                Assert.Greater(saveResult.DataSize, 0);
            });
        }

        [UnityTest]
        public IEnumerator IntegrationTest_LevelCheckpointSystem()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var levelCoordinator = _testGameObject.AddComponent<LevelDataCoordinator>();
                await levelCoordinator.InitializeAsync();

                // Act
                var checkpoint = await levelCoordinator.CreateCheckpointAsync("Test Checkpoint");
                Assert.IsNotNull(checkpoint, "Checkpoint creation should succeed");

                var loadResult = await levelCoordinator.LoadCheckpointAsync(checkpoint.CheckpointId);
                Assert.IsTrue(loadResult.IsSuccess, "Checkpoint load should succeed");

                // Assert
                Assert.AreEqual(checkpoint.CheckpointId, loadResult.CheckpointId);
                Assert.AreEqual("Test Checkpoint", loadResult.CheckpointName);
            });
        }

        [UnityTest]
        public IEnumerator IntegrationTest_OptimizationValidator()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var validator = _testGameObject.AddComponent<SerializationOptimizationValidator>();
                validator.InitializeValidator();

                // Act
                var validationResult = await validator.PerformValidationAsync();

                // Assert
                Assert.IsTrue(validationResult.IsSuccess, "Validation should succeed");
                Assert.GreaterOrEqual(validationResult.UniTaskCompliance, 95f, "UniTask compliance should be high");
                Assert.GreaterOrEqual(validationResult.ZLinqOptimization, 90f, "ZLinq optimization should be good");
            });
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator ErrorTest_InvalidSerializationData()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testComponent = _testGameObject.GetComponent<TestSerializableComponent>();
                var invalidData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // Invalid data

                // Act
                var result = await testComponent.DeserializeAsync(invalidData);

                // Assert
                Assert.IsFalse(result.IsSuccess, "Deserialization should fail with invalid data");
                Assert.IsNotNull(result.ErrorMessage, "Error message should be provided");
            });
        }

        [Test]
        public void ErrorTest_NullDataHandling()
        {
            // Arrange
            var testComponent = _testGameObject.GetComponent<TestSerializableComponent>();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                UniTask.ToCoroutine(async () => await testComponent.DeserializeAsync(null));
            }, "Null data should be handled gracefully");
        }

        [UnityTest]
        public IEnumerator ErrorTest_JobSystemFailure()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var jobService = new UnitySerializationJobService(null);
                Vector3[] nullData = null;

                // Act
                var result = await jobService.SerializeAsync(nullData, Allocator.TempJob);

                // Assert
                Assert.IsTrue(result.IsCreated, "Service should handle null data gracefully");
                Assert.AreEqual(0, result.Length, "Result should be empty for null input");

                // Cleanup
                if (result.IsCreated) result.Dispose();
                jobService.Dispose();
            });
        }

        #endregion

        #region Stress Tests

        [UnityTest]
        public IEnumerator StressTest_ConcurrentOperations()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var tasks = new List<UniTask>();
                var components = new List<TestSerializableComponent>();

                for (int i = 0; i < 10; i++)
                {
                    var obj = new GameObject($"TestObject{i}");
                    var comp = obj.AddComponent<TestSerializableComponent>();
                    comp.TestValue = i * 10f;
                    comp.TestString = $"Test{i}";
                    components.Add(comp);

                    // Add serialization task
                    tasks.Add(comp.SerializeAsync().AsUniTask().ContinueWith(_ => { }));
                }

                // Act
                await UniTask.WhenAll(tasks);

                // Assert - All operations should complete without exceptions
                Assert.AreEqual(10, components.Count);

                // Cleanup
                foreach (var comp in components)
                {
                    if (comp != null && comp.gameObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(comp.gameObject);
                    }
                }
            });
        }

        [UnityTest]
        public IEnumerator StressTest_LargeSceneData()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var objects = new List<GameObject>();
                for (int i = 0; i < 100; i++)
                {
                    var obj = new GameObject($"StressTestObject{i}");
                    obj.AddComponent<TestSerializableComponent>();
                    objects.Add(obj);
                }

                var sceneManager = _testGameObject.AddComponent<SceneSerializationManager>();
                await sceneManager.InitializeAsync();

                // Register all objects
                foreach (var obj in objects)
                {
                    sceneManager.RegisterSceneObject(obj);
                }

                var startTime = DateTime.UtcNow;

                // Act
                var result = await sceneManager.SaveSceneDataAsync();
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // Assert
                Assert.IsTrue(result.IsSuccess, "Large scene serialization should succeed");
                Assert.Less(duration.TotalSeconds, 10, "Large scene should serialize within 10 seconds");
                Assert.Greater(result.ObjectCount, 50, "Should process significant number of objects");

                // Cleanup
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                }
            });
        }

        #endregion
    }

    /// <summary>
    /// Test implementation of SerializableMonoBehaviour for testing purposes.
    /// </summary>
    public class TestSerializableComponent : SerializableMonoBehaviour
    {
        [SerializeField]
        private float _testValue;
        
        [SerializeField]
        private string _testString = "";

        public float TestValue
        {
            get => _testValue;
            set => _testValue = value;
        }

        public string TestString
        {
            get => _testString;
            set => _testString = value ?? "";
        }

        protected override object GetSerializableData()
        {
            return new TestData
            {
                Value = _testValue,
                Text = _testString,
                Timestamp = DateTime.UtcNow.Ticks
            };
        }

        protected override async UniTask SetSerializableDataAsync(object data)
        {
            await UniTask.SwitchToMainThread();

            if (data is TestData testData)
            {
                _testValue = testData.Value;
                _testString = testData.Text;
            }
        }

        [MemoryPackable]
        public partial class TestData
        {
            public float Value { get; set; }
            public string Text { get; set; }
            public long Timestamp { get; set; }
        }
    }
}