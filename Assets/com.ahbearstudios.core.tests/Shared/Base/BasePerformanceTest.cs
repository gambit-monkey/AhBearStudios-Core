using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Tests.Shared.Base
{
    /// <summary>
    /// Base class for performance tests that measure frame budget compliance and zero-allocation patterns.
    /// Provides lightweight TDD-compliant test doubles for testing service performance interactions.
    /// Ensures Unity game development performance requirements are met with robust TDD patterns.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    [TestFixture]
    public abstract class BasePerformanceTest : BaseServiceTest
    {
        protected PerformanceTestHelper PerformanceHelper { get; private set; }
        protected AllocationTracker AllocationTracker { get; private set; }
        protected ServiceTestContainer ServiceContainer { get; private set; }

        // Use shared constants for consistency across all test classes
        protected static readonly TimeSpan FrameBudget = TestConstants.FrameBudget;
        protected static readonly TimeSpan WarningThreshold = TestConstants.WarningThreshold;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Initialize performance test specific components
            PerformanceHelper = new PerformanceTestHelper();
            AllocationTracker = new AllocationTracker();
            ServiceContainer = new ServiceTestContainer();

            // Register all test doubles in the service container for performance interaction testing
            RegisterTestDoubles();

            OnPerformanceSetup();
        }

        [TearDown]
        public override void TearDown()
        {
            OnPerformanceTearDown();

            // Clean up performance test components
            PerformanceHelper?.Dispose();
            AllocationTracker?.Dispose();
            ServiceContainer?.Dispose();

            base.TearDown();
        }

        /// <summary>
        /// Registers all test doubles in the service container for performance interaction testing.
        /// Enables realistic performance testing with service dependencies.
        /// </summary>
        private void RegisterTestDoubles()
        {
            ServiceContainer.RegisterInstance(StubLogging);
            ServiceContainer.RegisterInstance(SpyMessageBus);
            ServiceContainer.RegisterInstance(FakeSerialization);
            ServiceContainer.RegisterInstance(FakePooling);
            ServiceContainer.RegisterInstance(NullProfiler);
            ServiceContainer.RegisterInstance(StubHealthCheck);
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
        /// Unity Test Runner compatible async operation with TDD test double integration.
        /// </summary>
        protected async UniTask<PerformanceResult> MeasureFrameBudgetAsync(
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
        /// Unity Test Runner compatible with frame budget validation.
        /// </summary>
        protected async UniTask<BulkPerformanceResult> MeasureBulkOperationAsync<T>(
            Func<IEnumerable<T>, UniTask> bulkOperation,
            IEnumerable<T> testData,
            string operationName,
            int maxItemsPerFrame = 100)
        {
            var items = testData.ToList();
            var itemCount = items.Count;

            // Test with increasing batch sizes
            var results = new List<PerformanceResult>();
            var batchSizes = new int[] { 1, 10, 50, 100, Math.Min(500, itemCount), Math.Min(1000, itemCount) };

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
        /// Unity Test Runner compatible async allocation tracking.
        /// </summary>
        protected async UniTask<AllocationResult> MeasureAllocationsAsync(Func<UniTask> operation, string operationName)
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
        /// Unity Test Runner compatible with realistic game load testing (1000+ operations).
        /// </summary>
        protected async UniTask<StressTestResult> PerformStressTestAsync(
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
        /// Logs performance metrics for analysis using the test double logging service.
        /// Integrates with TDD patterns by using shared test doubles.
        /// </summary>
        protected void LogPerformanceMetrics(PerformanceResult result)
        {
            var metricsMessage = $"Performance Metrics for '{result.OperationName}': " +
                               $"Duration: {result.Duration.TotalMilliseconds:F2}ms, " +
                               $"Frame Budget: {(result.Duration < FrameBudget ? "PASS" : "FAIL")}, " +
                               $"Memory: {result.MemoryUsed:F2}MB";

            // Log using the stub logging service for consistency with TDD patterns
            StubLogging.LogInfo(metricsMessage);

            // Also output to test console for immediate visibility
            TestContext.WriteLine(metricsMessage);
        }

        /// <summary>
        /// Measures performance of service interactions using test doubles.
        /// Validates that service coordination maintains frame budget compliance.
        /// Essential for integration performance testing following TDD patterns.
        /// </summary>
        protected async UniTask<PerformanceResult> MeasureServiceInteractionPerformanceAsync(
            Func<UniTask> serviceOperation,
            string operationName,
            int expectedLogEntries = 1,
            int expectedMessages = 0)
        {
            // Capture initial state
            var initialLogCount = StubLogging.RecordedLogs.Count;
            var initialMessageCount = SpyMessageBus.PublishedMessages.Count;

            // Measure the service operation performance
            var result = await MeasureFrameBudgetAsync(serviceOperation, operationName);

            // Verify service interactions occurred as expected
            var actualLogEntries = StubLogging.RecordedLogs.Count - initialLogCount;
            var actualMessages = SpyMessageBus.PublishedMessages.Count - initialMessageCount;

            Assert.That(actualLogEntries, Is.GreaterThanOrEqualTo(expectedLogEntries),
                $"Service operation should generate at least {expectedLogEntries} log entries");

            if (expectedMessages > 0)
            {
                Assert.That(actualMessages, Is.GreaterThanOrEqualTo(expectedMessages),
                    $"Service operation should generate at least {expectedMessages} messages");
            }

            // Log comprehensive metrics
            LogPerformanceMetrics(result);

            return result;
        }

        /// <summary>
        /// Validates performance of message publishing and correlation tracking.
        /// Tests frame budget compliance for message bus operations with correlation.
        /// </summary>
        protected async UniTask<PerformanceResult> MeasureMessageFlowPerformanceAsync<TMessage>(
            Func<Guid, UniTask<TMessage>> messageOperation,
            string operationName) where TMessage : IMessage
        {
            var correlationId = CreateTestCorrelationId(operationName);

            var result = await MeasureFrameBudgetAsync(async () =>
            {
                var message = await messageOperation(correlationId);
                SpyMessageBus.PublishMessage(message);
            }, operationName);

            // Verify correlation tracking performance
            var correlatedMessage = SpyMessageBus.GetLastMessage<TMessage>();
            Assert.That(correlatedMessage, Is.Not.Null, "Message should be published");
            Assert.That(correlatedMessage.CorrelationId, Is.EqualTo(correlationId),
                "Correlation ID should be maintained in performance scenarios");

            LogPerformanceMetrics(result);
            return result;
        }

        /// <summary>
        /// Measures zero-allocation patterns with service interactions.
        /// Validates that service operations using test doubles maintain zero-allocation performance.
        /// Critical for Unity game development performance requirements.
        /// </summary>
        protected async UniTask<AllocationResult> MeasureServiceAllocationPatternAsync(
            Func<UniTask> serviceOperation,
            string operationName,
            long maxAllowedBytes = 1024)
        {
            // Use allocation tracker with service operation
            var allocationResult = await MeasureAllocationsAsync(serviceOperation, operationName);

            // Verify allocation compliance
            Assert.That(allocationResult.TotalBytes, Is.LessThanOrEqualTo(maxAllowedBytes),
                $"Service operation '{operationName}' allocated {allocationResult.TotalBytes} bytes, " +
                $"exceeding maximum of {maxAllowedBytes} bytes");

            // Log allocation details using stub logging service
            StubLogging.LogInfo($"Allocation Pattern for '{operationName}': " +
                              $"{allocationResult.TotalBytes} bytes, " +
                              $"GC Collections: Gen0={allocationResult.Gen0Collections}, " +
                              $"Gen1={allocationResult.Gen1Collections}, Gen2={allocationResult.Gen2Collections}");

            return allocationResult;
        }

        /// <summary>
        /// Validates concurrent service operation performance.
        /// Tests that multiple services can operate concurrently within frame budget.
        /// Uses test doubles for realistic concurrent service interaction testing.
        /// </summary>
        protected async UniTask<PerformanceResult> MeasureConcurrentServicePerformanceAsync(
            Func<int, UniTask> concurrentOperation,
            string operationName,
            int concurrencyLevel = 10)
        {
            var tasks = new List<UniTask>();

            for (int i = 0; i < concurrencyLevel; i++)
            {
                tasks.Add(concurrentOperation(i));
            }

            var result = await MeasureFrameBudgetAsync(
                () => UniTask.WhenAll(tasks),
                $"{operationName}_Concurrent_{concurrencyLevel}");

            // Verify no errors occurred during concurrent execution
            AssertNoErrors();

            // Verify services remained healthy
            Assert.That(StubLogging.IsEnabled, Is.True, "Logging service should remain enabled");
            Assert.That(SpyMessageBus.IsEnabled, Is.True, "Message bus should remain enabled");
            Assert.That(FakeSerialization.IsEnabled, Is.True, "Serialization service should remain enabled");

            LogPerformanceMetrics(result);
            return result;
        }

        /// <summary>
        /// Measures pooling service performance impact on frame budget.
        /// Validates that object pooling operations maintain 60 FPS performance.
        /// </summary>
        protected async UniTask<PerformanceResult> MeasurePoolingPerformanceAsync<T>(
            Func<UniTask> poolingOperation,
            string operationName,
            int expectedPoolingCalls = 1) where T : class, AhBearStudios.Core.Pooling.IPooledObject, new()
        {
            var initialGetCalls = FakePooling.GetCallCount<T>();
            var initialReturnCalls = FakePooling.ReturnCallCount<T>();

            var result = await MeasureFrameBudgetAsync(poolingOperation, operationName);

            var actualGetCalls = FakePooling.GetCallCount<T>() - initialGetCalls;
            var actualReturnCalls = FakePooling.ReturnCallCount<T>() - initialReturnCalls;

            Assert.That(actualGetCalls + actualReturnCalls, Is.GreaterThanOrEqualTo(expectedPoolingCalls),
                $"Pooling operation should generate at least {expectedPoolingCalls} pooling calls");

            LogPerformanceMetrics(result);
            return result;
        }

        /// <summary>
        /// Comprehensive performance validation that includes all service health checks.
        /// Should be called at the end of complex performance tests to ensure system stability.
        /// </summary>
        protected void AssertComprehensivePerformanceHealth()
        {
            // Verify all services remain enabled and healthy after performance testing
            Assert.That(StubLogging.IsEnabled, Is.True, "Logging service should remain enabled");
            Assert.That(SpyMessageBus.IsEnabled, Is.True, "Message bus service should remain enabled");
            Assert.That(FakeSerialization.IsEnabled, Is.True, "Serialization service should remain enabled");
            Assert.That(FakePooling.IsEnabled, Is.True, "Pooling service should remain enabled");
            Assert.That(NullProfiler.IsEnabled, Is.False, "Null profiler should remain disabled");
            Assert.That(StubHealthCheck.IsEnabled, Is.True, "Health check service should remain enabled");

            // Verify no errors were generated during performance testing
            AssertNoErrors();

            // Verify service container is still properly configured
            Assert.That(ServiceContainer.RegisteredServiceCount, Is.GreaterThan(0),
                "Service container should maintain registered services");

            // Log comprehensive performance health status
            StubLogging.LogInfo("Comprehensive performance health check completed successfully");
        }
    }
}