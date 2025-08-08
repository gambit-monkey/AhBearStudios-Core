using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;

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

        /// <summary>
        /// Creates a channel with enhanced configuration including retry policy and rate limiting.
        /// </summary>
        /// <param name="configuration">Base channel configuration</param>
        /// <param name="retryPolicy">Retry policy configuration</param>
        /// <param name="rateLimit">Rate limiting configuration</param>
        /// <param name="loggingService">Optional logging service</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with created and configured channel instance</returns>
        UniTask<IAlertChannel> CreateEnhancedChannelAsync(
            ChannelConfig configuration,
            RetryPolicyConfig retryPolicy = null,
            RateLimitConfig rateLimit = null,
            ILoggingService loggingService = null,
            Guid correlationId = default);

        /// <summary>
        /// Creates channels configured for emergency escalation scenarios.
        /// </summary>
        /// <param name="loggingService">Logging service for emergency log channel</param>
        /// <returns>UniTask with collection of emergency channels</returns>
        UniTask<IEnumerable<IAlertChannel>> CreateEmergencyChannelsAsync(ILoggingService loggingService);
    }

    /// <summary>
    /// Extended configuration class for channel creation with additional factory-specific settings.
    /// </summary>
    public sealed class ExtendedChannelConfiguration
    {
        /// <summary>
        /// Gets or sets the unique name identifier for this channel.
        /// </summary>
        public FixedString64Bytes Name { get; set; }

        /// <summary>
        /// Gets or sets the channel type enum.
        /// </summary>
        public AlertChannelType ChannelType { get; set; }

        /// <summary>
        /// Gets or sets whether this channel is enabled for alert processing.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum alert severity level that this channel will process.
        /// </summary>
        public AlertSeverity MinimumSeverity { get; set; } = AlertSeverity.Info;

        /// <summary>
        /// Gets or sets the maximum alert severity level that this channel will process.
        /// </summary>
        public AlertSeverity MaximumSeverity { get; set; } = AlertSeverity.Emergency;

        /// <summary>
        /// Gets or sets the message format template for this channel.
        /// </summary>
        public string MessageFormat { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}";

        /// <summary>
        /// Gets or sets the timestamp format string used in message formatting.
        /// </summary>
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Gets or sets whether batch processing is enabled for this channel.
        /// </summary>
        public bool EnableBatching { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of alerts to include in a single batch.
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum time to wait before flushing a partial batch.
        /// </summary>
        public TimeSpan BatchFlushInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the retry policy configuration for failed alert deliveries.
        /// </summary>
        public RetryPolicyConfig RetryPolicy { get; set; } = RetryPolicyConfig.Default;

        /// <summary>
        /// Gets or sets the timeout for individual alert send operations.
        /// </summary>
        public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets whether health monitoring is enabled for this channel.
        /// </summary>
        public bool EnableHealthMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for performing channel health checks.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the strongly-typed channel settings based on the channel type.
        /// </summary>
        public IChannelSettings TypedSettings { get; set; }

        /// <summary>
        /// Gets or sets the rate limiting configuration for this channel.
        /// </summary>
        public RateLimitConfig RateLimit { get; set; } = RateLimitConfig.Default;

        /// <summary>
        /// Gets or sets the priority level for this channel during alert processing.
        /// </summary>
        public int Priority { get; set; } = 500;

        /// <summary>
        /// Gets or sets whether this channel should participate in emergency escalation.
        /// </summary>
        public bool IsEmergencyChannel { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to auto-start the channel after creation.
        /// </summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum queue size for buffered channels.
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the flush interval for buffered channels.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets additional initialization parameters.
        /// </summary>
        public Dictionary<string, object> InitializationParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates an extended configuration from a base configuration.
        /// </summary>
        /// <param name="baseConfig">Base channel configuration</param>
        /// <param name="channelType">Channel type</param>
        /// <returns>Extended channel configuration</returns>
        public static ExtendedChannelConfiguration FromBase(ChannelConfig baseConfig, AlertChannelType channelType)
        {
            return new ExtendedChannelConfiguration
            {
                Name = baseConfig.Name,
                ChannelType = channelType,
                IsEnabled = baseConfig.IsEnabled,
                MinimumSeverity = baseConfig.MinimumSeverity,
                MaximumSeverity = baseConfig.MaximumSeverity,
                MessageFormat = baseConfig.MessageFormat,
                TimestampFormat = baseConfig.TimestampFormat,
                EnableBatching = baseConfig.EnableBatching,
                BatchSize = baseConfig.BatchSize,
                BatchFlushInterval = baseConfig.BatchFlushInterval,
                RetryPolicy = baseConfig.RetryPolicy,
                SendTimeout = baseConfig.SendTimeout,
                EnableHealthMonitoring = baseConfig.EnableHealthMonitoring,
                HealthCheckInterval = baseConfig.HealthCheckInterval,
                TypedSettings = baseConfig.TypedSettings,
                RateLimit = baseConfig.RateLimit,
                Priority = baseConfig.Priority,
                IsEmergencyChannel = baseConfig.IsEmergencyChannel
            };
        }

        /// <summary>
        /// Creates default configuration for a channel type.
        /// </summary>
        /// <param name="channelType">Channel type</param>
        /// <param name="name">Channel name</param>
        /// <returns>Default extended configuration</returns>
        public static ExtendedChannelConfiguration Default(AlertChannelType channelType, FixedString64Bytes name)
        {
            var config = new ExtendedChannelConfiguration
            {
                Name = name,
                ChannelType = channelType,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Info,
                MaximumSeverity = AlertSeverity.Emergency
            };

            // Set type-specific defaults
            switch (channelType)
            {
                case AlertChannelType.Log:
                    config.MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}";
                    config.TypedSettings = LogChannelSettings.Default;
                    break;

                case AlertChannelType.Console:
                    config.MessageFormat = "[{Timestamp:HH:mm:ss.fff}] [{Severity}] {Source}: {Message}";
                    config.TypedSettings = ConsoleChannelSettings.Default;
                    break;

                case AlertChannelType.Network:
                    config.MessageFormat = "{{\"timestamp\":\"{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\",\"severity\":\"{Severity}\",\"source\":\"{Source}\",\"message\":\"{Message}\",\"tag\":\"{Tag}\",\"correlationId\":\"{CorrelationId}\"}}";
                    config.EnableBatching = true;
                    config.BatchSize = 10;
                    config.MinimumSeverity = AlertSeverity.Critical;
                    break;

                case AlertChannelType.Email:
                    config.MessageFormat = "<h3>Alert Details</h3><p><strong>Timestamp:</strong> {Timestamp:yyyy-MM-dd HH:mm:ss}</p><p><strong>Severity:</strong> {Severity}</p><p><strong>Source:</strong> {Source}</p><p><strong>Message:</strong> {Message}</p>";
                    config.EnableBatching = true;
                    config.BatchSize = 5;
                    config.MinimumSeverity = AlertSeverity.Critical;
                    break;

                case AlertChannelType.UnityConsole:
                    config.MessageFormat = "[{Source}] {Message}";
                    config.MinimumSeverity = AlertSeverity.Warning;
                    config.TypedSettings = UnityChannelSettings.Default;
                    break;

                case AlertChannelType.UnityNotification:
                    config.MessageFormat = "{Message}";
                    config.MinimumSeverity = AlertSeverity.Critical;
                    break;
            }

            // Set common defaults for all channel types
            config.EnableHealthMonitoring = true;
            config.HealthCheckInterval = TimeSpan.FromMinutes(5);
            config.SendTimeout = TimeSpan.FromSeconds(10);
            config.Priority = 500;
            config.RetryPolicy = RetryPolicyConfig.Default;
            config.RateLimit = RateLimitConfig.Default;

            return config;
        }

        /// <summary>
        /// Converts this extended configuration to a base ChannelConfig.
        /// </summary>
        /// <returns>Base channel configuration</returns>
        public ChannelConfig ToChannelConfig()
        {
            return new ChannelConfig
            {
                Name = Name,
                ChannelType = ChannelType,
                IsEnabled = IsEnabled,
                MinimumSeverity = MinimumSeverity,
                MaximumSeverity = MaximumSeverity,
                MessageFormat = MessageFormat,
                TimestampFormat = TimestampFormat,
                EnableBatching = EnableBatching,
                BatchSize = BatchSize,
                BatchFlushInterval = BatchFlushInterval,
                RetryPolicy = RetryPolicy,
                SendTimeout = SendTimeout,
                EnableHealthMonitoring = EnableHealthMonitoring,
                HealthCheckInterval = HealthCheckInterval,
                TypedSettings = TypedSettings,
                RateLimit = RateLimit,
                Priority = Priority,
                IsEmergencyChannel = IsEmergencyChannel
            };
        }
    }

    /// <summary>
    /// Result of channel creation operation.
    /// </summary>
    public sealed class ChannelCreationResult
    {
        /// <summary>
        /// Gets or sets whether the creation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the created channel instance.
        /// </summary>
        public IAlertChannel Channel { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during creation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the configuration used for creation.
        /// </summary>
        public ChannelConfig Configuration { get; set; }

        /// <summary>
        /// Gets or sets the time taken for creation.
        /// </summary>
        public TimeSpan CreationTime { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="channel">Created channel</param>
        /// <param name="configuration">Configuration used</param>
        /// <param name="creationTime">Time taken</param>
        /// <returns>Successful creation result</returns>
        public static ChannelCreationResult Success(IAlertChannel channel, ChannelConfig configuration, TimeSpan creationTime)
        {
            return new ChannelCreationResult
            {
                Success = true,
                Channel = channel,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="configuration">Configuration that failed</param>
        /// <param name="creationTime">Time taken before failure</param>
        /// <returns>Failed creation result</returns>
        public static ChannelCreationResult Failure(string error, ChannelConfig configuration = null, TimeSpan creationTime = default)
        {
            return new ChannelCreationResult
            {
                Success = false,
                Error = error,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }
    }
}