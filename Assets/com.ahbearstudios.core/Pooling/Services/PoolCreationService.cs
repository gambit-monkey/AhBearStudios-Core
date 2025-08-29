using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Strategies;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool creation service.
    /// Handles complex pool instantiation logic with strategy selection and validation.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public sealed class PoolCreationService : IPoolCreationService
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingStrategySelector _strategySelector;
        private readonly IPoolTypeSelector _poolTypeSelector;
        private readonly IPoolValidationService _validationService;
        private readonly IPooledNetworkBufferFactory _bufferFactory;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolCreationService.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="strategySelector">Strategy selector for choosing pooling strategies</param>
        /// <param name="poolTypeSelector">Pool type selector for choosing pool implementations</param>
        /// <param name="validationService">Validation service for pool operations</param>
        /// <param name="bufferFactory">Factory for creating network buffers</param>
        public PoolCreationService(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolingStrategySelector strategySelector = null,
            IPoolTypeSelector poolTypeSelector = null,
            IPoolValidationService validationService = null,
            IPooledNetworkBufferFactory bufferFactory = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _alertService = alertService;
            _profilerService = profilerService;
            
            // Initialize strategy selector with fallback if none provided
            _strategySelector = strategySelector ?? CreateDefaultStrategySelector();
            
            // Initialize pool type selector with fallback if none provided
            _poolTypeSelector = poolTypeSelector ?? new PoolTypeSelector(_loggingService, _messageBusService);
            
            _validationService = validationService ?? new PoolValidationService();
            _bufferFactory = bufferFactory ?? new PooledNetworkBufferFactory();
        }

        #endregion

        #region Pool Creation

        /// <inheritdoc />
        public IObjectPool<T> CreatePool<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var poolType = typeof(T);
            var poolTypeName = poolType.Name;

            _loggingService.LogInfo($"Creating pool for type {poolTypeName} with configuration: {configuration.Name}");

            // Validate configuration first
            if (!ValidateConfiguration<T>(configuration))
            {
                var errors = GetValidationErrors<T>(configuration);
                var errorMessage = $"Invalid configuration for pool type {poolTypeName}: {string.Join(", ", errors)}";
                _loggingService.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(configuration));
            }

            try
            {
                // Select appropriate strategy and pool type
                var strategy = _strategySelector.SelectStrategy(configuration);
                var selectedPoolType = _poolTypeSelector.SelectPoolType<T>(configuration);
                
                // Create the pool instance
                var pool = CreatePoolInstance<T>(selectedPoolType, configuration, strategy);
                
                _loggingService.LogInfo($"Successfully created {selectedPoolType} pool for type {poolTypeName}");
                return pool;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to create pool for type {poolTypeName}", ex);
                _alertService?.RaiseAlert(
                    $"Pool creation failed for type {poolTypeName}: {ex.Message}",
                    AlertSeverity.Critical,
                    "PoolCreationService");
                throw;
            }
        }

        /// <inheritdoc />
        public IObjectPool<T> CreatePool<T>(string poolName = null) where T : class, IPooledObject, new()
        {
            var name = poolName ?? typeof(T).Name;
            var config = PoolConfiguration.CreateDefault(name);
            return CreatePool<T>(config);
        }

        #endregion

        #region Pool Type Selection

        /// <inheritdoc />
        public PoolType SelectPoolType<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return _poolTypeSelector.SelectPoolType<T>(configuration);
        }

        #endregion

        #region Strategy Creation

        /// <inheritdoc />
        public IPoolingStrategy CreateStrategy(PoolConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return _strategySelector.SelectStrategy(configuration);
        }

        /// <inheritdoc />
        public IPoolingStrategy CreateDefaultStrategy(PoolType poolType)
        {
            // Create a basic configuration to get default strategy
            var config = PoolConfiguration.CreateDefault($"Default_{poolType}");
            return _strategySelector.SelectStrategy(config);
        }

        #endregion

        #region Validation

        /// <inheritdoc />
        public bool ValidateConfiguration<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            var errors = GetValidationErrors<T>(configuration);
            return errors.Length == 0;
        }

        /// <inheritdoc />
        public string[] GetValidationErrors<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            if (configuration == null)
                return new[] { "Configuration cannot be null" };

            var errors = new List<string>();
            var poolType = typeof(T);

            // Basic configuration validation
            if (string.IsNullOrWhiteSpace(configuration.Name))
                errors.Add("Pool name cannot be null or empty");

            if (configuration.InitialCapacity < 0)
                errors.Add("Initial capacity cannot be negative");

            if (configuration.MaxCapacity <= 0)
                errors.Add("Max capacity must be greater than zero");

            if (configuration.InitialCapacity > configuration.MaxCapacity)
                errors.Add("Initial capacity cannot exceed max capacity");

            // Type-specific validation
            if (poolType == typeof(PooledNetworkBuffer))
            {
                // Network buffer pools have specific requirements
                if (configuration.MaxCapacity > 10000)
                    errors.Add("Network buffer pools should not exceed 10,000 objects for memory efficiency");
            }
            else if (poolType == typeof(ManagedLogData))
            {
                // Log data pools have specific requirements
                if (configuration.InitialCapacity == 0)
                    errors.Add("Log data pools should have non-zero initial capacity for performance");
            }

            // Performance budget validation
            if (configuration.PerformanceBudget != null)
            {
                if (configuration.PerformanceBudget.MaxOperationTime <= TimeSpan.Zero)
                    errors.Add("Performance budget max operation time must be greater than zero");

                if (configuration.PerformanceBudget.MaxValidationTime <= TimeSpan.Zero)
                    errors.Add("Performance budget max validation time must be greater than zero");

                if (configuration.PerformanceBudget.TargetFrameRate <= 0)
                    errors.Add("Performance budget target frame rate must be greater than zero");

                if (configuration.PerformanceBudget.FrameTimePercentage <= 0 || configuration.PerformanceBudget.FrameTimePercentage > 1.0)
                    errors.Add("Performance budget frame time percentage must be between 0 and 1");
            }

            return errors.ToArray();
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Creates a pool instance using the appropriate factory based on pool type.
        /// </summary>
        /// <typeparam name="T">Type of objects that will be pooled</typeparam>
        /// <param name="poolType">The type of pool to create</param>
        /// <param name="configuration">Pool configuration</param>
        /// <param name="strategy">Pooling strategy to use</param>
        /// <returns>Configured pool instance</returns>
        private IObjectPool<T> CreatePoolInstance<T>(PoolType poolType, PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            _loggingService.LogInfo($"Creating {poolType} pool for {typeof(T).Name}");

            return poolType switch
            {
                PoolType.SmallBuffer => CreateNetworkBufferPool<T>(PoolType.SmallBuffer, configuration, strategy),
                PoolType.MediumBuffer => CreateNetworkBufferPool<T>(PoolType.MediumBuffer, configuration, strategy),
                PoolType.LargeBuffer => CreateNetworkBufferPool<T>(PoolType.LargeBuffer, configuration, strategy),
                PoolType.CompressionBuffer => CreateNetworkBufferPool<T>(PoolType.CompressionBuffer, configuration, strategy),
                PoolType.ManagedLogData => CreateManagedLogDataPool<T>(configuration, strategy),
                PoolType.Generic => CreateGenericPool<T>(configuration, strategy),
                _ => CreateGenericPool<T>(configuration, strategy)
            };
        }

        /// <summary>
        /// Creates network buffer pools using type-safe casting for PooledNetworkBuffer types.
        /// </summary>
        private IObjectPool<T> CreateNetworkBufferPool<T>(PoolType poolType, PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            // Network buffer pools only work with PooledNetworkBuffer types
            if (typeof(T) == typeof(PooledNetworkBuffer))
            {
                // Create strategy factory for network buffer pools
                var configBuilder = new PoolingStrategyConfigBuilder();
                var adaptiveNetworkStrategyFactory = new AdaptiveNetworkStrategyFactory(
                    _loggingService, _profilerService, _alertService, _messageBusService, configBuilder);

                IObjectPool<PooledNetworkBuffer> networkBufferPool = poolType switch
                {
                    PoolType.SmallBuffer => new SmallBufferPool(configuration, adaptiveNetworkStrategyFactory),
                    PoolType.MediumBuffer => new MediumBufferPool(configuration, 
                        new HighPerformanceStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder)),
                    PoolType.LargeBuffer => new LargeBufferPool(configuration, 
                        new DynamicSizeStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder)),
                    PoolType.CompressionBuffer => new CompressionBufferPool(configuration, adaptiveNetworkStrategyFactory),
                    _ => throw new NotSupportedException($"Network buffer pool type {poolType} is not supported")
                };

                return (IObjectPool<T>)(object)networkBufferPool;
            }

            // For non-PooledNetworkBuffer types, fall back to generic pool
            _loggingService.LogWarning($"Type {typeof(T).Name} is not compatible with network buffer pool {poolType}, using generic pool");
            return CreateGenericPool<T>(configuration, strategy);
        }

        /// <summary>
        /// Creates managed log data pools using type-safe casting.
        /// </summary>
        private IObjectPool<T> CreateManagedLogDataPool<T>(PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            if (typeof(T) == typeof(ManagedLogData))
            {
                // For ManagedLogData, use a high-performance strategy with the generic pool
                var configBuilder = new PoolingStrategyConfigBuilder();
                var highPerformanceStrategyFactory = new HighPerformanceStrategyFactory(
                    _loggingService, _profilerService, _alertService, _messageBusService, configBuilder);

                var highPerformanceStrategy = highPerformanceStrategyFactory.CreateDefault();
                return new GenericObjectPool<T>(configuration, _messageBusService, highPerformanceStrategy);
            }

            // For non-ManagedLogData types, fall back to generic pool with provided strategy
            _loggingService.LogWarning($"Type {typeof(T).Name} is not compatible with ManagedLogData pool, using generic pool");
            return CreateGenericPool<T>(configuration, strategy);
        }

        /// <summary>
        /// Creates generic pools that can handle any IPooledObject type.
        /// </summary>
        private IObjectPool<T> CreateGenericPool<T>(PoolConfiguration configuration, IPoolingStrategy strategy)
            where T : class, IPooledObject, new()
        {
            return new GenericObjectPool<T>(configuration, _messageBusService, strategy);
        }

        /// <summary>
        /// Creates a default strategy selector when none is provided.
        /// </summary>
        private IPoolingStrategySelector CreateDefaultStrategySelector()
        {
            try
            {
                // Create a shared config builder for all strategy factories
                var configBuilder = new PoolingStrategyConfigBuilder();

                // Create strategy factories with dependencies
                var fixedSizeFactory = new FixedSizeStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder);
                var dynamicSizeFactory = new DynamicSizeStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder);
                var highPerformanceFactory = new HighPerformanceStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder);
                var adaptiveNetworkFactory = new AdaptiveNetworkStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder);
                var circuitBreakerFactory = new CircuitBreakerStrategyFactory(_loggingService, _profilerService, _alertService, _messageBusService, configBuilder);

                return new PoolingStrategySelector(
                    _loggingService,
                    _profilerService,
                    _alertService,
                    _messageBusService,
                    fixedSizeFactory,
                    dynamicSizeFactory,
                    highPerformanceFactory,
                    adaptiveNetworkFactory,
                    circuitBreakerFactory);
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to create default strategy selector, using simple fallback", ex);
                return new SimpleStrategySelector(_loggingService, _profilerService, _alertService, _messageBusService);
            }
        }

        #endregion
    }
}