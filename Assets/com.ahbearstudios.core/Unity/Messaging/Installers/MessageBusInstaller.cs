using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Builders;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Factories;
using AhBearStudios.Core.Messaging.HealthChecks;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Unity.Logging.Installers;
using Reflex.Core;
using Unity.Collections;

namespace AhBearStudios.Unity.Messaging.Installers
{
    /// <summary>
    /// Production-ready Reflex installer for the messaging system.
    /// Integrates with the bootstrap process and registers all messaging components with comprehensive validation.
    /// Follows AhBearStudios Core Development Guidelines for enterprise-grade reliability.
    /// </summary>
    public sealed class MessageBusInstaller : IBootstrapInstaller
    {
        #region Private Fields

        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;
        private readonly FixedString128Bytes _correlationId;

        #endregion

        #region IBootstrapInstaller Properties

        /// <inheritdoc />
        public string InstallerName => "MessageBusInstaller";

        /// <inheritdoc />
        public int Priority => 200; // After logging (100) but before most other systems

        /// <inheritdoc />
        public bool IsEnabled { get; private set; } = true;

        /// <inheritdoc />
        public Type[] Dependencies => new[]
        {
            typeof(LoggingInstaller)
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageBusInstaller class.
        /// </summary>
        /// <param name="config">Optional message bus configuration. If null, default config will be used.</param>
        /// <param name="logger">Optional logging service for installer operations</param>
        public MessageBusInstaller(MessageBusConfig config = null, ILoggingService logger = null)
        {
            _config = config ?? CreateDefaultConfig();
            _logger = logger;
            _correlationId = new FixedString128Bytes($"MsgBusInstaller-{Guid.NewGuid():N}");
        }

        #endregion

        #region IBootstrapInstaller Implementation

        /// <inheritdoc />
        public bool ValidateInstaller()
        {
            try
            {
                _logger?.LogInfo($"[{_correlationId}] Validating MessageBusInstaller");

                // Validate configuration
                if (!ValidateConfiguration())
                {
                    _logger?.LogError($"[{_correlationId}] Configuration validation failed");
                    return false;
                }

                // Validate dependencies
                if (!ValidateDependencies())
                {
                    _logger?.LogError($"[{_correlationId}] Dependency validation failed");
                    return false;
                }

                // Validate system requirements
                if (!ValidateSystemRequirements())
                {
                    _logger?.LogError($"[{_correlationId}] System requirements validation failed");
                    return false;
                }

                _logger?.LogInfo($"[{_correlationId}] MessageBusInstaller validation successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Validation failed with exception");
                return false;
            }
        }

        /// <inheritdoc />
        public void PreInstall()
        {
            try
            {
                _logger?.LogInfo($"[{_correlationId}] Starting MessageBusInstaller pre-installation");

                // Validate system state before installation
                if (!ValidateInstaller())
                {
                    IsEnabled = false;
                    _logger?.LogError($"[{_correlationId}] Pre-installation validation failed - installer disabled");
                    return;
                }

                // Log configuration details
                LogConfigurationDetails();

                // Prepare system resources
                PrepareSystemResources();

                _logger?.LogInfo($"[{_correlationId}] MessageBusInstaller pre-installation completed");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Pre-installation failed");
                IsEnabled = false;
                throw;
            }
        }

        /// <inheritdoc />
        public void Install(ContainerBuilder builder)
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning($"[{_correlationId}] MessageBusInstaller is disabled - skipping installation");
                return;
            }

            try
            {
                _logger?.LogInfo($"[{_correlationId}] Installing MessageBus components");

                // Register configuration as singleton
                builder.Bind<MessageBusConfig>().FromInstance(_config).AsSingle();

                // Register builder for runtime configuration creation
                builder.Bind<MessageBusConfigBuilder>().To<MessageBusConfigBuilder>().AsTransient();

                // Register factory as singleton
                builder.Bind<IMessageBusFactory>().To<MessageBusFactory>().AsSingle();

                // Register core service as singleton
                builder.Bind<IMessageBusService>().To<MessageBusService>().AsSingle();

                // Register health check
                builder.Bind<MessageBusHealthCheck>().To<MessageBusHealthCheck>().AsSingle();

                // Register message scope factory
                builder.Bind<Func<IMessageScope>>()
                    .FromMethod(container => () => container.Resolve<IMessageBusService>().CreateScope())
                    .AsSingle();

                // Register extension method support
                RegisterExtensionMethods(builder);

                _logger?.LogInfo($"[{_correlationId}] MessageBus components installed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Installation failed");
                IsEnabled = false;
                throw;
            }
        }

