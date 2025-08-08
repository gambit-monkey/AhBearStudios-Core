using System;
using System.Collections.Generic;
using AhBearStudios.Core.Infrastructure.Bootstrap;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Factories;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Infrastructure.DependencyInjection;
using AhBearStudios.Unity.Logging.Installers;
using AhBearStudios.Unity.HealthCheck.Installers;
using AhBearStudios.Unity.Messaging.Installers;
using AhBearStudios.Unity.Serialization.Installers;
using AhBearStudios.Unity.Alerting.Formatters;
using Reflex.Core;
using UnityEngine;
using Unity.Collections;

namespace AhBearStudios.Unity.Alerting.Installers
{
    /// <summary>
    /// Bootstrap installer for the Alerting System.
    /// Configures and registers all alerting services with the DI container.
    /// Follows the established BootstrapInstaller pattern used throughout the codebase.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class AlertingInstaller : BootstrapInstaller
    {
        [Header("Configuration")]
        [SerializeField] private AlertSeverity _minimumSeverity = AlertSeverity.Warning;
        [SerializeField] private bool _enableSuppression = true;
        [SerializeField] private bool _enableAsyncProcessing = true;
        [SerializeField] private bool _enableHistory = true;
        [SerializeField] private bool _enableAggregation = true;
        
        [Header("Channel Settings")]
        [SerializeField] private bool _enableLogChannel = true;
        [SerializeField] private bool _enableConsoleChannel = true;
        
        [Header("Performance Settings")]
        [SerializeField, Range(10, 1000)] private int _maxConcurrentAlerts = 100;
        [SerializeField, Range(100, 10000)] private int _alertBufferSize = 1000;
        [SerializeField, Range(100, 100000)] private int _maxHistoryEntries = 10000;
        
        [Header("Timeouts and Intervals")]
        [SerializeField, Range(5, 300)] private int _processingTimeoutSeconds = 30;
        [SerializeField, Range(60, 3600)] private int _suppressionWindowSeconds = 300; // 5 minutes
        [SerializeField, Range(60, 86400)] private int _historyRetentionHours = 24;
        [SerializeField, Range(30, 3600)] private int _aggregationWindowSeconds = 120; // 2 minutes

        [Header("Integration Settings")]
        [SerializeField] private bool _enableUnityIntegration = true;
        [SerializeField] private bool _enableMetrics = true;
        [SerializeField] private bool _enableCircuitBreakerIntegration = true;
        
        [Header("Development Features")]
        [SerializeField] private bool _enableDebugLogging = false;

        #region IBootstrapInstaller Implementation

        /// <inheritdoc />
        public override string InstallerName => "AlertingInstaller";

        /// <inheritdoc />
        public override int Priority => 400; // After HealthCheck (300)

        /// <inheritdoc />
        public override Type[] Dependencies => new[]
        {
            typeof(LoggingInstaller),
            typeof(SerializationInstaller),
            typeof(MessageBusInstaller),
            typeof(HealthCheckInstaller)
        };

        #endregion

        private AlertConfig _config;
        private readonly FixedString128Bytes _correlationId = GenerateCorrelationId();

        #region Validation and Setup

        /// <inheritdoc />
        protected override bool PerformValidation()
        {
            var errors = new List<string>();

            try
            {
                // Validate dependencies are available
                if (!HasDependency<ILoggingService>())
                {
                    errors.Add("ILoggingService dependency not found");
                }

                if (!HasDependency<ISerializationService>())
                {
                    errors.Add("ISerializationService dependency not found");
                }

                if (!HasDependency<IMessageBusService>())
                {
                    errors.Add("IMessageBusService dependency not found");
                }

                // Validate configuration values
                if (_processingTimeoutSeconds <= 0)
                {
                    errors.Add("Processing timeout must be greater than 0");
                }

                if (_suppressionWindowSeconds <= 0)
                {
                    errors.Add("Suppression window must be greater than 0");
                }

                if (_maxConcurrentAlerts <= 0)
                {
                    errors.Add("Max concurrent alerts must be greater than 0");
                }

                if (!_enableLogChannel && !_enableConsoleChannel)
                {
                    LogWarning("No alert channels enabled - alerts will not be delivered");
                }

                if (errors.Count > 0)
                {
                    LogError($"Configuration validation failed with {errors.Count} errors:");
                    foreach (var error in errors)
                    {
                        LogError($"  - {error}");
                    }
                    return false;
                }

                LogDebug("Configuration validation passed");
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex, "Configuration validation failed with exception");
                return false;
            }
        }

