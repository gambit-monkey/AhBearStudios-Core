using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Infrastructure.Bootstrap;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Factories;
using AhBearStudios.Core.Logging.HealthChecks;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Unity.Logging.ScriptableObjects;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Unity.Logging.Targets;
using Reflex.Core;

namespace AhBearStudios.Unity.Logging.Installers
{
    /// <summary>
    /// Production-ready Reflex DI installer for the logging system.
    /// Follows the standard Reflex IInstaller pattern with enhanced Bootstrap lifecycle management.
    /// 
    /// Reflex Compliance:
    /// - Implements IInstaller interface via IBootstrapInstaller inheritance
    /// - Uses standard InstallBindings(ContainerBuilder) method for dependency registration
    /// - Follows Reflex patterns for singleton/transient service registration
    /// - Implements safe optional dependency resolution using container.HasBinding()
    /// - Maintains proper service lifecycle management through Bootstrap phases
    /// 
    /// Provides comprehensive logging infrastructure with Unity integration.
    /// Follows AhBearStudios Core Development Guidelines for Unity Game Development First approach.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute early in the bootstrap process
    public class LoggingInstaller : BootstrapInstaller
    {
        [Header("Configuration")]
        [SerializeField] private LoggingConfigurationAsset _configAsset;
        [SerializeField] private bool _createDefaultConfigIfMissing = true;

        [Header("Override Settings")]
        [SerializeField] private bool _overrideGlobalMinimumLevel = false;
        [SerializeField] private LogLevel _overrideMinimumLevel = LogLevel.Info;

        #region IBootstrapInstaller Implementation

        /// <inheritdoc />
        public override string InstallerName => "LoggingInstaller";

        /// <inheritdoc />
        public override int Priority => 50; // Very high priority - logging is foundational infrastructure

        /// <inheritdoc />
        public override Type[] Dependencies => Array.Empty<Type>(); // No dependencies - logging is foundational

        #endregion

        #region Validation and Setup

