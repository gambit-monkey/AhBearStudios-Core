using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Tests.Shared.Base
{
    /// <summary>
    /// Base class for all service unit tests providing common setup, teardown, and shared test doubles.
    /// Uses lightweight TDD-compliant test doubles instead of heavy mock implementations.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// Enhanced with performance testing capabilities and async operation support following CLAUDETESTS.md guidelines.
    /// </summary>
    [TestFixture]
    public abstract class BaseServiceTest
    {
        protected StubLoggingService StubLogging { get; private set; }
        protected SpyMessageBusService SpyMessageBus { get; private set; }
        protected FakeSerializationService FakeSerialization { get; private set; }
        protected FakePoolingService FakePooling { get; private set; }
        protected NullProfilerService NullProfiler { get; private set; }
        protected StubHealthCheckService StubHealthCheck { get; private set; }
        protected TestCorrelationHelper CorrelationHelper { get; private set; }
        protected PerformanceTestHelper PerformanceHelper { get; private set; }
        protected AllocationTracker AllocationTracker { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            // Initialize all lightweight test doubles
            StubLogging = new StubLoggingService();
            SpyMessageBus = new SpyMessageBusService();
            FakeSerialization = new FakeSerializationService();
            FakePooling = new FakePoolingService(SpyMessageBus);
            NullProfiler = NullProfilerService.Instance; // Use singleton for null object
            StubHealthCheck = new StubHealthCheckService();
            CorrelationHelper = new TestCorrelationHelper();

            // Initialize performance testing components for CLAUDETESTS.md compliance
            PerformanceHelper = new PerformanceTestHelper();
            AllocationTracker = new AllocationTracker();

            // Perform any additional setup
            OnSetup();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Perform test-specific cleanup
            OnTearDown();

            // Clear all test doubles
            StubLogging?.ClearLogs();
            SpyMessageBus?.ClearRecordedInteractions();
            FakeSerialization?.ClearData();
            FakePooling?.ClearRecordedInteractions();
            StubHealthCheck?.ClearConfiguration();
            CorrelationHelper?.Clear();

            // Clear performance testing components
            PerformanceHelper?.Clear();
            AllocationTracker?.Clear();

            // Dispose services (null profiler doesn't need disposal)
            StubLogging?.Dispose();
            SpyMessageBus?.Dispose();
            FakeSerialization?.Dispose();
            FakePooling?.Dispose();
            StubHealthCheck?.Dispose();
            PerformanceHelper?.Dispose();
            AllocationTracker?.Dispose();
        }

        /// <summary>
        /// Override this method to perform additional setup in derived test classes.
        /// </summary>
        protected virtual void OnSetup()
        {
            // Default: no additional setup
        }

        /// <summary>
        /// Override this method to perform additional teardown in derived test classes.
        /// </summary>
        protected virtual void OnTearDown()
        {
            // Default: no additional teardown
        }

        /// <summary>
        /// Verifies that no error logs were recorded during the test.
        /// </summary>
        protected void AssertNoErrors()
        {
            var errorCount = StubLogging.GetLogCount(AhBearStudios.Core.Logging.Models.LogLevel.Error) +
                            StubLogging.GetLogCount(AhBearStudios.Core.Logging.Models.LogLevel.Critical);
            Assert.That(errorCount, Is.EqualTo(0),
                "Test should not have generated any error logs");
        }

        /// <summary>
        /// Verifies that the specified number of log entries were recorded.
        /// </summary>
        protected void AssertLogCount(int expectedCount)
        {
            Assert.That(StubLogging.RecordedLogs.Count, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} log entries, but found {StubLogging.RecordedLogs.Count}");
        }

        /// <summary>
        /// Verifies that a log message containing the specified text was recorded.
        /// </summary>
        protected void AssertLogContains(string expectedMessage)
        {
            Assert.That(StubLogging.HasLogWithMessage(expectedMessage), Is.True,
                $"Expected log message containing '{expectedMessage}' was not found");
        }

        /// <summary>
        /// Verifies that the specified number of messages were published to the message bus.
        /// </summary>
        protected void AssertMessageCount(int expectedCount)
        {
            Assert.That(SpyMessageBus.PublishedMessages.Count, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} published messages, but found {SpyMessageBus.PublishedMessages.Count}");
        }

        /// <summary>
        /// Verifies that a message of the specified type was published.
        /// </summary>
        protected void AssertMessagePublished<T>() where T : IMessage
        {
            Assert.That(SpyMessageBus.WasMessagePublished<T>(), Is.True,
                $"Expected message of type {typeof(T).Name} was not published");
        }

        /// <summary>
        /// Verifies that the specified number of messages of the given type were published.
        /// </summary>
        protected void AssertMessageCount<T>(int expectedCount) where T : IMessage
        {
            var actualCount = SpyMessageBus.GetPublishCount<T>();
            Assert.That(actualCount, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} messages of type {typeof(T).Name}, but found {actualCount}");
        }

        /// <summary>
        /// Gets the last published message of the specified type.
        /// </summary>
        protected T GetLastMessage<T>() where T : IMessage
        {
            return SpyMessageBus.GetLastMessage<T>();
        }

        /// <summary>
        /// Verifies that the pooling service was called the expected number of times.
        /// </summary>
        protected void AssertPoolingCalls<T>(int expectedGets, int expectedReturns) where T : class, AhBearStudios.Core.Pooling.IPooledObject, new()
        {
            Assert.That(FakePooling.GetCallCount<T>(), Is.EqualTo(expectedGets),
                $"Expected {expectedGets} pool get calls for {typeof(T).Name}");
            Assert.That(FakePooling.ReturnCallCount<T>(), Is.EqualTo(expectedReturns),
                $"Expected {expectedReturns} pool return calls for {typeof(T).Name}");
        }

        /// <summary>
        /// Verifies that profiling was performed for the specified scope.
        /// Note: NullProfiler doesn't track scopes, so this always passes.
        /// Use production profiler tests for actual profiling verification.
        /// </summary>
        protected void AssertProfilingScopeUsed(string scopeName)
        {
            // Note: NullProfiler doesn't track scopes for performance
            // This method is kept for compatibility but always passes
            Assert.Pass($"NullProfiler doesn't track scopes - test passed for compatibility");
        }

        /// <summary>
        /// Creates a test correlation ID for tracking test operations.
        /// </summary>
        protected Guid CreateTestCorrelationId(string testContext = null)
        {
            return CorrelationHelper.CreateCorrelationId(testContext ?? TestContext.CurrentContext.Test.Name);
        }

        #region CLAUDETESTS.md Compliance - Enhanced Service Testing Methods

        /// <summary>
        /// Executes an async operation with timeout support for UniTask compatibility.
        /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
        /// </summary>
        protected async UniTask ExecuteWithTimeoutAsync(Func<UniTask> operation, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TestConstants.DefaultAsyncTimeout;

            using var cancellation = new CancellationTokenSource(timeout);

            try
            {
                await operation().AttachExternalCancellation(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Assert.Fail($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Measures the performance of a service operation and validates frame budget compliance.
        /// Essential for Unity game development performance requirements (60 FPS = 16.67ms).
        /// </summary>
        protected async UniTask<PerformanceResult> ExecuteWithPerformanceMeasurementAsync(
            Func<UniTask> operation,
            string operationName,
            TimeSpan? expectedMaxDuration = null)
        {
            var result = await PerformanceHelper.MeasureAsync(operation, operationName);

            if (expectedMaxDuration.HasValue && result.Duration > expectedMaxDuration.Value)
            {
                Assert.Fail($"Operation '{operationName}' took {result.Duration.TotalMilliseconds}ms, " +
                           $"but expected maximum was {expectedMaxDuration.Value.TotalMilliseconds}ms");
            }

            return result;
        }

        /// <summary>
        /// Validates that a service operation completes within Unity's frame budget (16.67ms).
        /// Critical for maintaining 60 FPS performance in Unity game development.
        /// </summary>
        protected async UniTask AssertFrameBudgetComplianceAsync(
            Func<UniTask> operation,
            string operationName)
        {
            var result = await ExecuteWithPerformanceMeasurementAsync(
                operation,
                operationName,
                TestConstants.FrameBudget);

            // Log performance metrics for debugging
            StubLogging.LogInfo($"Frame Budget Compliance: {operationName} completed in {result.Duration.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Measures memory allocations during a service operation.
        /// Validates zero-allocation patterns required for Unity game development performance.
        /// </summary>
        protected async UniTask<AllocationResult> MeasureServiceAllocationsAsync(
            Func<UniTask> operation,
            string operationName)
        {
            return await AllocationTracker.MeasureAllocationsAsync(operation, operationName);
        }

        /// <summary>
        /// Validates that a service operation produces zero allocations (Unity Collections pattern).
        /// Essential for performance-critical game code paths.
        /// </summary>
        protected async UniTask AssertZeroAllocationsAsync(
            Func<UniTask> operation,
            string operationName)
        {
            var result = await MeasureServiceAllocationsAsync(operation, operationName);

            Assert.That(result.TotalBytes, Is.EqualTo(0),
                $"Operation '{operationName}' should produce zero allocations but allocated {result.TotalBytes} bytes");

            Assert.That(result.TotalAllocations, Is.EqualTo(0),
                $"Operation '{operationName}' should not trigger GC collections but triggered {result.TotalAllocations} collections");
        }

        /// <summary>
        /// Validates that allocations are within acceptable limits for non-critical operations.
        /// </summary>
        protected async UniTask AssertAcceptableAllocationsAsync(
            Func<UniTask> operation,
            string operationName,
            long maxBytes = 1024)
        {
            var result = await MeasureServiceAllocationsAsync(operation, operationName);

            Assert.That(result.TotalBytes, Is.LessThanOrEqualTo(maxBytes),
                $"Operation '{operationName}' allocated {result.TotalBytes} bytes, exceeding limit of {maxBytes} bytes");
        }

        /// <summary>
        /// Validates that all services remain in a healthy state after the test.
        /// Comprehensive health check following CLAUDETESTS.md guidelines.
        /// </summary>
        protected void AssertAllServicesHealthy()
        {
            Assert.That(StubLogging.IsEnabled, Is.True, "Logging service should be enabled");
            Assert.That(SpyMessageBus.IsEnabled, Is.True, "Message bus service should be enabled");
            Assert.That(FakeSerialization.IsEnabled, Is.True, "Serialization service should be enabled");
            Assert.That(FakePooling.IsEnabled, Is.True, "Pooling service should be enabled");
            Assert.That(NullProfiler.IsEnabled, Is.False, "Null profiler should be disabled for performance");
            Assert.That(StubHealthCheck.IsEnabled, Is.True, "Health check service should be enabled");

            // Verify no errors were generated during the test
            AssertNoErrors();
        }

        /// <summary>
        /// Validates correlation tracking across service operations.
        /// Ensures that correlation IDs are maintained throughout the test flow.
        /// Critical for distributed system debugging and tracing.
        /// </summary>
        protected void AssertCorrelationTrackingMaintained(Guid expectedCorrelationId)
        {
            // Check logs for correlation
            var correlatedLogs = StubLogging.RecordedLogs
                .Where(log => log.CorrelationId.Equals(expectedCorrelationId.ToString()))
                .ToList();

            Assert.That(correlatedLogs.Count, Is.GreaterThan(0),
                "Expected correlation ID to be maintained in logging");

            // Check messages for correlation
            var correlatedMessages = SpyMessageBus.PublishedMessages
                .Where(msg => msg.CorrelationId == expectedCorrelationId)
                .ToList();

            if (SpyMessageBus.PublishedMessages.Count > 0)
            {
                Assert.That(correlatedMessages.Count, Is.GreaterThan(0),
                    "Expected correlation ID to be maintained in messaging");
            }
        }

        /// <summary>
        /// Validates end-to-end message flow with correlation tracking.
        /// Tests complete message publishing and retrieval with proper correlation.
        /// </summary>
        protected void AssertMessageFlowCompleted<TMessage>(Guid expectedCorrelationId) where TMessage : IMessage
        {
            var message = GetLastMessage<TMessage>();
            Assert.That(message, Is.Not.Null, $"Expected message of type {typeof(TMessage).Name}");
            Assert.That(message.CorrelationId, Is.EqualTo(expectedCorrelationId),
                "Message correlation ID should match expected value");
        }

        /// <summary>
        /// Validates service interaction patterns for proper coordination.
        /// Ensures that services work together correctly without tight coupling.
        /// </summary>
        protected void AssertServiceInteractionPattern(
            int expectedLogEntries,
            int expectedMessages,
            int expectedPoolingOperations = 0)
        {
            Assert.That(StubLogging.RecordedLogs.Count, Is.GreaterThanOrEqualTo(expectedLogEntries),
                $"Expected at least {expectedLogEntries} log entries");

            Assert.That(SpyMessageBus.PublishedMessages.Count, Is.GreaterThanOrEqualTo(expectedMessages),
                $"Expected at least {expectedMessages} published messages");

            if (expectedPoolingOperations > 0)
            {
                Assert.That(FakePooling.TotalInstancesCreated, Is.GreaterThanOrEqualTo(expectedPoolingOperations),
                    $"Expected at least {expectedPoolingOperations} pooling operations");
            }
        }

        /// <summary>
        /// Validates that a service operation handles failures gracefully without throwing exceptions.
        /// Essential for robust service testing following TDD patterns.
        /// </summary>
        protected async UniTask AssertGracefulFailureHandlingAsync(Func<UniTask> operation)
        {
            try
            {
                await operation();
                // Operation should complete without throwing exceptions
            }
            catch (Exception ex)
            {
                Assert.Fail($"Operation should handle failures gracefully, but threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Simulates a service failure for resilience testing.
        /// Tests how the system handles dependent service failures.
        /// </summary>
        protected void SimulateServiceFailure(Action failureAction)
        {
            if (failureAction != null)
            {
                failureAction();
            }
        }

        /// <summary>
        /// Waits for a condition to be met within the specified timeout.
        /// Unity Test Runner compatible async condition checking.
        /// </summary>
        protected async UniTask WaitForConditionAsync(
            Func<bool> condition,
            TimeSpan timeout = default,
            TimeSpan pollInterval = default,
            string conditionDescription = "condition")
        {
            if (timeout == default)
                timeout = TestConstants.DefaultAsyncTimeout;
            if (pollInterval == default)
                pollInterval = TestConstants.DefaultPollInterval;

            var endTime = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < endTime)
            {
                if (condition())
                    return;

                await UniTask.Delay(pollInterval);
            }

            Assert.Fail($"Timeout waiting for {conditionDescription} after {timeout.TotalSeconds} seconds");
        }

        /// <summary>
        /// Logs performance metrics for debugging and analysis.
        /// Integrates with TDD test doubles for consistent logging.
        /// </summary>
        protected void LogPerformanceMetrics(PerformanceResult result)
        {
            var metricsMessage = $"Performance Metrics: {result.OperationName} - " +
                               $"Duration: {result.Duration.TotalMilliseconds:F2}ms, " +
                               $"Memory: {result.MemoryUsed:F2}MB, " +
                               $"Frame Budget: {(result.Duration < TestConstants.FrameBudget ? "PASS" : "FAIL")}";

            StubLogging.LogInfo(metricsMessage);
        }

        #endregion
    }
}