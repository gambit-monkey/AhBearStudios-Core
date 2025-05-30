using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Data;
using System.Collections.Generic;
using System.Linq;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Mock implementation of IBurstLogger for testing
    /// </summary>
    public class MockBurstLogger : IBurstLogger
    {
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private LogLevel _minimumLevel = LogLevel.Trace;

        public struct LogEntry
        {
            public LogLevel Level;
            public string Message;
            public string Tag;
            public LogProperties Properties;
            public bool HasProperties;

            public LogEntry(LogLevel level, string message, string tag, LogProperties properties = default, bool hasProperties = false)
            {
                Level = level;
                Message = message;
                Tag = tag;
                Properties = properties;
                HasProperties = hasProperties;
            }
        }

        public IReadOnlyList<LogEntry> LogEntries => _logEntries;
        public LogLevel MinimumLevel 
        { 
            get => _minimumLevel; 
            set => _minimumLevel = value; 
        }

        public void Log(LogLevel level, string message, string tag)
        {
            if (IsEnabled(level))
            {
                _logEntries.Add(new LogEntry(level, message, tag));
            }
        }

        public void Log(LogLevel level, string message, string tag, LogProperties properties)
        {
            if (IsEnabled(level))
            {
                _logEntries.Add(new LogEntry(level, message, tag, properties, true));
            }
        }

        public bool IsEnabled(LogLevel level)
        {
            return level >= _minimumLevel;
        }

        public void Clear()
        {
            _logEntries.Clear();
        }

        public int GetLogCount(LogLevel level)
        {
            return _logEntries.Count(e => e.Level == level);
        }

        public LogEntry? GetLastLogEntry()
        {
            return _logEntries.LastOrDefault();
        }

        public LogEntry? GetFirstLogEntry()
        {
            return _logEntries.FirstOrDefault();
        }
    }

    /// <summary>
    /// Tests for IBurstLogger implementations
    /// </summary>
    [TestFixture]
    public class BurstLoggerTests
    {
        private MockBurstLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new MockBurstLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _logger?.Clear();
        }

        [Test]
        public void BurstLogger_Log_WithBasicMessage_LogsCorrectly()
        {
            // Arrange
            var level = LogLevel.Info;
            var message = "Test message";
            var tag = "TestTag";

            // Act
            _logger.Log(level, message, tag);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
            var entry = _logger.LogEntries[0];
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Message, Is.EqualTo(message));
            Assert.That(entry.Tag, Is.EqualTo(tag));
            Assert.That(entry.HasProperties, Is.False);
        }

        [Test]
        public void BurstLogger_Log_WithProperties_LogsCorrectly()
        {
            // Arrange
            var level = LogLevel.Warning;
            var message = "Test message with properties";
            var tag = "TestTag";
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key1", "Value1");
            properties.Add("Key2", "Value2");

            // Act
            _logger.Log(level, message, tag, properties);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
            var entry = _logger.LogEntries[0];
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Message, Is.EqualTo(message));
            Assert.That(entry.Tag, Is.EqualTo(tag));
            Assert.That(entry.HasProperties, Is.True);
        }

        [Test]
        public void BurstLogger_IsEnabled_ReturnsCorrectValues()
        {
            // Arrange
            _logger.MinimumLevel = LogLevel.Warning;

            // Act & Assert
            Assert.That(_logger.IsEnabled(LogLevel.Trace), Is.False);
            Assert.That(_logger.IsEnabled(LogLevel.Debug), Is.False);
            Assert.That(_logger.IsEnabled(LogLevel.Info), Is.False);
            Assert.That(_logger.IsEnabled(LogLevel.Warning), Is.True);
            Assert.That(_logger.IsEnabled(LogLevel.Error), Is.True);
            Assert.That(_logger.IsEnabled(LogLevel.Fatal), Is.True);
        }

        [Test]
        public void BurstLogger_Log_BelowMinimumLevel_DoesNotLog()
        {
            // Arrange
            _logger.MinimumLevel = LogLevel.Error;
            var level = LogLevel.Info;
            var message = "This should not be logged";
            var tag = "TestTag";

            // Act
            _logger.Log(level, message, tag);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void BurstLogger_Log_AtMinimumLevel_Logs()
        {
            // Arrange
            _logger.MinimumLevel = LogLevel.Warning;
            var level = LogLevel.Warning;
            var message = "This should be logged";
            var tag = "TestTag";

            // Act
            _logger.Log(level, message, tag);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
        }

        [Test]
        public void BurstLogger_Log_MultipleMessages_LogsInOrder()
        {
            // Arrange
            var messages = new[]
            {
                ("First message", LogLevel.Info, "Tag1"),
                ("Second message", LogLevel.Warning, "Tag2"),
                ("Third message", LogLevel.Error, "Tag3")
            };

            // Act
            foreach (var (message, level, tag) in messages)
            {
                _logger.Log(level, message, tag);
            }

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(3));
            for (int i = 0; i < messages.Length; i++)
            {
                var entry = _logger.LogEntries[i];
                var expected = messages[i];
                Assert.That(entry.Message, Is.EqualTo(expected.Item1));
                Assert.That(entry.Level, Is.EqualTo(expected.Item2));
                Assert.That(entry.Tag, Is.EqualTo(expected.Item3));
            }
        }

        [Test]
        public void BurstLogger_Log_WithNullMessage_HandlesGracefully()
        {
            // Arrange
            string nullMessage = null;
            var level = LogLevel.Error;
            var tag = "TestTag";

            // Act & Assert
            Assert.DoesNotThrow(() => _logger.Log(level, nullMessage, tag));
        }

        [Test]
        public void BurstLogger_Log_WithNullTag_HandlesGracefully()
        {
            // Arrange
            var message = "Test message";
            var level = LogLevel.Info;
            string nullTag = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _logger.Log(level, message, nullTag));
        }

        [Test]
        public void BurstLogger_Log_WithEmptyStrings_HandlesCorrectly()
        {
            // Arrange
            var emptyMessage = string.Empty;
            var emptyTag = string.Empty;
            var level = LogLevel.Debug;

            // Act
            _logger.Log(level, emptyMessage, emptyTag);

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
            var entry = _logger.LogEntries[0];
            Assert.That(entry.Message, Is.EqualTo(emptyMessage));
            Assert.That(entry.Tag, Is.EqualTo(emptyTag));
        }

        [Test]
        public void BurstLogger_Log_WithVeryLongMessage_HandlesCorrectly()
        {
            // Arrange
            var longMessage = new string('A', 1000);
            var level = LogLevel.Info;
            var tag = "TestTag";

            // Act & Assert
            Assert.DoesNotThrow(() => _logger.Log(level, longMessage, tag));
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(1));
        }

        [Test]
        public void BurstLogger_Log_AllLevels_LogsCorrectly()
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

            // Act
            foreach (var level in levels)
            {
                _logger.Log(level, $"Message for {level}", $"Tag{level}");
            }

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(levels.Length));
            for (int i = 0; i < levels.Length; i++)
            {
                Assert.That(_logger.LogEntries[i].Level, Is.EqualTo(levels[i]));
            }
        }

        [Test]
        public void BurstLogger_GetLogCount_ReturnsCorrectCounts()
        {
            // Arrange
            _logger.Log(LogLevel.Info, "Info 1", "Tag");
            _logger.Log(LogLevel.Info, "Info 2", "Tag");
            _logger.Log(LogLevel.Warning, "Warning 1", "Tag");
            _logger.Log(LogLevel.Error, "Error 1", "Tag");

            // Act & Assert
            Assert.That(_logger.GetLogCount(LogLevel.Info), Is.EqualTo(2));
            Assert.That(_logger.GetLogCount(LogLevel.Warning), Is.EqualTo(1));
            Assert.That(_logger.GetLogCount(LogLevel.Error), Is.EqualTo(1));
            Assert.That(_logger.GetLogCount(LogLevel.Debug), Is.EqualTo(0));
        }

        [Test]
        public void BurstLogger_Clear_RemovesAllEntries()
        {
            // Arrange
            _logger.Log(LogLevel.Info, "Message 1", "Tag");
            _logger.Log(LogLevel.Warning, "Message 2", "Tag");

            // Act
            _logger.Clear();

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void BurstLogger_GetLastLogEntry_ReturnsLastEntry()
        {
            // Arrange
            _logger.Log(LogLevel.Info, "First", "Tag");
            _logger.Log(LogLevel.Warning, "Last", "Tag");

            // Act
            var lastEntry = _logger.GetLastLogEntry();

            // Assert
            Assert.That(lastEntry.HasValue, Is.True);
            Assert.That(lastEntry.Value.Message, Is.EqualTo("Last"));
            Assert.That(lastEntry.Value.Level, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void BurstLogger_GetFirstLogEntry_ReturnsFirstEntry()
        {
            // Arrange
            _logger.Log(LogLevel.Info, "First", "Tag");
            _logger.Log(LogLevel.Warning, "Second", "Tag");

            // Act
            var firstEntry = _logger.GetFirstLogEntry();

            // Assert
            Assert.That(firstEntry.HasValue, Is.True);
            Assert.That(firstEntry.Value.Message, Is.EqualTo("First"));
            Assert.That(firstEntry.Value.Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void BurstLogger_GetLastLogEntry_WhenEmpty_ReturnsNull()
        {
            // Act
            var lastEntry = _logger.GetLastLogEntry();

            // Assert
            Assert.That(lastEntry.HasValue, Is.False);
        }

        [Test]
        public void BurstLogger_MinimumLevel_CanBeChanged()
        {
            // Arrange
            _logger.MinimumLevel = LogLevel.Debug;
            _logger.Log(LogLevel.Trace, "Should not log", "Tag");
            _logger.Log(LogLevel.Debug, "Should log", "Tag");

            // Act
            _logger.MinimumLevel = LogLevel.Warning;
            _logger.Log(LogLevel.Info, "Should not log after change", "Tag");
            _logger.Log(LogLevel.Warning, "Should log after change", "Tag");

            // Assert
            Assert.That(_logger.LogEntries.Count, Is.EqualTo(2));
            Assert.That(_logger.LogEntries[0].Level, Is.EqualTo(LogLevel.Debug));
            Assert.That(_logger.LogEntries[1].Level, Is.EqualTo(LogLevel.Warning));
        }
    }
}