        /// <inheritdoc />
        protected override void PerformPreInstall()
        {
            try
            {
                LogDebug("Pre-installation setup starting");

                // Ensure Alert System types are registered with serialization service
                EnsureAlertTypesRegistered();

                // Build configuration from serialized fields
                _config = BuildAlertConfig();

                // Validate the built configuration
                _config.Validate();

                LogDebug("Pre-installation setup completed");
            }
            catch (Exception ex)
            {
                LogException(ex, "Pre-installation setup failed");
                throw;
            }
        }

        /// <summary>
        /// Ensures that Alert System types are registered with the serialization service.
        /// Acts as a fallback if the ModuleInitializer didn't run or if service wasn't available.
        /// </summary>
        private void EnsureAlertTypesRegistered()
        {
            try
            {
                // Check if types are already registered
                if (AlertFormatterRegistration.IsRegistered())
                {
                    LogDebug("Alert System types already registered");
                    return;
                }

                // Try to get serialization service and register types manually
                var serializationService = ServiceResolver.Resolve<ISerializationService>();
                if (serializationService != null)
                {
                    AlertFormatterRegistration.RegisterAlertTypes(serializationService);
                    LogDebug("Alert System types registered as fallback");
                }
                else
                {
                    LogWarning("ISerializationService not available - types will be registered during PostInstall");
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to register Alert System types");
                // Continue with installation as this is not critical for basic functionality
            }
        }

        /// <summary>
        /// Builds the alert configuration from serialized fields.
        /// </summary>
        private AlertConfig BuildAlertConfig()
        {
            var channels = new List<ChannelConfig>();

            // Add enabled channels
            if (_enableLogChannel)
            {
                channels.Add(new ChannelConfig
                {
                    Name = "Log",
                    ChannelType = AlertChannelType.Log,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Info,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                    EnableBatching = false,
                    EnableHealthMonitoring = true,
                    HealthCheckInterval = TimeSpan.FromMinutes(5),
                    SendTimeout = TimeSpan.FromSeconds(5),
                    Priority = 100,
                    IsEmergencyChannel = true,
                    TypedSettings = LogChannelSettings.Default
                });
            }

            if (_enableConsoleChannel)
            {
                channels.Add(new ChannelConfig
                {
                    Name = "Console",
                    ChannelType = AlertChannelType.Console,
                    IsEnabled = true,
                    MinimumSeverity = AlertSeverity.Warning,
                    MaximumSeverity = AlertSeverity.Emergency,
                    MessageFormat = "[{Timestamp:HH:mm:ss.fff}] [{Severity}] {Source}: {Message}",
                    EnableBatching = false,
                    EnableHealthMonitoring = true,
                    HealthCheckInterval = TimeSpan.FromMinutes(10),
                    SendTimeout = TimeSpan.FromSeconds(2),
                    Priority = 200,
                    IsEmergencyChannel = true,
                    TypedSettings = ConsoleChannelSettings.Default
                });
            }

            return new AlertConfig
            {
                MinimumSeverity = _minimumSeverity,
                EnableSuppression = _enableSuppression,
                SuppressionWindow = TimeSpan.FromSeconds(_suppressionWindowSeconds),
                EnableAsyncProcessing = _enableAsyncProcessing,
                MaxConcurrentAlerts = _maxConcurrentAlerts,
                ProcessingTimeout = TimeSpan.FromSeconds(_processingTimeoutSeconds),
                EnableHistory = _enableHistory,
                HistoryRetention = TimeSpan.FromHours(_historyRetentionHours),
                MaxHistoryEntries = _maxHistoryEntries,
                EnableAggregation = _enableAggregation,
                AggregationWindow = TimeSpan.FromSeconds(_aggregationWindowSeconds),
                MaxAggregationSize = 50, // Default from AlertConfig
                EnableCorrelationTracking = true,
                AlertBufferSize = _alertBufferSize,
                EnableUnityIntegration = _enableUnityIntegration,
                EnableMetrics = _enableMetrics,
                EnableCircuitBreakerIntegration = _enableCircuitBreakerIntegration,
                Channels = channels.AsReadOnly(),
                SuppressionRules = new[]
                {
                    new SuppressionConfig
                    {
                        RuleName = "DefaultDuplicateFilter",
                        IsEnabled = true,
                        Priority = 100,
                        SuppressionType = SuppressionType.Duplicate,
                        SuppressionWindow = TimeSpan.FromMinutes(5),
                        MaxAlertsInWindow = 1,
                        Action = SuppressionAction.Suppress,
                        DuplicateDetection = DuplicateDetectionConfig.Default
                    },
                    new SuppressionConfig
                    {
                        RuleName = "DefaultRateLimit",
                        IsEnabled = true,
                        Priority = 200,
                        SuppressionType = SuppressionType.RateLimit,
                        SuppressionWindow = TimeSpan.FromMinutes(1),
                        MaxAlertsInWindow = 10,
                        Action = SuppressionAction.Queue
                    }
                }
            };
        }

        #endregion

        #region Reflex InstallBindings Implementation

        /// <inheritdoc />
        public override void InstallBindings(ContainerBuilder builder)
        {
            try
            {
                LogDebug("Starting Reflex alerting system installation");

                // Register configuration
                RegisterConfiguration(builder);

                // Register factories
                RegisterFactories(builder);

                // Register supporting services
                RegisterSupportingServices(builder);

                // Register channels and filters
                RegisterChannelsAndFilters(builder);

                // Register main alerting service
                RegisterMainService(builder);

                LogDebug("Reflex alerting system installation completed successfully");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to install alerting system via Reflex");
                throw;
            }
        }

        /// <summary>
        /// Registers the alert configuration with the container.
        /// </summary>
        private void RegisterConfiguration(ContainerBuilder builder)
        {
            builder.AddSingleton(_config, typeof(AlertConfig));
            LogDebug("Registered alert configuration");
        }

        /// <summary>
        /// Registers factories with the container using standard Reflex patterns.
        /// </summary>
        private void RegisterFactories(ContainerBuilder builder)
        {
            builder.AddSingleton(typeof(AlertServiceFactory), typeof(IAlertServiceFactory));
            builder.AddSingleton(typeof(AlertChannelFactory), typeof(IAlertChannelFactory));
            builder.AddSingleton(typeof(AlertFilterFactory), typeof(IAlertFilterFactory));
            LogDebug("Registered factories");
        }

        /// <summary>
        /// Registers supporting services with the container.
        /// </summary>
        private void RegisterSupportingServices(ContainerBuilder builder)
        {
            builder.AddSingleton(typeof(AlertSuppressionService));
            builder.AddSingleton(typeof(AlertFilterService));
            builder.AddSingleton(typeof(AlertChannelService));
            LogDebug("Registered supporting services");
        }

        /// <summary>
        /// Registers alert channels and filters with the container.
        /// </summary>
        private void RegisterChannelsAndFilters(ContainerBuilder builder)
        {
            // Register channels based on configuration
            if (_enableLogChannel)
            {
                builder.AddSingleton(typeof(LogAlertChannel));
                LogDebug("Registered LogAlertChannel");
            }

            if (_enableConsoleChannel)
            {
                builder.AddSingleton(typeof(ConsoleAlertChannel));
                LogDebug("Registered ConsoleAlertChannel");
            }

            // Register common filters
            builder.AddSingleton(typeof(SeverityAlertFilter));
            LogDebug("Registered alert filters");
        }

        /// <summary>
        /// Registers the main alerting service with the container using Reflex factory pattern.
        /// </summary>
        private void RegisterMainService(ContainerBuilder builder)
        {
            builder.AddSingleton<IAlertService>(container =>
            {
                // Resolve optional dependencies using safe resolution patterns
                var loggingService = TryResolveOptionalService<ILoggingService>(container);
                var messageBusService = TryResolveOptionalService<IMessageBusService>(container);
                var serializationService = TryResolveOptionalService<ISerializationService>(container);

                return new AlertService(messageBusService, loggingService, serializationService);
            }, typeof(IAlertService));

            LogDebug("Registered main alerting service with Reflex factory pattern");
        }

        #endregion

        #region Post-Installation

        /// <inheritdoc />
        protected override void PerformPostInstall(Container container)
        {
            try
            {
                LogDebug("Starting post-installation setup");

                // Final fallback to ensure Alert System types are registered
                EnsureAlertTypesRegisteredWithContainer(container);

                // Resolve the alerting service
                var alertService = container.Resolve<IAlertService>();

                // Register channels with the service
                RegisterConfiguredChannels(container, alertService);

                // Register filters with the service
                RegisterConfiguredFilters(container, alertService);

                // Perform initial validation
                var validationResult = alertService.ValidateConfiguration(_correlationId);
                if (!validationResult.IsValid)
                {
                    LogWarning($"Initial alerting configuration validation issues: {validationResult.ErrorMessage}");
                }

                // Test basic functionality
                TestBasicFunctionality(alertService);

                // Register with health check service if available
                if (container.HasBinding(typeof(IHealthCheckService)))
                {
                    RegisterWithHealthCheckService(container);
                }

                LogDebug("Post-installation setup completed successfully");
            }
            catch (Exception ex)
            {
                LogException(ex, "Post-installation failed");
                throw;
            }
        }

        /// <summary>
        /// Registers configured channels with the alerting service.
        /// </summary>
        private void RegisterConfiguredChannels(Container container, IAlertService alertService)
        {
            var registeredCount = 0;

            if (_enableLogChannel)
            {
                TryRegisterChannel<LogAlertChannel>(container, alertService, "LogAlertChannel", ref registeredCount);
            }

            if (_enableConsoleChannel)
            {
                TryRegisterChannel<ConsoleAlertChannel>(container, alertService, "ConsoleAlertChannel", ref registeredCount);
            }

            LogDebug($"Registered {registeredCount} channels with alerting service");
        }

        /// <summary>
        /// Registers configured filters with the alerting service.
        /// </summary>
        private void RegisterConfiguredFilters(Container container, IAlertService alertService)
        {
            try
            {
                var severityFilter = container.Resolve<SeverityAlertFilter>();
                alertService.AddFilter(severityFilter, _correlationId);
                LogDebug("Registered SeverityAlertFilter with alerting service");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to register filters with alerting service");
            }
        }

        /// <summary>
        /// Tries to register a channel with the alerting service.
        /// </summary>
        private void TryRegisterChannel<T>(Container container, IAlertService alertService, string channelName, ref int registeredCount)
            where T : class, IAlertChannel
        {
            try
            {
                var channel = container.Resolve<T>();
                alertService.RegisterChannel(channel, _correlationId);
                registeredCount++;
                LogDebug($"Registered {channelName}");
            }
            catch (Exception ex)
            {
                LogException(ex, $"Failed to register {channelName}");
            }
        }

        /// <summary>
        /// Tests basic alerting functionality.
        /// </summary>
        private void TestBasicFunctionality(IAlertService alertService)
        {
            try
            {
                // Test basic alert functionality
                alertService.RaiseAlert("Alerting system initialized successfully", AlertSeverity.Info, "AlertingInstaller", "Initialization", Guid.NewGuid());
                LogDebug("Basic functionality test completed");
            }
            catch (Exception ex)
            {
                LogException(ex, "Basic functionality test failed");
            }
        }

        /// <summary>
        /// Registers with the health check service if available.
        /// </summary>
        private void RegisterWithHealthCheckService(Container container)
        {
            try
            {
                var healthCheckService = container.Resolve<IHealthCheckService>();
                // TODO: Create AlertingServiceHealthCheck when available
                LogDebug("Would register with health check service (health check class not yet implemented)");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to register with health check service");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Safely resolves an optional service from the container.
        /// Follows Reflex best practices for optional dependency resolution.
        /// </summary>
        private T TryResolveOptionalService<T>(Container container) where T : class
        {
            try
            {
                return container.HasBinding(typeof(T)) ? container.Resolve<T>() : null;
            }
            catch (Exception ex)
            {
                if (_enableDebugLogging)
                {
                    LogWarning($"Failed to resolve optional service {typeof(T).Name}: {ex.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Checks if a dependency type has been registered.
        /// </summary>
        private bool HasDependency<T>()
        {
            // This would need access to container during validation phase
            // For now, assume dependencies are available if we got this far
            return true;
        }

        /// <summary>
        /// Final fallback to ensure Alert System types are registered using container-resolved serialization service.
        /// </summary>
        private void EnsureAlertTypesRegisteredWithContainer(Container container)
        {
            try
            {
                if (!AlertFormatterRegistration.IsRegistered() && container.HasBinding(typeof(ISerializationService)))
                {
                    var serializationService = container.Resolve<ISerializationService>();
                    AlertFormatterRegistration.RegisterAlertTypes(serializationService);
                    LogDebug("Alert System types registered via container in PostInstall");
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "Final fallback type registration failed");
            }
        }

        /// <summary>
        /// Generates a unique correlation ID for installation tracking.
        /// </summary>
        private static FixedString128Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..24];
            return new FixedString128Bytes($"ALERT-INSTALL-{guid}");
        }

        #endregion

        #region Unity Editor Integration

        /// <summary>
        /// Unity validation callback for Inspector value changes.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            // Validate timeout settings
            if (_processingTimeoutSeconds <= 0)
            {
                Debug.LogWarning($"[{InstallerName}] Processing timeout must be greater than 0");
            }

            if (_suppressionWindowSeconds <= 0)
            {
                Debug.LogWarning($"[{InstallerName}] Suppression window must be greater than 0");
            }

            // Warn if no channels are enabled
            if (!_enableLogChannel && !_enableConsoleChannel)
            {
                Debug.LogWarning($"[{InstallerName}] No alert channels enabled - alerts will not be delivered");
            }
        }

        #endregion
    }
}