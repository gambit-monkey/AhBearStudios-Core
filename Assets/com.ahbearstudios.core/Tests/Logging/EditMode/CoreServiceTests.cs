using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using NUnit.Framework;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Filters;
using AhBearStudios.Core.Tests.Shared;
using AhBearStudios.Core.Logging.Tests.Utilities;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Comprehensive tests for the core LoggingService functionality.
    /// Tests basic logging operations, correlation tracking, target management,
    /// and Unity-specific features like Job System compatibility.
    /// </summary>
    [TestFixture]
    public class CoreServiceTests
    {
        private ILoggingService _loggingService;
        private MockProfilerService _mockProfiler;
        private MockAlertService _mockAlerts;
        private MockHealthCheckService _mockHealthCheck;
        private MockMessageBusService _mockMessageBus;
        private MockLogTarget _mockTarget;
        private LoggingConfig _config;

        [SetUp]
        public void SetUp()
        {
            // Create mock dependencies
            _mockProfiler = new MockProfilerService();
            _mockAlerts = new MockAlertService();
            _mockHealthCheck = new MockHealthCheckService();
            _mockMessageBus = new MockMessageBusService();
            _mockTarget = TestUtilities.CreateMockLogTarget();

            // Create test configuration
            _config = new LoggingConfig
            {
                IsEnabled = true,
                MinimumLevel = LogLevel.Debug,
                Channels = new List<LogChannelConfig>
                {
                    new LogChannelConfig { Name = "Test", IsEnabled = true, MinimumLevel = LogLevel.Debug }
                }
            };

            // Create logging service with mocked dependencies
            _loggingService = new LoggingService(_config, _mockProfiler, _mockAlerts, _mockHealthCheck, _mockMessageBus);
            _loggingService.RegisterTarget(_mockTarget);
        }

        [TearDown]
        public void TearDown()
        {
            _loggingService?.Dispose();
            _mockProfiler?.Dispose();
            _mockAlerts?.Dispose();
            _mockHealthCheck?.Dispose();
            _mockMessageBus?.Dispose();
            _mockTarget?.Dispose();
            TestUtilities.CleanupTempFiles();
        }

        #region Basic Logging Operations Tests

        [Test]
        public void LogDebug_WithValidMessage_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Debug test message";
            var sourceContext = "TestClass";

            // Act
            _loggingService.LogDebug(message, correlationId, sourceContext);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Debug));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
            Assert.That(logMessage.SourceContext.ToString(), Is.EqualTo(sourceContext));
        }

        [Test]
        public void LogInfo_WithValidMessage_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Info test message";
            var sourceContext = "TestClass";

            // Act
            _loggingService.LogInfo(message, correlationId, sourceContext);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Info));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
        }

        [Test]
        public void LogWarning_WithValidMessage_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Warning test message";

            // Act
            _loggingService.LogWarning(message, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
        }

        [Test]
        public void LogError_WithValidMessage_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Error test message";

            // Act
            _loggingService.LogError(message, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
        }

        [Test]
        public void LogCritical_WithValidMessage_WritesToTargetAndTriggersAlert()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Critical test message";

            // Act
            _loggingService.LogCritical(message, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Critical));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            
            // Verify alert was triggered
            Assert.That(_mockAlerts.Alerts.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LogException_WithValidException_WritesToTargetWithExceptionData()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Exception test message";
            var exception = TestDataFactory.CreateTestException();

            // Act
            _loggingService.LogException(message, exception, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.HasException, Is.True);
            Assert.That(logMessage.Exception, Is.EqualTo(exception));
        }

        [Test]
        public void Log_WithAllParameters_WritesToTargetWithAllData()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Complete test message";
            var sourceContext = "TestClass";
            var exception = TestDataFactory.CreateTestException();
            var properties = TestDataFactory.CreateTestProperties();
            var channel = "CustomChannel";

            // Act
            _loggingService.Log(LogLevel.Warning, message, correlationId, sourceContext, exception, properties, channel);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
            Assert.That(logMessage.SourceContext.ToString(), Is.EqualTo(sourceContext));
            Assert.That(logMessage.Channel.ToString(), Is.EqualTo(channel));
            Assert.That(logMessage.HasException, Is.True);
            Assert.That(logMessage.HasProperties, Is.True);
        }

        #endregion

        #region Burst-Compatible Logging Tests

        [Test]
        public void LogDebug_WithBurstCompatibleData_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testData = new TestUnmanagedStruct { Value = 42, IsValid = true };

            // Act
            _loggingService.LogDebug("Burst test message", testData, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Debug));
            Assert.That(logMessage.Message.ToString(), Contains.Substring("Burst test message"));
        }

        [Test]
        public void LogInfo_WithBurstCompatibleData_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testData = new TestUnmanagedStruct { Value = 123, IsValid = true };

            // Act
            _loggingService.LogInfo("Burst info message", testData, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void LogWarning_WithBurstCompatibleData_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testData = new TestUnmanagedStruct { Value = 456, IsValid = false };

            // Act
            _loggingService.LogWarning("Burst warning message", testData, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void LogError_WithBurstCompatibleData_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testData = new TestUnmanagedStruct { Value = 789, IsValid = false };

            // Act
            _loggingService.LogError("Burst error message", testData, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void LogCritical_WithBurstCompatibleData_WritesToTarget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testData = new TestUnmanagedStruct { Value = 999, IsValid = false };

            // Act
            _loggingService.LogCritical("Burst critical message", testData, correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.Level, Is.EqualTo(LogLevel.Critical));
        }

        #endregion

        #region Correlation Tracking Tests

        [Test]
        public void CorrelationId_AcrossMultipleLogs_IsConsistentlyPropagated()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act
            _loggingService.LogDebug("Debug message", correlationId);
            _loggingService.LogInfo("Info message", correlationId);
            _loggingService.LogWarning("Warning message", correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(3));
            TestUtilities.AssertCorrelationIdPropagation(_mockTarget.Messages.Select(m => 
                TestDataFactory.CreateLogEntry(m.Level, m.Channel.ToString(), m.Message.ToString(), 
                    m.CorrelationId.ToString(), m.SourceContext.ToString())), correlationId.ToString());
        }

        [Test]
        public void BeginScope_WithCorrelationId_CreatesHierarchicalContext()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var scopeName = "TestScope";

            // Act
            using (var scope = _loggingService.BeginScope(scopeName, correlationId))
            {
                _loggingService.LogInfo("Message in scope", correlationId);
            }

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            var logMessage = _mockTarget.Messages[0];
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
        }

        #endregion

        #region Target Management Tests

        [Test]
        public void RegisterTarget_WithValidTarget_AddsToTargetList()
        {
            // Arrange
            var additionalTarget = TestUtilities.CreateMockLogTarget("AdditionalTarget");
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act
            _loggingService.RegisterTarget(additionalTarget, correlationId);

            // Assert
            var targets = _loggingService.GetTargets();
            Assert.That(targets.Count, Is.EqualTo(2)); // Original + additional
            Assert.That(targets.Any(t => t.Name == "AdditionalTarget"), Is.True);
        }

        [Test]
        public void UnregisterTarget_WithValidTargetName_RemovesFromTargetList()
        {
            // Arrange
            var targetName = _mockTarget.Name;
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act
            var result = _loggingService.UnregisterTarget(targetName, correlationId);

            // Assert
            Assert.That(result, Is.True);
            var targets = _loggingService.GetTargets();
            Assert.That(targets.Any(t => t.Name == targetName), Is.False);
        }

        [Test]
        public void UnregisterTarget_WithInvalidTargetName_ReturnsFalse()
        {
            // Arrange
            var invalidTargetName = "NonExistentTarget";
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act
            var result = _loggingService.UnregisterTarget(invalidTargetName, correlationId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetTargets_ReturnsReadOnlyCollection()
        {
            // Act
            var targets = _loggingService.GetTargets();

            // Assert
            Assert.That(targets, Is.Not.Null);
            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets.First().Name, Is.EqualTo(_mockTarget.Name));
        }

        #endregion

        #region Filtering Tests

        [Test]
        public void SetMinimumLevel_ChangesLogLevelFiltering()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            
            // Act
            _loggingService.SetMinimumLevel(LogLevel.Warning, correlationId);
            _loggingService.LogDebug("Debug message", correlationId);
            _loggingService.LogInfo("Info message", correlationId);
            _loggingService.LogWarning("Warning message", correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            Assert.That(_mockTarget.Messages[0].Level, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void AddFilter_WithValidFilter_FiltersMessages()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var filter = new TestLogFilter(LogLevel.Error);

            // Act
            _loggingService.AddFilter(filter, correlationId);
            _loggingService.LogInfo("Info message", correlationId);
            _loggingService.LogError("Error message", correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            Assert.That(_mockTarget.Messages[0].Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void RemoveFilter_WithValidFilterName_RemovesFiltering()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var filter = new TestLogFilter(LogLevel.Error);

            // Act
            _loggingService.AddFilter(filter, correlationId);
            _loggingService.RemoveFilter(filter.Name, correlationId);
            _loggingService.LogInfo("Info message", correlationId);
            _loggingService.LogError("Error message", correlationId);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(2));
        }

        #endregion

        #region Channel Management Tests

        [Test]
        public void RegisterChannel_WithValidChannel_AddsToChannelList()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var channel = new TestLogChannel("NewChannel");

            // Act
            _loggingService.RegisterChannel(channel, correlationId);

            // Assert
            var channels = _loggingService.GetChannels();
            Assert.That(channels.Any(c => c.Name == "NewChannel"), Is.True);
        }

        [Test]
        public void UnregisterChannel_WithValidChannelName_RemovesFromChannelList()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var channel = new TestLogChannel("TempChannel");
            _loggingService.RegisterChannel(channel, correlationId);

            // Act
            var result = _loggingService.UnregisterChannel("TempChannel", correlationId);

            // Assert
            Assert.That(result, Is.True);
            var channels = _loggingService.GetChannels();
            Assert.That(channels.Any(c => c.Name == "TempChannel"), Is.False);
        }

        [Test]
        public void GetChannel_WithValidName_ReturnsChannel()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var channel = new TestLogChannel("TestChannel");
            _loggingService.RegisterChannel(channel, correlationId);

            // Act
            var result = _loggingService.GetChannel("TestChannel");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("TestChannel"));
        }

        [Test]
        public void HasChannel_WithValidName_ReturnsTrue()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var channel = new TestLogChannel("ExistingChannel");
            _loggingService.RegisterChannel(channel, correlationId);

            // Act
            var result = _loggingService.HasChannel("ExistingChannel");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasChannel_WithInvalidName_ReturnsFalse()
        {
            // Act
            var result = _loggingService.HasChannel("NonExistentChannel");

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region Async Operations Tests

        [Test]
        public async Task FlushAsync_WithValidCorrelationId_CompletesSuccessfully()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            _loggingService.LogInfo("Test message", correlationId);

            // Act
            await _loggingService.FlushAsync(correlationId);

            // Assert
            Assert.That(_mockTarget.FlushCount, Is.EqualTo(1));
        }

        [Test]
        public async Task FlushAsync_WithTimeout_CompletesWithinTimeLimit()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            _loggingService.LogInfo("Test message", correlationId);

            // Act & Assert
            await TestUtilities.AssertExecutionTimeAsync(
                async () => await _loggingService.FlushAsync(correlationId),
                TimeSpan.FromSeconds(5));
        }

        [Test]
        public void ValidateConfiguration_WithValidConfig_ReturnsValidResult()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act
            var result = _loggingService.ValidateConfiguration(correlationId);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void PerformMaintenance_WithValidCorrelationId_CompletesSuccessfully()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act & Assert
            Assert.DoesNotThrow(() => _loggingService.PerformMaintenance(correlationId));
        }

        [Test]
        public void GetStatistics_ReturnsValidStatistics()
        {
            // Arrange
            _loggingService.LogInfo("Test message 1");
            _loggingService.LogInfo("Test message 2");
            _loggingService.LogError("Test error");

            // Act
            var statistics = _loggingService.GetStatistics();

            // Assert
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics.TotalMessages, Is.EqualTo(3));
        }

        #endregion

        #region Performance Tests

        [Test]
        public void LogInfo_PerformanceTest_CompletesWithinFrameBudget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Performance test message";

            // Act & Assert
            TestUtilities.AssertFrameBudget(() => _loggingService.LogInfo(message, correlationId));
        }

        [Test]
        public void LogInfo_HighVolumeTest_MaintainsPerformance()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 1000;

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                for (int i = 0; i < messageCount; i++)
                {
                    _loggingService.LogInfo($"Message {i}", correlationId);
                }
            }, TimeSpan.FromSeconds(1));

            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(messageCount));
        }

        [Test]
        public void LogInfo_MemoryAllocationTest_StaysWithinLimits()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Memory test message";

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    _loggingService.LogInfo(message, correlationId);
                }
            }, 1024 * 1024); // 1MB limit
        }

        #endregion

        #region Concurrent Access Tests

        [Test]
        public async Task LogInfo_ConcurrentAccess_ThreadSafeOperation()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var threadCount = 10;
            var messagesPerThread = 100;

            // Act
            await TestUtilities.RunConcurrentStressTest(
                async (index) =>
                {
                    _loggingService.LogInfo($"Concurrent message {index}", correlationId);
                    await Task.Delay(1); // Simulate some work
                },
                threadCount,
                messagesPerThread);

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(threadCount * messagesPerThread));
            TestUtilities.AssertUniqueIds(_mockTarget.Messages.Select(m => 
                TestDataFactory.CreateLogEntry(m.Level, m.Channel.ToString(), m.Message.ToString(), 
                    m.CorrelationId.ToString(), m.SourceContext.ToString())));
        }

        #endregion

        #region Service Lifecycle Tests

        [Test]
        public void IsEnabled_WhenConfigured_ReturnsCorrectValue()
        {
            // Assert
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [Test]
        public void Configuration_WhenAccessed_ReturnsCorrectConfiguration()
        {
            // Act
            var config = _loggingService.Configuration;

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.IsEnabled, Is.True);
            Assert.That(config.MinimumLevel, Is.EqualTo(LogLevel.Debug));
        }

        [Test]
        public void Dispose_WhenCalled_CleansUpResources()
        {
            // Act
            _loggingService.Dispose();

            // Assert
            Assert.DoesNotThrow(() => _loggingService.Dispose()); // Should not throw on second dispose
        }

        #endregion

        #region Test Helper Classes

        private struct TestUnmanagedStruct
        {
            public int Value;
            public bool IsValid;
        }

        private class TestLogFilter : ILogFilter
        {
            private readonly LogLevel _minimumLevel;

            public TestLogFilter(LogLevel minimumLevel)
            {
                _minimumLevel = minimumLevel;
                Name = $"TestFilter_{minimumLevel}";
            }

            public string Name { get; }

            public bool ShouldLog(in LogMessage message)
            {
                return message.Level >= _minimumLevel;
            }
        }

        private class TestLogChannel : ILogChannel
        {
            public TestLogChannel(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        #endregion
    }
}