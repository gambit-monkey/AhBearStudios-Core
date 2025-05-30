using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Tags;
using System.Collections.Generic;
using System.Linq;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Mock log target for testing purposes
    /// </summary>
    public class MockLogTarget : ILogTarget
    {
        private readonly List<LogMessage> _receivedMessages = new List<LogMessage>();
        private readonly HashSet<Tagging.TagCategory> _tagFilters = new HashSet<Tagging.TagCategory>();
        private string[] _includedTags;
        private string[] _excludedTags;
        private bool _processUntaggedMessages = true;

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
        public bool IsEnabled { get; set; } = true;
        public IReadOnlyList<LogMessage> ReceivedMessages => _receivedMessages;
        public int WriteCallCount { get; private set; }
        public int WriteBatchCallCount { get; private set; }
        public int FlushCallCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public MockLogTarget(string name = "MockTarget")
        {
            Name = name;
        }

        public void Write(in LogMessage entry)
        {
            if (!ShouldProcess(entry))
                return;

            WriteCallCount++;
            _receivedMessages.Add(entry);
        }

        public void WriteBatch(NativeList<LogMessage> entries)
        {
            WriteBatchCallCount++;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (ShouldProcess(entry))
                {
                    _receivedMessages.Add(entry);
                }
            }
        }

        public void Flush()
        {
            FlushCallCount++;
        }

        public bool IsLevelEnabled(LogLevel level)
        {
            return IsEnabled && level >= MinimumLevel;
        }

        public void AddTagFilter(Tagging.TagCategory tagCategory)
        {
            _tagFilters.Add(tagCategory);
        }

        public void RemoveTagFilter(Tagging.TagCategory tagCategory)
        {
            _tagFilters.Remove(tagCategory);
        }

        public void ClearTagFilters()
        {
            _tagFilters.Clear();
            _includedTags = null;
            _excludedTags = null;
            _processUntaggedMessages = true;
        }

        public void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages)
        {
            _includedTags = includedTags;
            _excludedTags = excludedTags;
            _processUntaggedMessages = processUntaggedMessages;
        }

        public void Dispose()
        {
            IsDisposed = true;
            _receivedMessages.Clear();
        }

        private bool ShouldProcess(LogMessage message)
        {
            if (!IsEnabled || message.Level < MinimumLevel)
                return false;

            var tag = message.Tag.ToString();
            
            // Check excluded tags first
            if (_excludedTags != null && _excludedTags.Contains(tag))
                return false;

            // Check if message has no tag
            if (string.IsNullOrEmpty(tag))
                return _processUntaggedMessages;

            // Check included tags
            if (_includedTags != null && _includedTags.Length > 0)
                return _includedTags.Contains(tag);

            return true;
        }

        public void Reset()
        {
            _receivedMessages.Clear();
            WriteCallCount = 0;
            WriteBatchCallCount = 0;
            FlushCallCount = 0;
        }
    }

    /// <summary>
    /// Tests for mock log target functionality
    /// </summary>
    [TestFixture]
    public class MockLogTargetTests
    {
        private MockLogTarget _target;

        [SetUp]
        public void SetUp()
        {
            _target = new MockLogTarget("TestTarget");
        }

        [TearDown]
        public void TearDown()
        {
            _target?.Dispose();
        }

        [Test]
        public void MockLogTarget_Constructor_SetsNameCorrectly()
        {
            // Arrange
            var targetName = "CustomTarget";

            // Act
            using var target = new MockLogTarget(targetName);

            // Assert
            Assert.That(target.Name, Is.EqualTo(targetName));
        }

        [Test]
        public void MockLogTarget_DefaultState_IsCorrect()
        {
            // Assert
            Assert.That(_target.IsEnabled, Is.True);
            Assert.That(_target.MinimumLevel, Is.EqualTo(LogLevel.Trace));
            Assert.That(_target.ReceivedMessages, Is.Empty);
            Assert.That(_target.IsDisposed, Is.False);
        }

        [Test]
        public void Write_WithValidMessage_AddsToReceivedMessages()
        {
            // Arrange
            var message = new LogMessage(LogLevel.Info, "Test message", "TestTag");

            // Act
            _target.Write(message);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1));
            Assert.That(_target.ReceivedMessages[0], Is.EqualTo(message));
            Assert.That(_target.WriteCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Write_WithLevelBelowMinimum_DoesNotAdd()
        {
            // Arrange
            _target.MinimumLevel = LogLevel.Warning;
            var message = new LogMessage(LogLevel.Info, "Test message", "TestTag");

            // Act
            _target.Write(message);

            // Assert
            Assert.That(_target.ReceivedMessages, Is.Empty);
            Assert.That(_target.WriteCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Write_WhenDisabled_DoesNotAdd()
        {
            // Arrange
            _target.IsEnabled = false;
            var message = new LogMessage(LogLevel.Error, "Test message", "TestTag");

            // Act
            _target.Write(message);

            // Assert
            Assert.That(_target.ReceivedMessages, Is.Empty);
            Assert.That(_target.WriteCallCount, Is.EqualTo(0));
        }

        [Test]
        public void WriteBatch_WithValidMessages_AddsAllToReceivedMessages()
        {
            // Arrange
            using var messages = new NativeList<LogMessage>(Allocator.Temp);
            messages.Add(new LogMessage(LogLevel.Info, "Message 1", "Tag1"));
            messages.Add(new LogMessage(LogLevel.Warning, "Message 2", "Tag2"));
            messages.Add(new LogMessage(LogLevel.Error, "Message 3", "Tag3"));

            // Act
            _target.WriteBatch(messages);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(3));
            Assert.That(_target.WriteBatchCallCount, Is.EqualTo(1));
        }

        [Test]
        public void WriteBatch_WithMixedLevels_FiltersCorrectly()
        {
            // Arrange
            _target.MinimumLevel = LogLevel.Warning;
            using var messages = new NativeList<LogMessage>(Allocator.Temp);
            messages.Add(new LogMessage(LogLevel.Debug, "Message 1", "Tag1"));    // Below minimum
            messages.Add(new LogMessage(LogLevel.Warning, "Message 2", "Tag2"));  // At minimum
            messages.Add(new LogMessage(LogLevel.Error, "Message 3", "Tag3"));    // Above minimum

            // Act
            _target.WriteBatch(messages);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(2));
            Assert.That(_target.ReceivedMessages[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(_target.ReceivedMessages[1].Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void IsLevelEnabled_ReturnsCorrectValues()
        {
            // Arrange
            _target.MinimumLevel = LogLevel.Warning;

            // Act & Assert
            Assert.That(_target.IsLevelEnabled(LogLevel.Debug), Is.False);
            Assert.That(_target.IsLevelEnabled(LogLevel.Info), Is.False);
            Assert.That(_target.IsLevelEnabled(LogLevel.Warning), Is.True);
            Assert.That(_target.IsLevelEnabled(LogLevel.Error), Is.True);
            Assert.That(_target.IsLevelEnabled(LogLevel.Fatal), Is.True);
        }

        [Test]
        public void IsLevelEnabled_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            _target.IsEnabled = false;

            // Act & Assert
            Assert.That(_target.IsLevelEnabled(LogLevel.Fatal), Is.False);
        }

        [Test]
        public void SetTagFilters_WithIncludedTags_FiltersCorrectly()
        {
            // Arrange
            _target.SetTagFilters(new[] { "AllowedTag" }, null, false);
            var allowedMessage = new LogMessage(LogLevel.Info, "Allowed", "AllowedTag");
            var blockedMessage = new LogMessage(LogLevel.Info, "Blocked", "BlockedTag");

            // Act
            _target.Write(allowedMessage);
            _target.Write(blockedMessage);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1));
            Assert.That(_target.ReceivedMessages[0].Tag.ToString(), Is.EqualTo("AllowedTag"));
        }

        [Test]
        public void SetTagFilters_WithExcludedTags_FiltersCorrectly()
        {
            // Arrange
            _target.SetTagFilters(null, new[] { "BlockedTag" }, true);
            var allowedMessage = new LogMessage(LogLevel.Info, "Allowed", "AllowedTag");
            var blockedMessage = new LogMessage(LogLevel.Info, "Blocked", "BlockedTag");

            // Act
            _target.Write(allowedMessage);
            _target.Write(blockedMessage);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1));
            Assert.That(_target.ReceivedMessages[0].Tag.ToString(), Is.EqualTo("AllowedTag"));
        }

        [Test]
        public void SetTagFilters_WithUntaggedMessages_ProcessesCorrectly()
        {
            // Arrange
            _target.SetTagFilters(null, null, false);
            var untaggedMessage = new LogMessage(LogLevel.Info, "Untagged", "");
            var taggedMessage = new LogMessage(LogLevel.Info, "Tagged", "SomeTag");

            // Act
            _target.Write(untaggedMessage);
            _target.Write(taggedMessage);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1));
            Assert.That(_target.ReceivedMessages[0].Tag.ToString(), Is.EqualTo("SomeTag"));
        }

        [Test]
        public void Flush_IncrementsFlushCallCount()
        {
            // Act
            _target.Flush();
            _target.Flush();

            // Assert
            Assert.That(_target.FlushCallCount, Is.EqualTo(2));
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            // Arrange
            _target.Write(new LogMessage(LogLevel.Info, "Test", "Tag"));
            _target.Flush();

            // Act
            _target.Reset();

            // Assert
            Assert.That(_target.ReceivedMessages, Is.Empty);
            Assert.That(_target.WriteCallCount, Is.EqualTo(0));
            Assert.That(_target.WriteBatchCallCount, Is.EqualTo(0));
            Assert.That(_target.FlushCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_SetsIsDisposedFlag()
        {
            // Act
            _target.Dispose();

            // Assert
            Assert.That(_target.IsDisposed, Is.True);
            Assert.That(_target.ReceivedMessages, Is.Empty);
        }

        [Test]
        public void ClearTagFilters_ResetsAllFilters()
        {
            // Arrange
            _target.SetTagFilters(new[] { "Include" }, new[] { "Exclude" }, false);

            // Act
            _target.ClearTagFilters();

            // Verify by testing message processing
            var message = new LogMessage(LogLevel.Info, "Test", "");
            _target.Write(message);

            // Assert
            Assert.That(_target.ReceivedMessages.Count, Is.EqualTo(1)); // Should process untagged now
        }
    }
}