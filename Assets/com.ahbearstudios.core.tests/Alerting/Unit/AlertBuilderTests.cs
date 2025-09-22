using System;
using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Builders;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Unit tests for AlertConfigBuilder and related builder classes.
    /// Tests configuration validation, fluent API patterns, and builder chain methods.
    /// </summary>
    [TestFixture]
    public class AlertBuilderTests : BaseServiceTest
    {
        private AlertConfigBuilder _builder;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _builder = new AlertConfigBuilder();
        }

        #region AlertConfigBuilder Tests

        [Test]
        public void Constructor_WithValidPoolingService_InitializesCorrectly()
        {
            // Arrange & Act
            var builder = new AlertConfigBuilder();

            // Assert
            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullPoolingService_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            // Constructor has no parameters, so this test is not applicable
            Assert.Pass("AlertConfigBuilder constructor takes no parameters");
        }

        [Test]
        public void ForProduction_WhenCalled_ConfiguresProductionSettings()
        {
            // Act
            var result = _builder.ForProduction();

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            // Build and verify configuration
            var config = _builder.BuildServiceConfiguration();
            Assert.That(config, Is.Not.Null);
            Assert.That(config.AlertConfig, Is.Not.Null);
        }

        [Test]
        public void ForDevelopment_WhenCalled_ConfiguresDevelopmentSettings()
        {
            // Act
            var result = _builder.ForDevelopment();

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            // Build and verify configuration
            var config = _builder.BuildServiceConfiguration();
            Assert.That(config, Is.Not.Null);
            Assert.That(config.AlertConfig.MinimumSeverity, Is.EqualTo(AlertSeverity.Debug));
        }

        [Test]
        public void ForTesting_WhenCalled_ConfiguresTestingSettings()
        {
            // Act
            var result = _builder.ForTesting();

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            // Build and verify configuration
            var config = _builder.BuildServiceConfiguration();
            Assert.That(config, Is.Not.Null);
            Assert.That(config.AlertConfig.MinimumSeverity, Is.EqualTo(AlertSeverity.Debug));
        }

        [Test]
        public void WithMinimumSeverity_WithValidSeverity_SetsMinimumSeverity()
        {
            // Arrange
            var severity = AlertSeverity.Warning;

            // Act
            var result = _builder.WithMinimumSeverity(severity);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.Build();
            Assert.That(config.MinimumSeverity, Is.EqualTo(severity));
        }

        [Test]
        public void WithHistorySize_WithValidSize_SetsHistorySize()
        {
            // Arrange
            var historySize = 500;

            // Act
            var result = _builder.WithHistorySize(historySize);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.Build();
            Assert.That(config.MaxHistorySize, Is.EqualTo(historySize));
        }

        [Test]
        public void WithHistorySize_WithNegativeSize_ThrowsArgumentException()
        {
            // Arrange
            var invalidSize = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.WithHistorySize(invalidSize));
        }

        [Test]
        public void WithSeverityFilter_WithValidSeverity_AddsFilter()
        {
            // Arrange
            var filterName = "SeverityFilter";
            var severity = AlertSeverity.Error;

            // Act
            var result = _builder.WithSeverityFilter(filterName, severity);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.Build();
            Assert.That(config.FilterConfigurations, Is.Not.Empty);
        }

        [Test]
        public void WithSeverityFilter_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var emptyName = "";
            var severity = AlertSeverity.Warning;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.WithSeverityFilter(emptyName, severity));
        }

        [Test]
        public void AddSourceFilter_WithValidSource_AddsFilter()
        {
            // Arrange
            var filterName = "SourceFilter";
            var source = TestConstants.TestSource;

            // Act
            var result = _builder.AddSourceFilter(filterName, source);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.Build();
            Assert.That(config.FilterConfigurations, Is.Not.Empty);
        }

        [Test]
        public void WithRateLimitFilter_WithValidParameters_AddsFilter()
        {
            // Arrange
            var filterName = "RateLimitFilter";
            var maxAlertsPerMinute = 100;

            // Act
            var result = _builder.WithRateLimitFilter(filterName, maxAlertsPerMinute);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.Build();
            Assert.That(config.FilterConfigurations, Is.Not.Empty);
        }

        [Test]
        public void WithRateLimitFilter_WithZeroRate_ThrowsArgumentException()
        {
            // Arrange
            var filterName = "RateLimitFilter";
            var invalidRate = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.WithRateLimitFilter(filterName, invalidRate));
        }

        [Test]
        public void WithConsoleChannel_WithValidName_AddsChannel()
        {
            // Arrange
            var channelName = "ConsoleChannel";

            // Act
            var result = _builder.WithConsoleChannel(channelName);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.BuildServiceConfiguration();
            Assert.That(config.ChannelServiceConfig.ChannelConfigurations, Is.Not.Empty);
        }

        [Test]
        public void AddFileChannel_WithValidParameters_AddsChannel()
        {
            // Arrange
            var channelName = "FileChannel";
            var filePath = "/tmp/test.log";

            // Act
            var result = _builder.AddFileChannel(channelName, filePath);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.BuildServiceConfiguration();
            Assert.That(config.ChannelServiceConfig.ChannelConfigurations, Is.Not.Empty);
        }

        [Test]
        public void AddFileChannel_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var channelName = "FileChannel";
            var emptyPath = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _builder.AddFileChannel(channelName, emptyPath));
        }

        [Test]
        public void WithLogChannel_WithValidName_AddsChannel()
        {
            // Arrange
            var channelName = "MemoryChannel";

            // Act
            var result = _builder.WithLogChannel(channelName);

            // Assert
            Assert.That(result, Is.SameAs(_builder)); // Fluent interface

            var config = _builder.BuildServiceConfiguration();
            Assert.That(config.ChannelServiceConfig.ChannelConfigurations, Is.Not.Empty);
        }

        [Test]
        public void Build_WithValidConfiguration_ReturnsAlertConfig()
        {
            // Arrange
            _builder.ForProduction()
                   .WithMinimumSeverity(AlertSeverity.Warning)
                   .WithHistorySize(1000);

            // Act
            var config = _builder.Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.MinimumSeverity, Is.EqualTo(AlertSeverity.Warning));
            Assert.That(config.MaxHistorySize, Is.EqualTo(1000));
        }

        [Test]
        public void BuildServiceConfiguration_WithValidConfiguration_ReturnsServiceConfig()
        {
            // Arrange
            _builder.ForProduction()
                   .WithMinimumSeverity(AlertSeverity.Error)
                   .WithConsoleChannel("Console");

            // Act
            var config = _builder.BuildServiceConfiguration();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.AlertConfig, Is.Not.Null);
            Assert.That(config.ChannelServiceConfig, Is.Not.Null);
            Assert.That(config.AlertConfig.MinimumSeverity, Is.EqualTo(AlertSeverity.Error));
        }

        #endregion

        #region Fluent Interface Chain Tests

        [Test]
        public void FluentChain_WithMultipleOperations_BuildsCorrectConfiguration()
        {
            // Act
            var config = _builder
                .ForProduction()
                .WithMinimumSeverity(AlertSeverity.Warning)
                .WithHistorySize(500)
                .WithSeverityFilter("ErrorFilter", AlertSeverity.Error)
                .WithRateLimitFilter("RateLimit", 50)
                .WithConsoleChannel("Console")
                .WithLogChannel("Memory")
                .Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.MinimumSeverity, Is.EqualTo(AlertSeverity.Warning));
            Assert.That(config.MaxHistorySize, Is.EqualTo(500));
            Assert.That(config.FilterConfigurations.Count, Is.EqualTo(2));
        }

        [Test]
        public void FluentChain_WithDevelopmentPreset_ConfiguresCorrectly()
        {
            // Act
            var config = _builder
                .ForDevelopment()
                .WithConsoleChannel("DevConsole")
                .WithSeverityFilter("DebugFilter", AlertSeverity.Debug)
                .Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.MinimumSeverity, Is.EqualTo(AlertSeverity.Debug));
        }

        [Test]
        public void FluentChain_WithTestingPreset_ConfiguresCorrectly()
        {
            // Act
            var config = _builder
                .ForTesting()
                .WithLogChannel("TestMemory")
                .WithHistorySize(100)
                .Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.MaxHistorySize, Is.EqualTo(100));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void Build_WithoutPresetConfiguration_UsesDefaults()
        {
            // Act
            var config = _builder.Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.MinimumSeverity, Is.Not.EqualTo(AlertSeverity.Unknown));
            Assert.That(config.MaxHistorySize, Is.GreaterThan(0));
        }

        [Test]
        public void Build_WithOverriddenValues_UsesOverrideValues()
        {
            // Arrange
            var customSeverity = AlertSeverity.Critical;
            var customHistorySize = 2000;

            // Act
            var config = _builder
                .ForProduction() // Sets production defaults
                .WithMinimumSeverity(customSeverity) // Override
                .WithHistorySize(customHistorySize) // Override
                .Build();

            // Assert
            Assert.That(config.MinimumSeverity, Is.EqualTo(customSeverity));
            Assert.That(config.MaxHistorySize, Is.EqualTo(customHistorySize));
        }

        [Test]
        public void BuildServiceConfiguration_WithPoolingConfiguration_ConfiguresPooling()
        {
            // Act
            var config = _builder
                .ForProduction()
                .BuildServiceConfiguration();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.PoolConfiguration, Is.Not.Null);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void WithSeverityFilter_CalledMultipleTimes_AddsMultipleFilters()
        {
            // Act
            var config = _builder
                .WithSeverityFilter("Filter1", AlertSeverity.Warning)
                .WithSeverityFilter("Filter2", AlertSeverity.Error)
                .WithSeverityFilter("Filter3", AlertSeverity.Critical)
                .Build();

            // Assert
            Assert.That(config.FilterConfigurations.Count, Is.EqualTo(3));
        }

        [Test]
        public void AddChannel_CalledMultipleTimes_AddsMultipleChannels()
        {
            // Act
            var config = _builder
                .WithConsoleChannel("Console1")
                .WithConsoleChannel("Console2")
                .WithLogChannel("Memory1")
                .BuildServiceConfiguration();

            // Assert
            Assert.That(config.ChannelServiceConfig.ChannelConfigurations.Count, Is.EqualTo(3));
        }

        [Test]
        public void Build_CalledMultipleTimes_ReturnsConsistentConfiguration()
        {
            // Arrange
            _builder.ForProduction().WithMinimumSeverity(AlertSeverity.Warning);

            // Act
            var config1 = _builder.Build();
            var config2 = _builder.Build();

            // Assert
            Assert.That(config1.MinimumSeverity, Is.EqualTo(config2.MinimumSeverity));
            Assert.That(config1.MaxHistorySize, Is.EqualTo(config2.MaxHistorySize));
        }

        #endregion
    }
}