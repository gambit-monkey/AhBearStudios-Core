using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Infrastructure.Bootstrap;
using AhBearStudios.Core.HealthChecking.Builders;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.HealthChecking.HealthChecks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.HealthChecks;
using AhBearStudios.Unity.Logging.Installers;
using AhBearStudios.Unity.Messaging.Installers;
using Reflex.Core;
using Unity.Collections;

namespace AhBearStudios.Unity.HealthCheck.Installers
{
    /// <summary>
    /// Production-ready Reflex installer for the HealthChecking system with Unity integration.
    /// Registers all health checking services, components, and Unity-specific implementations.
    /// Follows AhBearStudios Core Development Guidelines for enterprise-grade reliability.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthCheckInstaller", menuName = "AhBearStudios/Installers/HealthCheck Installer")]
    public sealed class HealthCheckInstaller : ScriptableObjectInstaller, IBootstrapInstaller
    {
        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private HealthCheckConfigAsset _healthCheckConfig;
        [SerializeField] private bool _enableAutomaticHealthChecks = true;
        [SerializeField] private bool _enableCircuitBreakers = true;
        [SerializeField] private bool _enableGracefulDegradation = true;

        [Header("Health Check Intervals")]
        [SerializeField, Range(5f, 300f)] private float _defaultHealthCheckInterval = 60f;
        [SerializeField, Range(1f, 60f)] private float _criticalSystemInterval = 30f;
        [SerializeField, Range(1f, 30f)] private float _selfMonitoringInterval = 15f;

        [Header("Circuit Breaker Settings")]
        [SerializeField, Range(1, 20)] private int _defaultFailureThreshold = 5;
        [SerializeField, Range(5f, 300f)] private float _defaultOpenTimeoutSeconds = 30f;
        [SerializeField, Range(1, 10)] private int _defaultSuccessThreshold = 2;

        [Header("Degradation Thresholds")]
        [SerializeField, Range(0.1f, 1f)] private float _minorDegradationThreshold = 0.10f;
        [SerializeField, Range(0.1f, 1f)] private float _moderateDegradationThreshold = 0.25f;
        [SerializeField, Range(0.1f, 1f)] private float _severeDegradationThreshold = 0.50f;
        [SerializeField, Range(0.1f, 1f)] private float _disableDegradationThreshold = 0.75f;

        [Header("Unity Integration")]
        [SerializeField] private bool _enableUnityHealthChecks = true;
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableMemoryMonitoring = true;
        [SerializeField] private bool _logHealthEvents = true;

        [Header("Development Features")]
        [SerializeField] private bool _enableDebugMode = false;
        [SerializeField] private bool _validateOnStart = true;
        [SerializeField] private bool _enableHealthCheckHistory = true;

        #endregion

        #region Private Fields

        private readonly FixedString128Bytes _correlationId = GenerateCorrelationId();
        private HealthCheckServiceConfig _runtimeConfig;
        private ILoggingService _logger;

        #endregion

        #region IBootstrapInstaller Properties

        /// <inheritdoc />
        public string InstallerName => "HealthCheckInstaller";

        /// <inheritdoc />
        public int Priority => 300; // After logging (100) and messaging (200)

        /// <inheritdoc />
        public bool IsEnabled { get; private set; } = true;

        /// <inheritdoc />
        public Type[] Dependencies => new[]
        {
            typeof(LoggingInstaller),
            typeof(MessageBusInstaller)
        };

        #endregion

        #region ScriptableObjectInstaller Implementation

