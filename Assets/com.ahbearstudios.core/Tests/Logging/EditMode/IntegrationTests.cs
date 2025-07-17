using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Unity.Collections;
using NUnit.Framework;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Filters;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Tests.Shared;
using AhBearStudios.Core.Logging.Tests.Utilities;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Integration tests for the complete logging pipeline and Unity-specific integration.
    /// Tests end-to-end functionality, system integration, and error scenarios.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        private ILoggingService _loggingService;
        private MockProfilerService _mockProfiler;
        private MockAlertService _mockAlerts;
        private MockHealthCheckService _mockHealthCheck;
        private MockMessageBusService _mockMessageBus;
        private List<ILogTarget> _targets;
        private List<ILogFilter> _filters;
        private string _tempLogFile;

        [SetUp]
        public void SetUp()
        {
            // Create mock dependencies
            _mockProfiler = new MockProfilerService();
            _mockAlerts = new MockAlertService();
            _mockHealthCheck = new MockHealthCheckService();
            _mockMessageBus = new MockMessageBusService();
            _tempLogFile = TestUtilities.CreateTempFilePath();

            // Create targets
            _targets = new List<ILogTarget>
            {
                TestUtilities.CreateMockLogTarget("MockTarget1", LogLevel.Debug),
                TestUtilities.CreateMockLogTarget("MockTarget2", LogLevel.Info),
                new ConsoleLogTarget(TestDataFactory.CreateLogTargetConfig("ConsoleTarget", LogLevel.Warning)),
                new FileLogTarget(CreateFileTargetConfig()),
                new SerilogTarget(TestDataFactory.CreateLogTargetConfig("SerilogTarget", LogLevel.Error), 
                    _mockProfiler, _mockAlerts)
            };

            // Create filters
            _filters = new List<ILogFilter>
            {
                new TestLevelFilter("LevelFilter", LogLevel.Debug),
                new TestSourceFilter("SourceFilter", new[] { "TestClass", "IntegrationTest" }),
                new TestCorrelationFilter("CorrelationFilter", new[] { "integration-test" })
            };

            // Create logging service
            var config = CreateIntegrationTestConfig();
            _loggingService = new LoggingService(config, _mockProfiler, _mockAlerts, _mockHealthCheck, _mockMessageBus);

            // Register targets and filters
            foreach (var target in _targets)
            {
                _loggingService.RegisterTarget(target);
            }

            foreach (var filter in _filters)
            {
                _loggingService.AddFilter(filter);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _loggingService?.Dispose();
            _mockProfiler?.Dispose();
            _mockAlerts?.Dispose();
            _mockHealthCheck?.Dispose();
            _mockMessageBus?.Dispose();
            
            foreach (var target in _targets ?? new List<ILogTarget>())
            {
                target?.Dispose();
            }

            TestUtilities.CleanupTempFiles();
        }

        #region End-to-End Pipeline Tests

        [Test]
        public void LoggingPipeline_CompleteFlow_ProcessesMessageThroughAllComponents()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var message = "Complete pipeline test message";
            var sourceContext = "IntegrationTest";

            // Act
            _loggingService.LogInfo(message, correlationId, sourceContext);
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget1 = _targets[0] as MockLogTarget;
            var mockTarget2 = _targets[1] as MockLogTarget;

            Assert.That(mockTarget1.Messages.Count, Is.EqualTo(1));
            Assert.That(mockTarget2.Messages.Count, Is.EqualTo(1));

            var loggedMessage = mockTarget1.Messages[0];
            Assert.That(loggedMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(loggedMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
            Assert.That(loggedMessage.SourceContext.ToString(), Is.EqualTo(sourceContext));
        }

        [Test]
        public void LoggingPipeline_WithFiltering_FiltersCorrectly()
        {
            // Arrange
            var allowedCorrelationId = new FixedString64Bytes("integration-test");
            var blockedCorrelationId = new FixedString64Bytes("blocked-test");
            var allowedSourceContext = "IntegrationTest";
            var blockedSourceContext = "BlockedTest";

            // Act
            _loggingService.LogInfo("Allowed message", allowedCorrelationId, allowedSourceContext);
            _loggingService.LogInfo("Blocked by correlation", blockedCorrelationId, allowedSourceContext);
            _loggingService.LogInfo("Blocked by source", allowedCorrelationId, blockedSourceContext);
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(1));
            Assert.That(mockTarget.Messages[0].Message.ToString(), Is.EqualTo("Allowed message"));
        }

        [Test]
        public void LoggingPipeline_WithMultipleTargets_DeliversToAllTargets()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var message = "Multi-target test message";
            var sourceContext = "IntegrationTest";

            // Act
            _loggingService.LogError(message, correlationId, sourceContext); // Error level reaches all targets
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget1 = _targets[0] as MockLogTarget;
            var mockTarget2 = _targets[1] as MockLogTarget;

            Assert.That(mockTarget1.Messages.Count, Is.EqualTo(1));
            Assert.That(mockTarget2.Messages.Count, Is.EqualTo(1));

            // Check file target
            Assert.That(File.Exists(_tempLogFile), Is.True);
            var fileContent = File.ReadAllText(_tempLogFile);
            Assert.That(fileContent, Contains.Substring(message));
        }

        [Test]
        public void LoggingPipeline_WithStructuredLogging_PreservesStructure()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var message = "Structured logging test";
            var sourceContext = "IntegrationTest";
            var properties = TestDataFactory.CreateTestProperties("test-user", "test-session", 12345);
            var exception = TestDataFactory.CreateTestException("Test exception");

            // Act
            _loggingService.Log(LogLevel.Error, message, correlationId, sourceContext, exception, properties);
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(1));

            var loggedMessage = mockTarget.Messages[0];
            Assert.That(loggedMessage.HasException, Is.True);
            Assert.That(loggedMessage.HasProperties, Is.True);
            Assert.That(loggedMessage.Exception.Message, Is.EqualTo("Test exception"));
            Assert.That(loggedMessage.Properties["UserId"], Is.EqualTo("test-user"));
            Assert.That(loggedMessage.Properties["SessionId"], Is.EqualTo("test-session"));
            Assert.That(loggedMessage.Properties["RequestId"], Is.EqualTo(12345));
        }

        [Test]
        public void LoggingPipeline_WithScopes_MaintainsHierarchy()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var outerScopeName = "OuterScope";
            var innerScopeName = "InnerScope";
            var sourceContext = "IntegrationTest";

            // Act
            using (var outerScope = _loggingService.BeginScope(outerScopeName, correlationId, sourceContext))
            {
                _loggingService.LogInfo("Outer scope message", correlationId, sourceContext);
                
                using (var innerScope = _loggingService.BeginScope(innerScopeName, correlationId, sourceContext))
                {
                    _loggingService.LogInfo("Inner scope message", correlationId, sourceContext);
                }
                
                _loggingService.LogInfo("Back to outer scope", correlationId, sourceContext);
            }
            
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(3));

            foreach (var message in mockTarget.Messages)
            {
                Assert.That(message.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
                Assert.That(message.SourceContext.ToString(), Is.EqualTo(sourceContext));
            }
        }

        [Test]
        public async Task LoggingPipeline_AsyncOperations_HandlesCorrectly()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var sourceContext = "IntegrationTest";
            var taskCount = 10;
            var messagesPerTask = 50;

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < taskCount; i++)
            {
                var taskIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < messagesPerTask; j++)
                    {
                        _loggingService.LogInfo($"Async task {taskIndex} message {j}", correlationId, sourceContext);
                    }
                }));
            }

            await Task.WhenAll(tasks);
            await _loggingService.FlushAsync();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(taskCount * messagesPerTask));

            // Verify all messages have correct correlation ID
            foreach (var message in mockTarget.Messages)
            {
                Assert.That(message.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
            }
        }

        #endregion

        #region System Integration Tests

        [Test]
        public void SystemIntegration_ProfilerService_RecordsMetrics()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var message = "Profiler integration test";
            var sourceContext = "IntegrationTest";

            // Act
            _loggingService.LogInfo(message, correlationId, sourceContext);
            _loggingService.FlushAsync().Wait();

            // Assert
            var metrics = _mockProfiler.GetMetrics();
            Assert.That(metrics.Count, Is.GreaterThan(0));
            Assert.That(metrics.Keys.Any(k => k.Contains("SerilogTarget")), Is.True);
        }

        [Test]
        public void SystemIntegration_AlertService_TriggersAlerts()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var message = "Critical alert test";
            var sourceContext = "IntegrationTest";

            // Act
            _loggingService.LogCritical(message, correlationId, sourceContext);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_mockAlerts.Alerts.Count, Is.GreaterThan(0));
            var alert = _mockAlerts.Alerts.First();
            Assert.That(alert.Severity, Is.EqualTo(AlertSeverity.Critical));
            Assert.That(alert.Message, Contains.Substring(message));
        }

        [Test]
        public void SystemIntegration_HealthCheckService_MonitorsHealth()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");

            // Act
            _loggingService.LogInfo("Health check test", correlationId, "IntegrationTest");
            var validationResult = _loggingService.ValidateConfiguration();

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [Test]
        public void SystemIntegration_MessageBusService_PublishesMessages()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var message = "Message bus integration test";
            var sourceContext = "IntegrationTest";

            // Act
            _loggingService.LogInfo(message, correlationId, sourceContext);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_mockMessageBus.PublishedMessages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void SystemIntegration_ConfigurationValidation_ValidatesCorrectly()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");

            // Act
            var validationResult = _loggingService.ValidateConfiguration(correlationId);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Errors, Is.Empty);
        }

        [Test]
        public void SystemIntegration_Statistics_ReportsAccurateData()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var messageCount = 10;

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"Statistics test message {i}", correlationId, "IntegrationTest");
            }
            _loggingService.FlushAsync().Wait();

            var statistics = _loggingService.GetStatistics();

            // Assert
            Assert.That(statistics.TotalMessages, Is.EqualTo(messageCount));
            Assert.That(statistics.ProcessedMessages, Is.EqualTo(messageCount));
            Assert.That(statistics.FilteredMessages, Is.EqualTo(0)); // No messages should be filtered
        }

        [Test]
        public void SystemIntegration_MaintenanceOperations_ExecuteCorrectly()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");

            // Act
            _loggingService.LogInfo("Before maintenance", correlationId, "IntegrationTest");
            _loggingService.PerformMaintenance(correlationId);
            _loggingService.LogInfo("After maintenance", correlationId, "IntegrationTest");
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(2));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        #endregion

        #region Error Scenario Tests

        [Test]
        public void ErrorScenario_TargetFailure_ContinuesLogging()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var failingTarget = _targets[0] as MockLogTarget;
            failingTarget.SetHealthy(false);

            // Act
            _loggingService.LogInfo("Message with failing target", correlationId, "IntegrationTest");
            _loggingService.FlushAsync().Wait();

            // Assert
            var workingTarget = _targets[1] as MockLogTarget;
            Assert.That(workingTarget.Messages.Count, Is.EqualTo(1));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [Test]
        public void ErrorScenario_FilterException_ContinuesProcessing()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var failingFilter = new TestFailingFilter("FailingFilter");
            _loggingService.AddFilter(failingFilter);

            // Act
            _loggingService.LogInfo("Message with failing filter", correlationId, "IntegrationTest");
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(1));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [Test]
        public void ErrorScenario_HighMemoryPressure_DegradesProperly()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var largeMessageCount = 10000;

            // Act
            for (int i = 0; i < largeMessageCount; i++)
            {
                var largeMessage = new string('X', 1000); // 1KB per message
                _loggingService.LogInfo($"Large message {i}: {largeMessage}", correlationId, "IntegrationTest");
            }
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_loggingService.IsEnabled, Is.True);
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ErrorScenario_ConfigurationChanges_HandlesDynamically()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            
            // Act
            _loggingService.LogInfo("Before configuration change", correlationId, "IntegrationTest");
            _loggingService.SetMinimumLevel(LogLevel.Warning, correlationId);
            _loggingService.LogInfo("After configuration change - should be filtered", correlationId, "IntegrationTest");
            _loggingService.LogWarning("After configuration change - should pass", correlationId, "IntegrationTest");
            _loggingService.FlushAsync().Wait();

            // Assert
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(2)); // First message and warning
            Assert.That(mockTarget.Messages[0].Message.ToString(), Contains.Substring("Before configuration"));
            Assert.That(mockTarget.Messages[1].Message.ToString(), Contains.Substring("should pass"));
        }

        [Test]
        public void ErrorScenario_ServiceDisposal_HandlesProperly()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            
            // Act
            _loggingService.LogInfo("Before disposal", correlationId, "IntegrationTest");
            _loggingService.Dispose();
            
            // Assert - Should not throw
            Assert.DoesNotThrow(() => _loggingService.LogInfo("After disposal", correlationId, "IntegrationTest"));
        }

        #endregion

        #region Performance Integration Tests

        [Test]
        public void PerformanceIntegration_HighThroughput_MaintainsPerformance()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var messageCount = 1000;
            var sourceContext = "IntegrationTest";

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"High throughput message {i}", correlationId, sourceContext);
            }
            _loggingService.FlushAsync().Wait();
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000)); // 2 seconds for 1000 messages
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(messageCount));
        }

        [Test]
        public void PerformanceIntegration_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var threadCount = 10;
            var messagesPerThread = 100;

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < messagesPerThread; j++)
                    {
                        _loggingService.LogInfo($"Thread {threadIndex} message {j}", correlationId, "IntegrationTest");
                    }
                }));
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Task.WaitAll(tasks.ToArray());
            _loggingService.FlushAsync().Wait();
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(3000)); // 3 seconds for concurrent access
            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(threadCount * messagesPerThread));
        }

        [Test]
        public void PerformanceIntegration_MemoryUsage_StayWithinLimits()
        {
            // Arrange
            var correlationId = new FixedString64Bytes("integration-test");
            var messageCount = 500;
            var sourceContext = "IntegrationTest";

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                for (int i = 0; i < messageCount; i++)
                {
                    _loggingService.LogInfo($"Memory test message {i}", correlationId, sourceContext);
                }
                _loggingService.FlushAsync().Wait();
            }, 5 * 1024 * 1024); // 5MB limit for complete integration

            var mockTarget = _targets[0] as MockLogTarget;
            Assert.That(mockTarget.Messages.Count, Is.EqualTo(messageCount));
        }

        #endregion

        #region Helper Methods

        private LoggingConfig CreateIntegrationTestConfig()
        {
            return new LoggingConfig
            {
                IsEnabled = true,
                MinimumLevel = LogLevel.Debug,
                Channels = new List<LogChannelConfig>
                {
                    new LogChannelConfig { Name = "IntegrationTest", IsEnabled = true, MinimumLevel = LogLevel.Debug },
                    new LogChannelConfig { Name = "Test", IsEnabled = true, MinimumLevel = LogLevel.Debug }
                }
            };
        }

        private ILogTargetConfig CreateFileTargetConfig()
        {
            var config = TestDataFactory.CreateLogTargetConfig("FileTarget", LogLevel.Info);
            config.Properties["FilePath"] = _tempLogFile;
            return config;
        }

        #endregion

        #region Helper Classes

        private class TestLevelFilter : ILogFilter
        {
            private readonly LogLevel _minimumLevel;

            public TestLevelFilter(string name, LogLevel minimumLevel)
            {
                Name = name;
                _minimumLevel = minimumLevel;
            }

            public string Name { get; }

            public bool ShouldLog(in LogMessage message)
            {
                return message.Level >= _minimumLevel;
            }
        }

        private class TestSourceFilter : ILogFilter
        {
            private readonly string[] _allowedSources;

            public TestSourceFilter(string name, string[] allowedSources)
            {
                Name = name;
                _allowedSources = allowedSources;
            }

            public string Name { get; }

            public bool ShouldLog(in LogMessage message)
            {
                return _allowedSources.Contains(message.SourceContext.ToString());
            }
        }

        private class TestCorrelationFilter : ILogFilter
        {
            private readonly string[] _allowedCorrelationIds;

            public TestCorrelationFilter(string name, string[] allowedCorrelationIds)
            {
                Name = name;
                _allowedCorrelationIds = allowedCorrelationIds;
            }

            public string Name { get; }

            public bool ShouldLog(in LogMessage message)
            {
                return _allowedCorrelationIds.Contains(message.CorrelationId.ToString());
            }
        }

        private class TestFailingFilter : ILogFilter
        {
            public TestFailingFilter(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public bool ShouldLog(in LogMessage message)
            {
                throw new InvalidOperationException("Filter intentionally failed for testing");
            }
        }

        #endregion
    }
}