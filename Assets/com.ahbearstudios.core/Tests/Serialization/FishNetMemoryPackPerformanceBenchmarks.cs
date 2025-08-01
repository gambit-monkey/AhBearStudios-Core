using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Tests.Mocks;
using MemoryPack;

namespace AhBearStudios.Core.Tests.Serialization
{
    /// <summary>
    /// Performance benchmarks comparing FishNet + MemoryPack integration against traditional approaches.
    /// These tests measure throughput, allocation, and latency characteristics.
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class FishNetMemoryPackPerformanceBenchmarks
    {
        private ILoggingService _mockLoggingService;
        private ISerializationService _mockSerializationService;
        private NetworkSerializationBufferPool _bufferPool;
        private FishNetSerializationAdapter _fishNetAdapter;
        private IPoolingService _mockPoolingService;

        private const int WarmupIterations = 100;
        private const int BenchmarkIterations = 10000;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _mockLoggingService = new MockLoggingService();
            _mockSerializationService = new MockSerializationService();
            _mockPoolingService = new MockPoolingService();

            var networkConfig = NetworkPoolingConfig.CreateHighPerformance();
            _bufferPool = new NetworkSerializationBufferPool(_mockPoolingService, _mockLoggingService, networkConfig);
            _fishNetAdapter = new FishNetSerializationAdapter(_mockLoggingService, _mockSerializationService, _bufferPool);

            // Warmup JIT and pools
            WarmupSystem();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _bufferPool?.Dispose();
        }

        private void WarmupSystem()
        {
            var testVector = new Vector3(1f, 2f, 3f);
            
            for (int i = 0; i < WarmupIterations; i++)
            {
                // Warmup standard serialization
                var data = _fishNetAdapter.SerializeToBytes(testVector);
                var result = _fishNetAdapter.DeserializeFromBytes<Vector3>(data);

                // Warmup pooled serialization
                var buffer = _fishNetAdapter.SerializeToPooledBuffer(testVector);
                try
                {
                    var pooledResult = _fishNetAdapter.DeserializeFromPooledBuffer<Vector3>(buffer);
                }
                finally
                {
                    _bufferPool.ReturnBuffer(buffer);
                }
            }
        }

        #region Throughput Benchmarks

        [Test]
        public void Benchmark_Vector3Serialization_Throughput()
        {
            // Arrange
            var testVector = new Vector3(1.5f, 2.5f, 3.5f);
            var results = new List<double>();

            // Run multiple benchmark rounds
            for (int round = 0; round < 5; round++)
            {
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < BenchmarkIterations; i++)
                {
                    var data = _fishNetAdapter.SerializeToBytes(testVector);
                    var result = _fishNetAdapter.DeserializeFromBytes<Vector3>(data);
                }

                stopwatch.Stop();
                var opsPerSecond = BenchmarkIterations / stopwatch.Elapsed.TotalSeconds;
                results.Add(opsPerSecond);
            }

            // Report results
            var avgThroughput = results.Average();
            var minThroughput = results.Min();
            var maxThroughput = results.Max();

            UnityEngine.Debug.Log($"Vector3 Serialization Throughput:");
            UnityEngine.Debug.Log($"  Average: {avgThroughput:N0} ops/sec");
            UnityEngine.Debug.Log($"  Min: {minThroughput:N0} ops/sec");
            UnityEngine.Debug.Log($"  Max: {maxThroughput:N0} ops/sec");

