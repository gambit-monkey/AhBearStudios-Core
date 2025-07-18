using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Unity.Collections;
using NUnit.Framework;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Tests.Utilities;
using AhBearStudios.Core.Tests.Shared;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Comprehensive tests for logging formatters including JsonFormatter, PlainTextFormatter,
    /// and Unity-specific formatters. Tests structured output, performance optimization,
    /// and configuration management.
    /// </summary>
    [TestFixture]
    public class FormatterTests
    {
        private MockProfilerService _mockProfiler;
        private FormatterConfig _defaultConfig;

        [SetUp]
        public void SetUp()
        {
            _mockProfiler = new MockProfilerService();
            _defaultConfig = new FormatterConfig
            {
                Name = "TestFormatter",
                FormatType = LogFormat.Json,
                Template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message}",
                IncludeTimestamp = true,
                IncludeLevel = true,
                IncludeMessage = true,
                IncludeSource = true,
                IncludeCorrelationId = true,
                IncludeProperties = true,
                IncludeException = true,
                Properties = new Dictionary<string, object>()
            };
        }

        [TearDown]
        public void TearDown()
        {
            _mockProfiler?.Dispose();
            TestUtilities.CleanupTempFiles();
        }

        #region JsonFormatter Tests

        [Test]
        public void JsonFormatter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.Json;

            // Act
            using (var formatter = new JsonFormatter(config, _mockProfiler))
            {
                // Assert
                Assert.That(formatter.Name, Is.EqualTo("TestFormatter"));
                Assert.That(formatter.FormatType, Is.EqualTo(LogFormat.Json));
            }
        }

        [Test]
        public void JsonFormatter_Format_WithSimpleMessage_ReturnsValidJson()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Simple test message");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.GetProperty("Level").GetString(), Is.EqualTo("Info"));
            Assert.That(root.GetProperty("Message").GetString(), Is.EqualTo("Simple test message"));
            Assert.That(root.GetProperty("Channel").GetString(), Is.EqualTo("Test"));
        }

        [Test]
        public void JsonFormatter_Format_WithException_IncludesExceptionData()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var exception = TestDataFactory.CreateTestException("Test exception message");
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error with exception",
                "test-correlation", "TestClass", exception);

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.GetProperty("Level").GetString(), Is.EqualTo("Error"));
            Assert.That(root.GetProperty("Exception").GetString(), Contains.Substring("Test exception message"));
            Assert.That(root.GetProperty("ExceptionType").GetString(), Contains.Substring("InvalidOperationException"));
        }

        [Test]
        public void JsonFormatter_Format_WithProperties_IncludesStructuredData()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var properties = TestDataFactory.CreateTestProperties("test-user", "test-session", 12345);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Message with properties",
                "test-correlation", "TestClass", properties: properties);

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.GetProperty("Properties").GetProperty("UserId").GetString(), Is.EqualTo("test-user"));
            Assert.That(root.GetProperty("Properties").GetProperty("SessionId").GetString(), Is.EqualTo("test-session"));
            Assert.That(root.GetProperty("Properties").GetProperty("RequestId").GetInt32(), Is.EqualTo(12345));
        }

        [Test]
        public void JsonFormatter_Format_WithCorrelationId_IncludesCorrelationData()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var correlationId = "test-correlation-id-123";
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Test", "Message with correlation",
                correlationId, "TestClass");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.GetProperty("CorrelationId").GetString(), Is.EqualTo(correlationId));
        }

        [Test]
        public void JsonFormatter_Format_WithConfiguredFields_IncludesOnlySelectedFields()
        {
            // Arrange
            var config = _defaultConfig;
            config.IncludeTimestamp = false;
            config.IncludeSource = false;
            config.IncludeProperties = false;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var properties = TestDataFactory.CreateTestProperties();
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Selective fields message",
                "test-correlation", "TestClass", properties: properties);

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.TryGetProperty("Timestamp", out _), Is.False);
            Assert.That(root.TryGetProperty("Source", out _), Is.False);
            Assert.That(root.TryGetProperty("Properties", out _), Is.False);
            Assert.That(root.TryGetProperty("Level", out _), Is.True);
            Assert.That(root.TryGetProperty("Message", out _), Is.True);
        }

        [Test]
        public void JsonFormatter_FormatBatch_WithMultipleMessages_ReturnsJsonArray()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var messages = TestDataFactory.CreateLogMessageBatch(3, LogLevel.Info);

            // Act
            var result = formatter.FormatBatch(messages);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(root.GetArrayLength(), Is.EqualTo(3));
        }

        [Test]
        public void JsonFormatter_Performance_FormattingSpeed()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var messages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var message in messages)
                {
                    var result = formatter.Format(message);
                    Assert.That(result, Is.Not.Null.And.Not.Empty);
                }
            }, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void JsonFormatter_Performance_FrameBudgetCompliance()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Performance test message");

            // Act & Assert
            TestUtilities.AssertFrameBudget(() =>
            {
                var result = formatter.Format(message);
                Assert.That(result, Is.Not.Null.And.Not.Empty);
            }, 0.1); // 0.1ms frame budget for formatting
        }

        [Test]
        public void JsonFormatter_ProfilerIntegration_RecordsMetrics()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Profiler integration test");

            // Act
            var result = formatter.Format(message);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            var metrics = _mockProfiler.GetMetrics();
            Assert.That(metrics.Count, Is.GreaterThan(0));
        }

        [Test]
        public void JsonFormatter_Memory_LimitedAllocation()
        {
            // Arrange
            var config = _defaultConfig;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var messages = TestDataFactory.CreateLogMessageBatch(50, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                foreach (var message in messages)
                {
                    var result = formatter.Format(message);
                    Assert.That(result, Is.Not.Null.And.Not.Empty);
                }
            }, 1024 * 1024); // 1MB limit
        }

        #endregion

        #region PlainTextFormatter Tests

        [Test]
        public void PlainTextFormatter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;

            // Act
            using (var formatter = new PlainTextFormatter(config))
            {
                // Assert
                Assert.That(formatter.Name, Is.EqualTo("TestFormatter"));
                Assert.That(formatter.FormatType, Is.EqualTo(LogFormat.PlainText));
            }
        }

        [Test]
        public void PlainTextFormatter_Format_WithSimpleMessage_ReturnsFormattedText()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}";
            var formatter = new PlainTextFormatter(config);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Simple test message");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("[INF]"));
            Assert.That(result, Contains.Substring("Simple test message"));
            Assert.That(result, Matches(@"\d{2}:\d{2}:\d{2}")); // HH:mm:ss timestamp format
        }

        [Test]
        public void PlainTextFormatter_Format_WithException_IncludesExceptionText()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message} {Exception}";
            var formatter = new PlainTextFormatter(config);
            var exception = TestDataFactory.CreateTestException("Test exception message");
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error with exception",
                "test-correlation", "TestClass", exception);

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("[ERR]"));
            Assert.That(result, Contains.Substring("Error with exception"));
            Assert.That(result, Contains.Substring("Test exception message"));
            Assert.That(result, Contains.Substring("InvalidOperationException"));
        }

        [Test]
        public void PlainTextFormatter_Format_WithProperties_IncludesPropertyText()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message} {Properties}";
            var formatter = new PlainTextFormatter(config);
            var properties = TestDataFactory.CreateTestProperties("test-user", "test-session", 12345);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Message with properties",
                "test-correlation", "TestClass", properties: properties);

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("test-user"));
            Assert.That(result, Contains.Substring("test-session"));
            Assert.That(result, Contains.Substring("12345"));
        }

        [Test]
        public void PlainTextFormatter_Format_WithCustomTemplate_UsesTemplate()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{Level} | {Channel} | {Message} | {CorrelationId}";
            var formatter = new PlainTextFormatter(config);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "CustomChannel", "Custom message",
                "custom-correlation", "CustomClass");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("Warning"));
            Assert.That(result, Contains.Substring("CustomChannel"));
            Assert.That(result, Contains.Substring("Custom message"));
            Assert.That(result, Contains.Substring("custom-correlation"));
            Assert.That(result, Contains.Substring("|")); // Template separator
        }

        [Test]
        public void PlainTextFormatter_FormatBatch_WithMultipleMessages_ReturnsMultilineText()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{Level}: {Message}";
            var formatter = new PlainTextFormatter(config);
            var messages = TestDataFactory.CreateLogMessageBatch(3, LogLevel.Info);

            // Act
            var result = formatter.FormatBatch(messages);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines.Length, Is.EqualTo(3));
            foreach (var line in lines)
            {
                Assert.That(line, Contains.Substring("Info:"));
            }
        }

        [Test]
        public void PlainTextFormatter_Performance_FormattingSpeed()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            var formatter = new PlainTextFormatter(config);
            var messages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var message in messages)
                {
                    var result = formatter.Format(message);
                    Assert.That(result, Is.Not.Null.And.Not.Empty);
                }
            }, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PlainTextFormatter_Performance_FrameBudgetCompliance()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            var formatter = new PlainTextFormatter(config);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Performance test message");

            // Act & Assert
            TestUtilities.AssertFrameBudget(() =>
            {
                var result = formatter.Format(message);
                Assert.That(result, Is.Not.Null.And.Not.Empty);
            }, 0.05); // 0.05ms frame budget for plain text formatting (faster than JSON)
        }

        #endregion

        #region StructuredFormatter Tests

        [Test]
        public void StructuredFormatter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.Structured;

            // Act
            using (var formatter = new StructuredFormatter(config))
            {
                // Assert
                Assert.That(formatter.Name, Is.EqualTo("TestFormatter"));
                Assert.That(formatter.FormatType, Is.EqualTo(LogFormat.Structured));
            }
        }

        [Test]
        public void StructuredFormatter_Format_WithSimpleMessage_ReturnsStructuredFormat()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.Structured;
            var formatter = new StructuredFormatter(config);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Structured test message");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("Level=Info"));
            Assert.That(result, Contains.Substring("Message=\"Structured test message\""));
            Assert.That(result, Contains.Substring("Channel=Test"));
        }

        [Test]
        public void StructuredFormatter_Format_WithProperties_IncludesAllProperties()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.Structured;
            var formatter = new StructuredFormatter(config);
            var properties = TestDataFactory.CreateTestProperties("test-user", "test-session", 12345);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Message with properties",
                "test-correlation", "TestClass", properties: properties);

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("UserId=test-user"));
            Assert.That(result, Contains.Substring("SessionId=test-session"));
            Assert.That(result, Contains.Substring("RequestId=12345"));
        }

        [Test]
        public void StructuredFormatter_Performance_FrameBudgetCompliance()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.Structured;
            var formatter = new StructuredFormatter(config);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Performance test message");

            // Act & Assert
            TestUtilities.AssertFrameBudget(() =>
            {
                var result = formatter.Format(message);
                Assert.That(result, Is.Not.Null.And.Not.Empty);
            }, 0.1); // 0.1ms frame budget for structured formatting
        }

        #endregion

        #region Unity-Specific Formatter Tests

        [Test]
        public void UnityJsonFormatter_WithUnityContext_IncludesUnitySpecificData()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.Json;
            config.Properties["IncludeUnityContext"] = true;
            var formatter = new JsonFormatter(config, _mockProfiler);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Unity", "Unity-specific message");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(IsValidJson(result), Is.True);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            Assert.That(root.GetProperty("Channel").GetString(), Is.EqualTo("Unity"));
            Assert.That(root.GetProperty("Message").GetString(), Is.EqualTo("Unity-specific message"));
        }

        [Test]
        public void UnityPlainTextFormatter_WithUnityTemplate_FormatsForUnityConsole()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "[{Level}] {Message} (Frame: {Frame})";
            config.Properties["IncludeFrameCount"] = true;
            var formatter = new PlainTextFormatter(config);
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Unity", "Unity console message");

            // Act
            var result = formatter.Format(logMessage);

            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Contains.Substring("[Warning]"));
            Assert.That(result, Contains.Substring("Unity console message"));
            Assert.That(result, Contains.Substring("Frame:"));
        }

        [Test]
        public void UnityFormatters_Performance_OptimizedForGameLoop()
        {
            // Arrange
            var jsonConfig = _defaultConfig;
            jsonConfig.FormatType = LogFormat.Json;
            var plainConfig = _defaultConfig;
            plainConfig.FormatType = LogFormat.PlainText;
            
            var jsonFormatter = new JsonFormatter(jsonConfig, _mockProfiler);
            var plainFormatter = new PlainTextFormatter(plainConfig);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Unity", "Game loop message");

            // Act & Assert - Test that both formatters can handle game loop frequency
            TestUtilities.SimulateUnityFrame(() =>
            {
                var jsonResult = jsonFormatter.Format(message);
                var plainResult = plainFormatter.Format(message);
                
                Assert.That(jsonResult, Is.Not.Null.And.Not.Empty);
                Assert.That(plainResult, Is.Not.Null.And.Not.Empty);
            }, 60); // 60 frames at 60 FPS
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Formatters_Configuration_RuntimeReconfiguration()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{Level}: {Message}";
            var formatter = new PlainTextFormatter(config);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Reconfiguration test");

            // Act
            var result1 = formatter.Format(message);
            
            // Reconfigure
            config.Template = "{Timestamp:HH:mm:ss} {Level} - {Message}";
            formatter.UpdateConfiguration(config);
            var result2 = formatter.Format(message);

            // Assert
            Assert.That(result1, Is.Not.Null.And.Not.Empty);
            Assert.That(result2, Is.Not.Null.And.Not.Empty);
            Assert.That(result1, Is.Not.EqualTo(result2));
            Assert.That(result2, Matches(@"\d{2}:\d{2}:\d{2}")); // Should have timestamp
        }

        [Test]
        public void Formatters_Configuration_InvalidConfiguration_HandlesGracefully()
        {
            // Arrange
            var config = _defaultConfig;
            config.FormatType = LogFormat.PlainText;
            config.Template = "{InvalidField} {Level}: {Message}";
            var formatter = new PlainTextFormatter(config);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Invalid configSo test");

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = formatter.Format(message);
                Assert.That(result, Is.Not.Null.And.Not.Empty);
            });
        }

        #endregion

        #region Memory and Performance Tests

        [Test]
        public void AllFormatters_Memory_EfficientAllocation()
        {
            // Arrange
            var jsonConfig = _defaultConfig;
            jsonConfig.FormatType = LogFormat.Json;
            var plainConfig = _defaultConfig;
            plainConfig.FormatType = LogFormat.PlainText;
            var structuredConfig = _defaultConfig;
            structuredConfig.FormatType = LogFormat.Structured;

            var jsonFormatter = new JsonFormatter(jsonConfig, _mockProfiler);
            var plainFormatter = new PlainTextFormatter(plainConfig);
            var structuredFormatter = new StructuredFormatter(structuredConfig);
            var messages = TestDataFactory.CreateLogMessageBatch(50, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                foreach (var message in messages)
                {
                    var jsonResult = jsonFormatter.Format(message);
                    var plainResult = plainFormatter.Format(message);
                    var structuredResult = structuredFormatter.Format(message);
                    
                    Assert.That(jsonResult, Is.Not.Null.And.Not.Empty);
                    Assert.That(plainResult, Is.Not.Null.And.Not.Empty);
                    Assert.That(structuredResult, Is.Not.Null.And.Not.Empty);
                }
            }, 2 * 1024 * 1024); // 2MB limit for all formatters
        }

        [Test]
        public void AllFormatters_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var jsonConfig = _defaultConfig;
            jsonConfig.FormatType = LogFormat.Json;
            var plainConfig = _defaultConfig;
            plainConfig.FormatType = LogFormat.PlainText;

            var jsonFormatter = new JsonFormatter(jsonConfig, _mockProfiler);
            var plainFormatter = new PlainTextFormatter(plainConfig);
            var messageCount = 100;

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < messageCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", $"Concurrent message {index}");
                    var jsonResult = jsonFormatter.Format(message);
                    var plainResult = plainFormatter.Format(message);
                    
                    Assert.That(jsonResult, Is.Not.Null.And.Not.Empty);
                    Assert.That(plainResult, Is.Not.Null.And.Not.Empty);
                }));
            }

            // Assert
            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
        }

        #endregion

        #region Helper Methods

        private static bool IsValidJson(string jsonString)
        {
            try
            {
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        #endregion
    }
}