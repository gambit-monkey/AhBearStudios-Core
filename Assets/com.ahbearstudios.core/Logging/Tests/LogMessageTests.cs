using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging;
using System;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Tests for LogMessage struct functionality
    /// </summary>
    [TestFixture]
    public class LogMessageTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state for each test
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any allocated containers
        }

        [Test]
        public void LogMessage_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var level = LogLevel.Info;
            var message = "Test message";
            var tag = "TestTag";
            var timestamp = DateTime.UtcNow.Ticks;

            // Act
            var logMessage = new LogMessage(level, message, tag, timestamp);

            // Assert
            Assert.That(logMessage.Level, Is.EqualTo(level));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.Tag.ToString(), Is.EqualTo(tag));
            Assert.That(logMessage.TimestampTicks, Is.EqualTo(timestamp));
        }

        [Test]
        public void LogMessage_WithLongMessage_TruncatesCorrectly()
        {
            // Arrange
            var longMessage = new string('A', 1000); // Longer than FixedString512Bytes
            var level = LogLevel.Debug;
            var tag = "TestTag";

            // Act
            var logMessage = new LogMessage(level, longMessage, tag);

            // Assert
            Assert.That(logMessage.Message.ToString().Length, Is.LessThanOrEqualTo(511)); // FixedString512Bytes capacity
            Assert.That(logMessage.Message.ToString(), Does.StartWith("AAA"));
        }

        [Test]
        public void LogMessage_WithLongTag_TruncatesCorrectly()
        {
            // Arrange
            var message = "Test message";
            var longTag = new string('T', 200); // Longer than FixedString128Bytes
            var level = LogLevel.Warning;

            // Act
            var logMessage = new LogMessage(level, message, longTag);

            // Assert
            Assert.That(logMessage.Tag.ToString().Length, Is.LessThanOrEqualTo(127)); // FixedString128Bytes capacity
            Assert.That(logMessage.Tag.ToString(), Does.StartWith("TTT"));
        }

        [Test]
        public void LogMessage_DefaultConstructor_HasValidDefaults()
        {
            // Act
            var logMessage = default(LogMessage);

            // Assert
            Assert.That(logMessage.Level, Is.EqualTo(default(LogLevel)));
            Assert.That(logMessage.Message.ToString(), Is.Empty);
            Assert.That(logMessage.Tag.ToString(), Is.Empty);
            Assert.That(logMessage.TimestampTicks, Is.EqualTo(0));
        }

        [Test]
        public void LogMessage_Equality_WorksCorrectly()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.Ticks;
            var message1 = new LogMessage(LogLevel.Error, "Message", "Tag", timestamp);
            var message2 = new LogMessage(LogLevel.Error, "Message", "Tag", timestamp);
            var message3 = new LogMessage(LogLevel.Info, "Message", "Tag", timestamp);

            // Act & Assert
            Assert.That(message1.Equals(message2), Is.True);
            Assert.That(message1.Equals(message3), Is.False);
            Assert.That(message1 == message2, Is.True);
            Assert.That(message1 != message3, Is.True);
        }

        [Test]
        public void LogMessage_GetHashCode_IsConsistent()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Fatal, "Test", "Tag");

            // Act
            var hash1 = message.GetHashCode();
            var hash2 = message.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void LogMessage_ToString_ReturnsFormattedString()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Info, "Test message", "TestTag");

            // Act
            var result = message.ToString();

            // Assert
            Assert.That(result, Does.Contain("Info"));
            Assert.That(result, Does.Contain("Test message"));
            Assert.That(result, Does.Contain("TestTag"));
        }

        [Test]
        public void LogMessage_WithNullString_HandlesGracefully()
        {
            // Arrange
            string nullMessage = null;
            string nullTag = null;

            // Act & Assert
            Assert.DoesNotThrow(() => new LogMessage(LogLevel.Error, nullMessage, nullTag));
        }

        [Test]
        public void LogMessage_WithEmptyStrings_HandlesCorrectly()
        {
            // Arrange
            var emptyMessage = string.Empty;
            var emptyTag = string.Empty;

            // Act
            var logMessage = new LogMessage(LogLevel.Debug, emptyMessage, emptyTag);

            // Assert
            Assert.That(logMessage.Message.ToString(), Is.Empty);
            Assert.That(logMessage.Tag.ToString(), Is.Empty);
        }

        [Test]
        public void LogMessage_MemoryLayout_IsOptimal()
        {
            // Act
            var size = System.Runtime.InteropServices.Marshal.SizeOf<LogMessage>();

            // Assert - Should be reasonably sized for efficient copying
            Assert.That(size, Is.LessThan(1024)); // Should be less than 1KB
        }
    }
}