        /// <inheritdoc />
        public void PostInstall()
        {
            if (!IsEnabled)
            {
                _logger?.LogWarning($"[{_correlationId}] MessageBusInstaller is disabled - skipping post-installation");
                return;
            }

            try
            {
                _logger?.LogInfo($"[{_correlationId}] Starting MessageBusInstaller post-installation");

                // Register health checks
                RegisterHealthChecks();

                // Configure alerts
                ConfigureAlerts();

                // Setup performance monitoring
                SetupPerformanceMonitoring();

                // Validate final installation
                ValidateFinalInstallation();

                _logger?.LogInfo($"[{_correlationId}] MessageBusInstaller post-installation completed");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Post-installation failed");
                throw;
            }
        }

        #endregion

        #region Private Validation Methods

        private bool ValidateConfiguration()
        {
            try
            {
                if (_config == null)
                {
                    _logger?.LogError($"[{_correlationId}] Configuration is null");
                    return false;
                }

                if (!_config.IsValid())
                {
                    _logger?.LogError($"[{_correlationId}] Configuration validation failed: {_config.GetValidationErrors()}");
                    return false;
                }

                // Validate critical configuration values
                if (_config.MaxConcurrentHandlers <= 0)
                {
                    _logger?.LogError($"[{_correlationId}] MaxConcurrentHandlers must be greater than 0");
                    return false;
                }

                if (_config.MaxQueueSize <= 0)
                {
                    _logger?.LogError($"[{_correlationId}] MaxQueueSize must be greater than 0");
                    return false;
                }

                if (_config.HandlerTimeout <= TimeSpan.Zero)
                {
                    _logger?.LogError($"[{_correlationId}] HandlerTimeout must be greater than zero");
                    return false;
                }

                // Validate threshold values
                if (_config.CriticalErrorRateThreshold <= _config.WarningErrorRateThreshold)
                {
                    _logger?.LogError($"[{_correlationId}] Critical error rate threshold must be greater than warning threshold");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Configuration validation exception");
                return false;
            }
        }

        private bool ValidateDependencies()
        {
            try
            {
                // Check required core dependencies
                if (!Container.HasBinding<ILoggingService>())
                {
                    _logger?.LogError($"[{_correlationId}] Required dependency ILoggingService not found");
                    return false;
                }

                // Check optional dependencies and log warnings
                if (_config.HealthChecksEnabled && !Container.HasBinding<IHealthCheckService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] Health checks enabled but IHealthCheckService not available");
                }

                if (_config.AlertsEnabled && !Container.HasBinding<IAlertService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] Alerts enabled but IAlertService not available");
                }

                if (_config.PerformanceMonitoring && !Container.HasBinding<IProfilerService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] Performance monitoring enabled but IProfilerService not available");
                }

