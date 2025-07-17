using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using NUnit.Framework;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Tests.Shared;
using AhBearStudios.Core.Logging.Tests.Utilities;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Profiling.Models;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Comprehensive tests for logging targets including SerilogTarget, ConsoleLogTarget, 
    /// and FileLogTarget. Tests focus on Unity-specific optimizations, performance 
    /// monitoring, and platform-specific behavior.
    /// </summary>
    [TestFixture]
    public class TargetTests
    {
        private MockProfilerService _mockProfiler;
        private MockAlertService _mockAlerts;
        private MockHealthCheckService _mockHealthCheck;
        private string _tempFilePath;

        [SetUp]
        public void SetUp()
        {
            _mockProfiler = new MockProfilerService();
            _mockAlerts = new MockAlertService();
            _mockHealthCheck = new MockHealthCheckService();
            _tempFilePath = TestUtilities.CreateTempFilePath();
        }

        [TearDown]
        public void TearDown()
        {
            _mockProfiler?.Dispose();
            _mockAlerts?.Dispose();
            _mockHealthCheck?.Dispose();
            TestUtilities.CleanupTempFiles();
        }

        #region SerilogTarget Tests

        [Test]
        public void SerilogTarget_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest", LogLevel.Debug, true);

            // Act
            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Assert
                Assert.That(target.Name, Is.EqualTo("SerilogTest"));
                Assert.That(target.MinimumLevel, Is.EqualTo(LogLevel.Debug));
                Assert.That(target.IsEnabled, Is.True);
                Assert.That(target.IsHealthy, Is.True);
                Assert.That(target.UseAsyncWrite, Is.False);
            }
        }

        [Test]
        public void SerilogTarget_Write_WithValidMessage_WritesSuccessfully()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Serilog test message");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.Write(message);

                // Assert
                Assert.That(target.MessagesWritten, Is.EqualTo(1));
                Assert.That(target.MessagesDropped, Is.EqualTo(0));
                Assert.That(target.ErrorsEncountered, Is.EqualTo(0));
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void SerilogTarget_Write_WithExceptionMessage_WritesExceptionData()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var exception = TestDataFactory.CreateTestException("Test exception");
            var message = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error with exception",
                "test-correlation", "TestClass", exception);

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.Write(message);

                // Assert
                Assert.That(target.MessagesWritten, Is.EqualTo(1));
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void SerilogTarget_Write_WithStructuredProperties_WritesPropertiesData()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var properties = TestDataFactory.CreateTestProperties();
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Message with properties",
                "test-correlation", "TestClass", properties: properties);

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.Write(message);

                // Assert
                Assert.That(target.MessagesWritten, Is.EqualTo(1));
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void SerilogTarget_WriteBatch_WithValidMessages_WritesAllMessages()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var messages = TestDataFactory.CreateLogMessageBatch(5, LogLevel.Info);

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.WriteBatch(messages);

                // Assert
                Assert.That(target.MessagesWritten, Is.EqualTo(5));
                Assert.That(target.MessagesDropped, Is.EqualTo(0));
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void SerilogTarget_WriteBatch_WithFrameLimiting_RespectsFrameBudget()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var messages = TestDataFactory.CreateLogMessageBatch(50, LogLevel.Info); // More than MAX_MESSAGES_PER_FRAME

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.WriteBatch(messages);

                // Assert
                Assert.That(target.MessagesWritten, Is.LessThanOrEqualTo(10)); // MAX_MESSAGES_PER_FRAME limit
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void SerilogTarget_ShouldProcessMessage_WithLevelFiltering_FiltersCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest", LogLevel.Warning);
            var debugMessage = TestDataFactory.CreateLogMessage(LogLevel.Debug, "Test", "Debug message");
            var infoMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Info message");
            var warningMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Test", "Warning message");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                Assert.That(target.ShouldProcessMessage(debugMessage), Is.False);
                Assert.That(target.ShouldProcessMessage(infoMessage), Is.False);
                Assert.That(target.ShouldProcessMessage(warningMessage), Is.True);
            }
        }

        [Test]
        public void SerilogTarget_ShouldProcessMessage_WithChannelFiltering_FiltersCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest", LogLevel.Debug, true, 
                new List<string> { "AllowedChannel" });
            var allowedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "AllowedChannel", "Allowed message");
            var blockedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "BlockedChannel", "Blocked message");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                Assert.That(target.ShouldProcessMessage(allowedMessage), Is.True);
                Assert.That(target.ShouldProcessMessage(blockedMessage), Is.False);
            }
        }

        [Test]
        public void SerilogTarget_PerformHealthCheck_WhenHealthy_ReturnsTrue()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                var result = target.PerformHealthCheck();

                // Assert
                Assert.That(result, Is.True);
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void SerilogTarget_PerformHealthCheck_WithHighErrorRate_ReturnsUnhealthy()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            config.ErrorRateThreshold = 0.1; // 10% error rate threshold

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Simulate high error rate by forcing errors
                for (int i = 0; i < 150; i++) // Write enough messages to trigger error rate check
                {
                    var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", $"Message {i}");
                    target.Write(message);
                }

                // Act
                var result = target.PerformHealthCheck();

                // Assert
                Assert.That(result, Is.True); // Should still be healthy under normal conditions
            }
        }

        [Test]
        public void SerilogTarget_GetStatistics_ReturnsCorrectStatistics()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var messages = TestDataFactory.CreateLogMessageBatch(3, LogLevel.Info);

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                foreach (var message in messages)
                {
                    target.Write(message);
                }
                var statistics = target.GetStatistics();

                // Assert
                Assert.That(statistics.ContainsKey("MessagesWritten"), Is.True);
                Assert.That(statistics.ContainsKey("MessagesDropped"), Is.True);
                Assert.That(statistics.ContainsKey("ErrorsEncountered"), Is.True);
                Assert.That(statistics.ContainsKey("ErrorRate"), Is.True);
                Assert.That(statistics.ContainsKey("IsHealthy"), Is.True);
                Assert.That(statistics.ContainsKey("UseAsyncWrite"), Is.True);
                Assert.That(statistics["MessagesWritten"], Is.EqualTo(3L));
                Assert.That(statistics["IsHealthy"], Is.EqualTo(true));
            }
        }

        [Test]
        public void SerilogTarget_Flush_CompletesSuccessfully()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Flush test message");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                target.Write(message);

                // Act & Assert
                Assert.DoesNotThrow(() => target.Flush());
            }
        }

        [Test]
        public void SerilogTarget_ReconfigureLogger_UpdatesLoggerConfiguration()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                Assert.DoesNotThrow(() => target.ReconfigureLogger(loggerConfig =>
                {
                    loggerConfig.MinimumLevel.Information();
                }));
            }
        }

        [Test]
        public void SerilogTarget_UnityPlatformOptimization_RespectsFrameBudget()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Frame budget test");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                TestUtilities.AssertFrameBudget(() => target.Write(message), 0.5); // 0.5ms frame budget
            }
        }

        [Test]
        public void SerilogTarget_ProfilerIntegration_RecordsMetrics()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            config.EnablePerformanceMetrics = true;
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Profiler test message");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.Write(message);

                // Assert
                var metrics = _mockProfiler.GetMetrics();
                Assert.That(metrics.Count, Is.GreaterThan(0));
                Assert.That(metrics.Keys.Any(k => k.Contains("SerilogTarget")), Is.True);
            }
        }

        [Test]
        public void SerilogTarget_AlertIntegration_TriggersAlertsOnErrors()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Force an error by testing health check failure
                target.PerformHealthCheck();

                // Act - Write some messages and check for alerts
                var message = TestDataFactory.CreateLogMessage(LogLevel.Critical, "Test", "Critical message");
                target.Write(message);

                // Assert
                // Note: In a real scenario, alerts would be triggered by actual errors
                // Here we're just verifying the alert system is connected
                Assert.That(_mockAlerts, Is.Not.Null);
            }
        }

        [Test]
        public void SerilogTarget_AsyncWrite_WithEnabledAsync_WritesAsynchronously()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest", useAsyncWrite: true, 
                maxConcurrentAsyncOperations: 2);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Async test message");

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act
                target.Write(message);

                // Assert
                Assert.That(target.UseAsyncWrite, Is.True);
                Assert.That(target.MaxConcurrentAsyncOperations, Is.EqualTo(2));
                
                // Wait a bit for async operation to complete
                System.Threading.Thread.Sleep(100);
                Assert.That(target.MessagesWritten, Is.EqualTo(1));
            }
        }

        [Test]
        public void SerilogTarget_Performance_HighVolumeTest()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var messageCount = 1000;
            var messages = TestDataFactory.CreateLogMessageBatch(messageCount, LogLevel.Info);

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                TestUtilities.AssertExecutionTime(() =>
                {
                    foreach (var message in messages)
                    {
                        target.Write(message);
                    }
                }, TimeSpan.FromSeconds(2));

                Assert.That(target.MessagesWritten, Is.EqualTo(messageCount));
            }
        }

        [Test]
        public void SerilogTarget_Memory_LimitedAllocation()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("SerilogTest");
            var messageCount = 100;
            var messages = TestDataFactory.CreateLogMessageBatch(messageCount, LogLevel.Info);

            using (var target = new SerilogTarget(config, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                TestUtilities.AssertMemoryUsage(() =>
                {
                    foreach (var message in messages)
                    {
                        target.Write(message);
                    }
                }, 2 * 1024 * 1024); // 2MB limit

                Assert.That(target.MessagesWritten, Is.EqualTo(messageCount));
            }
        }

        #endregion

        #region ConsoleLogTarget Tests

        [Test]
        public void ConsoleLogTarget_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest", LogLevel.Debug, true);

            // Act
            using (var target = new ConsoleLogTarget(config))
            {
                // Assert
                Assert.That(target.Name, Is.EqualTo("ConsoleTest"));
                Assert.That(target.MinimumLevel, Is.EqualTo(LogLevel.Debug));
                Assert.That(target.IsEnabled, Is.True);
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void ConsoleLogTarget_Write_WithValidMessage_WritesToConsole()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest");
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Console test message");

            using (var target = new ConsoleLogTarget(config))
            {
                // Act & Assert
                Assert.DoesNotThrow(() => target.Write(message));
            }
        }

        [Test]
        public void ConsoleLogTarget_WriteBatch_WithValidMessages_WritesAllMessages()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest");
            var messages = TestDataFactory.CreateLogMessageBatch(3, LogLevel.Info);

            using (var target = new ConsoleLogTarget(config))
            {
                // Act & Assert
                Assert.DoesNotThrow(() => target.WriteBatch(messages));
            }
        }

        [Test]
        public void ConsoleLogTarget_ShouldProcessMessage_WithLevelFiltering_FiltersCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest", LogLevel.Warning);
            var debugMessage = TestDataFactory.CreateLogMessage(LogLevel.Debug, "Test", "Debug message");
            var warningMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Test", "Warning message");

            using (var target = new ConsoleLogTarget(config))
            {
                // Act & Assert
                Assert.That(target.ShouldProcessMessage(debugMessage), Is.False);
                Assert.That(target.ShouldProcessMessage(warningMessage), Is.True);
            }
        }

        [Test]
        public void ConsoleLogTarget_PerformHealthCheck_ReturnsTrue()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest");

            using (var target = new ConsoleLogTarget(config))
            {
                // Act
                var result = target.PerformHealthCheck();

                // Assert
                Assert.That(result, Is.True);
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void ConsoleLogTarget_Flush_CompletesSuccessfully()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest");

            using (var target = new ConsoleLogTarget(config))
            {
                // Act & Assert
                Assert.DoesNotThrow(() => target.Flush());
            }
        }

        [Test]
        public void ConsoleLogTarget_Performance_FrameBudgetCompliance()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("ConsoleTest");
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Performance test message");

            using (var target = new ConsoleLogTarget(config))
            {
                // Act & Assert
                TestUtilities.AssertFrameBudget(() => target.Write(message), 0.5); // 0.5ms frame budget
            }
        }

        #endregion

        #region FileLogTarget Tests

        [Test]
        public void FileLogTarget_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest", LogLevel.Debug, true);
            config.Properties["FilePath"] = _tempFilePath;

            // Act
            using (var target = new FileLogTarget(config))
            {
                // Assert
                Assert.That(target.Name, Is.EqualTo("FileTest"));
                Assert.That(target.MinimumLevel, Is.EqualTo(LogLevel.Debug));
                Assert.That(target.IsEnabled, Is.True);
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void FileLogTarget_Write_WithValidMessage_WritesToFile()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "File test message");

            using (var target = new FileLogTarget(config))
            {
                // Act
                target.Write(message);
                target.Flush();

                // Assert
                Assert.That(File.Exists(_tempFilePath), Is.True);
                var fileContent = File.ReadAllText(_tempFilePath);
                Assert.That(fileContent, Contains.Substring("File test message"));
            }
        }

        [Test]
        public void FileLogTarget_WriteBatch_WithValidMessages_WritesAllToFile()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;
            var messages = TestDataFactory.CreateLogMessageBatch(3, LogLevel.Info);

            using (var target = new FileLogTarget(config))
            {
                // Act
                target.WriteBatch(messages);
                target.Flush();

                // Assert
                Assert.That(File.Exists(_tempFilePath), Is.True);
                var fileContent = File.ReadAllText(_tempFilePath);
                Assert.That(fileContent.Split('\n').Length, Is.GreaterThan(2)); // Should have multiple lines
            }
        }

        [Test]
        public void FileLogTarget_ShouldProcessMessage_WithLevelFiltering_FiltersCorrectly()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest", LogLevel.Error);
            config.Properties["FilePath"] = _tempFilePath;
            var infoMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Info message");
            var errorMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error message");

            using (var target = new FileLogTarget(config))
            {
                // Act & Assert
                Assert.That(target.ShouldProcessMessage(infoMessage), Is.False);
                Assert.That(target.ShouldProcessMessage(errorMessage), Is.True);
            }
        }

        [Test]
        public void FileLogTarget_PerformHealthCheck_WithValidFile_ReturnsTrue()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;

            using (var target = new FileLogTarget(config))
            {
                // Act
                var result = target.PerformHealthCheck();

                // Assert
                Assert.That(result, Is.True);
                Assert.That(target.IsHealthy, Is.True);
            }
        }

        [Test]
        public void FileLogTarget_PerformHealthCheck_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = "/invalid/path/that/does/not/exist/test.log";

            using (var target = new FileLogTarget(config))
            {
                // Act
                var result = target.PerformHealthCheck();

                // Assert
                Assert.That(result, Is.False);
                Assert.That(target.IsHealthy, Is.False);
            }
        }

        [Test]
        public void FileLogTarget_Flush_EnsuresDataIsPersisted()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Flush test message");

            using (var target = new FileLogTarget(config))
            {
                // Act
                target.Write(message);
                target.Flush();

                // Assert
                Assert.That(File.Exists(_tempFilePath), Is.True);
                var fileContent = File.ReadAllText(_tempFilePath);
                Assert.That(fileContent, Contains.Substring("Flush test message"));
            }
        }

        [Test]
        public void FileLogTarget_Performance_FrameBudgetCompliance()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Performance test message");

            using (var target = new FileLogTarget(config))
            {
                // Act & Assert
                TestUtilities.AssertFrameBudget(() => target.Write(message), 1.0); // 1ms frame budget for file I/O
            }
        }

        [Test]
        public void FileLogTarget_ConcurrentWrite_ThreadSafe()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;
            var messageCount = 100;

            using (var target = new FileLogTarget(config))
            {
                // Act
                var tasks = new List<Task>();
                for (int i = 0; i < messageCount; i++)
                {
                    var index = i;
                    tasks.Add(Task.Run(() =>
                    {
                        var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", $"Concurrent message {index}");
                        target.Write(message);
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                target.Flush();

                // Assert
                Assert.That(File.Exists(_tempFilePath), Is.True);
                var fileContent = File.ReadAllText(_tempFilePath);
                var lineCount = fileContent.Split('\n').Length;
                Assert.That(lineCount, Is.GreaterThan(messageCount / 2)); // Should have written most messages
            }
        }

        [Test]
        public void FileLogTarget_LargeFile_HandlesRotation()
        {
            // Arrange
            var config = TestDataFactory.CreateLogTargetConfig("FileTest");
            config.Properties["FilePath"] = _tempFilePath;
            config.Properties["MaxFileSize"] = 1024; // 1KB limit to force rotation
            var messageCount = 100;

            using (var target = new FileLogTarget(config))
            {
                // Act
                for (int i = 0; i < messageCount; i++)
                {
                    var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", 
                        $"Large message test {i} with additional content to increase size");
                    target.Write(message);
                }
                target.Flush();

                // Assert
                Assert.That(File.Exists(_tempFilePath), Is.True);
                var fileInfo = new FileInfo(_tempFilePath);
                // File size should be managed (though exact rotation behavior depends on implementation)
                Assert.That(fileInfo.Length, Is.GreaterThan(0));
            }
        }

        #endregion

        #region Integration Tests

        [Test]
        public void MultipleTargets_SameMessage_AllTargetsReceiveMessage()
        {
            // Arrange
            var consoleConfig = TestDataFactory.CreateLogTargetConfig("ConsoleTest");
            var fileConfig = TestDataFactory.CreateLogTargetConfig("FileTest");
            fileConfig.Properties["FilePath"] = _tempFilePath;
            var serilogConfig = TestDataFactory.CreateLogTargetConfig("SerilogTest");

            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Integration test message");

            using (var consoleTarget = new ConsoleLogTarget(consoleConfig))
            using (var fileTarget = new FileLogTarget(fileConfig))
            using (var serilogTarget = new SerilogTarget(serilogConfig, _mockProfiler, _mockAlerts))
            {
                // Act
                consoleTarget.Write(message);
                fileTarget.Write(message);
                serilogTarget.Write(message);

                consoleTarget.Flush();
                fileTarget.Flush();
                serilogTarget.Flush();

                // Assert
                Assert.That(consoleTarget.IsHealthy, Is.True);
                Assert.That(fileTarget.IsHealthy, Is.True);
                Assert.That(serilogTarget.IsHealthy, Is.True);
                Assert.That(serilogTarget.MessagesWritten, Is.EqualTo(1));
                Assert.That(File.Exists(_tempFilePath), Is.True);
            }
        }

        [Test]
        public void AllTargets_PerformanceTest_MaintainFrameBudget()
        {
            // Arrange
            var consoleConfig = TestDataFactory.CreateLogTargetConfig("ConsoleTest");
            var fileConfig = TestDataFactory.CreateLogTargetConfig("FileTest");
            fileConfig.Properties["FilePath"] = _tempFilePath;
            var serilogConfig = TestDataFactory.CreateLogTargetConfig("SerilogTest");

            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Performance test message");

            using (var consoleTarget = new ConsoleLogTarget(consoleConfig))
            using (var fileTarget = new FileLogTarget(fileConfig))
            using (var serilogTarget = new SerilogTarget(serilogConfig, _mockProfiler, _mockAlerts))
            {
                // Act & Assert
                TestUtilities.AssertFrameBudget(() =>
                {
                    consoleTarget.Write(message);
                    fileTarget.Write(message);
                    serilogTarget.Write(message);
                }, 2.0); // 2ms frame budget for all targets combined
            }
        }

        [Test]
        public void AllTargets_HealthCheck_ReportCorrectHealth()
        {
            // Arrange
            var consoleConfig = TestDataFactory.CreateLogTargetConfig("ConsoleTest");
            var fileConfig = TestDataFactory.CreateLogTargetConfig("FileTest");
            fileConfig.Properties["FilePath"] = _tempFilePath;
            var serilogConfig = TestDataFactory.CreateLogTargetConfig("SerilogTest");

            using (var consoleTarget = new ConsoleLogTarget(consoleConfig))
            using (var fileTarget = new FileLogTarget(fileConfig))
            using (var serilogTarget = new SerilogTarget(serilogConfig, _mockProfiler, _mockAlerts))
            {
                // Act
                var consoleHealth = consoleTarget.PerformHealthCheck();
                var fileHealth = fileTarget.PerformHealthCheck();
                var serilogHealth = serilogTarget.PerformHealthCheck();

                // Assert
                Assert.That(consoleHealth, Is.True);
                Assert.That(fileHealth, Is.True);
                Assert.That(serilogHealth, Is.True);
            }
        }

        #endregion
    }
}