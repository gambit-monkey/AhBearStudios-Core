using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Factories;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.HealthChecks;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Unity.Logging.ScriptableObjects;
using AhBearStudios.Unity.Logging.Targets;
using LoggingConfig = AhBearStudios.Unity.Logging.ScriptableObjects.LoggingConfig;

namespace AhBearStudios.Unity.Logging.Installers
{
    /// <summary>
    /// Enhanced Reflex MonoInstaller for the logging system with full bootstrap integration.
    /// Registers all logging services, targets, and configurations with comprehensive validation.
    /// Follows AhBearStudios Core Architecture bootstrap requirements with robust error handling.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute early in the bootstrap process
    public class LoggingInstaller : MonoInstaller, IBootstrapInstaller
    {
        [Header("Logging Configuration")] [SerializeField]
        private LoggingConfig _loggingConfig;

        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Info;
        [SerializeField] private bool _enableHighPerformanceMode = true;
        [SerializeField] private bool _enableBurstCompatibility = true;
        [SerializeField] private bool _enableBatching = true;
        [SerializeField] private int _batchSize = 100;
        [SerializeField] private float _flushIntervalSeconds = 0.1f;

        [Header("Unity-Specific Settings")] [SerializeField]
        private bool _enableUnityConsoleTarget = true;

        [SerializeField] private bool _enableFileLogging = true;
        [SerializeField] private string _logFilePath = "Logs/game.log";
        [SerializeField] private bool _enableMemoryTarget = false;
        [SerializeField] private int _memoryTargetCapacity = 1000;

        [Header("Advanced Settings")] [SerializeField]
        private bool _enableStructuredLogging = true;

        [SerializeField] private bool _enableCorrelationIds = true;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private int _maxCacheSize = 1000;

        [Header("System Integration Settings")] [SerializeField]
        private bool _enableHealthCheckIntegration = true;

        [SerializeField] private bool _enableAlertIntegration = true;
        [SerializeField] private bool _enableProfilerIntegration = true;
        [SerializeField] private float _healthCheckIntervalMinutes = 1.0f;

        [Header("Debug Settings")] [SerializeField]
        private bool _verboseInitialization = false;

        [SerializeField] private bool _validateConfiguration = true;
        [SerializeField] private bool _enableBootstrapLogging = true;

        // IBootstrapInstaller implementation
        public string InstallerName => "LoggingInstaller";
        public int Priority => 100; // High priority - logging is foundational
        public bool IsEnabled => enabled;

        public Type[] Dependencies => new[]
        {
            // Note: Logging has no dependencies as it's a foundation system
            // These would be actual types in a real implementation
            // typeof(CoreInfrastructureInstaller)
        };

        /// <summary>
        /// Validates that all dependencies and configurations are correct.
        /// Performs comprehensive validation before installation begins.
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool ValidateInstaller()
        {
            var errors = new List<string>();

            try
            {
                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Starting validation");
                }

                // Validate required dependencies (these services should exist for a complete system)
                ValidateDependencies(errors);

                // Validate configuration
                if (_validateConfiguration)
                {
                    ValidateLoggingConfiguration(errors);
                }

                // Validate Unity-specific settings
                ValidateUnitySettings(errors);

                // Validate system integration settings
                ValidateSystemIntegration(errors);

                if (errors.Count > 0)
                {
                    var errorMessage = $"LoggingInstaller validation failed:\n{string.Join("\n", errors)}";
                    Debug.LogError(errorMessage);

                    if (_enableBootstrapLogging)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BOOTSTRAP ERROR] {errorMessage}");
                    }