                if (_config.UseObjectPooling && !Container.HasBinding<IPoolingService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] Object pooling enabled but IPoolingService not available");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Dependency validation exception");
                return false;
            }
        }

        private bool ValidateSystemRequirements()
        {
            try
            {
                // Check memory requirements
                var availableMemory = GC.GetTotalMemory(false);
                var requiredMemory = _config.EstimatedMemoryRequirement;
                
                if (availableMemory < requiredMemory)
                {
                    _logger?.LogWarning($"[{_correlationId}] Available memory ({availableMemory:N0} bytes) may be insufficient for estimated requirement ({requiredMemory:N0} bytes)");
                }

                // Check thread pool capacity
                if (ThreadPool.ThreadCount < _config.MaxConcurrentHandlers)
                {
                    _logger?.LogWarning($"[{_correlationId}] Thread pool capacity may be insufficient for MaxConcurrentHandlers ({_config.MaxConcurrentHandlers})");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] System requirements validation exception");
                return false;
            }
        }

        #endregion

        #region Private Installation Methods

        private void LogConfigurationDetails()
        {
            _logger?.LogInfo($"[{_correlationId}] Installing MessageBus with configuration:");
            _logger?.LogInfo($"  InstanceName: {_config.InstanceName}");
            _logger?.LogInfo($"  AsyncSupport: {_config.AsyncSupport}");
            _logger?.LogInfo($"  PerformanceMonitoring: {_config.PerformanceMonitoring}");
            _logger?.LogInfo($"  HealthChecks: {_config.HealthChecksEnabled}");
            _logger?.LogInfo($"  Alerts: {_config.AlertsEnabled}");
            _logger?.LogInfo($"  MaxConcurrentHandlers: {_config.MaxConcurrentHandlers}");
            _logger?.LogInfo($"  MaxQueueSize: {_config.MaxQueueSize}");
            _logger?.LogInfo($"  RetryFailedMessages: {_config.RetryFailedMessages}");
            _logger?.LogInfo($"  UseCircuitBreaker: {_config.UseCircuitBreaker}");
            _logger?.LogInfo($"  UseObjectPooling: {_config.UseObjectPooling}");
        }

        private void PrepareSystemResources()
        {
            try
            {
                // Pre-allocate memory if configured
                if (_config.PreAllocateMemory)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                // Warm up thread pool if needed
                if (_config.WarmUpThreadPool)
                {
                    ThreadPool.SetMinThreads(_config.MaxConcurrentHandlers, _config.MaxConcurrentHandlers);
                }

                _logger?.LogInfo($"[{_correlationId}] System resources prepared successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Failed to prepare system resources");
                throw;
            }
        }

        private void RegisterExtensionMethods(ContainerBuilder builder)
        {
            try
            {
                // Register extension method delegates for fluent API support
                builder.Bind<Func<ContainerBuilder, ContainerBuilder>>()
                    .FromMethod(_ => (containerBuilder) => containerBuilder.AddMessageBusService(_config))
                    .AsSingle()
                    .WithId("MessageBusExtension");

                _logger?.LogInfo($"[{_correlationId}] Extension methods registered successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Failed to register extension methods");
                throw;
            }
        }

        private void RegisterHealthChecks()
        {
            try
            {
                if (!_config.HealthChecksEnabled)
                {
                    _logger?.LogInfo($"[{_correlationId}] Health checks disabled, skipping registration");
                    return;
                }

                if (!Container.HasBinding<IHealthCheckService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] IHealthCheckService not available, cannot register health checks");
                    return;
                }

                var healthService = Container.Resolve<IHealthCheckService>();
                var healthCheck = Container.Resolve<MessageBusHealthCheck>();

                healthService.RegisterHealthCheck(healthCheck);

                _logger?.LogInfo($"[{_correlationId}] MessageBus health check registered successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Failed to register health checks");
                throw;
            }
        }

        private void ConfigureAlerts()
        {
            try
            {
                if (!_config.AlertsEnabled)
                {
                    _logger?.LogInfo($"[{_correlationId}] Alerts disabled, skipping configuration");
                    return;
                }

                if (!Container.HasBinding<IAlertService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] IAlertService not available, cannot configure alerts");
                    return;
                }

                var alertService = Container.Resolve<IAlertService>();

                // Configure alert rules for critical thresholds
                var alertRules = new[]
                {
                    new AlertRule
                    {
                        Name = "MessageBus.HighErrorRate",
                        Description = "Message bus error rate exceeded critical threshold",
                        Severity = AlertSeverity.Critical,
                        Threshold = _config.CriticalErrorRateThreshold,
                        MetricName = "MessageBus.ErrorRate"
                    },
                    new AlertRule
                    {
                        Name = "MessageBus.LargeDeadLetterQueue",
                        Description = "Dead letter queue size exceeded critical threshold",
                        Severity = AlertSeverity.Critical,
                        Threshold = _config.CriticalQueueSizeThreshold,
                        MetricName = "MessageBus.DeadLetterQueueSize"
                    },
                    new AlertRule
                    {
                        Name = "MessageBus.SlowProcessing",
                        Description = "Message processing time exceeded critical threshold",
                        Severity = AlertSeverity.Warning,
                        Threshold = _config.CriticalProcessingTimeThreshold.TotalMilliseconds,
                        MetricName = "MessageBus.AverageProcessingTime"
                    }
                };

                foreach (var rule in alertRules)
                {
                    alertService.RegisterAlertRule(rule);
                }

                _logger?.LogInfo($"[{_correlationId}] Alert rules configured successfully ({alertRules.Length} rules)");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Failed to configure alerts");
                throw;
            }
        }

        private void SetupPerformanceMonitoring()
        {
            try
            {
                if (!_config.PerformanceMonitoring)
                {
                    _logger?.LogInfo($"[{_correlationId}] Performance monitoring disabled, skipping setup");
                    return;
                }

                if (!Container.HasBinding<IProfilerService>())
                {
                    _logger?.LogWarning($"[{_correlationId}] IProfilerService not available, cannot setup performance monitoring");
                    return;
                }

                var profilerService = Container.Resolve<IProfilerService>();

                // Register performance metrics
                var metrics = new[]
                {
                    "MessageBus.MessagesPublished",
                    "MessageBus.MessagesProcessed",
                    "MessageBus.MessagesFailed",
                    "MessageBus.AverageProcessingTime",
                    "MessageBus.ErrorRate",
                    "MessageBus.QueueDepth",
                    "MessageBus.MemoryUsage"
                };

                foreach (var metric in metrics)
                {
                    profilerService.RegisterMetric(metric);
                }

                // Setup metric alerts for performance thresholds
                profilerService.RegisterMetricAlert("MessageBus.AverageProcessingTime", 
                    _config.WarningProcessingTimeThreshold.TotalMilliseconds, 
                    AlertSeverity.Warning);

                profilerService.RegisterMetricAlert("MessageBus.ErrorRate", 
                    _config.WarningErrorRateThreshold, 
                    AlertSeverity.Warning);

                _logger?.LogInfo($"[{_correlationId}] Performance monitoring setup completed ({metrics.Length} metrics)");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Failed to setup performance monitoring");
                throw;
            }
        }

        private void ValidateFinalInstallation()
        {
            try
            {
                _logger?.LogInfo($"[{_correlationId}] Validating final installation");

                // Verify all required services are registered
                var requiredServices = new[]
                {
                    typeof(MessageBusConfig),
                    typeof(IMessageBusService),
                    typeof(IMessageBusFactory),
                    typeof(MessageBusConfigBuilder)
                };

                foreach (var serviceType in requiredServices)
                {
                    if (!Container.HasBinding(serviceType))
                    {
                        throw new InvalidOperationException($"Required service {serviceType.Name} is not registered");
                    }
                }

                // Test service resolution
                var messageBusService = Container.Resolve<IMessageBusService>();
                if (messageBusService == null)
                {
                    throw new InvalidOperationException("MessageBusService resolution failed");
                }

                // Test health status
                var healthStatus = messageBusService.GetHealthStatus();
                if (healthStatus == HealthStatus.Unhealthy)
                {
                    _logger?.LogWarning($"[{_correlationId}] MessageBus health status is unhealthy after installation");
                }

                // Verify configuration integrity
                var resolvedConfig = Container.Resolve<MessageBusConfig>();
                if (!ReferenceEquals(_config, resolvedConfig))
                {
                    throw new InvalidOperationException("Configuration instance mismatch");
                }

                _logger?.LogInfo($"[{_correlationId}] Final installation validation successful");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"[{_correlationId}] Final installation validation failed");
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private static MessageBusConfig CreateDefaultConfig()
        {
            return new MessageBusConfigBuilder()
                .WithInstanceName("DefaultMessageBus")
                .WithAsyncSupport(true)
                .WithPerformanceMonitoring(true)
                .WithHealthChecks(true)
                .WithAlerts(true)
                .WithRetryPolicy(true, 3, TimeSpan.FromSeconds(1))
                .WithCircuitBreaker(true)
                .WithObjectPooling(true)
                .WithMaxConcurrentHandlers(Environment.ProcessorCount * 2)
                .WithMaxQueueSize(10000)
                .WithHandlerTimeout(TimeSpan.FromSeconds(30))
                .WithStatisticsUpdateInterval(TimeSpan.FromSeconds(10))
                .WithHealthCheckInterval(TimeSpan.FromSeconds(30))
                .WithErrorRateThresholds(0.05, 0.10) // 5% warning, 10% critical
                .WithQueueSizeThresholds(1000, 5000) // 1000 warning, 5000 critical
                .WithProcessingTimeThresholds(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)) // 1s warning, 5s critical
                .Build();
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for ContainerBuilder to provide fluent API support for MessageBus registration.
    /// </summary>
    public static class MessageBusServiceExtensions
    {
        /// <summary>
        /// Adds MessageBus service with default configuration.
        /// </summary>
        /// <param name="builder">The container builder</param>
        /// <returns>The container builder for chaining</returns>
        public static ContainerBuilder AddMessageBusService(this ContainerBuilder builder)
        {
            return AddMessageBusService(builder, null);
        }

        /// <summary>
        /// Adds MessageBus service with custom configuration.
        /// </summary>
        /// <param name="builder">The container builder</param>
        /// <param name="config">The message bus configuration</param>
        /// <returns>The container builder for chaining</returns>
        public static ContainerBuilder AddMessageBusService(this ContainerBuilder builder, MessageBusConfig config)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var installer = new MessageBusInstaller(config);
            
            if (installer.ValidateInstaller())
            {
                installer.PreInstall();
                installer.Install(builder);
                installer.PostInstall();
            }
            else
            {
                throw new InvalidOperationException("MessageBusInstaller validation failed");
            }

            return builder;
        }

        /// <summary>
        /// Adds MessageBus service with configuration builder.
        /// </summary>
        /// <param name="builder">The container builder</param>
        /// <param name="configureAction">Action to configure the message bus</param>
        /// <returns>The container builder for chaining</returns>
        public static ContainerBuilder AddMessageBusService(this ContainerBuilder builder, 
            Action<MessageBusConfigBuilder> configureAction)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            var configBuilder = new MessageBusConfigBuilder();
            configureAction(configBuilder);
            var config = configBuilder.Build();

            return AddMessageBusService(builder, config);
        }
    }
}