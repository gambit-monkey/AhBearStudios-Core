using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory interface for creating and configuring alert channel instances.
    /// Provides abstraction for dependency injection and testing scenarios.
    /// Supports various channel types including logging, console, file, and network channels.
    /// </summary>
    public interface IAlertChannelFactory
    {
        /// <summary>
        /// Creates a new alert channel instance by type.
        /// </summary>
        /// <param name="channelType">Type of channel to create</param>
        /// <param name="name">Name for the channel instance</param>
        /// <param name="loggingService">Optional logging service for channels that need it</param>
        /// <returns>UniTask with created channel instance</returns>
        UniTask<IAlertChannel> CreateChannelAsync(AlertChannelType channelType, FixedString64Bytes name, ILoggingService loggingService = null);

        /// <summary>
        /// Creates a new alert channel instance by type name.
        /// </summary>
        /// <param name="channelTypeName">Name of the channel type</param>
        /// <param name="name">Name for the channel instance</param>
        /// <param name="loggingService">Optional logging service for channels that need it</param>
        /// <returns>UniTask with created channel instance</returns>
        UniTask<IAlertChannel> CreateChannelAsync(string channelTypeName, FixedString64Bytes name, ILoggingService loggingService = null);

        /// <summary>
        /// Creates and configures a new alert channel instance.
        /// </summary>
        /// <param name="configuration">Channel configuration</param>
        /// <param name="loggingService">Optional logging service for channels that need it</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with created and configured channel instance</returns>
        UniTask<IAlertChannel> CreateAndConfigureChannelAsync(ChannelConfig configuration, ILoggingService loggingService = null, Guid correlationId = default);

        /// <summary>
        /// Creates a logging channel with specific configuration.
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <param name="loggingService">Logging service instance</param>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="includeContext">Whether to include alert context</param>
        /// <param name="includeStackTrace">Whether to include stack traces</param>
        /// <returns>UniTask with configured logging channel</returns>
        UniTask<IAlertChannel> CreateLoggingChannelAsync(
            FixedString64Bytes name,
            ILoggingService loggingService,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool includeContext = true,
            bool includeStackTrace = false);

        /// <summary>
        /// Creates a console channel with specific configuration.
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="useColors">Whether to use color output</param>
        /// <param name="includeTimestamp">Whether to include timestamps</param>
        /// <returns>UniTask with configured console channel</returns>
        UniTask<IAlertChannel> CreateConsoleChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            bool useColors = true,
            bool includeTimestamp = true);

        /// <summary>
        /// Creates a file channel with specific configuration.
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <param name="filePath">Path to the log file</param>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="maxFileSize">Maximum file size before rotation</param>
        /// <param name="maxBackupFiles">Maximum number of backup files</param>
        /// <returns>UniTask with configured file channel</returns>
        UniTask<IAlertChannel> CreateFileChannelAsync(
            FixedString64Bytes name,
            string filePath,
            AlertSeverity minimumSeverity = AlertSeverity.Info,
            long maxFileSize = 10485760, // 10MB
            int maxBackupFiles = 5);

        /// <summary>
        /// Creates an in-memory channel for testing scenarios.
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="maxStoredAlerts">Maximum number of alerts to store</param>
        /// <returns>UniTask with configured memory channel</returns>
        UniTask<IAlertChannel> CreateMemoryChannelAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity = AlertSeverity.Debug,
            int maxStoredAlerts = 1000);

        /// <summary>
        /// Creates multiple channels from a collection of configurations.
        /// </summary>
        /// <param name="configurations">Collection of channel configurations</param>
        /// <param name="loggingService">Optional logging service</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with collection of created channels</returns>
        UniTask<IEnumerable<IAlertChannel>> CreateChannelsAsync(
            IEnumerable<ChannelConfig> configurations,
            ILoggingService loggingService = null,
            Guid correlationId = default);

        /// <summary>
        /// Creates channels optimized for development environments.
        /// </summary>
        /// <param name="loggingService">Logging service for log channel</param>
        /// <returns>UniTask with collection of development channels</returns>
        UniTask<IEnumerable<IAlertChannel>> CreateDevelopmentChannelsAsync(ILoggingService loggingService);

        /// <summary>
        /// Creates channels optimized for production environments.
        /// </summary>
        /// <param name="loggingService">Logging service for log channel</param>
        /// <param name="logFilePath">Optional log file path</param>
        /// <returns>UniTask with collection of production channels</returns>
        UniTask<IEnumerable<IAlertChannel>> CreateProductionChannelsAsync(ILoggingService loggingService, string logFilePath = null);

        /// <summary>
        /// Creates channels optimized for testing scenarios.
        /// </summary>
        /// <returns>UniTask with collection of test channels</returns>
        UniTask<IEnumerable<IAlertChannel>> CreateTestChannelsAsync();

        /// <summary>
        /// Creates channels configured for emergency escalation scenarios.
        /// </summary>
        /// <param name="loggingService">Logging service for emergency log channel</param>
        /// <returns>UniTask with collection of emergency channels</returns>
        UniTask<IEnumerable<IAlertChannel>> CreateEmergencyChannelsAsync(ILoggingService loggingService);

        /// <summary>
        /// Validates a channel configuration before creation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateChannelConfiguration(ChannelConfig configuration);

        /// <summary>
        /// Gets the default configuration for a specific channel type.
        /// </summary>
        /// <param name="channelType">Type of channel</param>
        /// <returns>Default configuration for the channel type</returns>
        ChannelConfig GetDefaultConfiguration(AlertChannelType channelType);

        /// <summary>
        /// Gets all supported channel types.
        /// </summary>
        /// <returns>Collection of supported channel types</returns>
        IEnumerable<AlertChannelType> GetSupportedChannelTypes();

        /// <summary>
        /// Checks if a channel type is supported by this factory.
        /// </summary>
        /// <param name="channelType">Channel type to check</param>
        /// <returns>True if supported, false otherwise</returns>
        bool IsChannelTypeSupported(AlertChannelType channelType);

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        /// <param name="channelType">Type of channel</param>
        /// <param name="name">Channel name</param>
        /// <param name="settings">Configuration settings</param>
        /// <returns>Channel configuration</returns>
        ChannelConfig CreateConfigurationFromSettings(AlertChannelType channelType, FixedString64Bytes name, Dictionary<string, object> settings);
    }
}