        /// <inheritdoc />
        protected override bool PerformValidation()
        {
            var errors = new List<string>();

            try
            {
                // Validate configuration asset
                if (_configAsset == null)
                {
                    if (_createDefaultConfigIfMissing)
                    {
                        LogWarning("No LoggingConfigAsset assigned, will use default configuration");
                    }
                    else
                    {
                        errors.Add("LoggingConfigAsset is required but not assigned");
                    }
                }
                else
                {
                    // Validate the configuration
                    var configErrors = _configAsset.ValidateConfiguration();
                    errors.AddRange(configErrors);
                }

                // Validate override settings
                if (_overrideGlobalMinimumLevel)
                {
                    LogDebug($"Global minimum level will be overridden to: {_overrideMinimumLevel}");
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
                // Create log directories if file logging is enabled
                if (_configAsset != null)
                {
                    CreateLogDirectories();
                }

                LogDebug("Pre-installation setup completed");
            }
            catch (Exception ex)
            {
                LogException(ex, "Pre-installation setup failed");
                throw;
            }
        }

        /// <summary>
        /// Creates log directories for file logging if they don't exist.
        /// </summary>
        private void CreateLogDirectories()
        {
            var config = _configAsset;
            if (config == null) return;
            
            // Create directories for all file-based targets
            foreach (var targetConfig in config.TargetConfigurations)
            {
                if (targetConfig != null && targetConfig.IsEnabled)
                {
                    // Check if this is a file-based target
                    if (targetConfig.TargetType == "File" || targetConfig.TargetType == "Serilog")
                    {
                        var properties = targetConfig.ToProperties();
                        if (properties != null && properties.TryGetValue("FilePath", out var filePathValue) && 
                            !string.IsNullOrWhiteSpace(filePathValue?.ToString()))
                        {
                            CreateDirectoryForFile(filePathValue.ToString(), $"{targetConfig.Name} log file");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a directory for the specified file path.
        /// </summary>
        private void CreateDirectoryForFile(string filePath, string description)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    LogDebug($"Created directory for {description}: {directory}");
                }
            }
            catch (Exception ex)
            {
                LogException(ex, $"Failed to create directory for {description} at path: {filePath}");
            }
        }

        #endregion

        #region Reflex InstallBindings Implementation

        /// <inheritdoc />
        /// <summary>
        /// Core Reflex installer method that registers all logging system dependencies.
        /// Follows the standard Reflex pattern for dependency registration.
        /// </summary>
        public override void InstallBindings(ContainerBuilder builder)
        {
            try
            {
                LogDebug("Starting Reflex logging system installation");

                // Core Reflex registration pattern: Configuration → Factories → Services → Targets
                RegisterConfiguration(builder);
                RegisterFactories(builder);
                RegisterSupportingServices(builder);
                RegisterTargets(builder);
                RegisterMainService(builder);
                RegisterHealthChecks(builder);

                LogDebug("Reflex logging system installation completed successfully");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to install logging system via Reflex");
                throw;
            }
        }

        /// <summary>
        /// Registers the logging configuration with the container.
        /// </summary>
        private void RegisterConfiguration(ContainerBuilder builder)
        {
            var config = GetLoggingConfig();
            builder.AddSingleton(config, typeof(LoggingConfig));
            LogDebug("Registered logging configuration");
        }


        /// <summary>
        /// Registers factories with the container using standard Reflex patterns.
        /// </summary>
        private void RegisterFactories(ContainerBuilder builder)
        {
            // Register all factories as singletons for better performance
            builder.AddSingleton(typeof(LogTargetFactory), typeof(ILogTargetFactory));
            builder.AddSingleton(typeof(LogFormatterFactory), typeof(ILogFormatterFactory));
            builder.AddSingleton(typeof(LoggingServiceFactory), typeof(ILoggingServiceFactory));
            builder.AddTransient(typeof(LogConfigBuilder), typeof(ILogConfigBuilder));
            LogDebug("Registered factories and builders");
        }

        /// <summary>
        /// Registers supporting services with the container.
        /// </summary>
        private void RegisterSupportingServices(ContainerBuilder builder)
        {
            // Register formatting service
            builder.AddSingleton(typeof(LogFormattingService));

            // Register batching service if enabled
            var config = GetLoggingConfig();
            if (config.BatchingEnabled)
            {
                builder.AddSingleton(typeof(LogBatchingService));
            }

            LogDebug("Registered supporting services");
        }

        /// <summary>
        /// Registers log targets with the container.
        /// </summary>
        private void RegisterTargets(ContainerBuilder builder)
        {
            var config = _configAsset;
            if (config == null) return;

            // Register Unity Console target if it exists in target configurations
            var unityConsoleTarget = config.TargetConfigurations.FirstOrDefault(t => 
                t != null && t.IsEnabled && t.TargetType == "UnityConsole");
            
            if (unityConsoleTarget != null)
            {
                builder.AddSingleton(typeof(UnityConsoleLogTarget));
                LogDebug("Registered Unity Console target");
            }

            // Note: File, Memory, and Serilog targets are created via factory during PostInstall
            // since they require complex configuration and factory resolution
            LogDebug("Target registration completed - factory-based targets will be created during PostInstall");
        }

        /// <summary>
        /// Registers the main logging service with the container using Reflex factory pattern.
        /// </summary>
        private void RegisterMainService(ContainerBuilder builder)
        {
            // Register main service with proper dependency injection
            builder.AddSingleton<ILoggingService>(container =>
            {
                var config = container.Resolve<LoggingConfig>();
                var formattingService = container.Resolve<LogFormattingService>();
                
                // Resolve optional services using safe resolution patterns
                var batchingService = config.BatchingEnabled ? container.Resolve<LogBatchingService>() : null;
                var healthCheckService = TryResolveOptionalService<IHealthCheckService>(container, true);
                var alertService = TryResolveOptionalService<IAlertService>(container, true);
                var profilerService = TryResolveOptionalService<IProfilerService>(container, true);
                var messageBusService = TryResolveOptionalService<IMessageBusService>(container, true) ?? NullMessageBusService.Instance;

                return new LoggingService(
                    config,
                    targets: null, // Targets registered in PostInstall phase
                    formattingService,
                    batchingService,
                    healthCheckService,
                    alertService,
                    profilerService,
                    messageBusService);
            }, typeof(ILoggingService));

            LogDebug("Registered main logging service with Reflex factory pattern");
        }

        /// <summary>
        /// Registers health checks with the container.
        /// </summary>
        private void RegisterHealthChecks(ContainerBuilder builder)
        {
            builder.AddSingleton(typeof(LoggingServiceHealthCheck));
            LogDebug("Registered health checks");
        }

        #endregion

        #region Post-Installation

        /// <inheritdoc />
        protected override void PerformPostInstall(Container container)
        {
            try
            {
                LogDebug("Starting post-installation setup");

                // Initialize and validate the logging service
                var loggingService = container.Resolve<ILoggingService>();

                // Register health check with the health check service (if available)
                if (container.HasBinding(typeof(IHealthCheckService)))
                {
                    RegisterWithHealthCheckService(container, loggingService);
                }

                // Perform initial health check
                ValidateServiceHealth(loggingService);

                // Register targets from configuration
                RegisterConfiguredTargets(container, loggingService);

                // Configure performance monitoring (if available)
                if (container.HasBinding(typeof(IProfilerService)))
                {
                    ConfigurePerformanceMonitoring(container);
                }

                // Log successful initialization
                loggingService.LogInfo("Logging system successfully initialized and validated", "Bootstrap", "LoggingInstaller");

                var targetCount = loggingService.GetTargets().Count;
                LogDebug($"Post-installation complete. {targetCount} targets registered");
            }
            catch (Exception ex)
            {
                LogException(ex, "Post-installation failed");
                throw;
            }
        }

        /// <summary>
        /// Registers the logging service with the health check system.
        /// </summary>
        private void RegisterWithHealthCheckService(Container container, ILoggingService loggingService)
        {
            try
            {
                var healthCheckService = container.Resolve<IHealthCheckService>();
                var loggingHealthCheck = container.Resolve<LoggingServiceHealthCheck>();
                healthCheckService.RegisterHealthCheck(loggingHealthCheck);
                LogDebug("Registered with health check service");
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to register with health check service");
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
                    LogWarning("Initial health check failed");
                }
                else
                {
                    LogDebug("Initial health check passed");
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "Health check validation failed");
            }
        }

        /// <summary>
        /// Registers all configured targets with the logging service.
        /// </summary>
        private void RegisterConfiguredTargets(Container container, ILoggingService loggingService)
        {
            if (_configAsset == null) return;

            var registeredCount = 0;

            // Register all configured targets
            foreach (var targetConfig in _configAsset.TargetConfigurations)
            {
                if (targetConfig != null && targetConfig.IsEnabled)
                {
                    if (targetConfig.TargetType == "UnityConsole")
                    {
                        TryRegisterTarget<UnityConsoleLogTarget>(container, loggingService, targetConfig.Name, ref registeredCount);
                    }
                    else
                    {
                        TryRegisterFactoryTarget(container, loggingService, targetConfig.Name, targetConfig.TargetType, ref registeredCount);
                    }
                }
            }

            LogDebug($"Registered {registeredCount} targets with logging service");
        }

        /// <summary>
        /// Tries to register a concrete target type with the logging service.
        /// </summary>
        private void TryRegisterTarget<T>(Container container, ILoggingService loggingService, string targetName, ref int registeredCount)
            where T : class, ILogTarget
        {
            try
            {
                var target = container.Resolve<T>();
                loggingService.RegisterTarget(target);
                registeredCount++;
                LogDebug($"Registered {targetName} target");
            }
            catch (Exception ex)
            {
                LogException(ex, $"Failed to register {targetName} target");
            }
        }

        /// <summary>
        /// Tries to register a factory-created target with the logging service.
        /// </summary>
        private void TryRegisterFactoryTarget(Container container, ILoggingService loggingService, string targetName, string targetType, ref int registeredCount)
        {
            try
            {
                // For factory-created targets, we need to resolve all ILogTarget instances
                // Since we can't use named bindings, we'll create the target directly using the factory
                var factory = container.Resolve<ILogTargetFactory>();
                var targetConfig = CreateTargetConfig(targetName, targetType);
                var target = factory.CreateTarget(targetConfig);
                
                loggingService.RegisterTarget(target);
                registeredCount++;
                LogDebug($"Registered {targetName} target");
            }
            catch (Exception ex)
            {
                LogException(ex, $"Failed to register {targetName} target");
            }
        }

        /// <summary>
        /// Creates a target configuration for the specified target name and type.
        /// </summary>
        private LogTargetConfig CreateTargetConfig(string targetName, string targetType)
        {
            var config = _configAsset;
            
            // Find the target configuration from the asset
            var targetConfig = config?.TargetConfigurations.FirstOrDefault(t => 
                t != null && t.Name == targetName && t.TargetType == targetType);
            
            if (targetConfig != null)
            {
                return targetConfig.ToLogTargetConfig();
            }
            
            // Fallback to default configuration
            return new LogTargetConfig
            {
                Name = targetName,
                TargetType = targetType,
                MinimumLevel = GetEffectiveMinimumLevel(),
                IsEnabled = true,
                Properties = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Configures performance monitoring for the logging system.
        /// Uses the IProfilerService event-driven alerting system.
        /// </summary>
        private void ConfigurePerformanceMonitoring(Container container)
        {
            try
            {
                var profilerService = container.Resolve<IProfilerService>();

                // Subscribe to threshold exceeded events for logging performance monitoring
                profilerService.ThresholdExceeded += (tag, value, unit) =>
                {
                    // Handle logging-related performance alerts
                    var tagName = tag.ToString();
                    if (tagName.Contains("Logging"))
                    {
                        LogWarning($"Logging performance threshold exceeded: {tagName} = {value} {unit}");
                        
                        // Optionally integrate with alert service if available
                        if (container.HasBinding(typeof(IAlertService)))
                        {
                            var alertService = container.Resolve<IAlertService>();
                            // Use basic alert method since AlertSeverity may not be available
                            alertService.RaiseAlert(
                                $"Logging Performance Alert: {tagName}", 
                                AlertSeverity.Warning, 
                                "LoggingInstaller", 
                                $"Threshold exceeded: {value} {unit}");
                        }
                    }
                };

                // Record initial baseline metrics for logging system
                var enabledTargetCount = _configAsset != null ? CountEnabledTargets(_configAsset) : 0;
                profilerService.RecordMetric("LoggingService.InitializedTargets", enabledTargetCount, "count");
                profilerService.RecordMetric("LoggingService.ConfigurationVersion", 1.0, "version");

                LogDebug("Configured performance monitoring with event-driven alerting");
            }
            catch (Exception ex) 
            {
                LogException(ex, "Failed to configure performance monitoring");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the effective logging configuration.
        /// </summary>
        private LoggingConfig GetLoggingConfig()
        {
            if (_configAsset != null)
            {
                // Create configuration directly from asset properties
                var config = new LoggingConfig
                {
                    GlobalMinimumLevel = _overrideGlobalMinimumLevel ? _overrideMinimumLevel : _configAsset.GlobalMinimumLevel,
                    IsLoggingEnabled = _configAsset.IsLoggingEnabled,
                    MaxQueueSize = _configAsset.MaxQueueSize,
                    FlushInterval = _configAsset.FlushInterval,
                    HighPerformanceMode = _configAsset.HighPerformanceMode,
                    BurstCompatibility = _configAsset.BurstCompatibility,
                    StructuredLogging = _configAsset.StructuredLogging,
                    BatchingEnabled = _configAsset.BatchingEnabled,
                    BatchSize = _configAsset.BatchSize,
                    CorrelationIdFormat = _configAsset.CorrelationIdFormat,
                    AutoCorrelationId = _configAsset.AutoCorrelationId,
                    MessageFormat = _configAsset.MessageFormat,
                    IncludeTimestamps = _configAsset.IncludeTimestamps,
                    TimestampFormat = _configAsset.TimestampFormat,
                    CachingEnabled = _configAsset.CachingEnabled,
                    MaxCacheSize = _configAsset.MaxCacheSize,
                    TargetConfigs = _configAsset.TargetConfigurations
                        .Where(t => t != null && t.IsEnabled)
                        .Select(t => t.ToLogTargetConfig())
                        .ToList()
                        .AsReadOnly(),
                    ChannelConfigs = _configAsset.ChannelConfigurations
                        .Where(c => c.IsEnabled)
                        .Select(c => c.ToLogChannelConfig())
                        .ToList()
                        .AsReadOnly()
                };
                
                return config;
            }

            // Create default configuration
            return LoggingConfig.Default with
            {
                GlobalMinimumLevel = _overrideGlobalMinimumLevel ? _overrideMinimumLevel : LogLevel.Info
            };
        }

        /// <summary>
        /// Gets the effective minimum log level.
        /// </summary>
        private LogLevel GetEffectiveMinimumLevel()
        {
            return _overrideGlobalMinimumLevel ? _overrideMinimumLevel : 
                   (_configAsset?.GlobalMinimumLevel ?? LogLevel.Info);
        }

        /// <summary>
        /// Safely resolves an optional service from the container.
        /// Follows Reflex best practices for optional dependency resolution.
        /// </summary>
        private T TryResolveOptionalService<T>(Container container, bool isEnabled) where T : class
        {
            if (!isEnabled) return null;
            
            try
            {
                return container.HasBinding(typeof(T)) ? container.Resolve<T>() : null;
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to resolve optional service {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Counts the number of enabled targets in the configuration asset.
        /// </summary>
        private int CountEnabledTargets(LoggingConfigurationAsset configAsset)
        {
            if (configAsset == null) return 0;
            
            var count = 0;
            
            // Count enabled targets from the target configurations
            foreach (var targetConfig in configAsset.TargetConfigurations)
            {
                if (targetConfig != null && targetConfig.IsEnabled)
                {
                    count++;
                }
            }
            
            return count;
        }

        #endregion

        #region Unity Editor Integration

        /// <summary>
        /// Unity validation callback for Inspector value changes.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Validate configuration asset reference
            if (_configAsset == null && !_createDefaultConfigIfMissing)
            {
                Debug.LogWarning($"[{InstallerName}] No LoggingConfigAsset assigned and default creation is disabled");
            }
        }

        #endregion
    }
}