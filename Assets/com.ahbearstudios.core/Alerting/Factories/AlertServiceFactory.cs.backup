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
    /// Simple factory for creating AlertService instances following CLAUDE.md guidelines.
    /// Factory focuses on creation only - complexity is handled by builders, validation by validators.
    /// Does not manage object lifecycle - caller responsibility.
    /// </summary>
    public sealed class AlertServiceFactory : IAlertServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IPoolingService _poolingService;
        private readonly Dictionary<string, object> _defaultSettings;

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
        /// Simple creation only - assumes configuration is pre-validated by builder.
        /// </summary>
        public async UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var alertService = new AlertService(null, _loggingService, _serializationService);
            alertService.SetMinimumSeverity(configuration.AlertConfig.MinimumSeverity);
            await CreateAndRegisterChannels(alertService, configuration.AlertConfig.Channels, correlationId);
            
            LogInfo($"Created AlertService with {configuration.Environment} configuration", correlationId);
            return alertService;
        }


        /// <summary>
        /// Creates an AlertService with custom channels and filters.
        /// Simple creation only - assumes channels and filters are pre-configured.
        /// </summary>
        public async UniTask<IAlertService> CreateCustomAlertServiceAsync(
            IEnumerable<IAlertChannel> channels,
            IEnumerable<IAlertFilter> filters,
            IMessageBusService messageBusService = null,
            ILoggingService loggingService = null,
            Guid correlationId = default)
        {
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));

            var alertService = new AlertService(messageBusService, loggingService ?? _loggingService, _serializationService);
            var correlationIdString = correlationId == default ? default(FixedString64Bytes) : new FixedString64Bytes(correlationId.ToString());

            foreach (var channel in channels)
                alertService.RegisterChannel(channel, correlationIdString);

            foreach (var filter in filters)
                alertService.AddFilter(filter, correlationIdString);

            LogInfo("Created custom AlertService configuration", correlationId);
            return alertService;
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
                EnableUnityIntegration = true,
                EnableMetrics = true,
                EnableTelemetry = true,
                MaxConcurrentOperations = 50,
                MaxQueuedAlerts = 5000,
                StartupTimeout = TimeSpan.FromSeconds(30),
                ShutdownTimeout = TimeSpan.FromSeconds(10),
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                MetricsInterval = TimeSpan.FromSeconds(30),
                MaxMemoryUsageMB = 50,
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
                EnableUnityIntegration = false,
                EnableMetrics = true,
                EnableTelemetry = true,
                MaxConcurrentOperations = 200,
                MaxQueuedAlerts = 20000,
                StartupTimeout = TimeSpan.FromSeconds(60),
                ShutdownTimeout = TimeSpan.FromSeconds(30),
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                MetricsInterval = TimeSpan.FromSeconds(30),
                MaxMemoryUsageMB = 200,
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
        /// Simple creation only - assumes settings are pre-validated.
        /// </summary>
        public AlertServiceConfiguration CreateConfigurationFromSettings(Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Start with default settings
            var configBuilder = new Dictionary<string, object>(_defaultSettings);
            
            // Override with provided settings
            foreach (var setting in settings)
            {
                configBuilder[setting.Key] = setting.Value;
            }

            // Build configuration using safe type conversion
            var config = new AlertServiceConfiguration
            {
                Environment = GetValueOrDefault<AlertEnvironmentType>(configBuilder, "Environment", AlertEnvironmentType.Production),
                EnableUnityIntegration = GetValueOrDefault<bool>(configBuilder, "EnableUnityIntegration", true),
                EnableMetrics = GetValueOrDefault<bool>(configBuilder, "EnableMetrics", true),
                EnableTelemetry = GetValueOrDefault<bool>(configBuilder, "EnableTelemetry", true),
                MaxConcurrentOperations = GetValueOrDefault<int>(configBuilder, "MaxConcurrentOperations", 50),
                MaxQueuedAlerts = GetValueOrDefault<int>(configBuilder, "MaxQueuedAlerts", 1000),
                MaxMemoryUsageMB = GetValueOrDefault<int>(configBuilder, "MaxMemoryUsageMB", 100),
                StartupTimeout = GetValueOrDefault<TimeSpan>(configBuilder, "StartupTimeout", TimeSpan.FromSeconds(30)),
                ShutdownTimeout = GetValueOrDefault<TimeSpan>(configBuilder, "ShutdownTimeout", TimeSpan.FromSeconds(10)),
                HealthCheckInterval = GetValueOrDefault<TimeSpan>(configBuilder, "HealthCheckInterval", TimeSpan.FromMinutes(1)),
                MetricsInterval = GetValueOrDefault<TimeSpan>(configBuilder, "MetricsInterval", TimeSpan.FromSeconds(30))
            };

            LogInfo("Created configuration from settings");
            return config;
        }

        private T GetValueOrDefault<T>(Dictionary<string, object> settings, string key, T defaultValue)
        {
            if (settings.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }


        #endregion

        #region Private Helper Methods

        private async UniTask CreateAndRegisterChannels(IAlertService alertService, IReadOnlyList<ChannelConfig> channelConfigs, Guid correlationId)
        {
            // Convert Guid correlationId to FixedString64Bytes for interface compatibility
            var correlationIdString = correlationId == default ? default(FixedString64Bytes) : new FixedString64Bytes(correlationId.ToString());

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
                alertService.RegisterChannel(channel, correlationIdString);
            }
        }


        private Dictionary<string, object> CreateDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                ["Environment"] = AlertEnvironmentType.Production,
                ["EnableUnityIntegration"] = true,
                ["EnableMetrics"] = true,
                ["EnableTelemetry"] = true,
                ["MaxConcurrentOperations"] = 50,
                ["MaxQueuedAlerts"] = 1000,
                ["MaxMemoryUsageMB"] = 100
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