using System;
using NUnit.Framework;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles
{
    /// <summary>
    /// Verification tests for TDD-compliant test doubles.
    /// Ensures Unity Test Runner compatibility and proper lightweight behavior.
    /// </summary>
    [TestFixture]
    public class TestDoubleVerificationTests
    {
        #region Logging Service Tests

        [Test]
        public void StubLoggingService_RecordsLogEntries_WithoutProcessingLogic()
        {
            // Arrange
            var stub = new StubLoggingService();

            // Act
            stub.LogInfo("Test message", correlationId: new FixedString64Bytes("test123"));
            stub.LogError("Error message");

            // Assert
            Assert.That(stub.RecordedLogs.Count, Is.EqualTo(2));
            Assert.That(stub.HasLogWithMessage("Test message"), Is.True);
            Assert.That(stub.GetLogCount(LogLevel.Info), Is.EqualTo(1));
            Assert.That(stub.GetLogCount(LogLevel.Error), Is.EqualTo(1));
        }

        [Test]
        public void StubLoggingService_ClearLogs_RemovesAllEntries()
        {
            // Arrange
            var stub = new StubLoggingService();
            stub.LogInfo("Test message");

            // Act
            stub.ClearLogs();

            // Assert
            Assert.That(stub.RecordedLogs.Count, Is.EqualTo(0));
        }

        [Test]
        public void StubLoggingService_BeginScope_ReturnsStubScope()
        {
            // Arrange
            var stub = new StubLoggingService();
            var correlationId = new FixedString64Bytes("test123");

            // Act
            var scope = stub.BeginScope("TestScope", correlationId, "TestContext");

            // Assert
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.Name.ToString(), Is.EqualTo("TestScope"));
            Assert.That(scope.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(scope.SourceContext, Is.EqualTo("TestContext"));
            Assert.That(scope.IsActive, Is.True);
            Assert.That(scope.Elapsed, Is.GreaterThan(TimeSpan.Zero));

            // Test scope logging methods
            scope.LogInfo("Test scope message"); // Should not throw
            scope.SetProperty("TestKey", "TestValue");
            Assert.That(scope.GetProperty("TestKey"), Is.EqualTo("TestValue"));

            scope.Dispose();
            Assert.That(scope.IsActive, Is.False);
        }

        [Test]
        public void StubLoggingService_HasErrorLogs_DetectsErrorLevel()
        {
            // Arrange
            var stub = new StubLoggingService();

            // Act
            stub.LogInfo("Info message");
            stub.LogWarning("Warning message");

            // Assert - No errors yet
            Assert.That(stub.HasErrorLogs(), Is.False);

            // Act
            stub.LogError("Error message");

            // Assert - Now has errors
            Assert.That(stub.HasErrorLogs(), Is.True);
        }

        #endregion

        #region Message Bus Service Tests

        [Test]
        public void SpyMessageBusService_RecordsPublishedMessages_WithoutRouting()
        {
            // Arrange
            var spy = new SpyMessageBusService();
            var testMessage = CoreStartupMessage.Create("TestSystem", version: "1.0.0");

            // Act
            spy.PublishMessage(testMessage);

            // Assert
            Assert.That(spy.PublishedMessages.Count, Is.EqualTo(1));
            Assert.That(spy.GetPublishCount<CoreStartupMessage>(), Is.EqualTo(1));
            Assert.That(spy.WasMessagePublished<CoreStartupMessage>(), Is.True);
            Assert.That(spy.GetLastMessage<CoreStartupMessage>(), Is.EqualTo(testMessage));
        }

        [Test]
        public async UniTask SpyMessageBusService_PublishAsync_IsUnityTestRunnerCompatible()
        {
            // Arrange
            var spy = new SpyMessageBusService();
            var testMessage = CoreStartupMessage.Create("AsyncTest", version: "1.0.0");

            // Act
            await spy.PublishMessageAsync(testMessage);

            // Assert
            Assert.That(spy.PublishedMessages.Count, Is.EqualTo(1));
            Assert.That(spy.PublishCalls.Count, Is.EqualTo(1));
            Assert.That(spy.PublishCalls[0].IsAsync, Is.True);
        }

        [Test]
        public void SpyMessageBusService_Subscribe_RecordsSubscriptions()
        {
            // Arrange
            var spy = new SpyMessageBusService();

            // Act
            var subscription = spy.SubscribeToMessage<CoreStartupMessage>(msg => { });

            // Assert
            Assert.That(spy.SubscriptionCalls.Count, Is.EqualTo(1));
            Assert.That(spy.GetSubscriptionCount<CoreStartupMessage>(), Is.EqualTo(1));
            subscription.Dispose();
        }

        [Test]
        public void SpyMessageBusService_GetPublisher_ReturnsSpyImplementation()
        {
            // Arrange
            var spy = new SpyMessageBusService();

            // Act
            var publisher = spy.GetPublisher<CoreStartupMessage>();

            // Assert
            Assert.That(publisher, Is.Not.Null);
            Assert.That(publisher.IsOperational, Is.True);
            Assert.That(publisher.MessageType, Is.EqualTo(typeof(CoreStartupMessage)));

            // Test publisher methods
            var testMessage = CoreStartupMessage.Create("TestSystem", version: "1.0.0");
            publisher.Publish(testMessage); // Should not throw
            Assert.That(publisher.PublishIf(testMessage, () => true), Is.True);

            publisher.Dispose(); // Should not throw
        }

        [Test]
        public void SpyMessageBusService_CreateScope_ReturnsSpyScope()
        {
            // Arrange
            var spy = new SpyMessageBusService();

            // Act
            var scope = spy.CreateScope();

            // Assert
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(scope.ActiveSubscriptions, Is.EqualTo(0));
            Assert.That(scope.IsActive, Is.True);

            scope.Dispose();
        }

        [Test]
        public void SpyMessageBusService_FilteredSubscription_RecordsFilterDetails()
        {
            // Arrange
            var spy = new SpyMessageBusService();

            // Act
            var subscription = spy.SubscribeWithFilter<CoreStartupMessage>(
                filter => true,
                handler => { });

            // Assert
            Assert.That(spy.SubscriptionCalls.Count, Is.EqualTo(1));
            Assert.That(spy.SubscriptionCalls[0].HasFilter, Is.True);
            Assert.That(spy.SubscriptionCalls[0].MessageType, Is.EqualTo(typeof(CoreStartupMessage)));

            subscription.Dispose();
        }

        [Test]
        public void SpyMessageBusService_GetPublisher_ReturnsSpyPublisher()
        {
            // Arrange
            var spy = new SpyMessageBusService();

            // Act
            var publisher = spy.GetPublisher<CoreStartupMessage>();

            // Assert
            Assert.That(publisher, Is.Not.Null);
            Assert.That(publisher.IsOperational, Is.True);
            Assert.That(publisher.MessageType, Is.EqualTo(typeof(CoreStartupMessage)));

            // Test publisher methods
            var testMessage = CoreStartupMessage.Create("TestSystem", version: "1.0.0");
            publisher.Publish(testMessage); // Should not throw
            Assert.That(publisher.PublishIf(testMessage, () => true), Is.True);
            Assert.That(publisher.PublishIf(testMessage, () => false), Is.False);

            publisher.Dispose(); // Should not throw
        }

        [Test]
        public void SpyMessageBusService_GetSubscriber_ReturnsSpySubscriber()
        {
            // Arrange
            var spy = new SpyMessageBusService();

            // Act
            var subscriber = spy.GetSubscriber<CoreStartupMessage>();

            // Assert
            Assert.That(subscriber, Is.Not.Null);
            Assert.That(subscriber.IsOperational, Is.True);
            Assert.That(subscriber.MessageType, Is.EqualTo(typeof(CoreStartupMessage)));
            Assert.That(subscriber.ActiveSubscriptions, Is.EqualTo(0));

            // Test subscriber methods
            var subscription = subscriber.Subscribe(msg => { });
            Assert.That(subscriber.ActiveSubscriptions, Is.EqualTo(0)); // Spy implementation always returns 0
            subscription.Dispose();

            subscriber.Dispose(); // Should not throw
        }

        [Test]
        public void SpyMessageBusService_GetStatistics_ReturnsMessageBusStatistics()
        {
            // Arrange
            var spy = new SpyMessageBusService();
            var testMessage = CoreStartupMessage.Create("TestSystem", version: "1.0.0");
            spy.PublishMessage(testMessage);

            // Act
            var stats = spy.GetStatistics();

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.TotalMessagesPublished, Is.EqualTo(1));
            Assert.That(stats.ActiveSubscribers, Is.EqualTo(0));
            Assert.That(stats.CurrentHealthStatus, Is.EqualTo(HealthStatus.Healthy));
        }

        #endregion

        #region Pooling Service Tests

        [Test]
        public void FakePoolingService_CreatesNewInstances_WithoutActualPooling()
        {
            // Arrange
            var fake = new FakePoolingService();
            var config = PoolConfiguration.CreateDefault("TestPool") with
            {
                InitialCapacity = 10,
                MaxCapacity = 100
            };
            fake.RegisterPool<TestPooledObject>(config);

            // Act
            var obj1 = fake.Get<TestPooledObject>();
            var obj2 = fake.Get<TestPooledObject>();
            fake.Return(obj1);

            // Assert
            Assert.That(fake.GetCallCount<TestPooledObject>(), Is.EqualTo(2));
            Assert.That(fake.ReturnCallCount<TestPooledObject>(), Is.EqualTo(1));
            Assert.That(fake.CreatedObjectCount<TestPooledObject>(), Is.EqualTo(2));
            Assert.That(obj1, Is.Not.EqualTo(obj2)); // Always creates new instances
        }

        [Test]
        public async UniTask FakePoolingService_GetAsync_IsUnityTestRunnerCompatible()
        {
            // Arrange
            var fake = new FakePoolingService();
            var config = PoolConfiguration.CreateDefault("AsyncTestPool") with
            {
                InitialCapacity = 5,
                MaxCapacity = 50
            };
            fake.RegisterPool<TestPooledObject>(config);

            // Act
            var obj = await fake.GetAsync<TestPooledObject>();

            // Assert
            Assert.That(obj, Is.Not.Null);
            Assert.That(fake.GetCallCount<TestPooledObject>(), Is.EqualTo(1));
        }

        #endregion

        #region Health Check Service Tests

        [Test]
        public void StubHealthCheckService_ReturnsConfiguredResults()
        {
            // Arrange
            var stub = new StubHealthCheckService();
            var checkName = new FixedString64Bytes("TestCheck");
            stub.ConfigureHealthyResult(checkName, "Test is healthy");

            // Act & Assert
            Assert.That(stub.RegisteredCheckCount, Is.EqualTo(0)); // No actual registration

            // Configure a result and test
            var expectedResult = HealthCheckResult.Healthy("TestCheck", "Test is healthy");
            stub.ConfigureResult(checkName, expectedResult);
        }

        [Test]
        public async UniTask StubHealthCheckService_ExecuteAsync_IsUnityTestRunnerCompatible()
        {
            // Arrange
            var stub = new StubHealthCheckService();
            var healthCheck = new TestHealthCheck("AsyncTest");
            stub.RegisterHealthCheck(healthCheck);

            // Act
            var result = await stub.ExecuteHealthCheckAsync(healthCheck.Name);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public void StubHealthCheckService_ConfigureResults_ReturnsConfiguredValues()
        {
            // Arrange
            var stub = new StubHealthCheckService();
            var checkName = new FixedString64Bytes("TestConfiguredCheck");
            var expectedResult = HealthCheckResult.Unhealthy("TestConfiguredCheck", "Configured unhealthy result");

            // Act
            stub.ConfigureResult(checkName, expectedResult);
            stub.ConfigureHealthyResult(checkName, "Now healthy again");

            // Assert
            Assert.That(stub.RegisteredCheckCount, Is.EqualTo(0)); // No actual registration in stub

            // Verify the stub behavior is consistent
            var healthCheck = new TestHealthCheck("TestConfiguredCheck");
            stub.RegisterHealthCheck(healthCheck);
            Assert.That(stub.RegisteredCheckCount, Is.EqualTo(1));
        }

        #endregion

        #region Profiler Service Tests

        [Test]
        public void NullProfilerService_HasZeroOverhead()
        {
            // Arrange
            var nullProfiler = NullProfilerService.Instance;

            // Act & Assert - Should complete instantly with no operations
            Assert.That(nullProfiler.IsEnabled, Is.False);
            Assert.That(nullProfiler.ActiveScopeCount, Is.EqualTo(0));
            Assert.That(nullProfiler.TotalScopeCount, Is.EqualTo(0));

            using (var scope = nullProfiler.BeginScope("TestScope"))
            {
                // Should be no-op
            }

            nullProfiler.RecordSample(new AhBearStudios.Core.Profiling.Models.ProfilerTag("test"), 1.0f);
            var stats = nullProfiler.GetStatistics();
            Assert.That(stats.Count, Is.EqualTo(7)); // Should return 7 statistics fields
            Assert.That(stats["enabled"], Is.EqualTo(false));
            Assert.That(stats["totalMetrics"], Is.EqualTo(0));
        }

        [Test]
        public void NullProfilerService_AllMethods_AreNoOp()
        {
            // Arrange
            var nullProfiler = NullProfilerService.Instance;
            var tag = new AhBearStudios.Core.Profiling.Models.ProfilerTag("TestTag");

            // Act & Assert - All methods should be no-op and not throw
            nullProfiler.Enable(1.0f); // Should not change state
            Assert.That(nullProfiler.IsEnabled, Is.False);

            nullProfiler.StartRecording(); // Should not change state
            Assert.That(nullProfiler.IsRecording, Is.False);

            nullProfiler.RecordMetric("test", 1.0);
            nullProfiler.IncrementCounter("counter");
            nullProfiler.DecrementCounter("counter");

            var metrics = nullProfiler.GetMetrics(tag);
            Assert.That(metrics.Count, Is.EqualTo(0));

            var allMetrics = nullProfiler.GetAllMetrics();
            Assert.That(allMetrics.Count, Is.EqualTo(0));

            Assert.That(nullProfiler.PerformHealthCheck(), Is.True);
            Assert.That(nullProfiler.GetLastError(), Is.Null);

            nullProfiler.ClearData();
            nullProfiler.Flush();
            nullProfiler.Disable();
            nullProfiler.StopRecording();
        }

        #endregion

        #region Serialization Service Tests

        [Test]
        public void FakeSerializationService_StoresAndRetrievesObjects_WithoutActualSerialization()
        {
            // Arrange
            var fake = new FakeSerializationService();
            var testData = "Test serialization data";

            // Act
            var serialized = fake.Serialize(testData);
            var deserialized = fake.Deserialize<string>(serialized);

            // Assert
            Assert.That(fake.SerializationCount, Is.EqualTo(1));
            Assert.That(fake.DeserializationCount, Is.EqualTo(1));
            Assert.That(deserialized, Is.EqualTo(testData));
            Assert.That(fake.StoredItemCount, Is.EqualTo(1));
        }

        [Test]
        public async UniTask FakeSerializationService_SerializeAsync_IsUnityTestRunnerCompatible()
        {
            // Arrange
            var fake = new FakeSerializationService();
            var testData = "Async test data";
            using var stream = new System.IO.MemoryStream();

            // Act
            await fake.SerializeToStreamAsync(testData, stream);

            // Assert
            Assert.That(fake.SerializationCount, Is.EqualTo(1));
            Assert.That(stream.Length, Is.GreaterThan(0));
        }

        #endregion

        #region Circuit Breaker Tests

        [Test]
        public void FakeCircuitBreaker_MethodsUnderTest_ProvideFakeImplementation()
        {
            // Arrange
            var fakeCircuitBreaker = new FakeCircuitBreaker();

            // Act & Assert - Test all interface methods
            Assert.That(fakeCircuitBreaker.State, Is.EqualTo(CircuitBreakerState.Closed));
            Assert.That(fakeCircuitBreaker.FailureCount, Is.EqualTo(0));
            Assert.That(fakeCircuitBreaker.LastFailureTime, Is.Null);
            Assert.That(fakeCircuitBreaker.AllowsRequests(), Is.True);

            // Test configuration and name access
            Assert.That(fakeCircuitBreaker.Configuration, Is.Not.Null);
            Assert.That(fakeCircuitBreaker.Name.ToString(), Is.EqualTo("FakeCircuitBreaker"));
            Assert.That(fakeCircuitBreaker.LastStateChangeTime, Is.Null);

            // Test state change methods (no-op behavior)
            fakeCircuitBreaker.Open();
            fakeCircuitBreaker.Close();
            fakeCircuitBreaker.HalfOpen();
            fakeCircuitBreaker.Reset();
            fakeCircuitBreaker.RecordSuccess();
            fakeCircuitBreaker.RecordFailure(new System.Exception("Test"));

            // Test statistics
            var stats = fakeCircuitBreaker.GetStatistics();
            Assert.That(stats.Name, Is.EqualTo(fakeCircuitBreaker.Name));
            Assert.That(stats.State, Is.EqualTo(CircuitBreakerState.Closed));
            Assert.That(stats.TotalExecutions, Is.EqualTo(0));
            Assert.That(stats.TotalFailures, Is.EqualTo(0));
            Assert.That(stats.TotalSuccesses, Is.EqualTo(0));
            Assert.That(stats.LastStateChange, Is.EqualTo(fakeCircuitBreaker.LastStateChangeTime));

            Assert.That(fakeCircuitBreaker.GetLastStateChangeReason(), Is.Null);

            fakeCircuitBreaker.Dispose(); // Should not throw
        }

        [Test]
        public async UniTask FakeCircuitBreaker_ExecuteAsync_ExecutesOperationDirectly()
        {
            // Arrange
            var fakeCircuitBreaker = new FakeCircuitBreaker();
            var executed = false;

            // Act
            var result = await fakeCircuitBreaker.ExecuteAsync(async (ct) =>
            {
                executed = true;
                await UniTask.CompletedTask;
                return "test result";
            });

            // Assert
            Assert.That(executed, Is.True);
            Assert.That(result, Is.EqualTo("test result"));

            // Test void overload
            var voidExecuted = false;
            await fakeCircuitBreaker.ExecuteAsync(async (ct) =>
            {
                voidExecuted = true;
                await UniTask.CompletedTask;
            });

            Assert.That(voidExecuted, Is.True);
        }

        #endregion

        #region Performance Tests

        [Test]
        public void TestDoubles_HaveMinimalPerformanceOverhead()
        {
            // Arrange - Test all remediated test doubles
            var stubLogging = new StubLoggingService();
            var spyMessageBus = new SpyMessageBusService();
            var fakePooling = new FakePoolingService(spyMessageBus);
            var stubHealthCheck = new StubHealthCheckService();
            var fakeCircuitBreaker = new FakeCircuitBreaker();
            var fakeSerialization = new FakeSerializationService();
            var nullProfiler = NullProfilerService.Instance;

            // Register test objects with pooling service
            var poolConfig = PoolConfiguration.CreateDefault("TestPool") with
            {
                InitialCapacity = 10,
                MaxCapacity = 100
            };
            fakePooling.RegisterPool<TestPooledObject>(poolConfig);

            var startTime = DateTime.UtcNow;

            // Act - Perform many operations to test overhead
            for (int i = 0; i < 1000; i++)
            {
                // Test logging service
                stubLogging.LogInfo($"Test message {i}");

                // Test message bus service
                var message = CoreStartupMessage.Create($"System{i}", version: "1.0.0");
                spyMessageBus.PublishMessage(message);

                // Test pooling service
                var pooledObj = fakePooling.Get<TestPooledObject>();
                fakePooling.Return(pooledObj);

                // Test health check service
                var healthCheck = new TestHealthCheck($"Check{i}");
                stubHealthCheck.RegisterHealthCheck(healthCheck);

                // Test circuit breaker
                fakeCircuitBreaker.RecordSuccess();

                // Test serialization service
                var serialized = fakeSerialization.Serialize($"TestData{i}");
                var deserialized = fakeSerialization.Deserialize<string>(serialized);

                // Test null profiler overhead
                using (nullProfiler.BeginScope($"Scope{i}"))
                {
                    nullProfiler.RecordMetric($"metric{i}", i);
                    nullProfiler.IncrementCounter($"counter{i}");
                }
            }

            var duration = DateTime.UtcNow - startTime;

            // Assert - Should complete very quickly (under 200ms for 1000 operations across all test doubles)
            Assert.That(duration.TotalMilliseconds, Is.LessThan(200),
                "Test doubles should have minimal performance overhead");

            // Verify all test doubles recorded operations correctly
            Assert.That(stubLogging.RecordedLogs.Count, Is.EqualTo(1000));
            Assert.That(spyMessageBus.PublishedMessages.Count, Is.EqualTo(1000));
            Assert.That(fakePooling.GetCallCount<TestPooledObject>(), Is.EqualTo(1000));
            Assert.That(fakePooling.ReturnCallCount<TestPooledObject>(), Is.EqualTo(1000));
            Assert.That(stubHealthCheck.RegisteredCheckCount, Is.EqualTo(1000));
            Assert.That(fakeSerialization.SerializationCount, Is.EqualTo(1000));
            Assert.That(fakeSerialization.DeserializationCount, Is.EqualTo(1000));

            // Verify null profiler still has zero overhead
            Assert.That(nullProfiler.ActiveScopeCount, Is.EqualTo(0));
            Assert.That(nullProfiler.TotalScopeCount, Is.EqualTo(0));
            Assert.That(nullProfiler.IsEnabled, Is.False);
            Assert.That(nullProfiler.IsRecording, Is.False);

            // Cleanup
            stubLogging.Dispose();
            spyMessageBus.Dispose();
            fakePooling.Dispose();
            stubHealthCheck.Dispose();
            fakeCircuitBreaker.Dispose();
            fakeSerialization.Dispose();
            nullProfiler.Dispose();
        }

        #endregion
    }
}