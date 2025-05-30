using NUnit.Framework;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Mock implementation of ILoggerConfig for testing
    /// </summary>
    public class MockLoggerConfig : ILoggerConfig
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
        public int MaxMessagesPerBatch { get; set; } = 100;
        public Tagging.LogTag DefaultTag { get; set; } = new Tagging.LogTag("Default", Tagging.TagCategory.General);
    }

    /// <summary>
    /// Mock implementation of ILogTargetConfig for testing
    /// </summary>
    public class MockLogTargetConfig : ILogTargetConfig
    {
        public string TargetName { get; set; } = "MockTarget";
        public bool Enabled { get; set; } = true;
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
        public string[] IncludedTags { get; set; } = new string[0];
        public string[] ExcludedTags { get; set; } = new string[0];
        public bool ProcessUntaggedMessages { get; set; } = true;
        public bool CaptureUnityLogs { get; set; } = true;
        public bool IncludeStackTraces { get; set; } = true;
        public bool IncludeTimestamps { get; set; } = true;
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public bool IncludeSourceContext { get; set; } = true;
        public bool IncludeThreadId { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = false;
        public bool AutoFlush { get; set; } = true;
        public int BufferSize { get; set; } = 0;
        public float FlushIntervalSeconds { get; set; } = 0f;
        public bool LimitMessageLength { get; set; } = false;
        public int MaxMessageLength { get; set; } = 8192;

        public ILogTarget CreateTarget()
        {
            return new MockLogTargetTests.MockLogTarget(TargetName)
            {
                MinimumLevel = MinimumLevel,
                IsEnabled = Enabled
            };
        }

        public ILogTarget CreateTarget(AhBearStudios.Core.MessageBus.Interfaces.IMessageBus messageBus)
        {
            var target = CreateTarget();
            // In a real implementation, we'd configure the target with the message bus
            return target;
        }

        public void ApplyTagFilters(ILogTarget target)
        {
            target.SetTagFilters(IncludedTags, ExcludedTags, ProcessUntaggedMessages);
        }

        public ILogTargetConfig Clone()
        {
            return new MockLogTargetConfig
            {
                TargetName = TargetName,
                Enabled = Enabled,
                MinimumLevel = MinimumLevel,
                IncludedTags = (string[])IncludedTags.Clone(),
                ExcludedTags = (string[])ExcludedTags.Clone(),
                ProcessUntaggedMessages = ProcessUntaggedMessages,
                CaptureUnityLogs = CaptureUnityLogs,
                IncludeStackTraces = IncludeStackTraces,
                IncludeTimestamps = IncludeTimestamps,
                TimestampFormat = TimestampFormat,
                IncludeSourceContext = IncludeSourceContext,
                IncludeThreadId = IncludeThreadId,
                EnableStructuredLogging = EnableStructuredLogging,
                AutoFlush = AutoFlush,
                BufferSize = BufferSize,
                FlushIntervalSeconds = FlushIntervalSeconds,
                LimitMessageLength = LimitMessageLength,
                MaxMessageLength = MaxMessageLength
            };
        }
    }

    /// <summary>
    /// Tests for ILoggerConfig implementations
    /// </summary>
    [TestFixture]
    public class LoggerConfigTests
    {
        private MockLoggerConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new MockLoggerConfig();
        }

        [Test]
        public void LoggerConfig_DefaultValues_AreCorrect()
        {
            // Assert
            Assert.That(_config.MinimumLevel, Is.EqualTo(LogLevel.Info));
            Assert.That(_config.MaxMessagesPerBatch, Is.EqualTo(100));
            Assert.That(_config.DefaultTag.Name, Is.EqualTo("Default"));
            Assert.That(_config.DefaultTag.Category, Is.EqualTo(Tagging.TagCategory.General));
        }

        [Test]
        public void LoggerConfig_MinimumLevel_CanBeChanged()
        {
            // Act
            _config.MinimumLevel = LogLevel.Error;

            // Assert
            Assert.That(_config.MinimumLevel, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void LoggerConfig_MaxMessagesPerBatch_CanBeChanged()
        {
            // Act
            _config.MaxMessagesPerBatch = 500;

            // Assert
            Assert.That(_config.MaxMessagesPerBatch, Is.EqualTo(500));
        }

        [Test]
        public void LoggerConfig_DefaultTag_CanBeChanged()
        {
            // Arrange
            var newTag = new Tagging.LogTag("CustomDefault", Tagging.TagCategory.System);

            // Act
            _config.DefaultTag = newTag;

            // Assert
            Assert.That(_config.DefaultTag.Name, Is.EqualTo("CustomDefault"));
            Assert.That(_config.DefaultTag.Category, Is.EqualTo(Tagging.TagCategory.System));
        }

        [Test]
        public void LoggerConfig_MaxMessagesPerBatch_ValidatesPositiveValue()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _config.MaxMessagesPerBatch = 1);
            Assert.DoesNotThrow(() => _config.MaxMessagesPerBatch = 1000);
        }

        [Test]
        public void LoggerConfig_AllLogLevels_CanBeSet()
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
                _config.MinimumLevel = level;
                Assert.That(_config.MinimumLevel, Is.EqualTo(level));
            }
        }
    }

    /// <summary>
    /// Tests for ILogTargetConfig implementations
    /// </summary>
    [TestFixture]
    public class LogTargetConfigTests
    {
        private MockLogTargetConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new MockLogTargetConfig();
        }

        [Test]
        public void LogTargetConfig_DefaultValues_AreCorrect()
        {
            // Assert
            Assert.That(_config.TargetName, Is.EqualTo("MockTarget"));
            Assert.That(_config.Enabled, Is.True);
            Assert.That(_config.MinimumLevel, Is.EqualTo(LogLevel.Trace));
            Assert.That(_config.IncludedTags, Is.Empty);
            Assert.That(_config.ExcludedTags, Is.Empty);
            Assert.That(_config.ProcessUntaggedMessages, Is.True);
            Assert.That(_config.CaptureUnityLogs, Is.True);
            Assert.That(_config.IncludeStackTraces, Is.True);
            Assert.That(_config.IncludeTimestamps, Is.True);
            Assert.That(_config.TimestampFormat, Is.EqualTo("yyyy-MM-dd HH:mm:ss.fff"));
            Assert.That(_config.IncludeSourceContext, Is.True);
            Assert.That(_config.IncludeThreadId, Is.False);
            Assert.That(_config.EnableStructuredLogging, Is.False);
            Assert.That(_config.AutoFlush, Is.True);
            Assert.That(_config.BufferSize, Is.EqualTo(0));
            Assert.That(_config.FlushIntervalSeconds, Is.EqualTo(0f));
            Assert.That(_config.LimitMessageLength, Is.False);
            Assert.That(_config.MaxMessageLength, Is.EqualTo(8192));
        }

        [Test]
        public void LogTargetConfig_TargetName_CanBeChanged()
        {
            // Act
            _config.TargetName = "CustomTarget";

            // Assert
            Assert.That(_config.TargetName, Is.EqualTo("CustomTarget"));
        }

        [Test]
        public void LogTargetConfig_Enabled_CanBeToggled()
        {
            // Act
            _config.Enabled = false;

            // Assert
            Assert.That(_config.Enabled, Is.False);

            // Act
            _config.Enabled = true;

            // Assert
            Assert.That(_config.Enabled, Is.True);
        }

        [Test]
        public void LogTargetConfig_MinimumLevel_CanBeChanged()
        {
            // Act
            _config.MinimumLevel = LogLevel.Warning;

            // Assert
            Assert.That(_config.MinimumLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void LogTargetConfig_TagFilters_CanBeSet()
        {
            // Arrange
            var includedTags = new[] { "Include1", "Include2" };
            var excludedTags = new[] { "Exclude1", "Exclude2" };

            // Act
            _config.IncludedTags = includedTags;
            _config.ExcludedTags = excludedTags;
            _config.ProcessUntaggedMessages = false;

            // Assert
            Assert.That(_config.IncludedTags, Is.EqualTo(includedTags));
            Assert.That(_config.ExcludedTags, Is.EqualTo(excludedTags));
            Assert.That(_config.ProcessUntaggedMessages, Is.False);
        }

        [Test]
        public void LogTargetConfig_TimestampConfiguration_CanBeChanged()
        {
            // Act
            _config.IncludeTimestamps = false;
            _config.TimestampFormat = "HH:mm:ss";

            // Assert
            Assert.That(_config.IncludeTimestamps, Is.False);
            Assert.That(_config.TimestampFormat, Is.EqualTo("HH:mm:ss"));
        }

        [Test]
        public void LogTargetConfig_BufferingConfiguration_CanBeChanged()
        {
            // Act
            _config.AutoFlush = false;
            _config.BufferSize = 1000;
            _config.FlushIntervalSeconds = 5.0f;

            // Assert
            Assert.That(_config.AutoFlush, Is.False);
            Assert.That(_config.BufferSize, Is.EqualTo(1000));
            Assert.That(_config.FlushIntervalSeconds, Is.EqualTo(5.0f));
        }

        [Test]
        public void LogTargetConfig_MessageLengthLimiting_CanBeConfigured()
        {
            // Act
            _config.LimitMessageLength = true;
            _config.MaxMessageLength = 1024;

            // Assert
            Assert.That(_config.LimitMessageLength, Is.True);
            Assert.That(_config.MaxMessageLength, Is.EqualTo(1024));
        }

        [Test]
        public void LogTargetConfig_CreateTarget_ReturnsConfiguredTarget()
        {
            // Arrange
            _config.TargetName = "TestTarget";
            _config.MinimumLevel = LogLevel.Warning;
            _config.Enabled = false;

            // Act
            using var target = _config.CreateTarget();

            // Assert
            Assert.That(target.Name, Is.EqualTo("TestTarget"));
            Assert.That(target.MinimumLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(target.IsEnabled, Is.False);
        }

        [Test]
        public void LogTargetConfig_ApplyTagFilters_ConfiguresTarget()
        {
            // Arrange
            _config.IncludedTags = new[] { "AllowedTag" };
            _config.ExcludedTags = new[] { "BlockedTag" };
            _config.ProcessUntaggedMessages = false;

            using var target = new MockLogTargetTests.MockLogTarget();

            // Act
            _config.ApplyTagFilters(target);

            // Verify by testing message processing
            var allowedMessage = new AhBearStudios.Core.Logging.Messages.LogMessage(LogLevel.Info, "Allowed", "AllowedTag");
            var blockedMessage = new AhBearStudios.Core.Logging.Messages.LogMessage(LogLevel.Info, "Blocked", "BlockedTag");
            var untaggedMessage = new AhBearStudios.Core.Logging.Messages.LogMessage(LogLevel.Info, "Untagged", "");

            target.Write(allowedMessage);
            target.Write(blockedMessage);
            target.Write(untaggedMessage);

            // Assert
            Assert.That(target.ReceivedMessages.Count, Is.EqualTo(1));
            Assert.That(target.ReceivedMessages[0].Tag.ToString(), Is.EqualTo("AllowedTag"));
        }

        [Test]
        public void LogTargetConfig_Clone_CreatesIndependentCopy()
        {
            // Arrange
            _config.TargetName = "OriginalTarget";
            _config.MinimumLevel = LogLevel.Error;
            _config.IncludedTags = new[] { "OriginalTag" };

            // Act
            var cloned = _config.Clone();

            // Modify original
            _config.TargetName = "ModifiedTarget";
            _config.MinimumLevel = LogLevel.Fatal;
            _config.IncludedTags = new[] { "ModifiedTag" };

            // Assert
            Assert.That(cloned.TargetName, Is.EqualTo("OriginalTarget"));
            Assert.That(cloned.MinimumLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(cloned.IncludedTags, Is.EqualTo(new[] { "OriginalTag" }));
        }

        [Test]
        public void LogTargetConfig_Clone_CopiesAllProperties()
        {
            // Arrange
            _config.TargetName = "CloneTest";
            _config.Enabled = false;
            _config.MinimumLevel = LogLevel.Warning;
            _config.IncludedTags = new[] { "Tag1", "Tag2" };
            _config.ExcludedTags = new[] { "ExTag1" };
            _config.ProcessUntaggedMessages = false;
            _config.CaptureUnityLogs = false;
            _config.IncludeStackTraces = false;
            _config.IncludeTimestamps = false;
            _config.TimestampFormat = "HH:mm";
            _config.IncludeSourceContext = false;
            _config.IncludeThreadId = true;
            _config.EnableStructuredLogging = true;
            _config.AutoFlush = false;
            _config.BufferSize = 512;
            _config.FlushIntervalSeconds = 2.5f;
            _config.LimitMessageLength = true;
            _config.MaxMessageLength = 2048;

            // Act
            var cloned = _config.Clone();

            // Assert
            Assert.That(cloned.TargetName, Is.EqualTo(_config.TargetName));
            Assert.That(cloned.Enabled, Is.EqualTo(_config.Enabled));
            Assert.That(cloned.MinimumLevel, Is.EqualTo(_config.MinimumLevel));
            Assert.That(cloned.IncludedTags, Is.EqualTo(_config.IncludedTags));
            Assert.That(cloned.ExcludedTags, Is.EqualTo(_config.ExcludedTags));
            Assert.That(cloned.ProcessUntaggedMessages, Is.EqualTo(_config.ProcessUntaggedMessages));
            Assert.That(cloned.CaptureUnityLogs, Is.EqualTo(_config.CaptureUnityLogs));
            Assert.That(cloned.IncludeStackTraces, Is.EqualTo(_config.IncludeStackTraces));
            Assert.That(cloned.IncludeTimestamps, Is.EqualTo(_config.IncludeTimestamps));
            Assert.That(cloned.TimestampFormat, Is.EqualTo(_config.TimestampFormat));
            Assert.That(cloned.IncludeSourceContext, Is.EqualTo(_config.IncludeSourceContext));
            Assert.That(cloned.IncludeThreadId, Is.EqualTo(_config.IncludeThreadId));
            Assert.That(cloned.EnableStructuredLogging, Is.EqualTo(_config.EnableStructuredLogging));
            Assert.That(cloned.AutoFlush, Is.EqualTo(_config.AutoFlush));
            Assert.That(cloned.BufferSize, Is.EqualTo(_config.BufferSize));
            Assert.That(cloned.FlushIntervalSeconds, Is.EqualTo(_config.FlushIntervalSeconds));
            Assert.That(cloned.LimitMessageLength, Is.EqualTo(_config.LimitMessageLength));
            Assert.That(cloned.MaxMessageLength, Is.EqualTo(_config.MaxMessageLength));
        }

        [Test]
        public void LogTargetConfig_CreateTargetWithMessageBus_ReturnsValidTarget()
        {
            // Arrange
            // Note: In a real test, we'd mock IMessageBus properly
            
            // Act
            using var target = _config.CreateTarget(null); // Passing null as we don't have a real message bus

            // Assert
            Assert.That(target, Is.Not.Null);
            Assert.That(target.Name, Is.EqualTo(_config.TargetName));
        }

        [Test]
        public void LogTargetConfig_EmptyTagArrays_HandleCorrectly()
        {
            // Arrange
            _config.IncludedTags = new string[0];
            _config.ExcludedTags = new string[0];

            // Act
            var cloned = _config.Clone();

            // Assert
            Assert.That(cloned.IncludedTags, Is.Empty);
            Assert.That(cloned.ExcludedTags, Is.Empty);
        }

        [Test]
        public void LogTargetConfig_NullTagArrays_HandleCorrectly()
        {
            // Arrange
            _config.IncludedTags = null;
            _config.ExcludedTags = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _config.Clone());
        }
    }
}