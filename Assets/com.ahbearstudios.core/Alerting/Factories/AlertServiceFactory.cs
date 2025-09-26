using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
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
using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Refactored factory for creating AlertService instances with decomposed service architecture.
    /// Creates and coordinates AlertOrchestrationService, AlertStateManagementService, and
    /// AlertHealthMonitoringService according to CLAUDE.md guidelines.
    /// Factory focuses on creation only - complexity is handled by builders, validation by validators.
    /// Does not manage object lifecycle - caller responsibility.
    /// </summary>
    public sealed class AlertServiceFactory : IAlertServiceFactory
    {
        #region Dependencies

        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IPoolingService _poolingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IProfilerService _profilerService;

        // New service factories for decomposed architecture
        private readonly AlertStateManagementServiceFactory _stateManagementServiceFactory;
        private readonly AlertHealthMonitoringServiceFactory _healthMonitoringServiceFactory;
        private readonly AlertOrchestrationServiceFactory _orchestrationServiceFactory;

        private readonly Dictionary<string, object> _defaultSettings;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertServiceFactory with all required dependencies.
        /// </summary>
        /// <param name="loggingService">Logging service for factory operations</param>
        /// <param name="messageBusService">Message bus service for service communication</param>
        /// <param name="serializationService">Serialization service for alert data serialization</param>
        /// <param name="poolingService">Pooling service for temporary allocations</param>
        /// <param name="healthCheckService">Health check service for monitoring</param>
        /// <param name="profilerService">Profiling service for performance monitoring</param>
        public AlertServiceFactory(
            ILoggingService loggingService = null,
            IMessageBusService messageBusService = null,
            ISerializationService serializationService = null,
            IPoolingService poolingService = null,
            IHealthCheckService healthCheckService = null,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService;
            _messageBusService = messageBusService;
            _serializationService = serializationService;
            _poolingService = poolingService;
            _healthCheckService = healthCheckService;
            _profilerService = profilerService;

            // Create service factories for decomposed architecture
            _stateManagementServiceFactory = new AlertStateManagementServiceFactory(
                _loggingService,
                _messageBusService,
                _poolingService,
                _serializationService,
                _profilerService);

            _healthMonitoringServiceFactory = new AlertHealthMonitoringServiceFactory(
                _loggingService,
                _messageBusService,
                _healthCheckService,
                _profilerService);

            _orchestrationServiceFactory = new AlertOrchestrationServiceFactory(
                _loggingService,
                _messageBusService,
                _profilerService);

            _defaultSettings = CreateDefaultSettings();
        }

        #endregion

        #region IAlertServiceFactory Implementation


        /// <summary>
        /// Creates a new AlertService instance with specific configuration using decomposed services.
        /// </summary>
        public async UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default)
        {
            using (_profilerService?.BeginScope("AlertServiceFactory.CreateAlertServiceAsync"))
            {
                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                var finalCorrelationId = correlationId == default
                    ? DeterministicIdGenerator.GenerateCorrelationId("AlertServiceFactory.CreateAlertServiceAsync", configuration.Environment.ToString())
                    : correlationId;

                try
                {
                    _loggingService?.LogInfo(
                        "Creating AlertService with decomposed architecture for {Environment} environment",
                        configuration.Environment,
                        correlationId: finalCorrelationId);

                    // Create decomposed services
                    var stateManagementService = await _stateManagementServiceFactory.CreateAlertStateManagementServiceAsync(configuration.AlertConfig);
                    var healthMonitoringService = await _healthMonitoringServiceFactory.CreateAlertHealthMonitoringServiceAsync(configuration.AlertConfig);
                    var orchestrationService = await _orchestrationServiceFactory.CreateAlertOrchestrationServiceAsync(
                        stateManagementService,
                        healthMonitoringService,
                        configuration.AlertConfig);

                    // Create subsystem services (legacy compatibility)
                    var channelService = await CreateChannelServiceAsync(configuration.AlertConfig, finalCorrelationId);
                    var filterService = CreateFilterService(configuration.AlertConfig, finalCorrelationId);
                    var suppressionService = CreateSuppressionService(configuration.AlertConfig, finalCorrelationId);

                    // Create main AlertService with decomposed services
                    var alertService = new AlertService(
                        orchestrationService,
                        stateManagementService,
                        healthMonitoringService,
                        channelService,
                        filterService,
                        suppressionService,
                        configuration,
                        _messageBusService,
                        _loggingService,
                        _serializationService,
                        _poolingService,
                        _profilerService);

                    _loggingService?.LogInfo(
                        "AlertService created successfully with decomposed architecture",
                        correlationId: finalCorrelationId);

                    return alertService;
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(
                        ex,
                        "Failed to create AlertService: {ErrorMessage}",
                        ex.Message,
                        correlationId: finalCorrelationId);
                    throw;
                }
            }
        }


        /// <summary>
        /// Gets the default configuration for alert services.
        /// </summary>
        public AlertServiceConfiguration GetDefaultConfiguration()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Production,
                EnableUnityIntegration = true,
                EnableMetrics = true,
                EnableTelemetry = true,
                MaxConcurrentOperations = 50,
                MaxQueuedAlerts = 1000,
                StartupTimeout = TimeSpan.FromSeconds(30),
                ShutdownTimeout = TimeSpan.FromSeconds(10),
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                MetricsInterval = TimeSpan.FromSeconds(30),
                MaxMemoryUsageMB = 100,
                AlertConfig = CreateDefaultAlertConfig()
            };
        }

        /// <summary>
        /// Creates a pre-configured AlertService for development environments.
        /// </summary>
        public async UniTask<IAlertService> CreateDevelopmentAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService = null)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertServiceFactory.CreateDevelopmentAlertServiceAsync", "Development");
            var config = GetDevelopmentConfiguration();
            return await CreateAlertServiceAsync(config, correlationId);
        }

        /// <summary>
        /// Creates a pre-configured AlertService for production environments.
        /// </summary>
        public async UniTask<IAlertService> CreateProductionAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertServiceFactory.CreateProductionAlertServiceAsync", "Production");
            var config = GetProductionConfiguration();
            return await CreateAlertServiceAsync(config, correlationId);
        }

        /// <summary>
        /// Creates a minimal AlertService for testing scenarios.
        /// </summary>
        public async UniTask<IAlertService> CreateTestAlertServiceAsync(IMessageBusService messageBusService = null)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertServiceFactory.CreateTestAlertServiceAsync", "Test");
            var config = GetTestConfiguration();
            return await CreateAlertServiceAsync(config, correlationId);
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
                AlertConfig = CreateDevelopmentAlertConfig()
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
                AlertConfig = CreateProductionAlertConfig()
            };
        }

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        public AlertServiceConfiguration CreateConfigurationFromSettings(Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var configBuilder = new Dictionary<string, object>(_defaultSettings);
            foreach (var setting in settings)
            {
                configBuilder[setting.Key] = setting.Value;
            }

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
                MetricsInterval = GetValueOrDefault<TimeSpan>(configBuilder, "MetricsInterval", TimeSpan.FromSeconds(30)),
                AlertConfig = CreateDefaultAlertConfig()
            };

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AlertServiceFactory.CreateConfigurationFromSettings", "ConfigFromSettings");
            _loggingService?.LogInfo("Created configuration from settings", correlationId: correlationId);
            return config;
        }

        #endregion


        #region Private Helper Methods

        /// <summary>
        /// Creates a channel service for the alert system.
        /// </summary>
        private async UniTask<IAlertChannelService> CreateChannelServiceAsync(AlertConfig config, Guid correlationId)
        {
            var channelServiceConfig = new AlertChannelServiceConfig
            {
                MaxChannels = 10,
                EnableMetrics = config.EnableMetrics,
                DefaultTimeout = TimeSpan.FromSeconds(30)
            };

            return new AlertChannelService(channelServiceConfig, _loggingService, _messageBusService);
        }

        /// <summary>
        /// Creates a filter service for the alert system.
        /// </summary>
        private IAlertFilterService CreateFilterService(AlertConfig config, Guid correlationId)
        {
            return new AlertFilterService(_loggingService);
        }

        /// <summary>
        /// Creates a suppression service for the alert system.
        /// </summary>
        private IAlertSuppressionService CreateSuppressionService(AlertConfig config, Guid correlationId)
        {
            return new AlertSuppressionService(_loggingService);
        }

        /// <summary>
        /// Creates default alert configuration.
        /// </summary>
        private AlertConfig CreateDefaultAlertConfig()
        {
            return new AlertConfig
            {
                MinimumSeverity = AlertSeverity.Info,
                EnableSuppression = true,
                SuppressionWindow = TimeSpan.FromMinutes(1),
                EnableAsyncProcessing = true,
                MaxConcurrentAlerts = 50,
                ProcessingTimeout = TimeSpan.FromSeconds(30),
                EnableHistory = true,
                HistoryRetentionHours = 24,
                MaxHistoryEntries = 10000,
                EnableAggregation = false,
                EnableCorrelationTracking = true,
                AlertBufferSize = 1000,
                EnableUnityIntegration = true,
                EnableMetrics = true,
                EnableCircuitBreakerIntegration = true,
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                HealthCheckTimeout = TimeSpan.FromSeconds(10),
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerRecoveryTimeout = TimeSpan.FromMinutes(1),
                EmergencyModeThreshold = 10,
                StatisticsUpdateInterval = TimeSpan.FromMinutes(1),
                Channels = new List<ChannelConfig>(),
                Filters = new List<FilterConfig>()
            };
        }

        /// <summary>
        /// Creates alert configuration optimized for development.
        /// </summary>
        private AlertConfig CreateDevelopmentAlertConfig()
        {
            var config = CreateDefaultAlertConfig();
            config.MinimumSeverity = AlertSeverity.Debug;
            config.EnableAsyncProcessing = false;
            config.ProcessingTimeout = TimeSpan.FromSeconds(10);
            config.HistoryRetentionHours = 8;
            config.MaxHistoryEntries = 5000;
            config.AlertBufferSize = 500;
            config.EnableCircuitBreakerIntegration = false;
            return config;
        }

        /// <summary>
        /// Creates alert configuration optimized for production.
        /// </summary>
        private AlertConfig CreateProductionAlertConfig()
        {
            var config = CreateDefaultAlertConfig();
            config.MinimumSeverity = AlertSeverity.Warning;
            config.SuppressionWindow = TimeSpan.FromMinutes(5);
            config.MaxConcurrentAlerts = 200;
            config.ProcessingTimeout = TimeSpan.FromSeconds(30);
            config.HistoryRetentionHours = 48;
            config.MaxHistoryEntries = 20000;
            config.EnableAggregation = true;
            config.AlertBufferSize = 2000;
            config.EnableUnityIntegration = false;
            return config;
        }

        /// <summary>
        /// Creates alert configuration optimized for testing.
        /// </summary>
        private AlertConfig CreateTestAlertConfig()
        {
            var config = CreateDefaultAlertConfig();
            config.MinimumSeverity = AlertSeverity.Debug;
            config.EnableSuppression = false;
            config.EnableAsyncProcessing = false;
            config.ProcessingTimeout = TimeSpan.FromSeconds(5);
            config.EnableHistory = false;
            config.MaxHistoryEntries = 100;
            config.EnableAggregation = false;
            config.AlertBufferSize = 50;
            config.EnableCircuitBreakerIntegration = false;
            config.EmergencyModeThreshold = 50; // Higher threshold for tests
            return config;
        }

        /// <summary>
        /// Gets test-specific configuration.
        /// </summary>
        private AlertServiceConfiguration GetTestConfiguration()
        {
            return new AlertServiceConfiguration
            {
                Environment = AlertEnvironmentType.Testing,
                EnableUnityIntegration = false,
                EnableMetrics = false,
                EnableTelemetry = false,
                MaxConcurrentOperations = 10,
                MaxQueuedAlerts = 100,
                StartupTimeout = TimeSpan.FromSeconds(5),
                ShutdownTimeout = TimeSpan.FromSeconds(2),
                HealthCheckInterval = TimeSpan.FromMinutes(5),
                MetricsInterval = TimeSpan.FromMinutes(1),
                MaxMemoryUsageMB = 10,
                AlertConfig = CreateTestAlertConfig()
            };
        }

        /// <summary>
        /// Creates default settings dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a value from settings dictionary with type safety and defaults.
        /// </summary>
        private T GetValueOrDefault<T>(Dictionary<string, object> settings, string key, T defaultValue)
        {
            if (settings.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        #endregion
    }
}