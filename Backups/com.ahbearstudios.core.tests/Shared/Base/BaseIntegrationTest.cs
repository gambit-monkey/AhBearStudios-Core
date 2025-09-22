using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.Mocks;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Shared.Base
{
    /// <summary>
    /// Base class for integration tests that test multiple services working together.
    /// Provides a complete mock ecosystem for testing service interactions.
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
        /// Registers all mock services in the service container for dependency injection testing.
        /// </summary>
        private void RegisterMockServices()
        {
            ServiceContainer.RegisterInstance(MockLogging);
            ServiceContainer.RegisterInstance(MockMessageBus);
            ServiceContainer.RegisterInstance(MockSerialization);
            ServiceContainer.RegisterInstance(MockPooling);
            ServiceContainer.RegisterInstance(MockProfiler);
            ServiceContainer.RegisterInstance(MockHealthCheck);
        }

        /// <summary>
        /// Executes an async operation and waits for completion with timeout.
        /// </summary>
        protected async Task ExecuteWithTimeoutAsync(Func<UniTask> operation, TimeSpan timeout = default)
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
        protected async Task<PerformanceResult> ExecuteWithPerformanceMeasurementAsync(
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
        protected async Task WaitForConditionAsync(
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
        /// </summary>
        protected void AssertAllServicesHealthy()
        {
            Assert.That(MockLogging.IsEnabled, Is.True, "Logging service should be enabled");
            Assert.That(MockMessageBus.IsEnabled, Is.True, "Message bus service should be enabled");
            Assert.That(MockSerialization.IsEnabled, Is.True, "Serialization service should be enabled");
            Assert.That(MockPooling.IsEnabled, Is.True, "Pooling service should be enabled");
            Assert.That(MockProfiler.IsEnabled, Is.True, "Profiler service should be enabled");
            Assert.That(MockHealthCheck.IsEnabled, Is.True, "Health check service should be enabled");
        }

        /// <summary>
        /// Verifies end-to-end message flow from source to destination.
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
        /// </summary>
        protected async Task AssertGracefulFailureHandlingAsync(Func<UniTask> operation)
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
    }
}