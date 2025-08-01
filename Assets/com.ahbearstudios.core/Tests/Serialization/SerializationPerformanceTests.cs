using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using AhBearStudios.Unity.Serialization.Components;
using AhBearStudios.Unity.Serialization.Formatters;
using AhBearStudios.Unity.Serialization.Jobs;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using MemoryPack;
using ZLinq;

namespace AhBearStudios.Core.Serialization.Tests
{
    /// <summary>
    /// Comprehensive performance test suite for the AhBearStudios Unity Serialization system.
    /// Measures throughput, memory usage, allocation patterns, and optimization effectiveness.
    /// </summary>
    public class SerializationPerformanceTests
    {
        private ProfilerMarker _serializationMarker;
        private ProfilerMarker _deserializationMarker;
        private ProfilerMarker _compressionMarker;
        private ProfilerMarker _jobSystemMarker;

        // Performance thresholds
        private const float MAX_SERIALIZATION_TIME_MS = 100f;
        private const float MAX_DESERIALIZATION_TIME_MS = 50f;
        private const long MAX_ALLOCATION_BYTES = 1024 * 1024; // 1MB
        private const int MIN_THROUGHPUT_OBJECTS_PER_SECOND = 1000;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _serializationMarker = new ProfilerMarker("Serialization.Performance");
            _deserializationMarker = new ProfilerMarker("Deserialization.Performance");
            _compressionMarker = new ProfilerMarker("Compression.Performance");
            _jobSystemMarker = new ProfilerMarker("JobSystem.Performance");

            // Initialize formatters
            UnityFormatterRegistration.RegisterFormatters();
        }

        [SetUp]
        public void SetUp()
        {
            // Force garbage collection before each test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #region Formatter Performance Tests

        [Test, Performance]
        public void PerformanceTest_Vector3Formatter_Throughput()
        {
            // Arrange
            var formatter = new UnityVector3Formatter();
            var testVectors = new Vector3[10000];
            for (int i = 0; i < testVectors.Length; i++)
            {
                testVectors[i] = new Vector3(i, i * 2f, i * 3f);
            }

            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);

            // Act
            using (_serializationMarker.Auto())
            {
                for (int i = 0; i < testVectors.Length; i++)
                {
                    var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
                    var arrayWriter = new ArrayBufferWriter<byte>();
                    writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);
                    
                    formatter.Serialize(ref writer, ref testVectors[i]);
                    writer.Flush();
                }
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var allocatedMemory = finalMemory - initialMemory;

            // Assert
            var throughputPerSecond = testVectors.Length / (stopwatch.ElapsedMilliseconds / 1000.0);
            
            Assert.Greater(throughputPerSecond, MIN_THROUGHPUT_OBJECTS_PER_SECOND, 
                $"Vector3 formatter throughput too low: {throughputPerSecond:F0} objects/sec");
            Assert.Less(stopwatch.ElapsedMilliseconds, MAX_SERIALIZATION_TIME_MS, 
                $"Vector3 formatter took too long: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(allocatedMemory, MAX_ALLOCATION_BYTES, 
                $"Vector3 formatter allocated too much memory: {allocatedMemory / 1024.0:F2}KB");

            UnityEngine.Debug.Log($"Vector3 Performance: {throughputPerSecond:F0} objects/sec, " +
                                $"{stopwatch.ElapsedMilliseconds}ms total, " +
                                $"{allocatedMemory / 1024.0:F2}KB allocated");
        }

        [Test, Performance]
        public void PerformanceTest_QuaternionFormatter_Compression()
        {
            // Arrange
            var standardFormatter = new UnityQuaternionFormatter();
            var compressedFormatter = new UnityQuaternionCompressedFormatter();
            var testQuaternions = new Quaternion[1000];
            
            for (int i = 0; i < testQuaternions.Length; i++)
            {
                testQuaternions[i] = Quaternion.Euler(
                    UnityEngine.Random.Range(0f, 360f),
                    UnityEngine.Random.Range(0f, 360f),
                    UnityEngine.Random.Range(0f, 360f)
                );
            }

            // Act - Standard formatter
            var standardSize = 0;
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < testQuaternions.Length; i++)
            {
                var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
                var arrayWriter = new ArrayBufferWriter<byte>();
                writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);
                
