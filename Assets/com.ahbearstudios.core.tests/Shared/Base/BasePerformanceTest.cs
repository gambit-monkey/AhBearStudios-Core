using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Shared.Base
{
    /// <summary>
    /// Base class for performance tests that measure frame budget compliance and zero-allocation patterns.
    /// Ensures Unity game development performance requirements are met.
    /// </summary>
    [TestFixture]
    public abstract class BasePerformanceTest : BaseServiceTest
    {
        protected PerformanceTestHelper PerformanceHelper { get; private set; }
        protected AllocationTracker AllocationTracker { get; private set; }

        // Unity 60 FPS frame budget: 16.67ms
        protected static readonly TimeSpan FrameBudget = TimeSpan.FromMilliseconds(16.67);
        protected static readonly TimeSpan WarningThreshold = TimeSpan.FromMilliseconds(10.0);

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            PerformanceHelper = new PerformanceTestHelper();
            AllocationTracker = new AllocationTracker();

            OnPerformanceSetup();
        }

        [TearDown]
        public override void TearDown()
        {
            OnPerformanceTearDown();

            PerformanceHelper?.Dispose();
            AllocationTracker?.Dispose();

            base.TearDown();
        }

        /// <summary>
        /// Override this method to perform additional performance test setup.
        /// </summary>
        protected virtual void OnPerformanceSetup()
        {
            // Default: no additional setup
        }

        /// <summary>
        /// Override this method to perform additional performance test teardown.
        /// </summary>
        protected virtual void OnPerformanceTearDown()
        {
            // Default: no additional teardown
        }

        /// <summary>
        /// Measures the performance of an operation and ensures it meets frame budget requirements.
        /// </summary>
        protected async Task<PerformanceResult> MeasureFrameBudgetAsync(
            Func<UniTask> operation,
            string operationName,
            bool enforceBudget = true)
        {
            var result = await PerformanceHelper.MeasureAsync(operation, operationName);

            if (enforceBudget)
            {
                Assert.That(result.Duration, Is.LessThan(FrameBudget),
                    $"Operation '{operationName}' took {result.Duration.TotalMilliseconds:F2}ms, " +
                    $"exceeding 60 FPS frame budget of {FrameBudget.TotalMilliseconds:F2}ms");
            }
            else if (result.Duration > WarningThreshold)
            {
                TestContext.WriteLine($"WARNING: Operation '{operationName}' took {result.Duration.TotalMilliseconds:F2}ms, " +
                                    $"approaching frame budget limit");
            }

            return result;
        }

        /// <summary>
        /// Measures the performance of a synchronous operation.
        /// </summary>
        protected PerformanceResult MeasureFrameBudget(
            Action operation,
            string operationName,
            bool enforceBudget = true)
        {
            var result = PerformanceHelper.Measure(operation, operationName);

            if (enforceBudget)
            {
                Assert.That(result.Duration, Is.LessThan(FrameBudget),
                    $"Operation '{operationName}' took {result.Duration.TotalMilliseconds:F2}ms, " +
                    $"exceeding 60 FPS frame budget of {FrameBudget.TotalMilliseconds:F2}ms");
            }
            else if (result.Duration > WarningThreshold)
            {
                TestContext.WriteLine($"WARNING: Operation '{operationName}' took {result.Duration.TotalMilliseconds:F2}ms, " +
                                    $"approaching frame budget limit");
            }

            return result;
        }

        /// <summary>
        /// Performs a bulk operation test to measure throughput and scalability.
        /// </summary>
        protected async Task<BulkPerformanceResult> MeasureBulkOperationAsync<T>(
            Func<IEnumerable<T>, UniTask> bulkOperation,
            IEnumerable<T> testData,
            string operationName,
            int maxItemsPerFrame = 100)
        {
            var items = testData.AsValueEnumerable().ToList();
            var itemCount = items.Count;

            // Test with increasing batch sizes
            var results = new List<PerformanceResult>();
            var batchSizes = new[] { 1, 10, 50, 100, Math.Min(500, itemCount), Math.Min(1000, itemCount) };

            foreach (var batchSize in batchSizes)
            {
                if (batchSize > itemCount) continue;

                var batch = items.Take(batchSize).ToList();
                var batchResult = await PerformanceHelper.MeasureAsync(
                    () => bulkOperation(batch),
                    $"{operationName}_Batch_{batchSize}");

                results.Add(batchResult);

                // Verify per-item processing time stays reasonable
                var perItemTime = batchResult.Duration.TotalMilliseconds / batchSize;
                if (perItemTime > 0.1) // 0.1ms per item max
                {
                    TestContext.WriteLine($"WARNING: Per-item processing time is {perItemTime:F3}ms for batch size {batchSize}");
                }
            }

            return new BulkPerformanceResult(operationName, results, itemCount);
        }

        /// <summary>
        /// Measures memory allocation during an operation to verify zero-allocation patterns.
        /// </summary>
        protected AllocationResult MeasureAllocations(Action operation, string operationName)
        {
            return AllocationTracker.MeasureAllocations(operation, operationName);
        }

        /// <summary>
        /// Measures memory allocation during an async operation.
        /// </summary>
        protected async Task<AllocationResult> MeasureAllocationsAsync(Func<UniTask> operation, string operationName)
        {
            return await AllocationTracker.MeasureAllocationsAsync(operation, operationName);
        }

        /// <summary>
        /// Verifies that an operation produces zero allocations (Unity Collections pattern).
        /// </summary>
        protected void AssertZeroAllocations(AllocationResult result)
        {
            Assert.That(result.TotalAllocations, Is.EqualTo(0),
                $"Operation '{result.OperationName}' should produce zero allocations but allocated {result.TotalBytes} bytes");
        }

        /// <summary>
        /// Verifies that allocations are within acceptable limits for non-critical operations.
        /// </summary>
        protected void AssertAcceptableAllocations(AllocationResult result, long maxBytes = 1024)
        {
            Assert.That(result.TotalBytes, Is.LessThanOrEqualTo(maxBytes),
                $"Operation '{result.OperationName}' allocated {result.TotalBytes} bytes, " +
                $"exceeding limit of {maxBytes} bytes");
        }

        /// <summary>
        /// Performs a stress test by repeatedly executing an operation.
        /// </summary>
        protected async Task<StressTestResult> PerformStressTestAsync(
            Func<UniTask> operation,
            string operationName,
            int iterations = 1000,
            TimeSpan? totalTimeLimit = null)
        {
            totalTimeLimit ??= TimeSpan.FromMinutes(1);

            var results = new List<PerformanceResult>();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations && stopwatch.Elapsed < totalTimeLimit; i++)
            {
                var result = await PerformanceHelper.MeasureAsync(operation, $"{operationName}_Iteration_{i}");
                results.Add(result);

                // Check if any iteration exceeded frame budget
                if (result.Duration > FrameBudget)
                {
                    TestContext.WriteLine($"WARNING: Iteration {i} exceeded frame budget: {result.Duration.TotalMilliseconds:F2}ms");
                }
            }

            return new StressTestResult(operationName, results, iterations);
        }

        /// <summary>
        /// Logs performance metrics for analysis.
        /// </summary>
        protected void LogPerformanceMetrics(PerformanceResult result)
        {
            TestContext.WriteLine($"Performance Metrics for '{result.OperationName}':");
            TestContext.WriteLine($"  Duration: {result.Duration.TotalMilliseconds:F2}ms");
            TestContext.WriteLine($"  Frame Budget Compliance: {(result.Duration < FrameBudget ? "PASS" : "FAIL")}");
            TestContext.WriteLine($"  Memory Used: {result.MemoryUsed:F2} MB");
        }
    }
}