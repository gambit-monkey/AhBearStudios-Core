using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Messages;
using System;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Mock implementation of ILogFormatter for testing
    /// </summary>
    public class MockLogFormatter : ILogFormatter
    {
        public bool SupportsStructuredLogging { get; set; } = false;
        public string FormatTemplate { get; set; } = "{Level}: {Message} [{Tag}]";
        public bool IncludeTimestamp { get; set; } = false;
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        public FixedString512Bytes Format(LogMessage message)
        {
            var result = new FixedString512Bytes();
            
            if (IncludeTimestamp)
            {
                var timestamp = new DateTime(message.TimestampTicks);
                result.Append(timestamp.ToString(TimestampFormat));
                result.Append(" ");
            }

            // Simple template replacement
            var formatted = FormatTemplate
                .Replace("{Level}", message.Level.ToString())
                .Replace("{Message}", message.Message.ToString())
                .Replace("{Tag}", message.Tag.ToString());

            result.Append(formatted);
            return result;
        }
    }

    /// <summary>
    /// Structured log formatter that supports properties
    /// </summary>
    public class StructuredLogFormatter : ILogFormatter
    {
        public bool SupportsStructuredLogging => true;

        public FixedString512Bytes Format(LogMessage message)
        {
            var result = new FixedString512Bytes();
            
            // Basic format: Level: Message [Tag] {Properties}
            result.Append(message.Level.ToString());
            result.Append(": ");
            result.Append(message.Message);
            
            if (!message.Tag.IsEmpty)
            {
                result.Append(" [");
                result.Append(message.Tag);
                result.Append("]");
            }

            // Note: In a real implementation, we'd format properties here
            // For this test, we'll just indicate they exist
            result.Append(" {structured}");

            return result;
        }
    }

    /// <summary>
    /// Tests for ILogFormatter implementations
    /// </summary>
    [TestFixture]
    public class LogFormatterTests
    {
        private MockLogFormatter _formatter;

        [SetUp]
        public void SetUp()
        {
            _formatter = new MockLogFormatter();
        }

        [Test]
        public void LogFormatter_Format_BasicMessage_ReturnsFormattedString()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Info, "Test message", "TestTag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.Contain("Info"));
            Assert.That(resultString, Does.Contain("Test message"));
            Assert.That(resultString, Does.Contain("TestTag"));
        }

        [Test]
        public void LogFormatter_Format_WithCustomTemplate_UsesTemplate()
        {
            // Arrange
            _formatter.FormatTemplate = "[{Level}] {Tag}: {Message}";
            var message = new LogMessage(LogLevel.Error, "Error occurred", "ErrorTag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Is.EqualTo("[Error] ErrorTag: Error occurred"));
        }

        [Test]
        public void LogFormatter_Format_WithTimestamp_IncludesTimestamp()
        {
            // Arrange
            _formatter.IncludeTimestamp = true;
            _formatter.TimestampFormat = "HH:mm:ss";
            var timestamp = new DateTime(2023, 10, 15, 14, 30, 45).Ticks;
            var message = new LogMessage(LogLevel.Warning, "Timestamped message", "Tag", timestamp);

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.StartWith("14:30:45"));
            Assert.That(resultString, Does.Contain("Warning"));
            Assert.That(resultString, Does.Contain("Timestamped message"));
        }

        [Test]
        public void LogFormatter_Format_WithEmptyMessage_HandlesGracefully()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Debug, "", "Tag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.Contain("Debug"));
            Assert.That(resultString, Does.Contain("Tag"));
        }

        [Test]
        public void LogFormatter_Format_WithEmptyTag_HandlesGracefully()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Trace, "Message without tag", "");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.Contain("Trace"));
            Assert.That(resultString, Does.Contain("Message without tag"));
        }

        [Test]
        public void LogFormatter_Format_AllLogLevels_FormatsCorrectly()
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

            // Act & Assert
            foreach (var level in levels)
            {
                var message = new LogMessage(level, $"Message for {level}", "Tag");
                var result = _formatter.Format(message);
                var resultString = result.ToString();
                
                Assert.That(resultString, Does.Contain(level.ToString()));
                Assert.That(resultString, Does.Contain($"Message for {level}"));
            }
        }

        [Test]
        public void LogFormatter_Format_VeryLongMessage_TruncatesAppropriately()
        {
            // Arrange
            var longMessage = new string('A', 1000);
            var message = new LogMessage(LogLevel.Info, longMessage, "Tag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            Assert.That(result.Length, Is.LessThanOrEqualTo(512)); // FixedString512Bytes limit
        }

        [Test]
        public void LogFormatter_SupportsStructuredLogging_DefaultIsFalse()
        {
            // Assert
            Assert.That(_formatter.SupportsStructuredLogging, Is.False);
        }

        [Test]
        public void LogFormatter_SupportsStructuredLogging_CanBeSet()
        {
            // Act
            _formatter.SupportsStructuredLogging = true;

            // Assert
            Assert.That(_formatter.SupportsStructuredLogging, Is.True);
        }

        [Test]
        public void StructuredLogFormatter_SupportsStructuredLogging_ReturnsTrue()
        {
            // Arrange
            var structuredFormatter = new StructuredLogFormatter();

            // Assert
            Assert.That(structuredFormatter.SupportsStructuredLogging, Is.True);
        }

        [Test]
        public void StructuredLogFormatter_Format_IncludesStructuredIndicator()
        {
            // Arrange
            var structuredFormatter = new StructuredLogFormatter();
            var message = new LogMessage(LogLevel.Info, "Structured message", "StructTag");

            // Act
            var result = structuredFormatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.Contain("structured"));
            Assert.That(resultString, Does.Contain("Info"));
            Assert.That(resultString, Does.Contain("Structured message"));
            Assert.That(resultString, Does.Contain("StructTag"));
        }

        [Test]
        public void LogFormatter_Format_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Error, "Message with special chars: \n\t\r", "Tag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.Contain("Error"));
            Assert.That(resultString, Does.Contain("special chars"));
        }

        [Test]
        public void LogFormatter_Format_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Info, "Unicode: 你好世界 🚀", "UnicodeTag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.Contain("Info"));
            Assert.That(resultString, Does.Contain("Unicode"));
        }

        [Test]
        public void LogFormatter_Format_MultipleTemplateReplacements_WorksCorrectly()
        {
            // Arrange
            _formatter.FormatTemplate = "{Level}|{Message}|{Tag}|{Level}"; // Level appears twice
            var message = new LogMessage(LogLevel.Fatal, "Fatal error", "FatalTag");

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Is.EqualTo("Fatal|Fatal error|FatalTag|Fatal"));
        }

        [Test]
        public void LogFormatter_Format_WithZeroTimestamp_HandlesCorrectly()
        {
            // Arrange
            _formatter.IncludeTimestamp = true;
            var message = new LogMessage(LogLevel.Info, "Zero timestamp", "Tag", 0);

            // Act
            var result = _formatter.Format(message);

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Does.StartWith("0001-01-01")); // DateTime.MinValue
        }

        [Test]
        public void LogFormatter_Format_ConsistentOutput_ForSameInput()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Debug, "Consistent test", "Tag");

            // Act
            var result1 = _formatter.Format(message);
            var result2 = _formatter.Format(message);

            // Assert
            Assert.That(result1.ToString(), Is.EqualTo(result2.ToString()));
        }

        [Test]
        public void LogFormatter_Format_Performance_HandlesQuicklyInLoop()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Info, "Performance test", "PerfTag");
            var iterations = 1000;

            // Act & Assert - Should complete without timeout
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    _formatter.Format(message);
                }
            });
        }
    }
}