                standardFormatter.Serialize(ref writer, ref testQuaternions[i]);
                writer.Flush();
                standardSize += arrayWriter.WrittenCount;
            }
            
            var standardTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // Act - Compressed formatter
            var compressedSize = 0;
            
            for (int i = 0; i < testQuaternions.Length; i++)
            {
                var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
                var arrayWriter = new ArrayBufferWriter<byte>();
                writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);
                
                compressedFormatter.Serialize(ref writer, ref testQuaternions[i]);
                writer.Flush();
                compressedSize += arrayWriter.WrittenCount;
            }
            
            stopwatch.Stop();
            var compressedTime = stopwatch.ElapsedMilliseconds;

            // Assert
            var compressionRatio = 1.0 - ((double)compressedSize / standardSize);
            
            Assert.Greater(compressionRatio, 0.3, "Compressed quaternion should achieve at least 30% size reduction");
            Assert.Less(compressedTime, standardTime * 2, "Compressed formatter should not be more than 2x slower");

            UnityEngine.Debug.Log($"Quaternion Compression: {compressionRatio:P1} size reduction, " +
                                $"Standard: {standardSize} bytes in {standardTime}ms, " +
                                $"Compressed: {compressedSize} bytes in {compressedTime}ms");
        }

        [Test, Performance]
        public void PerformanceTest_ColorFormatter_Variants()
        {
            // Arrange
            var standardFormatter = new UnityColorFormatter();
            var packedFormatter = new UnityColorPackedFormatter();
            var hdrFormatter = new UnityColorHDRFormatter();
            
            var testColors = new Color[5000];
            for (int i = 0; i < testColors.Length; i++)
            {
                testColors[i] = new Color(
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f)
                );
            }

            var results = new Dictionary<string, (long time, int size)>();

            // Test each formatter
            var formatters = new (string name, object formatter)[]
            {
                ("Standard", standardFormatter),
                ("Packed", packedFormatter),
                ("HDR", hdrFormatter)
            };

            foreach (var (name, formatter) in formatters)
            {
                var stopwatch = Stopwatch.StartNew();
                var totalSize = 0;

                for (int i = 0; i < testColors.Length; i++)
                {
                    var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
                    var arrayWriter = new ArrayBufferWriter<byte>();
                    writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);

                    if (formatter is UnityColorFormatter stdFormatter)
                    {
                        stdFormatter.Serialize(ref writer, ref testColors[i]);
                    }
                    else if (formatter is UnityColorPackedFormatter pckFormatter)
                    {
                        pckFormatter.Serialize(ref writer, ref testColors[i]);
                    }
                    else if (formatter is UnityColorHDRFormatter hdrForm)
                    {
                        hdrForm.Serialize(ref writer, ref testColors[i]);
                    }

                    writer.Flush();
                    totalSize += arrayWriter.WrittenCount;
                }

                stopwatch.Stop();
                results[name] = (stopwatch.ElapsedMilliseconds, totalSize);
            }

            // Assert and report
            foreach (var (name, (time, size)) in results)
            {
                Assert.Less(time, MAX_SERIALIZATION_TIME_MS, $"{name} formatter too slow: {time}ms");
                UnityEngine.Debug.Log($"{name} Color Formatter: {time}ms, {size} bytes, " +
                                    $"{testColors.Length / (time / 1000.0):F0} colors/sec");
            }

            // Packed should be smallest
            Assert.Less(results["Packed"].size, results["Standard"].size, "Packed formatter should be smaller");
        }

        #endregion

        #region Job System Performance Tests

        [UnityTest, Performance]
        public IEnumerator PerformanceTest_JobSystem_LargeArray()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testData = new Vector3[50000];
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = new Vector3(i, i * 2f, i * 3f);
                }

                var jobService = new UnitySerializationJobService(null);
                var initialMemory = GC.GetTotalMemory(false);
                var stopwatch = Stopwatch.StartNew();

                // Act
                NativeArray<byte> serializedData;
                using (_jobSystemMarker.Auto())
                {
                    serializedData = await jobService.SerializeAsync(testData, Allocator.TempJob);
                }

                stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(false);
                var allocatedMemory = finalMemory - initialMemory;

                // Assert
                var throughputPerSecond = testData.Length / (stopwatch.ElapsedMilliseconds / 1000.0);
                
                Assert.IsTrue(serializedData.IsCreated, "Serialization should succeed");
                Assert.Greater(serializedData.Length, 0, "Serialized data should not be empty");
                Assert.Greater(throughputPerSecond, MIN_THROUGHPUT_OBJECTS_PER_SECOND * 2, 
                    $"Job system throughput too low: {throughputPerSecond:F0} objects/sec");
                Assert.Less(allocatedMemory, MAX_ALLOCATION_BYTES, 
                    $"Job system allocated too much memory: {allocatedMemory / 1024.0:F2}KB");

                UnityEngine.Debug.Log($"Job System Performance: {throughputPerSecond:F0} objects/sec, " +
                                    $"{stopwatch.ElapsedMilliseconds}ms total, " +
                                    $"{serializedData.Length} bytes, " +
                                    $"{allocatedMemory / 1024.0:F2}KB allocated");

                // Cleanup
                if (serializedData.IsCreated) serializedData.Dispose();
                jobService.Dispose();
            });
        }

        [UnityTest, Performance]
        public IEnumerator PerformanceTest_BatchSerialization_Parallelism()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var batchCount = 10;
                var itemsPerBatch = 1000;
                var batches = new Vector3[batchCount][];
                
                for (int i = 0; i < batchCount; i++)
                {
                    batches[i] = new Vector3[itemsPerBatch];
                    for (int j = 0; j < itemsPerBatch; j++)
                    {
                        batches[i][j] = new Vector3(i, j, i * j);
                    }
                }

                var jobService = new UnitySerializationJobService(null);
                var stopwatch = Stopwatch.StartNew();

                // Act
                var serializedBatches = await jobService.SerializeBatchesAsync(batches, Allocator.TempJob);
                stopwatch.Stop();

                // Assert
                var totalItems = batchCount * itemsPerBatch;
                var throughputPerSecond = totalItems / (stopwatch.ElapsedMilliseconds / 1000.0);
                
                Assert.AreEqual(batchCount, serializedBatches.Length, "Should process all batches");
                Assert.Greater(throughputPerSecond, MIN_THROUGHPUT_OBJECTS_PER_SECOND * 3, 
                    $"Batch throughput too low: {throughputPerSecond:F0} objects/sec");
                
                foreach (var batch in serializedBatches)
                {
                    Assert.IsTrue(batch.IsCreated, "Each batch should be processed");
                    Assert.Greater(batch.Length, 0, "Each batch should contain data");
                }

                UnityEngine.Debug.Log($"Batch Performance: {throughputPerSecond:F0} objects/sec, " +
                                    $"{stopwatch.ElapsedMilliseconds}ms total, " +
                                    $"{batchCount} batches");

                // Cleanup
                foreach (var batch in serializedBatches)
                {
                    if (batch.IsCreated) batch.Dispose();
                }
                jobService.Dispose();
            });
        }

        [UnityTest, Performance]
        public IEnumerator PerformanceTest_CompressionThroughput()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var dataSize = 100000;
                var testData = new byte[dataSize];
                
                // Create pattern that compresses well
                for (int i = 0; i < dataSize; i++)
                {
                    testData[i] = (byte)(i % 16);
                }

                var compressionService = new UnityCompressionJobService(null);
                var algorithms = new[] { CompressionAlgorithm.None, CompressionAlgorithm.RLE, CompressionAlgorithm.LZ4 };
                var results = new Dictionary<CompressionAlgorithm, (long time, int size, double ratio)>();

                foreach (var algorithm in algorithms)
                {
                    var stopwatch = Stopwatch.StartNew();

                    using (_compressionMarker.Auto())
                    {
                        var compressedData = await compressionService.CompressAsync(testData, algorithm);
                        stopwatch.Stop();

                        var compressionRatio = 1.0 - ((double)compressedData.Length / testData.Length);
                        results[algorithm] = (stopwatch.ElapsedMilliseconds, compressedData.Length, compressionRatio);

                        // Test decompression
                        var decompressedData = await compressionService.DecompressAsync(compressedData, testData.Length, algorithm);
                        
                        // Verify correctness
                        Assert.AreEqual(testData.Length, decompressedData.Length, $"{algorithm} decompression size mismatch");
                        for (int i = 0; i < Math.Min(100, testData.Length); i++) // Sample verification
                        {
                            Assert.AreEqual(testData[i], decompressedData[i], $"{algorithm} decompression data mismatch at {i}");
                        }
                    }
                }

                // Assert and report
                foreach (var (algorithm, (time, size, ratio)) in results)
                {
                    var throughputMBps = (dataSize / (1024.0 * 1024.0)) / (time / 1000.0);
                    
                    if (algorithm != CompressionAlgorithm.None)
                    {
                        Assert.Greater(ratio, 0.1, $"{algorithm} should achieve some compression");
                    }
                    
                    UnityEngine.Debug.Log($"{algorithm} Compression: {throughputMBps:F2} MB/s, " +
                                        $"{ratio:P1} compression, {time}ms");
                }

                compressionService.Dispose();
            });
        }

        #endregion

        #region Memory Performance Tests

        [Test, Performance]
        public void PerformanceTest_ZLinq_AllocationComparison()
        {
            // Arrange
            var testData = new List<int>();
            for (int i = 0; i < 10000; i++)
            {
                testData.Add(i);
            }

            // Test regular LINQ allocations
            var initialMemory = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();

            // Regular LINQ operations (creates allocations)
            for (int i = 0; i < 100; i++)
            {
                var sum = testData.Sum();
                var max = testData.Max();
                var filtered = testData.Where(x => x > 5000).Count();
                var any = testData.Any(x => x > 9000);
            }

            stopwatch.Stop();
            var regularLinqTime = stopwatch.ElapsedMilliseconds;
            var regularLinqMemory = GC.GetTotalMemory(false) - initialMemory;

            // Force GC and reset
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Test ZLinq (zero allocations)
            initialMemory = GC.GetTotalMemory(false);
            stopwatch.Restart();

            for (int i = 0; i < 100; i++)
            {
                var sum = testData.AsValueEnumerable().Sum();
                var max = testData.AsValueEnumerable().Max();
                var filtered = testData.AsValueEnumerable().Where(x => x > 5000).Count();
                var any = testData.AsValueEnumerable().Any(x => x > 9000);
            }

            stopwatch.Stop();
            var zLinqTime = stopwatch.ElapsedMilliseconds;
            var zLinqMemory = GC.GetTotalMemory(false) - initialMemory;

            // Assert
            Assert.Less(zLinqMemory, regularLinqMemory / 10, 
                $"ZLinq should allocate much less memory. Regular: {regularLinqMemory}B, ZLinq: {zLinqMemory}B");
            Assert.LessOrEqual(zLinqTime, regularLinqTime * 1.5f, 
                $"ZLinq should not be significantly slower. Regular: {regularLinqTime}ms, ZLinq: {zLinqTime}ms");

            UnityEngine.Debug.Log($"LINQ Performance Comparison:\n" +
                                $"Regular LINQ: {regularLinqTime}ms, {regularLinqMemory}B allocated\n" +
                                $"ZLinq: {zLinqTime}ms, {zLinqMemory}B allocated\n" +
                                $"Memory savings: {((double)(regularLinqMemory - zLinqMemory) / regularLinqMemory):P1}");
        }

        [UnityTest, Performance]
        public IEnumerator PerformanceTest_SerializationMemoryLeaks()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var testData = new Vector3[1000];
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = new Vector3(i, i * 2f, i * 3f);
                }

                var jobService = new UnitySerializationJobService(null);
                var initialMemory = GC.GetTotalMemory(true);

                // Act - Perform many serialization cycles
                for (int cycle = 0; cycle < 50; cycle++)
                {
                    var serializedData = await jobService.SerializeAsync(testData, Allocator.TempJob);
                    
                    // Immediately dispose to test cleanup
                    if (serializedData.IsCreated)
                    {
                        serializedData.Dispose();
                    }

                    // Force GC every 10 cycles
                    if (cycle % 10 == 0)
                    {
                        GC.Collect();
                        await UniTask.Yield();
                    }
                }

                // Final memory check
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await UniTask.Delay(100); // Allow cleanup

                var finalMemory = GC.GetTotalMemory(false);
                var memoryDelta = finalMemory - initialMemory;

                // Assert
                Assert.Less(memoryDelta, MAX_ALLOCATION_BYTES / 10, 
                    $"Memory leak detected: {memoryDelta / 1024.0:F2}KB increase after 50 cycles");

                UnityEngine.Debug.Log($"Memory Leak Test: {memoryDelta / 1024.0:F2}KB delta after 50 cycles");

                jobService.Dispose();
            });
        }

        #endregion

        #region Component Performance Tests

        [UnityTest, Performance]
        public IEnumerator PerformanceTest_TransformSerializer_FrameRate()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var transformCount = 1000;
                var transforms = new List<(GameObject obj, TransformSerializer serializer)>();

                for (int i = 0; i < transformCount; i++)
                {
                    var obj = new GameObject($"Transform{i}");
                    obj.transform.position = UnityEngine.Random.insideUnitSphere * 100f;
                    obj.transform.rotation = UnityEngine.Random.rotation;
                    obj.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.5f, 2f);
                    
                    var serializer = obj.AddComponent<TransformSerializer>();
                    transforms.Add((obj, serializer));
                }

                var stopwatch = Stopwatch.StartNew();
                var frameCount = 0;
                var maxFrameTime = 0f;

                // Act - Simulate 60 frames of transform updates
                for (int frame = 0; frame < 60; frame++)
                {
                    var frameStartTime = stopwatch.ElapsedMilliseconds;

                    // Update all transforms
                    foreach (var (obj, serializer) in transforms)
                    {
                        obj.transform.position += Vector3.one * 0.1f;
                        await serializer.SaveIfChangedAsync();
                    }

                    frameCount++;
                    var frameTime = stopwatch.ElapsedMilliseconds - frameStartTime;
                    maxFrameTime = Mathf.Max(maxFrameTime, frameTime);

                    // Target 60 FPS (16.67ms per frame)
                    if (frameTime < 16.67f)
                    {
                        await UniTask.Delay((int)(16.67f - frameTime));
                    }
                }

                stopwatch.Stop();
                var averageFrameTime = (float)stopwatch.ElapsedMilliseconds / frameCount;

                // Assert
                Assert.Less(averageFrameTime, 16.67f, 
                    $"Average frame time too high: {averageFrameTime:F2}ms");
                Assert.Less(maxFrameTime, 33.33f, 
                    $"Max frame time too high: {maxFrameTime:F2}ms");

                UnityEngine.Debug.Log($"Transform Performance: {transformCount} transforms, " +
                                    $"Avg: {averageFrameTime:F2}ms/frame, " +
                                    $"Max: {maxFrameTime:F2}ms/frame");

                // Cleanup
                foreach (var (obj, _) in transforms)
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                }
            });
        }

        [UnityTest, Performance]
        public IEnumerator PerformanceTest_PersistentDataManager_Scalability()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange
                var componentCount = 500;
                var dataManager = new GameObject("DataManager").AddComponent<PersistentDataManager>();
                await dataManager.InitializeAsync();

                var components = new List<TestPerformanceComponent>();
                
                for (int i = 0; i < componentCount; i++)
                {
                    var obj = new GameObject($"Component{i}");
                    var comp = obj.AddComponent<TestPerformanceComponent>();
                    comp.Data = new float[100]; // Each component has some data
                    
                    for (int j = 0; j < comp.Data.Length; j++)
                    {
                        comp.Data[j] = UnityEngine.Random.Range(0f, 100f);
                    }
                    
                    dataManager.RegisterComponent($"comp_{i}", comp);
                    components.Add(comp);
                }

                // Act - Test save performance
                var stopwatch = Stopwatch.StartNew();
                var saveResult = await dataManager.SaveAllDataAsync();
                var saveTime = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                var loadResult = await dataManager.LoadAllDataAsync();
                var loadTime = stopwatch.ElapsedMilliseconds;

                // Assert
                Assert.IsTrue(saveResult.IsSuccess, "Save operation should succeed");
                Assert.IsTrue(loadResult.IsSuccess, "Load operation should succeed");
                Assert.AreEqual(componentCount, saveResult.SuccessfulOperations, "All components should save");
                Assert.AreEqual(componentCount, loadResult.SuccessfulOperations, "All components should load");

                var savePerformance = componentCount / (saveTime / 1000.0);
                var loadPerformance = componentCount / (loadTime / 1000.0);

                Assert.Greater(savePerformance, 50, $"Save performance too low: {savePerformance:F1} components/sec");
                Assert.Greater(loadPerformance, 100, $"Load performance too low: {loadPerformance:F1} components/sec");

                UnityEngine.Debug.Log($"Data Manager Performance: " +
                                    $"Save: {savePerformance:F1} comp/sec ({saveTime}ms), " +
                                    $"Load: {loadPerformance:F1} comp/sec ({loadTime}ms)");

                // Cleanup
                foreach (var comp in components)
                {
                    if (comp != null && comp.gameObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(comp.gameObject);
                    }
                }
                
                if (dataManager != null)
                {
                    UnityEngine.Object.DestroyImmediate(dataManager.gameObject);
                }
            });
        }

        #endregion

        #region Benchmark Tests

        [Test, Performance]
        public void BenchmarkTest_FormatterComparison()
        {
            // Arrange
            const int iterations = 10000;
            var testVector = new Vector3(1.234f, 5.678f, 9.012f);
            var results = new Dictionary<string, long>();

            // Benchmark Unity Vector3 Formatter
            var unityFormatter = new UnityVector3Formatter();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>();
                var arrayWriter = new ArrayBufferWriter<byte>();
                writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(arrayWriter);
                
                unityFormatter.Serialize(ref writer, ref testVector);
                writer.Flush();
            }

            stopwatch.Stop();
            results["Unity Vector3 Formatter"] = stopwatch.ElapsedMilliseconds;

            // Report results
            foreach (var (name, time) in results)
            {
                var operationsPerSecond = iterations / (time / 1000.0);
                UnityEngine.Debug.Log($"{name}: {operationsPerSecond:F0} ops/sec ({time}ms total)");
                
                Assert.Greater(operationsPerSecond, MIN_THROUGHPUT_OBJECTS_PER_SECOND, 
                    $"{name} performance below threshold");
            }
        }

        [UnityTest, Performance]
        public IEnumerator BenchmarkTest_EndToEndScenario()
        {
            return UniTask.ToCoroutine(async () =>
            {
                // Arrange - Simulate a realistic game scenario
                var gameObjects = new List<GameObject>();
                var sceneManager = new GameObject("SceneManager").AddComponent<SceneSerializationManager>();
                
                // Create a complex scene
                for (int i = 0; i < 100; i++)
                {
                    var obj = new GameObject($"GameObject{i}");
                    obj.transform.position = UnityEngine.Random.insideUnitSphere * 50f;
                    obj.transform.rotation = UnityEngine.Random.rotation;
                    
                    // Add various components
                    obj.AddComponent<TestPerformanceComponent>();
                    if (i % 3 == 0) obj.AddComponent<TransformSerializer>();
                    
                    gameObjects.Add(obj);
                    sceneManager.RegisterSceneObject(obj);
                }

                await sceneManager.InitializeAsync();

                // Act - Measure complete save/load cycle
                var totalStopwatch = Stopwatch.StartNew();
                
                var saveResult = await sceneManager.SaveSceneDataAsync();
                var saveTime = totalStopwatch.ElapsedMilliseconds;
                
                totalStopwatch.Restart();
                var loadResult = await sceneManager.LoadSceneDataAsync();
                var loadTime = totalStopwatch.ElapsedMilliseconds;
                
                totalStopwatch.Stop();
                var totalTime = saveTime + loadTime;

                // Assert
                Assert.IsTrue(saveResult.IsSuccess, "Scene save should succeed");
                Assert.IsTrue(loadResult.IsSuccess, "Scene load should succeed");
                Assert.Less(totalTime, 5000, $"End-to-end scenario too slow: {totalTime}ms");

                var objectsPerSecond = (saveResult.ObjectCount + loadResult.ObjectCount) / (totalTime / 1000.0);
                Assert.Greater(objectsPerSecond, 20, $"End-to-end throughput too low: {objectsPerSecond:F1} objects/sec");

                UnityEngine.Debug.Log($"End-to-End Benchmark: " +
                                    $"Save: {saveTime}ms ({saveResult.ObjectCount} objects), " +
                                    $"Load: {loadTime}ms ({loadResult.ObjectCount} objects), " +
                                    $"Total: {totalTime}ms, " +
                                    $"Throughput: {objectsPerSecond:F1} objects/sec");

                // Cleanup
                foreach (var obj in gameObjects)
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                }
                
                if (sceneManager != null)
                {
                    UnityEngine.Object.DestroyImmediate(sceneManager.gameObject);
                }
            });
        }

        #endregion
    }

    /// <summary>
    /// Test component for performance testing scenarios.
    /// </summary>
    public class TestPerformanceComponent : SerializableMonoBehaviour
    {
        public float[] Data { get; set; } = new float[10];

        protected override object GetSerializableData()
        {
            return new PerformanceTestData
            {
                Values = Data,
                Timestamp = DateTime.UtcNow.Ticks
            };
        }

        protected override async UniTask SetSerializableDataAsync(object data)
        {
            await UniTask.SwitchToMainThread();

            if (data is PerformanceTestData testData)
            {
                Data = testData.Values ?? new float[10];
            }
        }

        [MemoryPack.MemoryPackable]
        public partial class PerformanceTestData
        {
            public float[] Values { get; set; }
            public long Timestamp { get; set; }
        }
    }
}