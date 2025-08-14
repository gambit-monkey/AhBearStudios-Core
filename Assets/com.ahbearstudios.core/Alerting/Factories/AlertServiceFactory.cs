using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
using Unity.Profiling;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Pooling;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Concrete implementation of IAlertServiceFactory for creating and configuring AlertService instances.
    /// Designed for Unity game development with zero-allocation patterns and pooling support.
    /// Follows strict factory pattern - creates objects but does not manage their lifecycle.
    /// </summary>
    public sealed class AlertServiceFactory : IAlertServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IPoolingService _poolingService;
        private readonly Dictionary<string, object> _defaultSettings;
        private readonly ProfilerMarker _createServiceMarker;
        private readonly ProfilerMarker _validateConfigMarker;

        /// <summary>
        /// Initializes a new instance of the AlertServiceFactory class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for factory operations</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        /// <param name="poolingService">Optional pooling service for temporary allocations</param>
        public AlertServiceFactory(ILoggingService loggingService = null, ISerializationService serializationService = null, IPoolingService poolingService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            _poolingService = poolingService;
            _defaultSettings = CreateDefaultSettings();
            _createServiceMarker = new ProfilerMarker("AlertServiceFactory.CreateService");
            _validateConfigMarker = new ProfilerMarker("AlertServiceFactory.ValidateConfig");
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
                var logChannel = new LogAlertChannel(loggingService ?? _loggingService, messageBusService);
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
        /// Factory does not track created services - lifecycle management is caller's responsibility.
        /// </summary>
        public async UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default)
        {
            using (_createServiceMarker.Auto())
            {
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                var validationResult = ValidateConfiguration(configuration);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors.AsValueEnumerable().Select(e => e.Message))}", nameof(configuration));
                }

                var startTime = DateTime.UtcNow;
                
                try
                {
                    var alertService = new AlertService(null, _loggingService, _serializationService);

                    // Configure global settings
                    alertService.SetMinimumSeverity(configuration.AlertConfig.MinimumSeverity);

                    // Create and register channels
                    await CreateAndRegisterChannels(alertService, configuration.AlertConfig.Channels, correlationId);

                    var creationTime = DateTime.UtcNow - startTime;
                    LogInfo($"Created AlertService with {configuration.Environment} configuration in {creationTime.TotalMilliseconds:F2}ms", correlationId);
                    return alertService;
                }
                catch (Exception ex)
                {
                    var creationTime = DateTime.UtcNow - startTime;
                    LogError($"Failed to create configured AlertService: {ex.Message} (took {creationTime.TotalMilliseconds:F2}ms)", correlationId);
                    throw;
                }
            }
        }


        /// <summary>
        /// Creates an AlertService with custom channels and filters.
        /// Uses pooling service for temporary collections and parallel setup for performance.
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

            var startTime = DateTime.UtcNow;
            
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

                var creationTime = DateTime.UtcNow - startTime;
                LogInfo($"Created custom AlertService configuration in {creationTime.TotalMilliseconds:F2}ms", correlationId);
                return alertService;
            }
            catch (Exception ex)
            {
                var creationTime = DateTime.UtcNow - startTime;
                LogError($"Failed to create custom AlertService: {ex.Message} (took {creationTime.TotalMilliseconds:F2}ms)", correlationId);
                throw;
            }
        }

        /// <summary>
        /// Validates an alert service configuration before creation.
        /// Uses pooling service for temporary collections to avoid allocations.
        /// </summary>
        public ValidationResult ValidateConfiguration(AlertServiceConfiguration configuration)
        {
            using (_validateConfigMarker.Auto())
            {
                if (configuration == null)
                    return ValidationResult.Failure(new[] { new ValidationError("Configuration cannot be null") }, "AlertServiceFactory");

                var errors = new List<ValidationError>();

                // Validate basic settings
                if (configuration.MaxQueuedAlerts <= 0)
                    errors.Add(new ValidationError("MaxQueuedAlerts must be greater than 0"));

                if (configuration.MaxConcurrentOperations <= 0)
                    errors.Add(new ValidationError("MaxConcurrentOperations must be greater than 0"));

                if (configuration.StartupTimeout <= System.TimeSpan.Zero)
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

                return errors.AsValueEnumerable().Any() 
                    ? ValidationResult.Failure(errors, "AlertServiceFactory")
                    : ValidationResult.Success("AlertServiceFactory");
            }
        }

        /// <summary>
        /// Gets the default configuration for alert services.
        /// </summary>
        public AlertServiceConfiguration GetDefaultConfiguration()
        {
            return new AlertServiceConfiguration();
        }


        /// <summary>
        /// Creates a pre-configured AlertService for development environments.
        /// </summary>
        public async UniTask<IAlertService> CreateDevelopmentAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService = null)
        {
            var config = GetDevelopmentConfiguration();
            return await CreateAlertServiceAsync(config, Guid.NewGuid());
        }

        /// <summary>
        /// Creates a pre-configured AlertService for production environments.
        /// </summary>
        public async UniTask<IAlertService> CreateProductionAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService)
        {
            var config = GetProductionConfiguration();
            return await CreateAlertServiceAsync(config, Guid.NewGuid());
        }

        /// <summary>
        /// Creates a minimal AlertService for testing scenarios.
        /// </summary>
        public IAlertService CreateTestAlertService(IMessageBusService messageBusService = null)
        {
            return CreateAlertService(messageBusService);
        }

        /// <summary>
        /// Gets configuration optimized for development environments.
        /// </summary>
        public AlertServiceConfiguration GetDevelopmentConfiguration()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Development,
                GlobalMinimumSeverity = AlertSeverity.Debug,
                IsEnabled = true,
                MaxActiveAlerts = 5000,
                MaxHistorySize = 10000,
                MaintenanceInterval = TimeSpan.FromMinutes(10),
                AlertConfig = new AlertConfig
                {
                    MinimumSeverity = AlertSeverity.Debug,
                    EnableSuppression = true,
                    SuppressionWindow = TimeSpan.FromMinutes(2),
                    EnableAsyncProcessing = false,
                    MaxConcurrentAlerts = 50,
                    ProcessingTimeout = TimeSpan.FromSeconds(10),
                    EnableHistory = true,
                    HistoryRetention = TimeSpan.FromHours(8),
                    MaxHistoryEntries = 5000,
                    EnableAggregation = false,
                    EnableCorrelationTracking = true,
                    AlertBufferSize = 500,
                    EnableUnityIntegration = true,
                    EnableMetrics = true,
                    EnableCircuitBreakerIntegration = false
                }
            };
        }

        /// <summary>
        /// Gets configuration optimized for production environments.
        /// </summary>
        public AlertServiceConfiguration GetProductionConfiguration()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Production,
                GlobalMinimumSeverity = AlertSeverity.Warning,
                IsEnabled = true,
                MaxActiveAlerts = 20000,
                MaxHistorySize = 50000,
                MaintenanceInterval = TimeSpan.FromMinutes(5),
                AlertConfig = new AlertConfig
                {
                    MinimumSeverity = AlertSeverity.Warning,
                    EnableSuppression = true,
                    SuppressionWindow = TimeSpan.FromMinutes(5),
                    EnableAsyncProcessing = true,
                    MaxConcurrentAlerts = 200,
                    ProcessingTimeout = TimeSpan.FromSeconds(30),
                    EnableHistory = true,
                    HistoryRetention = TimeSpan.FromHours(48),
                    MaxHistoryEntries = 20000,
                    EnableAggregation = true,
                    AggregationWindow = TimeSpan.FromMinutes(2),
                    MaxAggregationSize = 100,
                    EnableCorrelationTracking = true,
                    AlertBufferSize = 2000,
                    EnableUnityIntegration = false,
                    EnableMetrics = true,
                    EnableCircuitBreakerIntegration = true
                }
            };
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
                    config.MaintenanceInterval = System.TimeSpan.FromMinutes(intervalMinutes);
            }

            // Apply custom settings
            config.CustomSettings = new Dictionary<string, object>(settings);

            return config;
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
                    AlertChannelType.Log => new LogAlertChannel(_loggingService, null),
                    AlertChannelType.Console => new ConsoleAlertChannel(null),
                    AlertChannelType.Memory => new MemoryAlertChannel(null, 1000),
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
                ["GlobalMinimumSeverity"] = AlertSeverity.Info,
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