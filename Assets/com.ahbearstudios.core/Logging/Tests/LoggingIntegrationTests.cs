using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Integration tests for the logging system components working together
    /// </summary>
    [TestFixture]
    public class LoggingIntegrationTests
    {
        private MockBurstLogger _logger;
        private MockLogTarget _target;
        private MockLogFormatter _formatter;
        private List<IDisposable> _disposables;

        [SetUp]
        public void SetUp()
        {
            _logger = new MockBurstLogger();
            _target = new MockLogTargetTests.MockLogTarget("IntegrationTarget");
            _formatter = new MockLogFormatter();
            _disposables = new List<IDisposable>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
            
            _target?.Dispose();
            _logger?.Clear();
        }

        [Test]
        public void LoggingPipeline_EndToEnd_WorksCorrectly()
        {
            // Assert
            Assert.That(formattedResults.Count, Is.EqualTo(3));
            Assert.That(formattedResults[0], Is.EqualTo("[Info] Tag1: Test message 1"));
            Assert.That(formattedResults[1], Is.EqualTo("[Warning] Tag2: Test message 2"));
            Assert.That(formattedResults[2], Is.EqualTo("[Error] Tag3: Test message 3"));
        }

        [Test]
        public void LoggingPipeline_ConfigurationChanges_TakeEffectImmediately()
        {
            // Arrange
            var message = "Configuration test";
            var tag = "ConfigTag";

            // Initial state - should log
            _logger.Log(LogLevel.Info, message, tag);
            var initialCount = _logger.LogEntries.Count;

            // Act - Change minimum level
            _logger.MinimumLevel = LogLevel.Error;
            _logger.Log(LogLevel.Info, message, tag); // Should not log
            _logger.Log(LogLevel.Error, message, tag); // Should log

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(initialCount + 1));
            Assert.That(_logger.LogEntries.Last().Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void LoggingPipeline_MemoryManagement_ProperlyDisposesResources()
        {
            // Arrange
            var propertiesList = new List<LogProperties>();
            
            // Act - Create multiple LogProperties instances
            for (int i = 0; i < 10; i++)
            {
                var properties = new LogProperties(Allocator.Temp);
                properties.Add($"Key{i}", $"Value{i}");
                propertiesList.Add(properties);
                _disposables.Add(properties);

                _logger.Log(LogLevel.Info, $"Message {i}", "Tag", properties);
            }

            // Assert - All containers should be created
            foreach (var props in propertiesList)
            {
                Assert.That(props.IsCreated, Is.True);
            }

            // Act - Dispose all
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();

            // Assert - All containers should be disposed
            foreach (var props in propertiesList)
            {
                Assert.That(props.IsCreated, Is.False);
            }
        }

        [Test]
        public void LoggingPipeline_ConcurrentAccess_RemainsThreadSafe()
        {
            // Arrange
            var messageCount = 100;
            var exceptions = new List<Exception>();

            // Act - Simulate concurrent access (basic test)
            System.Threading.Tasks.Parallel.For(0, messageCount, i =>
            {
                try
                {
                    _logger.Log(LogLevel.Info, $"Concurrent message {i}", $"Thread{i % 4}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // Assert
            Assert.That(exceptions, Is.Empty, "No exceptions should occur during concurrent access");
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(messageCount));
        }

        [Test]
        public void LoggingPipeline_LevelHierarchy_RespectsCorrectOrder()
        {
            // Arrange
            var levels = new[] 
            { 
                LogLevel.Trace, 
                LogLevel.Debug, 
                LogLevel.Info, 
                LogLevel.Warning, 
                LogLevel.Error, 
                LogLevel.Fatal 
            };

            // Test each minimum level
            foreach (var minimumLevel in levels)
            {
                _logger.Clear();
                _logger.MinimumLevel = minimumLevel;

                // Act - Log all levels
                foreach (var testLevel in levels)
                {
                    _logger.Log(testLevel, $"Message for {testLevel}", "Tag");
                }

                // Assert - Only levels >= minimum should be logged
                var expectedCount = levels.Count(l => l >= minimumLevel);
                Assert.That(_logger.LogEntries.Count, Is.EqualTo(expectedCount), 
                    $"Failed for minimum level {minimumLevel}");

                // Verify all logged entries are >= minimum level
                foreach (var entry in _logger.LogEntries)
                {
                    Assert.That(entry.Level, Is.GreaterThanOrEqualTo(minimumLevel));
                }
            }
        }

        [Test]
        public void LoggingPipeline_FlushOperations_ExecuteCorrectly()
        {
            // Arrange
            var logMessage = new LogMessage(LogLevel.Info, "Flush test", "FlushTag");
            
            // Act
            _target.Write(logMessage);
            _target.Write(logMessage);
            _target.Flush();
            _target.Write(logMessage);
            _target.Flush();

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(3));
            Assert.That(_target.FlushCallCount, Is.EqualTo(2));
        }

        [Test]
        public void LoggingPipeline_ComplexScenario_HandlesCorrectly()
        {
            // Arrange - Complex scenario with multiple components and configurations
            using var target2 = new MockLogTargetTests.MockLogTarget("ComplexTarget2");
            _disposables.Add(target2);

            _logger.MinimumLevel = LogLevel.Debug;
            _target.MinimumLevel = LogLevel.Info;
            _target.SetTagFilters(new[] { "Important" }, null, true);
            
            target2.MinimumLevel = LogLevel.Warning;
            target2.SetTagFilters(null, new[] { "Spam" }, true);

            using var properties = new LogProperties(Allocator.Temp);
            _disposables.Add(properties);
            properties.Add("SessionId", "ABC123");
            properties.Add("Operation", "ComplexTest");

            var scenarios = new[]
            {
                (LogLevel.Trace, "Trace message", "Debug", false),      // Filtered by logger
                (LogLevel.Debug, "Debug message", "Debug", false),      // Filtered by target1
                (LogLevel.Info, "Info message", "Important", true),     // Passes target1, filtered by target2
                (LogLevel.Warning, "Warning message", "Important", true), // Passes both targets
                (LogLevel.Error, "Error message", "Spam", false),       // Passes target1, filtered by target2
                (LogLevel.Fatal, "Fatal message", "Critical", true)     // Passes both targets
            };

            // Act
            foreach (var (level, message, tag, withProps) in scenarios)
            {
                if (withProps)
                {
                    _logger.Log(level, message, tag, properties);
                }
                else
                {
                    _logger.Log(level, message, tag);
                }

                var logMessage = new LogMessage(level, message, tag);
                _target.Write(logMessage);
                target2.Write(logMessage);
            }

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(5)); // All except Trace
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(3)); // Info and above with correct tags
            Assert.That(target2.ReceivedMessages.Count, Is.EqualTo(2)); // Warning and above, excluding Spam

            // Verify specific messages
            var target1Tags = _target.ReceivedMessages.Select(m => m.Tag.ToString()).ToArray();
            var target2Tags = target2.ReceivedMessages.Select(m => m.Tag.ToString()).ToArray();

            Assert.That(target1Tags, Contains.Item("Important"));
            Assert.That(target1Tags, Does.Not.Contain("Spam"));
            Assert.That(target2Tags, Does.Not.Contain("Spam"));
        }
    }
} Arrange
            var level = LogLevel.Warning;
            var message = "Integration test message";
            var tag = "IntegrationTag";

            // Act - Log message
            _logger.Log(level, message, tag);

            // Create log message for target
            var logMessage = new LogMessage(level, message, tag);
            _target.Write(logMessage);

            // Format the message
            var formatted = _formatter.Format(logMessage);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1));
            Assert.That(formatted.ToString(), Does.Contain("Warning"));
            Assert.That(formatted.ToString(), Does.Contain(message));
            Assert.That(formatted.ToString(), Does.Contain(tag));
        }

        [Test]
        public void LoggingPipeline_WithFiltering_FiltersCorrectly()
        {
            // Arrange
            _logger.MinimumLevel = LogLevel.Warning;
            _target.MinimumLevel = LogLevel.Error;

            var debugMessage = "Debug message";
            var warningMessage = "Warning message";
            var errorMessage = "Error message";

            // Act
            _logger.Log(LogLevel.Debug, debugMessage, "Tag");    // Filtered by logger
            _logger.Log(LogLevel.Warning, warningMessage, "Tag"); // Logged but filtered by target
            _logger.Log(LogLevel.Error, errorMessage, "Tag");     // Logged and passed through

            // Simulate what would happen in real pipeline
            foreach (var entry in _logger.LogEntries)
            {
                var logMessage = new LogMessage(entry.Level, entry.Message, entry.Tag);
                _target.Write(logMessage);
            }

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(2)); // Debug filtered out
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1)); // Warning filtered out by target
            Assert.That(_target.ReceivedMessages[0].Message.ToString(), Is.EqualTo(errorMessage));
        }

        [Test]
        public void LoggingPipeline_WithStructuredLogging_HandlesProperties()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            _disposables.Add(properties);
            
            properties.Add("UserId", "12345");
            properties.Add("Action", "Login");
            properties.Add("Success", "true");

            var message = "User login attempt";
            var tag = "Authentication";

            // Act
            _logger.Log(LogLevel.Info, message, tag, properties);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
            var entry = _logger.LogEntries[0];
            Assert.That(entry.HasProperties, Is.True);
            Assert.That(entry.Message, Is.EqualTo(message));
            Assert.That(entry.Tag, Is.EqualTo(tag));
        }

        [Test]
        public void LoggingPipeline_BatchOperations_ProcessesEfficiently()
        {
            // Arrange
            var messageCount = 50;
            using var logMessages = new NativeList<LogMessage>(messageCount, Allocator.Temp);
            _disposables.Add(logMessages);

            // Generate test messages
            for (int i = 0; i < messageCount; i++)
            {
                var level = (LogLevel)((i % 5) * 10); // Cycle through levels
                var message = $"Batch message {i}";
                var tag = $"BatchTag{i % 3}"; // Cycle through 3 tags
                
                logMessages.Add(new LogMessage(level, message, tag));
                _logger.Log(level, message, tag);
            }

            // Act
            _target.WriteBatch(logMessages);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(messageCount));
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(messageCount));
            Assert.That(_target.WriteBatchCallCount, Is.EqualTo(1));
        }

        [Test]
        public void LoggingPipeline_TagFiltering_WorksAcrossComponents()
        {
            // Arrange
            _target.SetTagFilters(new[] { "AllowedTag" }, new[] { "BlockedTag" }, false);

            var messages = new[]
            {
                ("Allowed message", "AllowedTag"),
                ("Blocked message", "BlockedTag"),
                ("Untagged message", ""),
                ("Other message", "OtherTag")
            };

            // Act
            foreach (var (msg, tag) in messages)
            {
                _logger.Log(LogLevel.Info, msg, tag);
                var logMessage = new LogMessage(LogLevel.Info, msg, tag);
                _target.Write(logMessage);
            }

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(4)); // Logger doesn't filter by tag
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1)); // Only allowed tag passes
            Assert.That(_target.ReceivedMessages[0].Tag.ToString(), Is.EqualTo("AllowedTag"));
        }

        [Test]
        public void LoggingPipeline_PerformanceTest_HandlesHighVolume()
        {
            // Arrange
            var messageCount = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                var level = LogLevel.Info;
                var message = $"Performance test message {i}";
                var tag = "PerfTag";

                _logger.Log(level, message, tag);
                
                var logMessage = new LogMessage(level, message, tag);
                _target.Write(logMessage);
                _formatter.Format(logMessage);
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(messageCount));
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(messageCount));
            Assert.That(duration.TotalSeconds, Is.LessThan(1.0)); // Should complete within 1 second
        }

        [Test]
        public void LoggingPipeline_ErrorHandling_RemainsStable()
        {
            // Arrange - Create problematic scenarios
            var testCases = new[]
            {
                (LogLevel.Error, (string)null, "Tag"),
                (LogLevel.Warning, "Message", (string)null),
                (LogLevel.Info, "", ""),
                (LogLevel.Debug, new string('X', 2000), "LongMessageTag") // Very long message
            };

            // Act & Assert - Should not throw exceptions
            foreach (var (level, message, tag) in testCases)
            {
                Assert.DoesNotThrow(() =>
                {
                    _logger.Log(level, message, tag);
                    var logMessage = new LogMessage(level, message ?? "", tag ?? "");
                    _target.Write(logMessage);
                    _formatter.Format(logMessage);
                });
            }

            // Verify some messages were processed
            Assert.That(_logger.LogEntries.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoggingPipeline_MultipleTargets_DistributesCorrectly()
        {
            // Arrange
            using var target2 = new MockLogTargetTests.MockLogTarget("SecondTarget");
            _disposables.Add(target2);
            
            target2.MinimumLevel = LogLevel.Warning;
            _target.MinimumLevel = LogLevel.Debug;

            var messages = new[]
            {
                (LogLevel.Debug, "Debug message"),
                (LogLevel.Info, "Info message"),
                (LogLevel.Warning, "Warning message"),
                (LogLevel.Error, "Error message")
            };

            // Act
            foreach (var (level, msg) in messages)
            {
                _logger.Log(level, msg, "TestTag");
                var logMessage = new LogMessage(level, msg, "TestTag");
                
                _target.Write(logMessage);
                target2.Write(logMessage);
            }

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(4));
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(4)); // Debug level and above
            Assert.That(target2.ReceivedMessages.Count, Is.EqualTo(2)); // Warning level and above
        }

        [Test]
        public void LoggingPipeline_FormatterIntegration_ProducesConsistentOutput()
        {
            // Arrange
            _formatter.FormatTemplate = "[{Level}] {Tag}: {Message}";
            
            var testMessages = new[]
            {
                (LogLevel.Info, "Test message 1", "Tag1"),
                (LogLevel.Warning, "Test message 2", "Tag2"),
                (LogLevel.Error, "Test message 3", "Tag3")
            };

            // Act
            var formattedResults = new List<string>();
            foreach (var (level, message, tag) in testMessages)
            {
                var logMessage = new LogMessage(level, message, tag);
                var formatted = _formatter.Format(logMessage);
                formattedResults.Add(formatted.ToString());
            }

            //