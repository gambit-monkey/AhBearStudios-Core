using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory interface for creating and configuring AlertService instances.
    /// Provides abstraction for dependency injection and testing scenarios.
    /// Designed for Unity game development with support for different deployment configurations.
    /// </summary>
    public interface IAlertServiceFactory
    {
        /// <summary>
        /// Creates a new AlertService instance with default configuration.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <returns>Configured AlertService instance</returns>
        IAlertService CreateAlertService(IMessageBusService messageBusService = null, ILoggingService loggingService = null);

        /// <summary>
        /// Creates a new AlertService instance with specific configuration.
        /// </summary>
        /// <param name="configuration">Alert service configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with configured AlertService instance</returns>
        UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default);

        /// <summary>
        /// Creates a pre-configured AlertService for development environments.
        /// Includes console and logging channels with debug-level filtering.
        /// </summary>
        /// <param name="loggingService">Logging service</param>
        /// <param name="messageBusService">Message bus service</param>
        /// <returns>Development-configured AlertService</returns>
        UniTask<IAlertService> CreateDevelopmentAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService = null);

        /// <summary>
        /// Creates a pre-configured AlertService for production environments.
        /// Includes logging channel with warning-level filtering and suppression.
        /// </summary>
        /// <param name="loggingService">Logging service</param>
        /// <param name="messageBusService">Message bus service</param>
        /// <returns>Production-configured AlertService</returns>
        UniTask<IAlertService> CreateProductionAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService);

        /// <summary>
        /// Creates a minimal AlertService for testing scenarios.
        /// Uses in-memory channels and minimal filtering.
        /// </summary>
        /// <returns>Test-configured AlertService</returns>
        IAlertService CreateTestAlertService();

        /// <summary>
        /// Creates an AlertService with custom channels and filters.
        /// </summary>
        /// <param name="channels">Collection of channels to register</param>
        /// <param name="filters">Collection of filters to register</param>
        /// <param name="suppressionRules">Collection of suppression rules</param>
        /// <param name="messageBusService">Message bus service</param>
        /// <param name="loggingService">Logging service</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with custom-configured AlertService</returns>
        UniTask<IAlertService> CreateCustomAlertServiceAsync(
            IEnumerable<IAlertChannel> channels,
            IEnumerable<IAlertFilter> filters,
            IEnumerable<AlertRule> suppressionRules = null,
            IMessageBusService messageBusService = null,
            ILoggingService loggingService = null,
            Guid correlationId = default);

        /// <summary>
        /// Validates an alert service configuration before creation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateConfiguration(AlertServiceConfiguration configuration);

        /// <summary>
        /// Gets the default configuration for alert services.
        /// </summary>
        /// <returns>Default alert service configuration</returns>
        AlertServiceConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Gets configuration optimized for development environments.
        /// </summary>
        /// <returns>Development alert service configuration</returns>
        AlertServiceConfiguration GetDevelopmentConfiguration();

        /// <summary>
        /// Gets configuration optimized for production environments.
        /// </summary>
        /// <returns>Production alert service configuration</returns>
        AlertServiceConfiguration GetProductionConfiguration();

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        /// <param name="settings">Configuration settings</param>
        /// <returns>Alert service configuration</returns>
        AlertServiceConfiguration CreateConfigurationFromSettings(Dictionary<string, object> settings);
    }

    /// <summary>
    /// Configuration class for AlertService creation.
    /// </summary>
    public sealed class AlertServiceConfiguration
    {
        /// <summary>
        /// Gets or sets the environment type for this configuration.
        /// </summary>
        public AlertEnvironmentType Environment { get; set; } = AlertEnvironmentType.Production;

        /// <summary>
        /// Gets or sets the global minimum severity level.
        /// </summary>
        public AlertSeverity GlobalMinimumSeverity { get; set; } = AlertSeverity.Information;

        /// <summary>
        /// Gets or sets whether the alert service is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of active alerts to maintain.
        /// </summary>
        public int MaxActiveAlerts { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum number of alerts to keep in history.
        /// </summary>
        public int MaxHistorySize { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the maintenance interval for cleanup operations.
        /// </summary>
        public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets channel configurations.
        /// </summary>
        public List<ChannelConfiguration> Channels { get; set; } = new List<ChannelConfiguration>();

        /// <summary>
        /// Gets or sets filter configurations.
        /// </summary>
        public List<FilterConfiguration> Filters { get; set; } = new List<FilterConfiguration>();

        /// <summary>
        /// Gets or sets suppression rule configurations.
        /// </summary>
        public List<SuppressionRuleConfiguration> SuppressionRules { get; set; } = new List<SuppressionRuleConfiguration>();

        /// <summary>
        /// Gets or sets source-specific minimum severity overrides.
        /// </summary>
        public Dictionary<string, AlertSeverity> SourceMinimumSeverities { get; set; } = new Dictionary<string, AlertSeverity>();

        /// <summary>
        /// Gets or sets additional custom settings.
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a default configuration.
        /// </summary>
        /// <returns>Default alert service configuration</returns>
        public static AlertServiceConfiguration Default()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Production,
                GlobalMinimumSeverity = AlertSeverity.Information,
                Channels = new List<ChannelConfiguration>
                {
                    ChannelConfiguration.DefaultLog(),
                    ChannelConfiguration.DefaultConsole()
                },
                Filters = new List<FilterConfiguration>
                {
                    FilterConfiguration.DefaultSeverity()
                }
            };
        }

        /// <summary>
        /// Creates a development-optimized configuration.
        /// </summary>
        /// <returns>Development alert service configuration</returns>
        public static AlertServiceConfiguration Development()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Development,
                GlobalMinimumSeverity = AlertSeverity.Debug,
                Channels = new List<ChannelConfiguration>
                {
                    ChannelConfiguration.DevelopmentLog(),
                    ChannelConfiguration.DevelopmentConsole()
                },
                Filters = new List<FilterConfiguration>
                {
                    FilterConfiguration.DevelopmentSeverity()
                }
            };
        }

        /// <summary>
        /// Creates a production-optimized configuration.
        /// </summary>
        /// <returns>Production alert service configuration</returns>
        public static AlertServiceConfiguration Production()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Production,
                GlobalMinimumSeverity = AlertSeverity.Warning,
                MaxActiveAlerts = 2000,
                MaxHistorySize = 10000,
                Channels = new List<ChannelConfiguration>
                {
                    ChannelConfiguration.ProductionLog()
                },
                Filters = new List<FilterConfiguration>
                {
                    FilterConfiguration.ProductionSeverity()
                },
                SuppressionRules = new List<SuppressionRuleConfiguration>
                {
                    SuppressionRuleConfiguration.DefaultRateLimit(),
                    SuppressionRuleConfiguration.DefaultDuplicateSuppression()
                }
            };
        }
    }

    /// <summary>
    /// Environment types for alert service configuration.
    /// </summary>
    public enum AlertEnvironmentType
    {
        /// <summary>
        /// Development environment with verbose logging and minimal filtering.
        /// </summary>
        Development = 0,

        /// <summary>
        /// Testing environment with in-memory channels and controlled output.
        /// </summary>
        Testing = 1,

        /// <summary>
        /// Staging environment with production-like configuration but additional monitoring.
        /// </summary>
        Staging = 2,

        /// <summary>
        /// Production environment with optimized performance and minimal noise.
        /// </summary>
        Production = 3
    }

    /// <summary>
    /// Configuration for individual channels.
    /// </summary>
    public sealed class ChannelConfiguration
    {
        public string Type { get; set; }
        public FixedString64Bytes Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public AlertSeverity MinimumSeverity { get; set; } = AlertSeverity.Information;
        public int MaxAlertsPerSecond { get; set; } = 100;
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        public static ChannelConfiguration DefaultLog()
        {
            return new ChannelConfiguration
            {
                Type = "Log",
                Name = "LogChannel",
                MinimumSeverity = AlertSeverity.Information,
                MaxAlertsPerSecond = 1000,
                Settings = new Dictionary<string, object>
                {
                    ["IncludeContext"] = true,
                    ["IncludeStackTrace"] = false,
                    ["LogPrefix"] = "[ALERT]"
                }
            };
        }

        public static ChannelConfiguration DefaultConsole()
        {
            return new ChannelConfiguration
            {
                Type = "Console",
                Name = "ConsoleChannel",
                MinimumSeverity = AlertSeverity.Warning,
                MaxAlertsPerSecond = 50,
                Settings = new Dictionary<string, object>
                {
                    ["UseColors"] = true,
                    ["IncludeTimestamp"] = true,
                    ["ExpandContext"] = false
                }
            };
        }

        public static ChannelConfiguration DevelopmentLog()
        {
            var config = DefaultLog();
            config.MinimumSeverity = AlertSeverity.Debug;
            config.Settings["IncludeStackTrace"] = true;
            return config;
        }

        public static ChannelConfiguration DevelopmentConsole()
        {
            var config = DefaultConsole();
            config.MinimumSeverity = AlertSeverity.Debug;
            config.Settings["ExpandContext"] = true;
            return config;
        }

        public static ChannelConfiguration ProductionLog()
        {
            var config = DefaultLog();
            config.MinimumSeverity = AlertSeverity.Warning;
            config.MaxAlertsPerSecond = 500;
            config.Settings["IncludeContext"] = false;
            return config;
        }
    }

    /// <summary>
    /// Configuration for individual filters.
    /// </summary>
    public sealed class FilterConfiguration
    {
        public string Type { get; set; }
        public FixedString64Bytes Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 100;
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        public static FilterConfiguration DefaultSeverity()
        {
            return new FilterConfiguration
            {
                Type = "Severity",
                Name = "SeverityFilter",
                Priority = 10,
                Settings = new Dictionary<string, object>
                {
                    ["MinimumSeverity"] = AlertSeverity.Information,
                    ["AllowCriticalAlways"] = true
                }
            };
        }

        public static FilterConfiguration DevelopmentSeverity()
        {
            var config = DefaultSeverity();
            config.Settings["MinimumSeverity"] = AlertSeverity.Debug;
            config.Priority = 5;
            return config;
        }

        public static FilterConfiguration ProductionSeverity()
        {
            var config = DefaultSeverity();
            config.Settings["MinimumSeverity"] = AlertSeverity.Warning;
            config.Priority = 10;
            return config;
        }
    }

    /// <summary>
    /// Configuration for suppression rules.
    /// </summary>
    public sealed class SuppressionRuleConfiguration
    {
        public string Name { get; set; }
        public string SourcePattern { get; set; }
        public string MessagePattern { get; set; }
        public AlertSeverity? Severity { get; set; }
        public TimeSpan SuppressionDuration { get; set; }
        public int? RateLimit { get; set; }
        public bool IsEnabled { get; set; } = true;

        public static SuppressionRuleConfiguration DefaultRateLimit()
        {
            return new SuppressionRuleConfiguration
            {
                Name = "DefaultRateLimit",
                SourcePattern = "*",
                MessagePattern = "*",
                SuppressionDuration = TimeSpan.FromMinutes(1),
                RateLimit = 50
            };
        }

        public static SuppressionRuleConfiguration DefaultDuplicateSuppression()
        {
            return new SuppressionRuleConfiguration
            {
                Name = "DefaultDuplicateSuppression",
                SourcePattern = "*",
                MessagePattern = "*",
                SuppressionDuration = TimeSpan.FromSeconds(30)
            };
        }
    }
}