                    return false;
                }

                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Validation completed successfully");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller validation exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pre-installation setup and validation.
        /// Prepares the environment for logging system installation.
        /// </summary>
        public void PreInstall()
        {
            try
            {
                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Starting pre-installation setup");
                }

                // Ensure log directory exists for file logging
                if (_enableFileLogging && !string.IsNullOrWhiteSpace(_logFilePath))
                {
                    try
                    {
                        var directory = System.IO.Path.GetDirectoryName(_logFilePath);
                        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                        {
                            System.IO.Directory.CreateDirectory(directory);
                            if (_verboseInitialization)
                            {
                                Debug.Log($"LoggingInstaller: Created log directory: {directory}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"LoggingInstaller: Failed to create log directory: {ex.Message}");
                    }
                }

                // Validate container state
                if (Container == null)
                {
                    throw new InvalidOperationException("Container is not available during pre-installation");
                }

                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Pre-installation setup completed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller: Pre-installation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Installs all logging system bindings into the container.
        /// Follows the Builder → Config → Factory → Service pattern.
        /// </summary>
        /// <param name="builder">The container builder</param>
        public override void Install(ContainerBuilder builder)
        {
            try
            {
                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Starting installation");
                }

                // Step 1: Register configurations
                RegisterConfiguration(builder);

                // Step 2: Register builders
                RegisterBuilders(builder);

                // Step 3: Register factories
                RegisterFactories(builder);

                // Step 4: Register supporting services
                RegisterSupportingServices(builder);

                // Step 5: Register log targets
                RegisterTargets(builder);

                // Step 6: Register main logging service
                RegisterMainService(builder);

                // Step 7: Register health checks
                RegisterHealthChecks(builder);

                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Successfully installed all logging bindings");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller: Failed to install bindings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Post-installation setup, health check registration, and system validation.
        /// Completes the logging system integration with other core services.
        /// </summary>
        public void PostInstall()
        {
            try
            {
                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Starting post-installation setup");
                }

                // Initialize and validate the logging service
                var loggingService = Container.Resolve<ILoggingService>();

                // Register health check with the health check service (if available)
                if (_enableHealthCheckIntegration && Container.HasBinding<IHealthCheckService>())
                {
                    RegisterWithHealthCheckService(loggingService);
                }

                // Perform initial health check
                ValidateServiceHealth(loggingService);

                // Register targets from configuration
                RegisterConfiguredTargets(loggingService);

                // Configure performance monitoring (if available)
                if (_enableProfilerIntegration && Container.HasBinding<IProfilerService>())
                {
                    ConfigurePerformanceMonitoring();
                }

                // Log successful initialization
                loggingService.LogInfo("Logging system successfully initialized and validated", "Bootstrap",
                    "LoggingInstaller");

                if (_verboseInitialization)
                {
                    var targetCount = loggingService.GetRegisteredTargets().Count;
                    Debug.Log($"LoggingInstaller: Post-installation complete. {targetCount} targets registered.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller: Post-installation failed: {ex.Message}");
                throw;
            }
        }

        #region Private Validation Methods

        /// <summary>
        /// Validates required system dependencies.
        /// </summary>
        private void ValidateDependencies(List<string> errors)
        {
            // Note: In a complete system, these services should be available
            // For now, we'll check if they exist but not require them

            if (_enableHealthCheckIntegration && Container != null && !Container.HasBinding<IHealthCheckService>())
            {
                if (_verboseInitialization)
                {
                    Debug.LogWarning("IHealthCheckService not available - health check integration will be disabled");
                }
            }

            if (_enableAlertIntegration && Container != null && !Container.HasBinding<IAlertService>())
            {
                if (_verboseInitialization)
                {
                    Debug.LogWarning("IAlertService not available - alert integration will be disabled");
                }
            }

            if (_enableProfilerIntegration && Container != null && !Container.HasBinding<IProfilerService>())
            {
                if (_verboseInitialization)
                {
                    Debug.LogWarning("IProfilerService not available - profiler integration will be disabled");
                }
            }
        }

        /// <summary>
        /// Validates the logging configuration.
        /// </summary>
        private void ValidateLoggingConfiguration(List<string> errors)
        {
            try
            {
                var config = CreateLoggingConfig();
                var configErrors = config.Validate();
                errors.AddRange(configErrors);
            }
            catch (Exception ex)
            {
                errors.Add($"Configuration validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates Unity-specific settings.
        /// </summary>
        private void ValidateUnitySettings(List<string> errors)
        {
            // Validate file path for file logging
            if (_enableFileLogging && string.IsNullOrWhiteSpace(_logFilePath))
            {
                errors.Add("Log file path is required when file logging is enabled");
            }

            // Validate batch size
            if (_enableBatching && _batchSize <= 0)
            {
                errors.Add("Batch size must be greater than zero when batching is enabled");
            }

            // Validate memory target capacity
            if (_enableMemoryTarget && _memoryTargetCapacity <= 0)
            {
                errors.Add("Memory target capacity must be greater than zero when memory target is enabled");
            }

            // Validate flush interval
            if (_flushIntervalSeconds <= 0)
            {
                errors.Add("Flush interval must be greater than zero");
            }
        }

        /// <summary>
        /// Validates system integration settings.
        /// </summary>
        private void ValidateSystemIntegration(List<string> errors)
        {
            if (_healthCheckIntervalMinutes <= 0)
            {
                errors.Add("Health check interval must be greater than zero");
            }

            if (_maxCacheSize <= 0 && _enableCaching)
            {
                errors.Add("Max cache size must be greater than zero when caching is enabled");
            }
        }

        #endregion

        #region Private Registration Methods

        /// <summary>
        /// Registers the logging configuration with the container.
        /// </summary>
        private void RegisterConfiguration(ContainerBuilder builder)
        {
            var config = CreateLoggingConfig();
            builder.Bind<Core.Logging.Configs.LoggingConfig>().FromInstance(config);

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered logging configuration");
            }
        }

        /// <summary>
        /// Registers configuration builders with the container.
        /// </summary>
        private void RegisterBuilders(ContainerBuilder builder)
        {
            builder.Bind<ILogConfigBuilder>().To<LogConfigBuilder>().AsTransient();

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered configuration builders");
            }
        }

        /// <summary>
        /// Registers factories with the container.
        /// </summary>
        private void RegisterFactories(ContainerBuilder builder)
        {
            builder.Bind<ILogTargetFactory>().To<LogTargetFactory>().AsSingle();
            builder.Bind<ILoggingServiceFactory>().To<LoggingServiceFactory>().AsSingle();

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered factories");
            }
        }

        /// <summary>
        /// Registers supporting services with the container.
        /// </summary>
        private void RegisterSupportingServices(ContainerBuilder builder)
        {
            // Register formatting service
            builder.Bind<LogFormattingService>().AsSingle();

            // Register batching service if enabled
            if (_enableBatching)
            {
                builder.Bind<LogBatchingService>().AsSingle();
            }

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered supporting services");
            }
        }

        /// <summary>
        /// Registers log targets with the container.
        /// </summary>
        private void RegisterTargets(ContainerBuilder builder)
        {
            // Register Unity Console target
            if (_enableUnityConsoleTarget)
            {
                builder.Bind<ILogTarget>().To<UnityConsoleLogTarget>().AsSingle().WithId("UnityConsole");
            }

            // Register File target
            if (_enableFileLogging)
            {
                builder.Bind<ILogTarget>().FromMethod(provider =>
                {
                    var config = new LogTargetConfig
                    {
                        Name = "File",
                        TargetType = "File",
                        MinimumLevel = _globalMinimumLevel,
                        IsEnabled = true,
                        Properties = new Dictionary<string, object> { ["FilePath"] = _logFilePath }
                    };
                    var factory = provider.Resolve<ILogTargetFactory>();
                    return factory.CreateTarget(config);
                }).AsSingle().WithId("File");
            }

            // Register Memory target
            if (_enableMemoryTarget)
            {
                builder.Bind<ILogTarget>().FromMethod(provider =>
                {
                    var config = new LogTargetConfig
                    {
                        Name = "Memory",
                        TargetType = "Memory",
                        MinimumLevel = _globalMinimumLevel,
                        IsEnabled = true,
                        Properties = new Dictionary<string, object> { ["MaxEntries"] = _memoryTargetCapacity }
                    };
                    var factory = provider.Resolve<ILogTargetFactory>();
                    return factory.CreateTarget(config);
                }).AsSingle().WithId("Memory");
            }

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered log targets");
            }
        }

        /// <summary>
        /// Registers the main logging service with the container.
        /// </summary>
        private void RegisterMainService(ContainerBuilder builder)
        {
            builder.Bind<ILoggingService>().FromMethod(provider =>
            {
                var config = provider.Resolve<Core.Logging.Configs.LoggingConfig>();
                var formattingService = provider.Resolve<LogFormattingService>();
                var batchingService = _enableBatching ? provider.Resolve<LogBatchingService>() : null;

                // Resolve optional system services
                var healthCheckService = _enableHealthCheckIntegration && provider.HasBinding<IHealthCheckService>()
                    ? provider.Resolve<IHealthCheckService>()
                    : null;
                var alertService = _enableAlertIntegration && provider.HasBinding<IAlertService>()
                    ? provider.Resolve<IAlertService>()
                    : null;
                var profilerService = _enableProfilerIntegration && provider.HasBinding<IProfilerService>()
                    ? provider.Resolve<IProfilerService>()
                    : null;

                return new LoggingService(
                    config,
                    targets: null, // Targets will be registered in PostInstall
                    formattingService,
                    batchingService,
                    healthCheckService,
                    alertService,
                    profilerService);
            }).AsSingle();

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered main logging service");
            }
        }

        /// <summary>
        /// Registers health checks with the container.
        /// </summary>
        private void RegisterHealthChecks(ContainerBuilder builder)
        {
            builder.Bind<LoggingServiceHealthCheck>().AsSingle();

            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Registered health checks");
            }
        }

        #endregion

        #region Private Configuration Methods

        /// <summary>
        /// Creates the logging configuration from inspector settings and ScriptableObject.
        /// </summary>
        private Core.Logging.Configs.LoggingConfig CreateLoggingConfig()
        {
            // Start with ScriptableObject config if available
            var baseConfig = _loggingConfig?.Config ?? Core.Logging.Configs.LoggingConfig.Default;

            // Override with inspector settings
            return baseConfig with
            {
                GlobalMinimumLevel = _globalMinimumLevel,
                HighPerformanceMode = _enableHighPerformanceMode,
                BurstCompatibility = _enableBurstCompatibility,
                BatchingEnabled = _enableBatching,
                BatchSize = _batchSize,
                FlushInterval = TimeSpan.FromSeconds(_flushIntervalSeconds),
                StructuredLogging = _enableStructuredLogging,
                AutoCorrelationId = _enableCorrelationIds,
                CachingEnabled = _enableCaching,
                MaxCacheSize = _maxCacheSize
            };
        }

        #endregion

        #region Private Post-Install Methods

        /// <summary>
        /// Registers the logging service with the health check system.
        /// </summary>
        private void RegisterWithHealthCheckService(ILoggingService loggingService)
        {
            try
            {
                var healthCheckService = Container.Resolve<IHealthCheckService>();
                var loggingHealthCheck = Container.Resolve<LoggingServiceHealthCheck>();
                healthCheckService.RegisterHealthCheck(loggingHealthCheck);

                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Registered with health check service");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller: Failed to register with health check service: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates the health of the logging service.
        /// </summary>
        private void ValidateServiceHealth(ILoggingService loggingService)
        {
            try
            {
                var isHealthy = loggingService.PerformHealthCheck();
                if (!isHealthy)
                {
                    Debug.LogWarning("LoggingInstaller: Initial health check failed");
                }
                else if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Initial health check passed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller: Health check validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers all configured targets with the logging service.
        /// </summary>
        private void RegisterConfiguredTargets(ILoggingService loggingService)
        {
            var registeredCount = 0;

            // Register Unity Console target
            if (_enableUnityConsoleTarget)
            {
                try
                {
                    var unityTarget = Container.Resolve<ILogTarget>("UnityConsole");
                    loggingService.RegisterTarget(unityTarget);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LoggingInstaller: Failed to register Unity Console target: {ex.Message}");
                }
            }

            // Register File target
            if (_enableFileLogging)
            {
                try
                {
                    var fileTarget = Container.Resolve<ILogTarget>("File");
                    loggingService.RegisterTarget(fileTarget);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LoggingInstaller: Failed to register File target: {ex.Message}");
                }
            }

            // Register Memory target
            if (_enableMemoryTarget)
            {
                try
                {
                    var memoryTarget = Container.Resolve<ILogTarget>("Memory");
                    loggingService.RegisterTarget(memoryTarget);
                    registeredCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LoggingInstaller: Failed to register Memory target: {ex.Message}");
                }
            }

            if (_verboseInitialization)
            {
                Debug.Log($"LoggingInstaller: Registered {registeredCount} targets with logging service");
            }
        }

        /// <summary>
        /// Configures performance monitoring for the logging system.
        /// </summary>
        private void ConfigurePerformanceMonitoring()
        {
            try
            {
                var profilerService = Container.Resolve<IProfilerService>();

                // Register performance metrics
                profilerService.RegisterMetricAlert("LoggingService.ErrorRate", 0.1,
                    "High error rate in logging service");
                profilerService.RegisterMetricAlert("LoggingService.QueueSize", 5000,
                    "High queue size in logging service");

                if (_verboseInitialization)
                {
                    Debug.Log("LoggingInstaller: Configured performance monitoring");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoggingInstaller: Failed to configure performance monitoring: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Validates the inspector configuration when values change in the editor.
        /// </summary>
        private void OnValidate()
        {
            // Clamp values to valid ranges
            _batchSize = Mathf.Max(1, _batchSize);
            _flushIntervalSeconds = Mathf.Max(0.01f, _flushIntervalSeconds);
            _memoryTargetCapacity = Mathf.Max(10, _memoryTargetCapacity);
            _maxCacheSize = Mathf.Max(10, _maxCacheSize);
            _healthCheckIntervalMinutes = Mathf.Max(0.1f, _healthCheckIntervalMinutes);

            // Validate log file path
            if (_enableFileLogging && !string.IsNullOrWhiteSpace(_logFilePath))
            {
                try
                {
                    var directory = System.IO.Path.GetDirectoryName(_logFilePath);
                    if (string.IsNullOrEmpty(directory))
                    {
                        _logFilePath = "Logs/game.log";
                    }
                }
                catch
                {
                    _logFilePath = "Logs/game.log";
                }
            }
        }
    }
}