using System;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Production-ready factory for creating PoolingService instances with refactored architecture.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// Handles complex initialization and dependency wiring with proper separation of concerns.
    /// </summary>
    public sealed class PoolingServiceFactory : IPoolingServiceFactory
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly ISerializationService _serializationService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ICircuitBreakerFactory _circuitBreakerFactory;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolingServiceFactory with refactored architecture support.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        /// <param name="serializationService">Serialization service for pool state persistence</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="healthCheckService">Health check service for monitoring</param>
        /// <param name="circuitBreakerFactory">Circuit breaker factory for resilience</param>
        public PoolingServiceFactory(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            ISerializationService serializationService,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IHealthCheckService healthCheckService = null,
            ICircuitBreakerFactory circuitBreakerFactory = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            _alertService = alertService;
            _profilerService = profilerService;
            _healthCheckService = healthCheckService;
            _circuitBreakerFactory = circuitBreakerFactory;
        }

        #endregion

        #region Synchronous Creation

        /// <inheritdoc />
        public IPoolingService CreatePoolingService(PoolingServiceConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (!configuration.IsValid())
                throw new ArgumentException("Invalid pooling service configuration", nameof(configuration));

            _loggingService.LogInfo($"Creating PoolingService with refactored architecture: {configuration.ServiceName}");

            try
            {
                // Create core services
                var poolRegistry = new PoolRegistry(_loggingService);
                var poolCreationService = CreatePoolCreationService();
                
                // Create new specialized services (refactored architecture)
                var messagePublisher = CreateMessagePublisher();
                var circuitBreakerHandler = CreateCircuitBreakerHandler(messagePublisher);
                var operationCoordinator = CreateOperationCoordinator(configuration, poolRegistry, circuitBreakerHandler, messagePublisher);
                
                // Create legacy specialized services (maintained for compatibility)
                var autoScalingService = CreateAutoScalingService(configuration);
                var errorRecoveryService = CreateErrorRecoveryService(configuration);
                var performanceMonitorService = CreatePerformanceMonitorService(configuration);

                // Create the main pooling service with refactored architecture
                var poolingService = new PoolingService(
                    configuration,
                    poolRegistry,
                    poolCreationService,
                    _loggingService,
                    _messageBusService,
                    operationCoordinator,
                    messagePublisher,
                    circuitBreakerHandler,
                    _serializationService,
                    _alertService,
                    _profilerService,
                    autoScalingService,
                    errorRecoveryService,
                    performanceMonitorService,
                    _healthCheckService);

                _loggingService.LogInfo($"Successfully created PoolingService with refactored architecture: {configuration.ServiceName}");
                return poolingService;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to create PoolingService: {configuration.ServiceName}", ex);
                _alertService?.RaiseAlert(
                    $"PoolingService creation failed: {ex.Message}",
                    Alerting.Models.AlertSeverity.Critical,
                    "PoolingServiceFactory");
                throw;
            }
        }

        #endregion

        #region Asynchronous Creation

        /// <inheritdoc />
        public async UniTask<IPoolingService> CreatePoolingServiceAsync(PoolingServiceConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (!configuration.IsValid())
                throw new ArgumentException("Invalid pooling service configuration", nameof(configuration));

            _loggingService.LogInfo($"Creating PoolingService asynchronously with refactored architecture: {configuration.ServiceName}");

            try
            {
                // Create core services
                var poolRegistry = new PoolRegistry(_loggingService);
                var poolCreationService = CreatePoolCreationService();
                
                // Create new specialized services asynchronously (refactored architecture)
                var messagePublisher = await CreateMessagePublisherAsync();
                var circuitBreakerHandler = await CreateCircuitBreakerHandlerAsync(messagePublisher);
                var operationCoordinator = await CreateOperationCoordinatorAsync(configuration, poolRegistry, circuitBreakerHandler, messagePublisher);
                
                // Create legacy specialized services asynchronously (maintained for compatibility)
                var autoScalingService = await CreateAutoScalingServiceAsync(configuration);
                var errorRecoveryService = await CreateErrorRecoveryServiceAsync(configuration);
                var performanceMonitorService = await CreatePerformanceMonitorServiceAsync(configuration);

                // Create the main pooling service with refactored architecture
                var poolingService = new PoolingService(
                    configuration,
                    poolRegistry,
                    poolCreationService,
                    _loggingService,
                    _messageBusService,
                    operationCoordinator,
                    messagePublisher,
                    circuitBreakerHandler,
                    _serializationService,
                    _alertService,
                    _profilerService,
                    autoScalingService,
                    errorRecoveryService,
                    performanceMonitorService,
                    _healthCheckService);

                _loggingService.LogInfo($"Successfully created PoolingService asynchronously with refactored architecture: {configuration.ServiceName}");
                return poolingService;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to create PoolingService asynchronously: {configuration.ServiceName}", ex);
                _alertService?.RaiseAlert(
                    $"PoolingService async creation failed: {ex.Message}",
                    Alerting.Models.AlertSeverity.Critical,
                    "PoolingServiceFactory");
                throw;
            }
        }

        #endregion

        #region Private Factory Methods - Core Services

        /// <summary>
        /// Creates the pool creation service with proper dependencies.
        /// </summary>
        private IPoolCreationService CreatePoolCreationService()
        {
            return new PoolCreationService(
                _loggingService,
                _messageBusService,
                _alertService,
                _profilerService);
        }

        #endregion

        #region Private Factory Methods - New Specialized Services

        /// <summary>
        /// Creates the message publisher service for pool events.
        /// </summary>
        private IPoolMessagePublisher CreateMessagePublisher()
        {
            return new PoolMessagePublisher(
                _messageBusService,
                _loggingService,
                _profilerService);
        }

        /// <summary>
        /// Creates the circuit breaker handler service.
        /// </summary>
        private IPoolCircuitBreakerHandler CreateCircuitBreakerHandler(IPoolMessagePublisher messagePublisher)
        {
            return new PoolCircuitBreakerHandler(
                _loggingService,
                messagePublisher,
                _profilerService,
                _alertService);
        }

        /// <summary>
        /// Creates the operation coordinator service.
        /// </summary>
        private IPoolOperationCoordinator CreateOperationCoordinator(
            PoolingServiceConfiguration configuration,
            IPoolRegistry poolRegistry,
            IPoolCircuitBreakerHandler circuitBreakerHandler,
            IPoolMessagePublisher messagePublisher)
        {
            // Create specialized services for operation coordinator if needed
            var autoScalingService = CreateAutoScalingService(configuration);
            var errorRecoveryService = CreateErrorRecoveryService(configuration);
            var performanceMonitorService = CreatePerformanceMonitorService(configuration);

            return new PoolOperationCoordinator(
                configuration,
                poolRegistry,
                circuitBreakerHandler,
                messagePublisher,
                _loggingService,
                _profilerService,
                _alertService,
                autoScalingService,
                errorRecoveryService,
                performanceMonitorService);
        }

        /// <summary>
        /// Creates the message publisher service asynchronously.
        /// </summary>
        private async UniTask<IPoolMessagePublisher> CreateMessagePublisherAsync()
        {
            // For now, delegate to synchronous method, but allows for future async initialization
            await UniTask.Yield();
            return CreateMessagePublisher();
        }

        /// <summary>
        /// Creates the circuit breaker handler service asynchronously.
        /// </summary>
        private async UniTask<IPoolCircuitBreakerHandler> CreateCircuitBreakerHandlerAsync(IPoolMessagePublisher messagePublisher)
        {
            // For now, delegate to synchronous method, but allows for future async initialization
            await UniTask.Yield();
            return CreateCircuitBreakerHandler(messagePublisher);
        }

        /// <summary>
        /// Creates the operation coordinator service asynchronously.
        /// </summary>
        private async UniTask<IPoolOperationCoordinator> CreateOperationCoordinatorAsync(
            PoolingServiceConfiguration configuration,
            IPoolRegistry poolRegistry,
            IPoolCircuitBreakerHandler circuitBreakerHandler,
            IPoolMessagePublisher messagePublisher)
        {
            // For now, delegate to synchronous method, but allows for future async initialization
            await UniTask.Yield();
            return CreateOperationCoordinator(configuration, poolRegistry, circuitBreakerHandler, messagePublisher);
        }

        #endregion

        #region Private Factory Methods - Legacy Specialized Services

        /// <summary>
        /// Creates the auto-scaling service if enabled in configuration.
        /// </summary>
        private IPoolAutoScalingService CreateAutoScalingService(PoolingServiceConfiguration configuration)
        {
            if (!configuration.EnableAutoScaling)
                return null;

            // Use factory if available, otherwise create directly
            var factory = new PoolAutoScalingServiceFactory(_loggingService, _messageBusService, _alertService, _profilerService);
            var autoScalingConfig = new PoolAutoScalingConfiguration
            {
                CheckInterval = configuration.AutoScalingCheckInterval,
                ScaleUpThreshold = configuration.ScalingExpansionThreshold,
                ScaleDownThreshold = configuration.ScalingContractionThreshold,
                EnableAutoScaling = true
            };
            
            return factory.CreateAutoScalingService(autoScalingConfig);
        }

        /// <summary>
        /// Creates the error recovery service if enabled in configuration.
        /// </summary>
        private IPoolErrorRecoveryService CreateErrorRecoveryService(PoolingServiceConfiguration configuration)
        {
            if (!configuration.EnableErrorRecovery)
                return null;

            // Use factory if available, otherwise create directly
            var factory = new PoolErrorRecoveryServiceFactory(_loggingService, _messageBusService, _alertService, _profilerService);
            var errorRecoveryConfig = new PoolErrorRecoveryConfiguration
            {
                DefaultMaxRetries = configuration.MaxRetryAttempts,
                InitialRetryDelay = configuration.RetryDelay,
                EnableEmergencyRecovery = configuration.EnableEmergencyRecovery,
                EnableCircuitBreaker = configuration.EnableCircuitBreaker
            };
            
            return factory.CreateErrorRecoveryService(errorRecoveryConfig);
        }

        /// <summary>
        /// Creates the performance monitor service if enabled in configuration.
        /// </summary>
        private IPoolPerformanceMonitorService CreatePerformanceMonitorService(PoolingServiceConfiguration configuration)
        {
            if (!configuration.EnablePerformanceBudgets)
                return null;

            // Use factory if available, otherwise create directly
            var factory = new PoolPerformanceMonitorServiceFactory(_loggingService, _messageBusService, _alertService, _profilerService);
            var performanceConfig = new PoolPerformanceMonitorConfiguration
            {
                EnablePerformanceMonitoring = true,
                EnablePerformanceBudgets = configuration.EnablePerformanceBudgets,
                EnableStatisticsCollection = configuration.EnableDetailedStatistics,
                StatisticsRetentionPeriod = configuration.StatisticsRetentionPeriod,
                ReportingInterval = configuration.StatisticsUpdateInterval
            };
            
            return factory.CreatePerformanceMonitorService(performanceConfig);
        }

        /// <summary>
        /// Creates the auto-scaling service asynchronously if enabled in configuration.
        /// </summary>
        private async UniTask<IPoolAutoScalingService> CreateAutoScalingServiceAsync(PoolingServiceConfiguration configuration)
        {
            if (!configuration.EnableAutoScaling)
                return null;

            // For now, delegate to synchronous method, but allows for future async initialization
            await UniTask.Yield();
            return CreateAutoScalingService(configuration);
        }

        /// <summary>
        /// Creates the error recovery service asynchronously if enabled in configuration.
        /// </summary>
        private async UniTask<IPoolErrorRecoveryService> CreateErrorRecoveryServiceAsync(PoolingServiceConfiguration configuration)
        {
            if (!configuration.EnableErrorRecovery)
                return null;

            // For now, delegate to synchronous method, but allows for future async initialization
            await UniTask.Yield();
            return CreateErrorRecoveryService(configuration);
        }

        /// <summary>
        /// Creates the performance monitor service asynchronously if enabled in configuration.
        /// </summary>
        private async UniTask<IPoolPerformanceMonitorService> CreatePerformanceMonitorServiceAsync(PoolingServiceConfiguration configuration)
        {
            if (!configuration.EnablePerformanceBudgets)
                return null;

            // For now, delegate to synchronous method, but allows for future async initialization
            await UniTask.Yield();
            return CreatePerformanceMonitorService(configuration);
        }

        #endregion
    }
}