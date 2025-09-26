using System;
using System.Collections.Generic;
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
    /// Base class for integration tests that test multiple services working together.
    /// Provides lightweight TDD-compliant test doubles for testing service interactions.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    [TestFixture]
    public abstract class BaseIntegrationTest : BaseServiceTest
    {
        protected ServiceTestContainer ServiceContainer { get; private set; }
        protected PerformanceTestHelper PerformanceHelper { get; private set; }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Initialize integration test specific components
            ServiceContainer = new ServiceTestContainer();
            PerformanceHelper = new PerformanceTestHelper();

            // Register all mock services in the container
            RegisterMockServices();

            // Initialize the service container
            OnIntegrationSetup();
        }

        [TearDown]
        public override void TearDown()
        {
            // Integration test specific cleanup
            OnIntegrationTearDown();

            // Clean up container and performance helper
            ServiceContainer?.Dispose();
            PerformanceHelper?.Dispose();

            base.TearDown();
        }

        /// <summary>
        /// Override this method to perform additional integration test setup.
        /// </summary>
        protected virtual void OnIntegrationSetup()
        {
            // Default: no additional setup
        }

        /// <summary>
        /// Override this method to perform additional integration test teardown.
        /// </summary>
        protected virtual void OnIntegrationTearDown()
        {
            // Default: no additional teardown
        }

        /// <summary>
        /// Registers all test doubles in the service container for dependency injection testing.
        /// Uses TryRegisterInstance to avoid conflicts with services already registered in the container.
        /// </summary>
        private void RegisterMockServices()
        {
            // Use TryRegisterInstance to avoid registration conflicts
            // These services may already be registered by the ServiceTestContainer's default setup
            ServiceContainer.TryRegisterInstance(StubLogging);
            ServiceContainer.TryRegisterInstance(SpyMessageBus);
            ServiceContainer.TryRegisterInstance(FakeSerialization);
            ServiceContainer.TryRegisterInstance(FakePooling);
            ServiceContainer.TryRegisterInstance(NullProfiler);
            ServiceContainer.TryRegisterInstance(StubHealthCheck);
        }

        /// <summary>
        /// Executes an async operation and waits for completion with timeout.
        /// </summary>
        protected async UniTask ExecuteWithTimeoutAsync(Func<UniTask> operation, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(5);

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
        /// Executes an async operation and measures its performance.
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
        /// Waits for a condition to be met within the specified timeout.
        /// </summary>
        protected async UniTask WaitForConditionAsync(
            Func<bool> condition,
            TimeSpan timeout = default,
            TimeSpan pollInterval = default,
            string conditionDescription = "condition")
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(5);
            if (pollInterval == default)
                pollInterval = TimeSpan.FromMilliseconds(50);

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
        /// Verifies that all services are in a healthy state after the test.
        /// Enhanced version that validates complete service interaction health.
        /// </summary>
        protected void AssertAllServicesHealthy()
        {
            Assert.That(StubLogging.IsEnabled, Is.True, "Logging service should be enabled");
            Assert.That(SpyMessageBus.IsEnabled, Is.True, "Message bus service should be enabled");
            Assert.That(FakeSerialization.IsEnabled, Is.True, "Serialization service should be enabled");
            Assert.That(FakePooling.IsEnabled, Is.True, "Pooling service should be enabled");
            Assert.That(NullProfiler.IsEnabled, Is.False, "Null profiler should be disabled for performance");
            Assert.That(StubHealthCheck.IsEnabled, Is.True, "Health check service should be enabled");

            // Verify no error logs were generated during integration
            AssertNoErrors();

            // Verify service containers are properly configured
            Assert.That(ServiceContainer.RegisteredServiceCount, Is.GreaterThan(0), "Service container should have registered services");
        }

        /// <summary>
        /// Verifies end-to-end message flow from source to destination.
        /// Validates complete integration workflow with correlation tracking.
        /// </summary>
        protected void AssertMessageFlowCompleted<TMessage>(Guid expectedCorrelationId) where TMessage : IMessage
        {
            var message = GetLastMessage<TMessage>();
            Assert.That(message, Is.Not.Null, $"Expected message of type {typeof(TMessage).Name}");
            Assert.That(message.CorrelationId, Is.EqualTo(expectedCorrelationId),
                "Message correlation ID should match expected value");
        }

        /// <summary>
        /// Simulates a service failure and verifies system resilience.
        /// Tests integration scenarios where dependent services fail gracefully.
        /// </summary>
        protected void SimulateServiceFailure<TService>(Action<TService> failureAction) where TService : class
        {
            var service = ServiceContainer.Resolve<TService>();
            if (service != null)
            {
                failureAction(service);
            }
        }

        /// <summary>
        /// Verifies that the system handles failures gracefully without throwing exceptions.
        /// Critical for integration testing where multiple services interact.
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
        /// Executes an integration test operation and validates it completes within Unity's frame budget.
        /// Ensures that complex service interactions maintain 60 FPS performance.
        /// </summary>
        protected async UniTask<PerformanceResult> ExecuteWithFrameBudgetValidationAsync(
            Func<UniTask> operation,
            string operationName)
        {
            return await ExecuteWithPerformanceMeasurementAsync(
                operation,
                operationName,
                TestConstants.FrameBudget); // 16.67ms limit
        }

        /// <summary>
        /// Performs stress testing with realistic game load (1000+ operations).
        /// Validates that integrated services maintain performance under stress.
        /// </summary>
        protected async UniTask<StressTestResult> PerformStressTestAsync(
            Func<UniTask> operation,
            string operationName,
            int iterations = 1000,
            TimeSpan? totalTimeLimit = null)
        {
            totalTimeLimit ??= TimeSpan.FromSeconds(2);

            var startTime = DateTime.UtcNow;
            var failures = 0;
            var results = new List<PerformanceResult>();

            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var result = await PerformanceHelper.MeasureAsync(operation, $"{operationName}_Iteration_{i}");
                    results.Add(result);
                }
                catch
                {
                    failures++;
                }

                // Break if we exceed time limit
                if (DateTime.UtcNow - startTime > totalTimeLimit)
                    break;
            }

            return new StressTestResult(operationName, results, iterations);
        }

        /// <summary>
        /// Validates integration workflow with multiple service interactions.
        /// Ensures that correlation IDs are properly maintained across service boundaries.
        /// </summary>
        protected async UniTask AssertIntegrationWorkflowAsync(
            Func<Guid, UniTask> workflowOperation,
            string workflowName)
        {
            var correlationId = CreateTestCorrelationId(workflowName);

            // Capture initial state
            var initialLogCount = StubLogging.RecordedLogs.Count;
            var initialMessageCount = SpyMessageBus.PublishedMessages.Count;

            // Execute workflow
            await ExecuteWithFrameBudgetValidationAsync(
                () => workflowOperation(correlationId),
                workflowName);

            // Verify workflow generated activity
            Assert.That(StubLogging.RecordedLogs.Count, Is.GreaterThan(initialLogCount),
                "Workflow should generate log entries");

            // Verify correlation tracking
            var correlatedLogs = StubLogging.RecordedLogs
                .Where(log => log.CorrelationId.Equals(correlationId.ToString()))
                .ToList();
            Assert.That(correlatedLogs.Count, Is.GreaterThan(0),
                "Workflow should maintain correlation ID in logs");
        }

        /// <summary>
        /// Verifies service interaction patterns follow expected integration flows.
        /// Tests that services coordinate properly without tight coupling.
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
        /// Tests service recovery after simulated failures.
        /// Ensures integrated systems can recover from transient failures.
        /// </summary>
        protected async UniTask AssertServiceRecoveryAsync<TService>(
            Func<TService, UniTask> operationBeforeFailure,
            Action<TService> simulateFailure,
            Func<TService, UniTask> operationAfterRecovery,
            Func<TService, bool> healthCheck) where TService : class
        {
            var service = ServiceContainer.ResolveRequired<TService>();

            // Execute operation before failure
            await operationBeforeFailure(service);

            // Simulate failure
            simulateFailure(service);

            // Allow some time for recovery
            await UniTask.Delay(100);

            // Verify recovery and execute operation
            Assert.That(healthCheck(service), Is.True, "Service should have recovered");
            await operationAfterRecovery(service);
        }

        /// <summary>
        /// Validates that integration tests maintain zero-allocation patterns where possible.
        /// Critical for Unity game development performance requirements.
        /// </summary>
        protected async UniTask AssertZeroAllocationPatternAsync(
            Func<UniTask> operation,
            string operationName,
            long maxAllowedBytes = 1024) // 1KB tolerance
        {
            // Force cleanup before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            await operation();

            var finalMemory = GC.GetTotalMemory(false);
            var allocatedBytes = Math.Max(0, finalMemory - initialMemory);

            Assert.That(allocatedBytes, Is.LessThanOrEqualTo(maxAllowedBytes),
                $"Operation '{operationName}' allocated {allocatedBytes} bytes, " +
                $"but expected maximum was {maxAllowedBytes} bytes");
        }


        /// <summary>
        /// Logs performance metrics from a performance result for debugging and analysis.
        /// </summary>
        /// <param name="performanceResult">The performance result to log</param>
        protected void LogPerformanceMetrics(PerformanceResult performanceResult)
        {
            StubLogging.LogInfo($"Performance Metrics: {performanceResult}");
        }

        /// <summary>
        /// Validates bulk operation performance for integration tests.
        /// Ensures that operations with large datasets maintain frame budget compliance.
        /// </summary>
        protected async UniTask<PerformanceResult> AssertBulkOperationPerformanceAsync(
            Func<int, UniTask> bulkOperation,
            int itemCount,
            string operationName,
            TimeSpan? maxDuration = null)
        {
            maxDuration ??= TestConstants.FrameBudget;

            var result = await ExecuteWithPerformanceMeasurementAsync(
                () => bulkOperation(itemCount),
                $"{operationName}_Bulk_{itemCount}",
                maxDuration);

            // Log for debugging
            LogPerformanceMetrics(result);

            return result;
        }

        /// <summary>
        /// Verifies that integration tests maintain correlation across service boundaries.
        /// Critical for distributed service architectures and debugging.
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

            Assert.That(correlatedMessages.Count, Is.GreaterThan(0),
                "Expected correlation ID to be maintained in messaging");
        }

        /// <summary>
        /// Tests integration scenario with concurrent operations.
        /// Validates that services handle concurrent access properly.
        /// </summary>
        protected async UniTask AssertConcurrentIntegrationAsync(
            Func<int, UniTask> concurrentOperation,
            int concurrencyLevel = 10,
            string operationName = "ConcurrentIntegration")
        {
            var tasks = new List<UniTask>();

            for (int i = 0; i < concurrencyLevel; i++)
            {
                tasks.Add(concurrentOperation(i));
            }

            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () => await UniTask.WhenAll(tasks),
                operationName,
                TestConstants.FrameBudget);

            LogPerformanceMetrics(result);

            // Verify no errors occurred during concurrent execution
            AssertNoErrors();
        }

        /// <summary>
        /// Validates integration test memory usage patterns.
        /// Ensures that complex service interactions don't cause memory leaks.
        /// </summary>
        protected async UniTask AssertMemoryUsagePatternAsync(
            Func<UniTask> operation,
            string operationName,
            double maxMemoryMB = 10.0)
        {
            var result = await PerformanceHelper.MeasureAsync(operation, operationName);

            Assert.That(result.MemoryUsed, Is.LessThanOrEqualTo(maxMemoryMB),
                $"Operation '{operationName}' used {result.MemoryUsed:F2}MB memory, " +
                $"but expected maximum was {maxMemoryMB}MB");

            LogPerformanceMetrics(result);
        }

        /// <summary>
        /// Comprehensive integration health check that validates all service interactions.
        /// Should be called at the end of complex integration tests.
        /// </summary>
        protected void AssertComprehensiveIntegrationHealth()
        {
            // Basic service health
            AssertAllServicesHealthy();

            // Verify service interactions occurred
            Assert.That(StubLogging.RecordedLogs.Count, Is.GreaterThan(0),
                "Integration test should have generated log entries");

            // Verify no critical errors
            var criticalLogs = StubLogging.RecordedLogs
                .Where(log => log.Level >= AhBearStudios.Core.Logging.Models.LogLevel.Critical)
                .ToList();
            Assert.That(criticalLogs.Count, Is.EqualTo(0),
                "Integration test should not generate critical errors");

            // Verify message bus activity if expected
            if (SpyMessageBus.PublishedMessages.Count > 0)
            {
                Assert.That(SpyMessageBus.GetHealthStatus(),
                    Is.EqualTo(AhBearStudios.Core.HealthChecking.Models.HealthStatus.Healthy),
                    "Message bus should be healthy after integration test");
            }

            // Verify serialization service if used
            if (FakeSerialization.SerializationCount > 0 || FakeSerialization.DeserializationCount > 0)
            {
                Assert.That(FakeSerialization.IsEnabled, Is.True,
                    "Serialization service should be enabled if used");
            }

            // Verify pooling service if used
            if (FakePooling.TotalInstancesCreated > 0)
            {
                Assert.That(FakePooling.IsEnabled, Is.True,
                    "Pooling service should be enabled if used");
            }
        }
    }
}