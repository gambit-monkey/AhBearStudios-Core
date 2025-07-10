using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Factories;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.HealthChecks;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.Installers
{
    /// <summary>
    /// Reflex MonoInstaller for the logging system.
    /// Registers all logging services, targets, and configurations with the DI container.
    /// Follows AhBearStudios Core Architecture bootstrap requirements.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute early in the bootstrap process
    public class LoggingInstaller : MonoInstaller, IBootstrapInstaller
    {
        [Header("Logging Configuration")]
        [SerializeField] private LoggingConfigAsset _loggingConfigAsset;
        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Info;
        [SerializeField] private bool _enableHighPerformanceMode = true;
        [SerializeField] private bool _enableBurstCompatibility = true;
        [SerializeField] private bool _enableBatching = true;
        [SerializeField] private int _batchSize = 100;
        [SerializeField] private float _flushIntervalSeconds = 0.1f;

        [Header("Unity-Specific Settings")]
        [SerializeField] private bool _enableUnityConsoleTarget = true;
        [SerializeField] private bool _enableFileLogging = true;
        [SerializeField] private string _logFilePath = "Logs/game.log";
        [SerializeField] private bool _enableMemoryTarget = false;
        [SerializeField] private int _memoryTargetCapacity = 1000;

        [Header("Advanced Settings")]
        [SerializeField] private bool _enableStructuredLogging = true;
        [SerializeField] private bool _enableCorrelationIds = true;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private int _maxCacheSize = 1000;

        [Header("Debug Settings")]
        [SerializeField] private bool _verboseInitialization = false;
        [SerializeField] private bool _validateConfiguration = true;

        // IBootstrapInstaller implementation
        public string InstallerName => "LoggingInstaller";
        public int Priority => 100; // High priority - logging is foundational
        public bool IsEnabled => enabled;
        public Type[] Dependencies => new[] { typeof(CoreSystemsInstaller) }; // Depends on core systems

        /// <summary>
        /// Validates that all dependencies and configurations are correct.
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool ValidateInstaller()
        {
            var errors = new List<string>();

            // Check that required dependencies are available
            if (!Container.HasBinding<IMessageBusService>())
            {
                errors.Add("IMessageBusService is not registered - required for logging system");
            }

            if (!Container.HasBinding<IHealthCheckService>())
            {
                errors.Add("IHealthCheckService is not registered - required for logging system");
            }

            if (!Container.HasBinding<IAlertService>())
            {
                errors.Add("IAlertService is not registered - required for logging system");
            }

            // Validate configuration
            if (_validateConfiguration)
            {
                var config = CreateLoggingConfig();
                var configErrors = config.Validate();
                errors.AddRange(configErrors);
            }

            // Validate file path for file logging
            if (_enableFileLogging && string.IsNullOrWhiteSpace(_logFilePath))
            {
                errors.Add("Log file path is required when file logging is enabled");
            }

            if (errors.Count > 0)
            {
                Debug.LogError($"LoggingInstaller validation failed:\n{string.Join("\n", errors)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Pre-installation setup and validation.
        /// </summary>
        public void PreInstall()
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

            // Validate Unity-specific requirements
            if (_enableUnityConsoleTarget && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogWarning("LoggingInstaller: Unity Console target may have limited functionality on WebGL");
            }
        }

        /// <summary>
        /// Main installation method that registers all logging services with Reflex.
        /// </summary>
        /// <param name="builder">The container builder for service registration</param>
        public override void InstallBindings(ContainerBuilder builder)
        {
            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Installing logging system bindings");
            }

            try
            {
                // 1. Register configuration
                RegisterConfiguration(builder);

                // 2. Register core services
                RegisterCoreServices(builder);

                // 3. Register factories
                RegisterFactories(builder);

                // 4. Register targets
                RegisterTargets(builder);

                // 5. Register health checks
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
        /// </summary>
        public void PostInstall()
        {
            if (_verboseInitialization)
            {
                Debug.Log("LoggingInstaller: Starting post-installation setup");
            }

            try
            {
                // Initialize and validate the logging service
                var loggingService = Container.Resolve<ILoggingService>();
                
                // Register health check with the health check service
                var healthCheckService = Container.Resolve<IHealthCheckService>();
                var loggingHealthCheck = Container.Resolve<LoggingServiceHealthCheck>();
                healthCheckService.RegisterHealthCheck(loggingHealthCheck);

                // Perform initial health check
                _ = loggingHealthCheck.CheckHealthAsync();

                // Register targets from configuration
                RegisterConfiguredTargets(loggingService);

                // Log successful initialization
                loggingService.LogInfo("Logging system successfully initialized and validated", "Unity.Logging.Bootstrap");

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

        /// <summary>
        /// Registers the logging configuration with the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        private void RegisterConfiguration(ContainerBuilder builder)
        {
            var config = CreateLoggingConfig();
            builder.Bind<LoggingConfig>().FromInstance(config);

            // Register builder for runtime configuration changes
            builder.Bind<ILogConfigBuilder>().To<LogConfigBuilder>().AsTransient();
        }

        /// <summary>
        /// Registers core logging services with the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        private void RegisterCoreServices(ContainerBuilder builder)
        {
            // Register the main logging service as singleton
            builder.Bind<ILoggingService>().To<LoggingService>().AsSingle();

            // Register supporting services
            builder.Bind<LogFormattingService>().AsSingle();
            
            if (_enableBatching)
            {
                builder.Bind<LogBatchingService>().AsSingle();
            }
        }

        /// <summary>
        /// Registers factories with the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        private void RegisterFactories(ContainerBuilder builder)
        {
            builder.Bind<ILogTargetFactory>().To<LogTargetFactory>().AsSingle();
        }

        /// <summary>
        /// Registers log targets with the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        private void RegisterTargets(ContainerBuilder builder)
        {
            // Register Unity Console target
            if (_enableUnityConsoleTarget)
            {
                var unityConsoleConfig = new LogTargetConfig
                {
                    Name = "UnityConsole",
                    TargetType = "UnityConsole",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    UseAsyncWrite = false // Unity Debug.Log is not async
                };
                
                builder.Bind<LogTargetConfig>().FromInstance(unityConsoleConfig).WithId("UnityConsole");
                builder.Bind<ILogTarget>().To<UnityConsoleLogTarget>().AsSingle().WithId("UnityConsole");
            }

            // Register File target
            if (_enableFileLogging)
            {
                var fileConfig = new LogTargetConfig
                {
                    Name = "File",
                    TargetType = "File",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    UseAsyncWrite = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["FilePath"] = _logFilePath,
                        ["MaxFileSize"] = 10 * 1024 * 1024, // 10MB
                        ["MaxFiles"] = 5
                    }
                };
                
                builder.Bind<LogTargetConfig>().FromInstance(fileConfig).WithId("File");
                builder.Bind<ILogTarget>().To<FileLogTarget>().AsSingle().WithId("File");
            }

            // Register Memory target
            if (_enableMemoryTarget)
            {
                var memoryConfig = new LogTargetConfig
                {
                    Name = "Memory",
                    TargetType = "Memory",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["Capacity"] = _memoryTargetCapacity
                    }
                };
                
                builder.Bind<LogTargetConfig>().FromInstance(memoryConfig).WithId("Memory");
                builder.Bind<ILogTarget>().To<MemoryLogTarget>().AsSingle().WithId("Memory");
            }
        }

        /// <summary>
        /// Registers health checks with the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        private void RegisterHealthChecks(ContainerBuilder builder)
        {
            builder.Bind<LoggingServiceHealthCheck>().AsSingle();
        }

        /// <summary>
        /// Creates the logging configuration from inspector settings and ScriptableObject.
        /// </summary>
        /// <returns>The configured LoggingConfig instance</returns>
        private LoggingConfig CreateLoggingConfig()
        {
            // Start with ScriptableObject config if available
            var baseConfig = _loggingConfigAsset?.Config ?? LoggingConfig.Default;

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

        /// <summary>
        /// Registers all configured targets with the logging service.
        /// </summary>
        /// <param name="loggingService">The logging service instance</param>
        private void RegisterConfiguredTargets(ILoggingService loggingService)
        {
            var targetFactory = Container.Resolve<ILogTargetFactory>();

            // Register Unity Console target
            if (_enableUnityConsoleTarget)
            {
                var unityTarget = Container.Resolve<ILogTarget>("UnityConsole");
                loggingService.RegisterTarget(unityTarget);
            }

            // Register File target
            if (_enableFileLogging)
            {
                var fileTarget = Container.Resolve<ILogTarget>("File");
                loggingService.RegisterTarget(fileTarget);
            }

            // Register Memory target
            if (_enableMemoryTarget)
            {
                var memoryTarget = Container.Resolve<ILogTarget>("Memory");
                loggingService.RegisterTarget(memoryTarget);
            }
        }

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

    /// <summary>
    /// Interface for bootstrap installers that integrate with the AhBearStudios bootstrap system.
    /// </summary>
    public interface IBootstrapInstaller : IInstaller
    {
        /// <summary>
        /// Gets the name of this installer.
        /// </summary>
        string InstallerName { get; }

        /// <summary>
        /// Gets the execution priority (lower numbers execute first).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether this installer is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the types this installer depends on.
        /// </summary>
        Type[] Dependencies { get; }

        /// <summary>
        /// Validates that this installer can be executed successfully.
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        bool ValidateInstaller();

        /// <summary>
        /// Pre-installation setup.
        /// </summary>
        void PreInstall();

        /// <summary>
        /// Post-installation setup.
        /// </summary>
        void PostInstall();
    }

    /// <summary>
    /// Base interface for Reflex installers (placeholder).
    /// </summary>
    public interface IInstaller
    {
        /// <summary>
        /// Installs bindings into the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        void Install(ContainerBuilder builder);
    }

    /// <summary>
    /// Base MonoBehaviour installer class for Reflex (simplified placeholder).
    /// </summary>
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        /// <summary>
        /// Gets the Reflex container instance.
        /// </summary>
        protected static Container Container { get; private set; }

        /// <summary>
        /// Abstract method for installing bindings.
        /// </summary>
        /// <param name="builder">The container builder</param>
        public abstract void InstallBindings(ContainerBuilder builder);

        /// <summary>
        /// Installs bindings into the container.
        /// </summary>
        /// <param name="builder">The container builder</param>
        public virtual void Install(ContainerBuilder builder)
        {
            InstallBindings(builder);
        }

        /// <summary>
        /// Called after installation is complete.
        /// </summary>
        public virtual void Start()
        {
            // Override in derived classes for post-installation logic
        }
    }

    /// <summary>
    /// Simplified placeholder for Reflex Container.
    /// </summary>
    public class Container
    {
        public static bool HasBinding<T>() => true; // Placeholder
        public static T Resolve<T>() => default(T); // Placeholder
        public static T Resolve<T>(string id) => default(T); // Placeholder
    }

    /// <summary>
    /// Simplified placeholder for Reflex ContainerBuilder.
    /// </summary>
    public class ContainerBuilder
    {
        public ContainerBuilder Bind<T>() => this;
        public ContainerBuilder To<TImpl>() => this;
        public ContainerBuilder FromInstance<T>(T instance) => this;
        public ContainerBuilder AsSingle() => this;
        public ContainerBuilder AsTransient() => this;
        public ContainerBuilder WithId(string id) => this;
    }
}