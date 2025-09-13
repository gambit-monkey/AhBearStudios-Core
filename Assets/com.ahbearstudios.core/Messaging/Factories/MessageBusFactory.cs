using System;
using System.Collections.Generic;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Builders;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.HealthChecks;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging.Factories
{
    /// <summary>
    /// Factory for creating message bus service instances.
    /// Creates orchestrator and all specialized services with proper dependency resolution.
    /// Handles validation, configuration, and dependency injection without circular dependencies.
    /// </summary>
    public sealed class MessageBusFactory : IMessageBusFactory
    {
        private readonly ILoggingService _logger;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;
        private readonly ICircuitBreakerFactory _circuitBreakerFactory;

        /// <summary>
        /// Initializes a new instance of the MessageBusFactory class.
        /// </summary>
        /// <param name="logger">The logging service</param>
        /// <param name="healthCheckService">The health check service</param>
        /// <param name="alertService">The alert service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <param name="circuitBreakerFactory">The circuit breaker factory</param>
        /// <exception cref="ArgumentNullException">Thrown when any required service is null</exception>
        public MessageBusFactory(
            ILoggingService logger,
            IHealthCheckService healthCheckService,
            IAlertService alertService,
            IProfilerService profilerService,
            IPoolingService poolingService,
            ICircuitBreakerFactory circuitBreakerFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            _circuitBreakerFactory = circuitBreakerFactory ?? throw new ArgumentNullException(nameof(circuitBreakerFactory));

            _logger.LogInfo("MessageBusFactory initialized successfully");
        }

        /// <summary>
        /// Creates a new message bus service instance with the specified configuration.
        /// </summary>
        /// <param name="config">The message bus configuration</param>
        /// <returns>A configured message bus service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="ArgumentException">Thrown when config is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when factory dependencies are not available</exception>
        public IMessageBusService CreateMessageBus(MessageBusConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _logger.LogInfo($"Creating message bus orchestrator with configuration: {config.InstanceName}");

            try
            {
                // Validate configuration
                ValidateConfiguration(config);

                // Create individual service configurations from the main config
                var publishingConfig = CreatePublishingConfig(config);
                var subscriptionConfig = CreateSubscriptionConfig(config);
                var monitoringConfig = CreateMonitoringConfig(config);
                var healthConfig = CreateHealthConfig(config);

                // Create specialized services (no circular dependencies)
                var publishingService = CreatePublishingService(publishingConfig);
                var subscriptionService = CreateSubscriptionService(subscriptionConfig);
                var monitoringService = CreateMonitoringService(monitoringConfig);
                // TODO: Implement these services when they become available
                IMessageBusHealthService healthService = null; // CreateHealthService(healthConfig);
                IMessageRetryService retryService = null; // CreateRetryService();
                IDeadLetterQueueService deadLetterQueueService = null; // CreateDeadLetterQueueService();
                IMessageCircuitBreakerService circuitBreakerService = null; // CreateCircuitBreakerService();

                // Create the orchestrator
                var messageBusService = new MessageBusService(
                    publishingService,
                    subscriptionService,
                    monitoringService,
                    healthService,
                    retryService,
                    deadLetterQueueService,
                    circuitBreakerService,
                    _logger,
                    _profilerService);

                // Register health check if enabled
                if (config.HealthChecksEnabled)
                {
                    RegisterHealthCheck(messageBusService, config);
                }

                _logger.LogInfo($"Message bus orchestrator '{config.InstanceName}' created successfully");
                return messageBusService;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to create message bus '{config.InstanceName}'", ex);
                
                if (config.AlertsEnabled)
                {
                    _alertService.RaiseAlert(
                        $"Failed to create message bus: {ex.Message}",
                        AlertSeverity.Critical,
                        "MessageBusFactory",
                        "Creation");
                }

                throw;
            }
        }

        /// <summary>
        /// Creates a message bus service with default configuration.
        /// </summary>
        /// <returns>A message bus service instance with default settings</returns>
        public IMessageBusService CreateDefaultMessageBus()
        {
            _logger.LogInfo("Creating message bus with default configuration");
            return CreateMessageBus(MessageBusConfig.Default);
        }

        /// <summary>
        /// Creates a high-performance message bus service instance.
        /// </summary>
        /// <returns>A message bus service optimized for high throughput</returns>
        public IMessageBusService CreateHighPerformanceMessageBus()
        {
            _logger.LogInfo("Creating high-performance message bus");
            return CreateMessageBus(MessageBusConfig.HighPerformance);
        }

        /// <summary>
        /// Creates a reliable message bus service instance.
        /// </summary>
        /// <returns>A message bus service optimized for reliability</returns>
        public IMessageBusService CreateReliableMessageBus()
        {
            _logger.LogInfo("Creating reliable message bus");
            return CreateMessageBus(MessageBusConfig.Reliable);
        }

        #region Service Creation Methods

        /// <summary>
        /// Creates the message publishing service configuration from the main config.
        /// </summary>
        /// <param name="config">The main message bus configuration</param>
        /// <returns>Publishing service configuration</returns>
        private MessagePublishingConfig CreatePublishingConfig(MessageBusConfig config)
        {
            return new MessagePublishingConfigBuilder()
                .WithMaxConcurrentPublishers(config.MaxConcurrentHandlers)
                .WithBatchSize(Math.Min(config.MaxConcurrentHandlers, 100))
                .WithPublishTimeout(config.HandlerTimeout)
                .WithCircuitBreakerEnabled(true)
                .WithRetryEnabled(config.RetryFailedMessages)
                .WithPerformanceMonitoring(config.PerformanceMonitoring)
                .Build();
        }

        /// <summary>
        /// Creates the message subscription service configuration from the main config.
        /// </summary>
        /// <param name="config">The main message bus configuration</param>
        /// <returns>Subscription service configuration</returns>
        private MessageSubscriptionConfig CreateSubscriptionConfig(MessageBusConfig config)
        {
            return new MessageSubscriptionConfigBuilder()
                .WithMaxSubscribers(config.MaxConcurrentHandlers)
                .WithMaxConcurrentHandlers(config.MaxConcurrentHandlers)
                .WithHandlerTimeout(config.HandlerTimeout)
                .WithAsyncEnabled(true)
                .WithFilteringEnabled(true)
                .WithPriorityRouting(true)
                .WithScopedSubscriptions(true)
                .WithPerformanceMonitoring(config.PerformanceMonitoring)
                .Build();
        }

        /// <summary>
        /// Creates the message bus monitoring service configuration from the main config.
        /// </summary>
        /// <param name="config">The main message bus configuration</param>
        /// <returns>Monitoring service configuration</returns>
        private MessageBusMonitoringConfig CreateMonitoringConfig(MessageBusConfig config)
        {
            return new MessageBusMonitoringConfigBuilder()
                .WithMonitoringEnabled(true)
                .WithPerformanceTrendAnalysis(config.PerformanceMonitoring)
                .WithStatisticsUpdateInterval(config.StatisticsUpdateInterval)
                .WithHistoricalDataRetention(TimeSpan.FromMinutes(10))
                .WithAnomalyDetection(true)
                .WithPerTypeStatistics(config.PerformanceMonitoring)
                .Build();
        }

        /// <summary>
        /// Creates the message bus health service configuration from the main config.
        /// </summary>
        /// <param name="config">The main message bus configuration</param>
        /// <returns>Health service configuration</returns>
        private MessageBusHealthConfig CreateHealthConfig(MessageBusConfig config)
        {
            return new MessageBusHealthConfigBuilder()
                .WithHealthMonitoringEnabled(config.HealthChecksEnabled)
                .WithSystemHealthCheckInterval(config.HealthCheckInterval)
                .WithHealthAlertingEnabled(config.AlertsEnabled)
                .WithCriticalServicesOverride(true)
                .WithPerformanceOptimization(config.PerformanceMonitoring)
                .Build();
        }

        /// <summary>
        /// Creates the message publishing service instance.
        /// </summary>
        /// <param name="config">The publishing service configuration</param>
        /// <returns>Publishing service instance</returns>
        private IMessagePublishingService CreatePublishingService(MessagePublishingConfig config)
        {
            // Create circuit breaker service first (no dependencies on message bus)
            var circuitBreakerConfig = new MessageCircuitBreakerConfigBuilder()
                .WithDefaultConfig()
                .WithStateChangeMessages(config.PerformanceMonitoringEnabled)
                .WithPerformanceMonitoring(config.PerformanceMonitoringEnabled)
                .Build();

            var circuitBreakerService = new MessageCircuitBreakerService(
                circuitBreakerConfig,
                _logger,
                _circuitBreakerFactory,
                null); // No message bus dependency

            // Create publishing service
            return new MessagePublishingService(
                config,
                _logger,
                _alertService,
                _profilerService,
                _poolingService);
        }

        /// <summary>
        /// Creates the message subscription service instance.
        /// </summary>
        /// <param name="config">The subscription service configuration</param>
        /// <returns>Subscription service instance</returns>
        private IMessageSubscriptionService CreateSubscriptionService(MessageSubscriptionConfig config)
        {
            return new MessageSubscriptionService(
                config,
                _logger,
                null, // messageBusAdapter
                _alertService,
                _profilerService,
                _poolingService);
        }

        /// <summary>
        /// Creates the message bus monitoring service instance.
        /// </summary>
        /// <param name="config">The monitoring service configuration</param>
        /// <returns>Monitoring service instance</returns>
        private IMessageBusMonitoringService CreateMonitoringService(MessageBusMonitoringConfig config)
        {
            return new MessageBusMonitoringService(
                config,
                _logger,
                _profilerService,
                _poolingService);
        }

        /// <summary>
        /// Creates the message bus health service instance.
        /// </summary>
        /// <param name="config">The health service configuration</param>
        /// <returns>Health service instance</returns>
        private IMessageBusHealthService CreateHealthService(MessageBusHealthConfig config)
        {
            // TODO: Implement MessageBusHealthService
            throw new NotImplementedException("MessageBusHealthService is not yet implemented");
            /*
            return new MessageBusHealthService(
                config,
                _logger,
                _alertService,
                _profilerService);
            */
        }

        /// <summary>
        /// Creates the message retry service instance.
        /// </summary>
        /// <returns>Retry service instance</returns>
        private IMessageRetryService CreateRetryService()
        {
            // TODO: Implement MessageRetryService
            throw new NotImplementedException("MessageRetryService is not yet implemented");
            /*
            // Use default retry configuration for now
            var retryConfig = new RetryPolicy
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromSeconds(1),
                BackoffStrategy = RetryBackoffStrategy.Exponential,
                Enabled = true
            };

            return new MessageRetryService(
                retryConfig,
                _logger,
                _profilerService,
                _poolingService);
            */
        }

        /// <summary>
        /// Creates the dead letter queue service instance.
        /// </summary>
        /// <returns>Dead letter queue service instance</returns>
        private IDeadLetterQueueService CreateDeadLetterQueueService()
        {
            // TODO: Implement DeadLetterQueueService
            throw new NotImplementedException("DeadLetterQueueService is not yet implemented");
            /*
            return new DeadLetterQueueService(
                _logger,
                _profilerService,
                _poolingService);
            */
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates the message bus configuration for correctness and completeness.
        /// </summary>
        /// <param name="config">The configuration to validate</param>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
        private void ValidateConfiguration(MessageBusConfig config)
        {
            _logger.LogInfo($"Validating configuration for message bus '{config.InstanceName}'");

            if (!config.IsValid())
            {
                var errorMessage = "Message bus configuration is invalid";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(config));
            }

            // Validate that required services are available for enabled features
            if (config.HealthChecksEnabled && _healthCheckService == null)
            {
                var errorMessage = "Health checks are enabled but IHealthCheckService is not available";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            if (config.AlertsEnabled && _alertService == null)
            {
                var errorMessage = "Alerts are enabled but IAlertService is not available";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            if (config.PerformanceMonitoring && _profilerService == null)
            {
                var errorMessage = "Performance monitoring is enabled but IProfilerService is not available";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validate configuration ranges
            if (config.MaxConcurrentHandlers > 10000)
            {
                _logger.LogWarning($"Very high MaxConcurrentHandlers value: {config.MaxConcurrentHandlers}");
            }

            if (config.MaxQueueSize > 100000)
            {
                _logger.LogWarning($"Very high MaxQueueSize value: {config.MaxQueueSize}");
            }

            if (config.HandlerTimeout.TotalMinutes > 10)
            {
                _logger.LogWarning($"Very high HandlerTimeout value: {config.HandlerTimeout}");
            }

            _logger.LogInfo($"Configuration validation completed for message bus '{config.InstanceName}'");
        }

        /// <summary>
        /// Registers the health check for the message bus service.
        /// </summary>
        /// <param name="messageBusService">The message bus service</param>
        /// <param name="config">The message bus configuration</param>
        private void RegisterHealthCheck(IMessageBusService messageBusService, MessageBusConfig config)
        {
            try
            {
                _logger.LogInfo($"Registering health check for message bus '{config.InstanceName}'");

                var healthCheck = new MessageBusHealthCheck(messageBusService, config, _logger);
                _healthCheckService.RegisterHealthCheck(healthCheck);

                _logger.LogInfo($"Health check registered successfully for message bus '{config.InstanceName}'");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to register health check for message bus '{config.InstanceName}'", ex);
                
                if (config.AlertsEnabled)
                {
                    _alertService.RaiseAlert(
                        $"Failed to register health check: {ex.Message}",
                        AlertSeverity.Warning,
                        "MessageBusFactory",
                        "HealthCheck");
                }
            }
        }

        /// <summary>
        /// Validates that all required dependencies are available.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when required dependencies are missing</exception>
        public void ValidateDependencies()
        {
            var missingDependencies = new List<string>();

            if (_logger == null) missingDependencies.Add(nameof(ILoggingService));
            if (_healthCheckService == null) missingDependencies.Add(nameof(IHealthCheckService));
            if (_alertService == null) missingDependencies.Add(nameof(IAlertService));
            if (_profilerService == null) missingDependencies.Add(nameof(IProfilerService));
            if (_poolingService == null) missingDependencies.Add(nameof(IPoolingService));
            if (_circuitBreakerFactory == null) missingDependencies.Add(nameof(ICircuitBreakerFactory));

            if (missingDependencies.AsValueEnumerable().Any())
            {
                var errorMessage = $"Required dependencies are missing: {string.Join(", ", missingDependencies)}";
                throw new InvalidOperationException(errorMessage);
            }
        }

        #endregion
    }
}