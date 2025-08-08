using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;
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
                throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}", nameof(configuration));
            }

            try
            {
                var alertService = new AlertService(null, _loggingService, _serializationService);

                // Configure global settings
                alertService.SetMinimumSeverity(configuration.GlobalMinimumSeverity);

                // Apply source-specific severity overrides
                foreach (var sourceSeverity in configuration.SourceMinimumSeverities)
                {
                    alertService.SetMinimumSeverity(sourceSeverity.Key, sourceSeverity.Value);
                }

                // Create and register channels
                await CreateAndRegisterChannels(alertService, configuration.Channels, correlationId);

                // Create and register filters
                await CreateAndRegisterFilters(alertService, configuration.Filters, correlationId);

                // Apply suppression rules
                await ApplySuppressionRules(alertService, configuration.SuppressionRules, correlationId);

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
            var config = AlertServiceConfiguration.Development();
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
                    Settings = new Dictionary<string, object>
                    {
                        ["IncludeStackTrace"] = true,
                        ["IncludeContext"] = true,
                        ["LogPrefix"] = "[DEV-ALERT]"
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
                    Settings = new Dictionary<string, object>
                    {
                        ["IncludeStackTrace"] = false,
                        ["IncludeContext"] = false,
                        ["LogPrefix"] = "[ALERT]"
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
            if (configuration.MaxActiveAlerts <= 0)
                errors.Add(new ValidationError("MaxActiveAlerts must be greater than 0"));

            if (configuration.MaxHistorySize <= 0)
                errors.Add(new ValidationError("MaxHistorySize must be greater than 0"));

            if (configuration.MaintenanceInterval <= TimeSpan.Zero)
                errors.Add(new ValidationError("MaintenanceInterval must be positive"));

            // Validate channels
            if (configuration.Channels == null)
                errors.Add(new ValidationError("Channels collection cannot be null"));
            else
            {
                foreach (var channelConfig in configuration.Channels)
                {
                    if (string.IsNullOrEmpty(channelConfig.Type))
                        errors.Add(new ValidationError($"Channel type cannot be empty"));

                    if (channelConfig.MaxAlertsPerSecond <= 0)
                        errors.Add(new ValidationError($"Channel {channelConfig.Type} MaxAlertsPerSecond must be positive"));
                }
            }

            // Validate filters
            if (configuration.Filters == null)
                errors.Add(new ValidationError("Filters collection cannot be null"));
            else
            {
                foreach (var filterConfig in configuration.Filters)
                {
                    if (string.IsNullOrEmpty(filterConfig.Type))
                        errors.Add(new ValidationError("Filter type cannot be empty"));
                }
            }

            // Validate suppression rules
            if (configuration.SuppressionRules != null)
            {
                foreach (var ruleConfig in configuration.SuppressionRules)
                {
                    if (string.IsNullOrEmpty(ruleConfig.Name))
                        errors.Add(new ValidationError("Suppression rule name cannot be empty"));

                    if (ruleConfig.SuppressionDuration <= TimeSpan.Zero)
                        errors.Add(new ValidationError($"Suppression rule {ruleConfig.Name} duration must be positive"));
                }
            }

            return errors.Any() 
                ? ValidationResult.Failure(errors, "AlertServiceFactory")
                : ValidationResult.Success("AlertServiceFactory");
        }

        /// <summary>
        /// Gets the default configuration for alert services.
        /// </summary>
        public AlertServiceConfiguration GetDefaultConfiguration()
        {
            return AlertServiceConfiguration.Default();
        }

        /// <summary>
        /// Gets configuration optimized for development environments.
        /// </summary>
        public AlertServiceConfiguration GetDevelopmentConfiguration()
        {
            return AlertServiceConfiguration.Development();
        }

        /// <summary>
        /// Gets configuration optimized for production environments.
        /// </summary>
        public AlertServiceConfiguration GetProductionConfiguration()
        {
            return AlertServiceConfiguration.Production();
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

            return config;
        }

        #endregion

        #region Private Helper Methods

        private async UniTask CreateAndRegisterChannels(IAlertService alertService, List<ChannelConfiguration> channelConfigs, Guid correlationId)
        {
            foreach (var channelConfig in channelConfigs)
            {
                if (!channelConfig.IsEnabled)
                    continue;

                IAlertChannel channel = channelConfig.Type.ToLowerInvariant() switch
                {
                    "log" => new LogAlertChannel(_loggingService),
                    "console" => new ConsoleAlertChannel(),
                    "test" => new TestAlertChannel(),
                    _ => throw new NotSupportedException($"Channel type '{channelConfig.Type}' is not supported")
                };

                var config = new ChannelConfig
                {
                    Name = channelConfig.Name.ToString(),
                    IsEnabled = channelConfig.IsEnabled,
                    MinimumSeverity = channelConfig.MinimumSeverity,
                    Settings = channelConfig.Settings
                };

                await channel.InitializeAsync(config, correlationId);
                alertService.RegisterChannel(channel, correlationId);
            }
        }

        private async UniTask CreateAndRegisterFilters(IAlertService alertService, List<FilterConfiguration> filterConfigs, Guid correlationId)
        {
            foreach (var filterConfig in filterConfigs)
            {
                if (!filterConfig.IsEnabled)
                    continue;

                IAlertFilter filter = filterConfig.Type.ToLowerInvariant() switch
                {
                    "severity" => CreateSeverityFilter(filterConfig),
                    "source" => CreateSourceFilter(filterConfig),
                    "ratelimit" => CreateRateLimitFilter(filterConfig),
                    _ => throw new NotSupportedException($"Filter type '{filterConfig.Type}' is not supported")
                };

                if (filter != null)
                {
                    filter.Priority = filterConfig.Priority;
                    await UniTask.CompletedTask; // Placeholder for async filter configuration if needed
                    alertService.AddFilter(filter, correlationId);
                }
            }
        }

        private async UniTask ApplySuppressionRules(IAlertService alertService, List<SuppressionRuleConfiguration> ruleConfigs, Guid correlationId)
        {
            foreach (var ruleConfig in ruleConfigs)
            {
                if (!ruleConfig.IsEnabled)
                    continue;

                AlertRule rule;
                
                if (ruleConfig.RateLimit.HasValue)
                {
                    rule = AlertRule.CreateRateLimit(
                        ruleConfig.SourcePattern,
                        ruleConfig.RateLimit.Value,
                        ruleConfig.SuppressionDuration);
                }
                else
                {
                    rule = AlertRule.CreateDuplicateSuppression(
                        ruleConfig.SourcePattern,
                        ruleConfig.SuppressionDuration);
                }

                rule.Name = ruleConfig.Name;
                alertService.AddSuppressionRule(rule, correlationId);
            }

            await UniTask.CompletedTask;
        }

        private IAlertFilter CreateSeverityFilter(FilterConfiguration config)
        {
            var minSeverity = AlertSeverity.Information;
            if (config.Settings.TryGetValue("MinimumSeverity", out var severityValue))
            {
                if (severityValue is AlertSeverity severity)
                    minSeverity = severity;
                else if (Enum.TryParse<AlertSeverity>(severityValue.ToString(), out var parsedSeverity))
                    minSeverity = parsedSeverity;
            }

            return new SeverityAlertFilter(config.Name.ToString(), minSeverity);
        }

        private IAlertFilter CreateSourceFilter(FilterConfiguration config)
        {
            var allowedSources = new List<string>();
            if (config.Settings.TryGetValue("AllowedSources", out var sourcesValue))
            {
                if (sourcesValue is IEnumerable<string> sources)
                    allowedSources.AddRange(sources);
            }

            return new SourceAlertFilter(config.Name.ToString(), allowedSources);
        }

        private IAlertFilter CreateRateLimitFilter(FilterConfiguration config)
        {
            var maxAlertsPerMinute = 60;
            if (config.Settings.TryGetValue("MaxAlertsPerMinute", out var rateValue))
            {
                if (int.TryParse(rateValue.ToString(), out var rate))
                    maxAlertsPerMinute = rate;
            }

            return new RateLimitAlertFilter(config.Name.ToString(), maxAlertsPerMinute);
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