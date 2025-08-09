using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;\nusing AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Configs;\nusing AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Concrete implementation of IAlertServiceFactory for creating and configuring AlertService instances.
    /// Provides factory methods for different deployment environments with appropriate configurations.
    /// Designed for Unity game development with comprehensive dependency injection support.
    /// </summary>
    public sealed class AlertServiceFactory : IAlertServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly Dictionary<string, object> _defaultSettings;

        /// <summary>
        /// Initializes a new instance of the AlertServiceFactory class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for factory operations</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        public AlertServiceFactory(ILoggingService loggingService = null, ISerializationService serializationService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            _defaultSettings = CreateDefaultSettings();
        }

        #region IAlertServiceFactory Implementation

        /// <summary>
        /// Creates a new AlertService instance with default configuration.
        /// </summary>
        public IAlertService CreateAlertService(IMessageBusService messageBusService = null, ILoggingService loggingService = null)
        {
            try
            {
                var alertService = new AlertService(messageBusService, loggingService ?? _loggingService, _serializationService);
                
                // Add default logging channel
                var logChannel = new LogAlertChannel(loggingService ?? _loggingService);
                alertService.RegisterChannel(logChannel);

                LogInfo("Created AlertService with default configuration");
                return alertService;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create AlertService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new AlertService instance with specific configuration.
        /// </summary>
        public async UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors.ZSelect(e => e.Message))}", nameof(configuration));
            }

            try
            {
                var alertService = new AlertService(null, _loggingService, _serializationService);

                // Configure global settings
                alertService.SetMinimumSeverity(configuration.AlertConfig.MinimumSeverity);

                // Source-specific overrides would be configured in AlertConfig if needed

                // Create and register channels
                await CreateAndRegisterChannels(alertService, configuration.AlertConfig.Channels, correlationId);

                // No filters to configure - they are created directly in specific factory methods

                // Apply suppression rules
                // Suppression rules are configured through AlertConfig

                LogInfo($"Created AlertService with {configuration.Environment} configuration", correlationId);
                return alertService;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create configured AlertService: {ex.Message}", correlationId);
                throw;
            }
        }

        /// <summary>
        /// Creates a pre-configured AlertService for development environments.
        /// </summary>
        public async UniTask<IAlertService> CreateDevelopmentAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService = null)
        {
            // Use default AlertServiceConfiguration for development
            var alertService = new AlertService(messageBusService, loggingService, _serializationService);

            try
            {
                // Development-specific setup
                alertService.SetMinimumSeverity(AlertSeverity.Debug);

                // Add development channels
                var logChannel = new LogAlertChannel(loggingService);
                await logChannel.InitializeAsync(new ChannelConfig
                {
                    Name = "DevelopmentLog",
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Debug,
                    TypedSettings = new LogChannelSettings
                    {
                        IncludeStackTrace = true,
                        IncludeContext = true,
                        LogCategory = "DevAlerts"
                    }
                });
                alertService.RegisterChannel(logChannel);

                // Add development filters
                var severityFilter = SeverityAlertFilter.CreateForDevelopment();
                alertService.AddFilter(severityFilter);

                LogInfo("Created development AlertService configuration");
                return alertService;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create development AlertService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a pre-configured AlertService for production environments.
        /// </summary>
        public async UniTask<IAlertService> CreateProductionAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService)
        {
            if (loggingService == null)
                throw new ArgumentNullException(nameof(loggingService));
            if (messageBusService == null)
                throw new ArgumentNullException(nameof(messageBusService));

            var alertService = new AlertService(messageBusService, loggingService, _serializationService);

            try
            {
                // Production-specific setup
                alertService.SetMinimumSeverity(AlertSeverity.Warning);

                // Add production channels
                var logChannel = new LogAlertChannel(loggingService);
                await logChannel.InitializeAsync(new ChannelConfig
                {
                    Name = "ProductionLog",
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Warning,
                    TypedSettings = new LogChannelSettings
                    {
                        IncludeStackTrace = false,
                        IncludeContext = false,
                        LogCategory = "Alerts"
                    }
                });
                alertService.RegisterChannel(logChannel);

                // Add production filters
                var severityFilter = SeverityAlertFilter.CreateForProduction();
                alertService.AddFilter(severityFilter);

                // Add production suppression rules
                alertService.AddSuppressionRule(AlertRule.CreateRateLimit("*", 50, TimeSpan.FromMinutes(1)));
                alertService.AddSuppressionRule(AlertRule.CreateDuplicateSuppression("*", TimeSpan.FromSeconds(30)));

                LogInfo("Created production AlertService configuration");
                return alertService;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create production AlertService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a minimal AlertService for testing scenarios.
        /// </summary>
        public IAlertService CreateTestAlertService()
        {
            try
            {
                var alertService = new AlertService(null, null, _serializationService);
                
                // Add minimal test channel
                var testChannel = new TestAlertChannel();
                alertService.RegisterChannel(testChannel);

                LogInfo("Created test AlertService configuration");
                return alertService;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create test AlertService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates an AlertService with custom channels and filters.
        /// </summary>
        public async UniTask<IAlertService> CreateCustomAlertServiceAsync(
            IEnumerable<IAlertChannel> channels,
            IEnumerable<IAlertFilter> filters,
            IEnumerable<AlertRule> suppressionRules = null,
            IMessageBusService messageBusService = null,
            ILoggingService loggingService = null,
            Guid correlationId = default)
        {
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));

            try
            {
                var alertService = new AlertService(messageBusService, loggingService ?? _loggingService, _serializationService);

                // Register custom channels
                foreach (var channel in channels)
                {
                    alertService.RegisterChannel(channel, correlationId);
                }

                // Register custom filters
                foreach (var filter in filters)
                {
                    alertService.AddFilter(filter, correlationId);
                }

                // Apply custom suppression rules
                if (suppressionRules != null)
                {
                    foreach (var rule in suppressionRules)
                    {
                        alertService.AddSuppressionRule(rule, correlationId);
                    }
                }

                LogInfo("Created custom AlertService configuration", correlationId);
                return alertService;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create custom AlertService: {ex.Message}", correlationId);
                throw;
            }
        }

        /// <summary>
        /// Validates an alert service configuration before creation.
        /// </summary>
        public ValidationResult ValidateConfiguration(AlertServiceConfiguration configuration)
        {
            if (configuration == null)
                return ValidationResult.Failure(new[] { new ValidationError("Configuration cannot be null") }, "AlertServiceFactory");

            var errors = new List<ValidationError>();

            // Validate basic settings
            if (configuration.MaxQueuedAlerts <= 0)
                errors.Add(new ValidationError("MaxQueuedAlerts must be greater than 0"));

            if (configuration.MaxConcurrentOperations <= 0)
                errors.Add(new ValidationError("MaxConcurrentOperations must be greater than 0"));

            if (configuration.StartupTimeout <= TimeSpan.Zero)
                errors.Add(new ValidationError("StartupTimeout must be positive"));

            // Validate channels through AlertConfig
            if (configuration.AlertConfig?.Channels == null)
                errors.Add(new ValidationError("AlertConfig.Channels collection cannot be null"));
            else
            {
                foreach (var channelConfig in configuration.AlertConfig.Channels)
                {
                    try
                    {
                        channelConfig.Validate();
                    }
                    catch (InvalidOperationException ex)
                    {
                        errors.Add(new ValidationError($"Channel validation failed: {ex.Message}"));
                    }
                }
            }

            // Validate AlertConfig
            if (configuration.AlertConfig == null)
            {
                errors.Add(new ValidationError("AlertConfig cannot be null"));
            }
            else
            {
                try
                {
                    configuration.AlertConfig.Validate();
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add(new ValidationError($"AlertConfig validation failed: {ex.Message}"));
                }
            }

            return errors.ZAny() 
                ? ValidationResult.Failure(errors, "AlertServiceFactory")
                : ValidationResult.Success("AlertServiceFactory");
        }

        /// <summary>
        /// Gets the default configuration for alert services.
        /// </summary>
        public AlertServiceConfiguration GetDefaultConfiguration()
        {
            return new AlertServiceConfiguration();
        }

        /// <summary>
        /// Gets configuration optimized for development environments.
        /// </summary>
        public AlertServiceConfiguration GetDevelopmentConfiguration()
        {
            return new AlertServiceConfiguration { Environment = AlertEnvironmentType.Development };
        }

        /// <summary>
        /// Gets configuration optimized for production environments.
        /// </summary>
        public AlertServiceConfiguration GetProductionConfiguration()
        {
            return new AlertServiceConfiguration { Environment = AlertEnvironmentType.Production };
        }

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        public AlertServiceConfiguration CreateConfigurationFromSettings(Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var config = new AlertServiceConfiguration();

            // Parse environment
            if (settings.TryGetValue("Environment", out var envValue))
            {
                if (Enum.TryParse<AlertEnvironmentType>(envValue.ToString(), out var environment))
                    config.Environment = environment;
            }

            // Parse global minimum severity
            if (settings.TryGetValue("GlobalMinimumSeverity", out var severityValue))
            {
                if (Enum.TryParse<AlertSeverity>(severityValue.ToString(), out var severity))
                    config.GlobalMinimumSeverity = severity;
            }

            // Parse enabled flag
            if (settings.TryGetValue("IsEnabled", out var enabledValue))
            {
                if (bool.TryParse(enabledValue.ToString(), out var enabled))
                    config.IsEnabled = enabled;
            }

            // Parse numeric settings
            if (settings.TryGetValue("MaxActiveAlerts", out var maxActiveValue))
            {
                if (int.TryParse(maxActiveValue.ToString(), out var maxActive))
                    config.MaxActiveAlerts = maxActive;
            }

            if (settings.TryGetValue("MaxHistorySize", out var maxHistoryValue))
            {
                if (int.TryParse(maxHistoryValue.ToString(), out var maxHistory))
                    config.MaxHistorySize = maxHistory;
            }

            // Parse maintenance interval
            if (settings.TryGetValue("MaintenanceIntervalMinutes", out var intervalValue))
            {
                if (double.TryParse(intervalValue.ToString(), out var intervalMinutes))
                    config.MaintenanceInterval = TimeSpan.FromMinutes(intervalMinutes);
            }

            // Apply custom settings
            config.CustomSettings = new Dictionary<string, object>(settings);

            return new AlertServiceConfiguration();
        }

        #endregion

        #region Private Helper Methods

        private async UniTask CreateAndRegisterChannels(IAlertService alertService, IReadOnlyList<ChannelConfig> channelConfigs, Guid correlationId)
        {
            foreach (var channelConfig in channelConfigs)
            {
                if (!channelConfig.IsEnabled)
                    continue;

                IAlertChannel channel = channelConfig.ChannelType switch
                {
                    AlertChannelType.Log => new LogAlertChannel(_loggingService),
                    AlertChannelType.Console => new ConsoleAlertChannel(),
                    AlertChannelType.Memory => new MemoryAlertChannel(null),
                    _ => throw new NotSupportedException($"Channel type '{channelConfig.ChannelType}' is not supported")
                };

                // The channelConfig is already a ChannelConfig, so use it directly
                var config = channelConfig;

                await channel.InitializeAsync(config, correlationId);
                alertService.RegisterChannel(channel, correlationId);
            }
        }


        private Dictionary<string, object> CreateDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                ["Environment"] = AlertEnvironmentType.Production,
                ["GlobalMinimumSeverity"] = AlertSeverity.Information,
                ["IsEnabled"] = true,
                ["MaxActiveAlerts"] = 1000,
                ["MaxHistorySize"] = 5000,
                ["MaintenanceIntervalMinutes"] = 5.0
            };
        }

        private void LogInfo(string message, Guid correlationId = default)
        {
            _loggingService?.LogInfo($"[AlertServiceFactory] {message}", correlationId.ToString(), "AlertServiceFactory");
        }

        private void LogError(string message, Guid correlationId = default)
        {
            _loggingService?.LogError($"[AlertServiceFactory] {message}", correlationId.ToString(), "AlertServiceFactory");
        }

        #endregion
    }

}