        /// <summary>
        /// Installs and registers all health checking components with the Reflex container
        /// </summary>
        /// <param name="builder">The container builder for service registration</param>
        public override void InstallBindings(ContainerBuilder builder)
        {
            if (!IsEnabled)
            {
                Debug.LogWarning($"[{InstallerName}] Installer is disabled, skipping installation");
                return;
            }

            try
            {
                // Resolve dependencies first
                _logger = Container.Resolve<ILoggingService>();
                
                _logger?.LogInfo($"[{InstallerName}] Starting health check system installation", _correlationId);

                // Build runtime configuration
                BuildRuntimeConfiguration();

                // Register core configurations
                RegisterConfigurations(builder);

                // Register core services
                RegisterCoreServices(builder);

                // Register health check factories
                RegisterFactories(builder);

                // Register supporting services
                RegisterSupportingServices(builder);

                // Register standard health checks
                RegisterStandardHealthChecks(builder);

                // Register Unity-specific health checks
                RegisterUnityHealthChecks(builder);

                // Register Unity components
                RegisterUnityComponents(builder);

                _logger?.LogInfo($"[{InstallerName}] Health check system installation completed successfully", _correlationId);
            }
            catch (Exception ex)
            {
                var errorMessage = $"[{InstallerName}] Critical error during health check system installation: {ex.Message}";
                Debug.LogError(errorMessage);
                _logger?.LogException(ex, "Failed to install health check system", _correlationId);
                
                IsEnabled = false;
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        #endregion

        #region IBootstrapInstaller Implementation

        /// <summary>
        /// Validates that all required dependencies are available
        /// </summary>
        /// <returns>True if all dependencies are satisfied</returns>
        public bool ValidateInstaller()
        {
            try
            {
                // Validate core dependencies
                if (!Container.HasBinding<ILoggingService>())
                {
                    Debug.LogError($"[{InstallerName}] ILoggingService dependency not found");
                    return false;
                }

                if (!Container.HasBinding<IMessageBusService>())
                {
                    Debug.LogError($"[{InstallerName}] IMessageBusService dependency not found");
                    return false;
                }

                // Validate configuration
                if (_healthCheckConfig == null && _validateOnStart)
                {
                    Debug.LogWarning($"[{InstallerName}] No health check configuration asset assigned, using defaults");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{InstallerName}] Validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pre-installation setup and validation
        /// </summary>
        public void PreInstall()
        {
            try
            {
                _logger = Container.Resolve<ILoggingService>();
                _logger?.LogInfo($"[{InstallerName}] Pre-installation setup starting", _correlationId);

                // Validate configuration consistency
                ValidateConfigurationConsistency();

                // Log installation parameters
                LogInstallationParameters();

                _logger?.LogInfo($"[{InstallerName}] Pre-installation setup completed", _correlationId);
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Pre-installation failed", _correlationId);
                throw;
            }
        }

        /// <summary>
        /// Post-installation configuration and health check registration
        /// </summary>
        public void PostInstall()
        {
            try
            {
                _logger?.LogInfo($"[{InstallerName}] Post-installation setup starting", _correlationId);

                // Get the installed health check service
                var healthCheckService = Container.Resolve<IHealthCheckService>();
                var alertService = Container.Resolve<IAlertService>();

                // Register self-monitoring health check
                RegisterSelfMonitoringHealthCheck(healthCheckService);

                // Configure automatic health checks
                ConfigureAutomaticHealthChecks(healthCheckService);

                // Setup health event handlers
                SetupHealthEventHandlers(healthCheckService, alertService);

                // Start automatic health monitoring if enabled
                if (_enableAutomaticHealthChecks)
                {
                    healthCheckService.StartAutomaticChecks();
                    _logger?.LogInfo("Automatic health checks started", _correlationId);
                }

                // Perform initial health check
                PerformInitialHealthCheck(healthCheckService);

                _logger?.LogInfo($"[{InstallerName}] Post-installation setup completed successfully", _correlationId);
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Post-installation failed", _correlationId);
                throw;
            }
        }

        #endregion

        #region Private Registration Methods

        /// <summary>
        /// Builds the runtime configuration from serialized fields and configuration asset
        /// </summary>
        private void BuildRuntimeConfiguration()
        {
            var configBuilder = new HealthCheckServiceConfigBuilder();

            // Apply configuration asset if available
            if (_healthCheckConfig != null)
            {
                configBuilder
                    .WithDefaultHealthCheckInterval(_healthCheckConfig.DefaultHealthCheckInterval)
                    .WithDefaultTimeout(_healthCheckConfig.DefaultTimeout)
                    .WithHistoryRetention(_healthCheckConfig.HistoryRetention);
            }
            else
            {
                // Use serialized field defaults
                configBuilder
                    .WithDefaultHealthCheckInterval(TimeSpan.FromSeconds(_defaultHealthCheckInterval))
                    .WithDefaultTimeout(TimeSpan.FromSeconds(30))
                    .WithHistoryRetention(TimeSpan.FromHours(24));
            }

            // Apply serialized settings
            configBuilder
                .WithAutomaticChecks(_enableAutomaticHealthChecks)
                .WithHistoryEnabled(_enableHealthCheckHistory);

            // Configure circuit breakers
            if (_enableCircuitBreakers)
            {
                configBuilder.WithCircuitBreakers(cb => cb
                    .WithDefaultFailureThreshold(_defaultFailureThreshold)
                    .WithDefaultOpenTimeout(TimeSpan.FromSeconds(_defaultOpenTimeoutSeconds))
                    .WithDefaultSuccessThreshold(_defaultSuccessThreshold)
                    .WithAutoHealthCheckIntegration(true));
            }

            // Configure graceful degradation
            if (_enableGracefulDegradation)
            {
                configBuilder.WithGracefulDegradation(gd => gd
                    .WithThresholds(
                        _minorDegradationThreshold,
                        _moderateDegradationThreshold,
                        _severeDegradationThreshold,
                        _disableDegradationThreshold)
                    .WithAutomaticDegradation(true)
                    .WithRecoveryMonitoring(true));
            }

            // Configure alerting
            configBuilder.WithAlerting(alert => alert
                .EnableAlerts(true)
                .WithAlertThreshold(consecutiveFailures: 3)
                .WithCircuitBreakerAlerts(_enableCircuitBreakers)
                .WithDegradationAlerts(_enableGracefulDegradation));

            _runtimeConfig = configBuilder.Build();
        }

        /// <summary>
        /// Registers all configuration objects
        /// </summary>
        private void RegisterConfigurations(ContainerBuilder builder)
        {
            builder.Bind<HealthCheckServiceConfig>().FromInstance(_runtimeConfig);
            
            if (_healthCheckConfig != null)
            {
                builder.Bind<HealthCheckConfigAsset>().FromInstance(_healthCheckConfig);
            }
        }

        /// <summary>
        /// Registers core health checking services
        /// </summary>
        private void RegisterCoreServices(ContainerBuilder builder)
        {
            // Core health check service
            builder.Bind<IHealthCheckService>().To<HealthCheckService>().AsSingle();

            // Circuit breaker implementation
            builder.Bind<ICircuitBreaker>().To<CircuitBreaker>().AsTransient();
        }

        /// <summary>
        /// Registers health check and circuit breaker factories
        /// </summary>
        private void RegisterFactories(ContainerBuilder builder)
        {
            builder.Bind<IHealthCheckServiceFactory>().To<HealthCheckServiceFactory>().AsSingle();
            builder.Bind<IHealthCheckFactory>().To<HealthCheckFactory>().AsSingle();
            builder.Bind<CircuitBreakerFactory>().To<CircuitBreakerFactory>().AsSingle();
        }

        /// <summary>
        /// Registers supporting services for health monitoring
        /// </summary>
        private void RegisterSupportingServices(ContainerBuilder builder)
        {
            builder.Bind<HealthAggregationService>().To<HealthAggregationService>().AsSingle();
            builder.Bind<HealthHistoryService>().To<HealthHistoryService>().AsSingle();
            builder.Bind<DegradationService>().To<DegradationService>().AsSingle();
            builder.Bind<HealthSchedulingService>().To<HealthSchedulingService>().AsSingle();
        }

        /// <summary>
        /// Registers standard cross-platform health checks
        /// </summary>
        private void RegisterStandardHealthChecks(ContainerBuilder builder)
        {
            // System resource health check
            builder.Bind<IHealthCheck>().To<SystemResourceHealthCheck>().AsSingle().WithId("SystemResource");
            
            // Message bus health check (if messaging is available)
            if (Container.HasBinding<IMessageBusService>())
            {
                builder.Bind<IHealthCheck>().To<MessageBusHealthCheck>().AsSingle().WithId("MessageBus");
            }

            // Self-monitoring health check
            builder.Bind<HealthCheckServiceHealthCheck>().To<HealthCheckServiceHealthCheck>().AsSingle();
        }

        /// <summary>
        /// Registers Unity-specific health checks
        /// </summary>
        private void RegisterUnityHealthChecks(ContainerBuilder builder)
        {
            if (_enableUnityHealthChecks)
            {
                builder.Bind<IHealthCheck>().To<UnitySystemHealthCheck>().AsSingle().WithId("UnitySystem");
                
                if (_enablePerformanceMonitoring)
                {
                    builder.Bind<IHealthCheck>().To<UnityPerformanceHealthCheck>().AsSingle().WithId("UnityPerformance");
                }
                
                if (_enableMemoryMonitoring)
                {
                    builder.Bind<IHealthCheck>().To<UnityMemoryHealthCheck>().AsSingle().WithId("UnityMemory");
                }
            }
        }

        /// <summary>
        /// Registers Unity UI and display components
        /// </summary>
        private void RegisterUnityComponents(ContainerBuilder builder)
        {
            // Register display components for DI
            builder.Bind<HealthCheckDisplayComponent>().AsTransient();
            builder.Bind<CircuitBreakerDisplayComponent>().AsTransient();
        }

        #endregion

        #region Private Configuration Methods

        /// <summary>
        /// Validates configuration consistency and logs warnings for potential issues
        /// </summary>
        private void ValidateConfigurationConsistency()
        {
            if (_enableCircuitBreakers && _defaultFailureThreshold <= 0)
            {
                Debug.LogWarning($"[{InstallerName}] Circuit breaker failure threshold should be > 0, got {_defaultFailureThreshold}");
            }

            if (_enableGracefulDegradation)
            {
                var thresholds = new[] { _minorDegradationThreshold, _moderateDegradationThreshold, _severeDegradationThreshold, _disableDegradationThreshold };
                for (int i = 1; i < thresholds.Length; i++)
                {
                    if (thresholds[i] <= thresholds[i - 1])
                    {
                        Debug.LogWarning($"[{InstallerName}] Degradation thresholds should be in ascending order");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Logs installation parameters for debugging and monitoring
        /// </summary>
        private void LogInstallationParameters()
        {
            var installParams = new Dictionary<string, object>
            {
                ["AutomaticHealthChecks"] = _enableAutomaticHealthChecks,
                ["CircuitBreakers"] = _enableCircuitBreakers,
                ["GracefulDegradation"] = _enableGracefulDegradation,
                ["UnityHealthChecks"] = _enableUnityHealthChecks,
                ["DefaultInterval"] = _defaultHealthCheckInterval,
                ["FailureThreshold"] = _defaultFailureThreshold,
                ["OpenTimeout"] = _defaultOpenTimeoutSeconds,
                ["HasConfigAsset"] = _healthCheckConfig != null
            };

            _logger?.LogInfo("Health check system installation parameters", installParams, _correlationId);
        }

        /// <summary>
        /// Registers the self-monitoring health check
        /// </summary>
        private void RegisterSelfMonitoringHealthCheck(IHealthCheckService healthCheckService)
        {
            var selfMonitoringCheck = Container.Resolve<HealthCheckServiceHealthCheck>();
            var config = new HealthCheckConfiguration
            {
                Name = selfMonitoringCheck.Name,
                Interval = TimeSpan.FromSeconds(_selfMonitoringInterval),
                Timeout = TimeSpan.FromSeconds(10),
                Enabled = true,
                Category = HealthCheckCategory.System
            };

            healthCheckService.RegisterHealthCheck(selfMonitoringCheck, config);
            _logger?.LogInfo("Self-monitoring health check registered", _correlationId);
        }

        /// <summary>
        /// Configures automatic health check intervals for registered checks
        /// </summary>
        private void ConfigureAutomaticHealthChecks(IHealthCheckService healthCheckService)
        {
            if (_enableAutomaticHealthChecks)
            {
                // Set intervals for different types of health checks
                healthCheckService.SetCheckInterval("SystemResource", TimeSpan.FromSeconds(_criticalSystemInterval));
                healthCheckService.SetCheckInterval("UnitySystem", TimeSpan.FromSeconds(_criticalSystemInterval));
                healthCheckService.SetCheckInterval("MessageBus", TimeSpan.FromSeconds(_defaultHealthCheckInterval));
                healthCheckService.SetCheckInterval("UnityPerformance", TimeSpan.FromSeconds(_defaultHealthCheckInterval));
                healthCheckService.SetCheckInterval("UnityMemory", TimeSpan.FromSeconds(_defaultHealthCheckInterval));

                _logger?.LogInfo("Automatic health check intervals configured", _correlationId);
            }
        }

        /// <summary>
        /// Sets up event handlers for health status changes and alerting
        /// </summary>
        private void SetupHealthEventHandlers(IHealthCheckService healthCheckService, IAlertService alertService)
        {
            // Health status change handler
            healthCheckService.HealthStatusChanged += (sender, args) =>
            {
                if (_logHealthEvents)
                {
                    _logger?.LogInfo($"Health status changed: {args.SystemName} -> {args.NewStatus}", _correlationId);
                }

                // Trigger alerts for critical status changes
                if (args.NewStatus == HealthStatus.Unhealthy)
                {
                    alertService?.RaiseAlert(
                        $"Health check failed: {args.SystemName}",
                        AlertSeverity.Critical,
                        "HealthCheck",
                        args.SystemName.ToString());
                }
            };

            // Circuit breaker state change handler
            healthCheckService.CircuitBreakerStateChanged += (sender, args) =>
            {
                if (_logHealthEvents)
                {
                    _logger?.LogInfo($"Circuit breaker state changed: {args.OperationName} -> {args.NewState}", _correlationId);
                }

                // Alert on circuit breaker opening
                if (args.NewState == CircuitBreakerState.Open)
                {
                    alertService?.RaiseAlert(
                        $"Circuit breaker opened: {args.OperationName}",
                        AlertSeverity.Warning,
                        "CircuitBreaker",
                        args.OperationName);
                }
            };

            // Degradation status change handler
            healthCheckService.DegradationStatusChanged += (sender, args) =>
            {
                if (_logHealthEvents)
                {
                    _logger?.LogInfo($"Degradation status changed: {args.SystemName} -> {args.NewLevel}", _correlationId);
                }

                // Alert on severe degradation
                if (args.NewLevel >= DegradationLevel.Severe)
                {
                    alertService?.RaiseAlert(
                        $"System degradation: {args.SystemName} -> {args.NewLevel}",
                        AlertSeverity.Critical,
                        "Degradation",
                        args.SystemName);
                }
            };

            _logger?.LogInfo("Health event handlers configured", _correlationId);
        }

        /// <summary>
        /// Performs an initial health check to validate system readiness
        /// </summary>
        private void PerformInitialHealthCheck(IHealthCheckService healthCheckService)
        {
            try
            {
                var overallHealth = healthCheckService.GetOverallHealth();
                _logger?.LogInfo($"Initial health check completed: {overallHealth}", _correlationId);

                if (overallHealth == HealthStatus.Unhealthy)
                {
                    Debug.LogWarning($"[{InstallerName}] System started with unhealthy status, check logs for details");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Initial health check failed", _correlationId);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generates a unique correlation ID for installation tracking
        /// </summary>
        private static FixedString128Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..24];
            return new FixedString128Bytes($"HC-INSTALL-{guid}");
        }

        #endregion

        #region Unity Event Handlers

        private void OnValidate()
        {
            // Validate threshold ordering
            if (_minorDegradationThreshold >= _moderateDegradationThreshold)
                _minorDegradationThreshold = Mathf.Max(0.05f, _moderateDegradationThreshold - 0.05f);
            
            if (_moderateDegradationThreshold >= _severeDegradationThreshold)
                _moderateDegradationThreshold = Mathf.Max(_minorDegradationThreshold + 0.05f, _severeDegradationThreshold - 0.05f);
            
            if (_severeDegradationThreshold >= _disableDegradationThreshold)
                _severeDegradationThreshold = Mathf.Max(_moderateDegradationThreshold + 0.05f, _disableDegradationThreshold - 0.05f);
        }

        #endregion
    }
}