            // Performance assertions
            Assert.Greater(avgThroughput, 50000, "Should achieve at least 50K ops/sec for Vector3");
        }

        [Test]
        public void Benchmark_PooledBufferSerialization_Throughput()
        {
            // Arrange
            var testVector = new Vector3(1.5f, 2.5f, 3.5f);
            var results = new List<double>();

            // Run multiple benchmark rounds
            for (int round = 0; round < 5; round++)
            {
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < BenchmarkIterations; i++)
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

                stopwatch.Stop();
                var opsPerSecond = BenchmarkIterations / stopwatch.Elapsed.TotalSeconds;
                results.Add(opsPerSecond);
            }

            // Report results
            var avgThroughput = results.Average();
            var poolStats = _bufferPool.GetStatistics();

            UnityEngine.Debug.Log($"Pooled Buffer Serialization Throughput:");
            UnityEngine.Debug.Log($"  Average: {avgThroughput:N0} ops/sec");
            UnityEngine.Debug.Log($"  Buffer Return Rate: {poolStats.BufferReturnRate:P2}");

            // Performance assertions
            Assert.Greater(avgThroughput, 40000, "Should achieve at least 40K ops/sec with pooled buffers");
            Assert.GreaterOrEqual(poolStats.BufferReturnRate, 0.99, "Should maintain 99%+ buffer return rate");
        }

        [Test]
        public void Benchmark_ComplexTypeSerialization_Throughput()
        {
            // Arrange
            var testData = new BenchmarkNetworkData
            {
                PlayerId = 12345,
                Position = new Vector3(10f, 20f, 30f),
                Rotation = Quaternion.Euler(45f, 90f, 180f),
                Velocity = new Vector3(1f, 0f, 2f),
                Health = 85.5f,
                MaxHealth = 100f,
                PlayerName = "BenchmarkPlayer",
                IsAlive = true,
                LastActionTime = DateTime.UtcNow,
                Experience = 1500,
                Level = 25
            };

            var results = new List<double>();

            // Run benchmark
            for (int round = 0; round < 3; round++)
            {
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < BenchmarkIterations / 10; i++) // Fewer iterations for complex types
                {
                    var data = _fishNetAdapter.SerializeToBytes(testData);
                    var result = _fishNetAdapter.DeserializeFromBytes<BenchmarkNetworkData>(data);
                }

                stopwatch.Stop();
                var opsPerSecond = (BenchmarkIterations / 10) / stopwatch.Elapsed.TotalSeconds;
                results.Add(opsPerSecond);
            }

            var avgThroughput = results.Average();
            
            UnityEngine.Debug.Log($"Complex Type Serialization Throughput:");
            UnityEngine.Debug.Log($"  Average: {avgThroughput:N0} ops/sec");

            Assert.Greater(avgThroughput, 5000, "Should achieve at least 5K ops/sec for complex types");
        }

        #endregion

        #region Latency Benchmarks

        [Test]
        public void Benchmark_SerializationLatency_Percentiles()
        {
            // Arrange
            var testVector = new Vector3(1f, 2f, 3f);
            var latencies = new List<double>();

            // Collect latency samples
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                var data = _fishNetAdapter.SerializeToBytes(testVector);
                var result = _fishNetAdapter.DeserializeFromBytes<Vector3>(data);
                
                stopwatch.Stop();
                latencies.Add(stopwatch.Elapsed.TotalMicroseconds);
            }

            // Calculate percentiles
            latencies.Sort();
            var p50 = latencies[latencies.Count / 2];
            var p90 = latencies[(int)(latencies.Count * 0.9)];
            var p99 = latencies[(int)(latencies.Count * 0.99)];
            var max = latencies.Last();

            UnityEngine.Debug.Log($"Serialization Latency Percentiles (microseconds):");
            UnityEngine.Debug.Log($"  P50: {p50:F2}μs");
            UnityEngine.Debug.Log($"  P90: {p90:F2}μs"); 
            UnityEngine.Debug.Log($"  P99: {p99:F2}μs");
            UnityEngine.Debug.Log($"  Max: {max:F2}μs");

            // Latency assertions (reasonable expectations for MemoryPack)
            Assert.Less(p50, 50, "P50 latency should be under 50 microseconds");
            Assert.Less(p90, 100, "P90 latency should be under 100 microseconds");
            Assert.Less(p99, 500, "P99 latency should be under 500 microseconds");
        }

        #endregion

        #region Memory Allocation Tests

        [Test]
        public void Benchmark_PooledVsStandard_AllocationComparison()
        {
            // This test would ideally use a memory profiler, but we'll simulate with GC tracking
            var testVector = new Vector3(1f, 2f, 3f);
            const int testIterations = 1000;

            // Force GC before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            // Standard serialization (should allocate)
            for (int i = 0; i < testIterations; i++)
            {
                var data = _fishNetAdapter.SerializeToBytes(testVector);
                var result = _fishNetAdapter.DeserializeFromBytes<Vector3>(data);
            }

            var standardMemory = GC.GetTotalMemory(false) - initialMemory;

            // Force GC again
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforePooledMemory = GC.GetTotalMemory(false);

            // Pooled serialization (should allocate less)
            for (int i = 0; i < testIterations; i++)
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

            var pooledMemory = GC.GetTotalMemory(false) - beforePooledMemory;

            UnityEngine.Debug.Log($"Memory Allocation Comparison ({testIterations} operations):");
            UnityEngine.Debug.Log($"  Standard approach: {standardMemory:N0} bytes");
            UnityEngine.Debug.Log($"  Pooled approach: {pooledMemory:N0} bytes");
            UnityEngine.Debug.Log($"  Memory savings: {((double)(standardMemory - pooledMemory) / standardMemory):P1}");

            // The pooled approach should allocate significantly less memory
            Assert.Less(pooledMemory, standardMemory, "Pooled approach should allocate less memory");
        }

        #endregion

        #region Concurrent Performance Tests

        [Test]
        public void Benchmark_ConcurrentThroughput_ScalabilityTest()
        {
            var testVector = new Vector3(1f, 2f, 3f);
            var threadCounts = new[] { 1, 2, 4, 8 };
            var results = new Dictionary<int, double>();

            foreach (var threadCount in threadCounts)
            {
                var tasks = new List<System.Threading.Tasks.Task>();
                var stopwatch = Stopwatch.StartNew();
                var iterationsPerThread = BenchmarkIterations / threadCount;

                for (int t = 0; t < threadCount; t++)
                {
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        for (int i = 0; i < iterationsPerThread; i++)
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
                    }));
                }

                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
                stopwatch.Stop();

                var totalOps = BenchmarkIterations;
                var opsPerSecond = totalOps / stopwatch.Elapsed.TotalSeconds;
                results[threadCount] = opsPerSecond;

                UnityEngine.Debug.Log($"Concurrent Performance ({threadCount} threads): {opsPerSecond:N0} ops/sec");
            }

            // Verify scaling characteristics
            Assert.Greater(results[2], results[1] * 1.5, "Should see significant improvement with 2 threads");
            Assert.Greater(results[4], results[2] * 1.3, "Should see good improvement with 4 threads");
        }

        #endregion

        #region Buffer Pool Performance Tests

        [Test]
        public void Benchmark_BufferPoolEfficiency_GetReturnCycles()
        {
            const int cycles = 10000;
            var bufferSizes = new[] { 1024, 16384, 65536 }; // Small, medium, large

            foreach (var size in bufferSizes)
            {
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < cycles; i++)
                {
                    var buffer = _bufferPool.GetBuffer(size);
                    _bufferPool.ReturnBuffer(buffer);
                }

                stopwatch.Stop();
                var cyclesPerSecond = cycles / stopwatch.Elapsed.TotalSeconds;

                UnityEngine.Debug.Log($"Buffer Pool ({size} bytes): {cyclesPerSecond:N0} get/return cycles/sec");
                
                // Buffer pool should be very fast
                Assert.Greater(cyclesPerSecond, 100000, $"Buffer pool should handle 100K+ cycles/sec for {size} byte buffers");
            }

            var finalStats = _bufferPool.GetStatistics();
            Assert.GreaterOrEqual(finalStats.BufferReturnRate, 0.99, "Should maintain 99%+ return rate");
        }

        #endregion
    }

    /// <summary>
    /// Complex test data structure for performance benchmarking.
    /// </summary>
    [MemoryPackable]
    public partial struct BenchmarkNetworkData
    {
        public int PlayerId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public string PlayerName { get; set; }
        public bool IsAlive { get; set; }
        public DateTime LastActionTime { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
    }
}