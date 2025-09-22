using System;
using NUnit.Framework;
using AhBearStudios.Core.Tests.Shared.Mocks;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Tests.Shared.Base
{
    /// <summary>
    /// Base class for all service unit tests providing common setup, teardown, and shared mock services.
    /// Follows the AhBearStudios Core testing patterns with dependency injection mocking.
    /// </summary>
    [TestFixture]
    public abstract class BaseServiceTest
    {
        protected MockLoggingService MockLogging { get; private set; }
        protected MockMessageBusService MockMessageBus { get; private set; }
        protected MockSerializationService MockSerialization { get; private set; }
        protected MockPoolingService MockPooling { get; private set; }
        protected MockProfilerService MockProfiler { get; private set; }
        protected MockHealthCheckService MockHealthCheck { get; private set; }
        protected TestCorrelationHelper CorrelationHelper { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            // Initialize all mock services
            MockLogging = new MockLoggingService();
            MockMessageBus = new MockMessageBusService();
            MockSerialization = new MockSerializationService();
            MockPooling = new MockPoolingService();
            MockProfiler = new MockProfilerService();
            MockHealthCheck = new MockHealthCheckService();
            CorrelationHelper = new TestCorrelationHelper();

            // Perform any additional setup
            OnSetup();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Perform test-specific cleanup
            OnTearDown();

            // Clear all mock services
            MockLogging?.Clear();
            MockMessageBus?.Clear();
            MockSerialization?.Clear();
            MockPooling?.Clear();
            MockProfiler?.Clear();
            MockHealthCheck?.Clear();
            CorrelationHelper?.Clear();

            // Dispose services
            MockLogging?.Dispose();
            MockMessageBus?.Dispose();
            MockSerialization?.Dispose();
            MockPooling?.Dispose();
            MockProfiler?.Dispose();
            MockHealthCheck?.Dispose();
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
            Assert.That(MockLogging.HasErrorLogs(), Is.False,
                "Test should not have generated any error logs");
        }

        /// <summary>
        /// Verifies that the specified number of log entries were recorded.
        /// </summary>
        protected void AssertLogCount(int expectedCount)
        {
            Assert.That(MockLogging.CallCount, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} log entries, but found {MockLogging.CallCount}");
        }

        /// <summary>
        /// Verifies that a log message containing the specified text was recorded.
        /// </summary>
        protected void AssertLogContains(string expectedMessage)
        {
            Assert.That(MockLogging.HasLogWithMessage(expectedMessage), Is.True,
                $"Expected log message containing '{expectedMessage}' was not found");
        }

        /// <summary>
        /// Verifies that the specified number of messages were published to the message bus.
        /// </summary>
        protected void AssertMessageCount(int expectedCount)
        {
            Assert.That(MockMessageBus.PublishCount, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} published messages, but found {MockMessageBus.PublishCount}");
        }

        /// <summary>
        /// Verifies that a message of the specified type was published.
        /// </summary>
        protected void AssertMessagePublished<T>() where T : IMessage
        {
            Assert.That(MockMessageBus.HasMessageOfType<T>(), Is.True,
                $"Expected message of type {typeof(T).Name} was not published");
        }

        /// <summary>
        /// Verifies that the specified number of messages of the given type were published.
        /// </summary>
        protected void AssertMessageCount<T>(int expectedCount) where T : IMessage
        {
            var actualCount = MockMessageBus.GetMessageCount<T>();
            Assert.That(actualCount, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} messages of type {typeof(T).Name}, but found {actualCount}");
        }

        /// <summary>
        /// Gets the last published message of the specified type.
        /// </summary>
        protected T GetLastMessage<T>() where T : IMessage
        {
            return MockMessageBus.GetLastMessage<T>();
        }

        /// <summary>
        /// Verifies that the pooling service was called the expected number of times.
        /// </summary>
        protected void AssertPoolingCalls<T>(int expectedGets, int expectedReturns) where T : class
        {
            Assert.That(MockPooling.GetCallCount<T>(), Is.EqualTo(expectedGets),
                $"Expected {expectedGets} pool get calls for {typeof(T).Name}");
            Assert.That(MockPooling.GetReturnCount<T>(), Is.EqualTo(expectedReturns),
                $"Expected {expectedReturns} pool return calls for {typeof(T).Name}");
        }

        /// <summary>
        /// Verifies that profiling was performed for the specified scope.
        /// </summary>
        protected void AssertProfilingScopeUsed(string scopeName)
        {
            Assert.That(MockProfiler.GetScopeCallCount(scopeName), Is.GreaterThan(0),
                $"Expected profiling scope '{scopeName}' to be used");
        }

        /// <summary>
        /// Creates a test correlation ID for tracking test operations.
        /// </summary>
        protected Guid CreateTestCorrelationId(string testContext = null)
        {
            return CorrelationHelper.CreateCorrelationId(testContext ?? TestContext.CurrentContext.Test.Name);
        }
    }
}