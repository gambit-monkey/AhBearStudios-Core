using System;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Strategies;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Implementation of pooling strategy selector that integrates with existing strategy factories.
    /// Follows Builder → Config → Factory → Service pattern as per CLAUDE.md guidelines.
    /// </summary>
    public class PoolingStrategySelector : IPoolingStrategySelector
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        
        // Strategy factories
        private readonly IFixedSizeStrategyFactory _fixedSizeStrategyFactory;
        private readonly IDynamicSizeStrategyFactory _dynamicSizeStrategyFactory;
        private readonly IHighPerformanceStrategyFactory _highPerformanceStrategyFactory;
        private readonly IAdaptiveNetworkStrategyFactory _adaptiveNetworkStrategyFactory;
        private readonly ICircuitBreakerStrategyFactory _circuitBreakerStrategyFactory;

        /// <summary>
        /// Initializes a new instance of the PoolingStrategySelector.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        /// <param name="fixedSizeStrategyFactory">Factory for fixed size strategies</param>
        /// <param name="dynamicSizeStrategyFactory">Factory for dynamic size strategies</param>
        /// <param name="highPerformanceStrategyFactory">Factory for high performance strategies</param>
        /// <param name="adaptiveNetworkStrategyFactory">Factory for adaptive network strategies</param>
        /// <param name="circuitBreakerStrategyFactory">Factory for circuit breaker strategies</param>
        public PoolingStrategySelector(
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService,
            IFixedSizeStrategyFactory fixedSizeStrategyFactory,
            IDynamicSizeStrategyFactory dynamicSizeStrategyFactory,
            IHighPerformanceStrategyFactory highPerformanceStrategyFactory,
            IAdaptiveNetworkStrategyFactory adaptiveNetworkStrategyFactory,
            ICircuitBreakerStrategyFactory circuitBreakerStrategyFactory)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _fixedSizeStrategyFactory = fixedSizeStrategyFactory ?? throw new ArgumentNullException(nameof(fixedSizeStrategyFactory));
            _dynamicSizeStrategyFactory = dynamicSizeStrategyFactory ?? throw new ArgumentNullException(nameof(dynamicSizeStrategyFactory));
            _highPerformanceStrategyFactory = highPerformanceStrategyFactory ?? throw new ArgumentNullException(nameof(highPerformanceStrategyFactory));
            _adaptiveNetworkStrategyFactory = adaptiveNetworkStrategyFactory ?? throw new ArgumentNullException(nameof(adaptiveNetworkStrategyFactory));
            _circuitBreakerStrategyFactory = circuitBreakerStrategyFactory ?? throw new ArgumentNullException(nameof(circuitBreakerStrategyFactory));

            _loggingService.LogInfo("PoolingStrategySelector initialized with all strategy factories");
        }

        /// <inheritdoc />
        public IPoolingStrategy SelectStrategy(PoolConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Selecting strategy for pool '{configuration.Name}' - Type: {configuration.StrategyType}");

            try
            {
                var strategy = configuration.StrategyType switch
                {
                    PoolingStrategyType.FixedSize => CreateFixedSizeStrategy(configuration),
                    PoolingStrategyType.Dynamic => CreateDynamicStrategy(configuration),
                    PoolingStrategyType.HighPerformance => CreateHighPerformanceStrategy(configuration),
                    PoolingStrategyType.AdaptiveNetwork => CreateAdaptiveNetworkStrategy(configuration),
                    PoolingStrategyType.CircuitBreaker => CreateCircuitBreakerStrategy(configuration),
                    PoolingStrategyType.Default => GetDefaultStrategy(),
                    _ => GetDefaultStrategy()
                };

                _loggingService.LogInfo($"Successfully created {strategy.Name} strategy for pool '{configuration.Name}'");
                return strategy;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to create strategy for pool '{configuration.Name}', falling back to default", ex);
                _alertService?.RaiseAlert(
                    $"Strategy creation failed for pool '{configuration.Name}'",
                    AlertSeverity.Warning,
                    source: "PoolingStrategySelector",
                    tag: "StrategyCreationFailure");

                return GetDefaultStrategy();
            }
        }

        /// <inheritdoc />
        public bool CanCreateStrategy(PoolConfiguration configuration)
        {
            if (configuration == null)
                return false;

            return configuration.StrategyType switch
            {
                PoolingStrategyType.FixedSize => _fixedSizeStrategyFactory != null,
                PoolingStrategyType.Dynamic => _dynamicSizeStrategyFactory != null,
                PoolingStrategyType.HighPerformance => _highPerformanceStrategyFactory != null,
                PoolingStrategyType.AdaptiveNetwork => _adaptiveNetworkStrategyFactory != null,
                PoolingStrategyType.CircuitBreaker => _circuitBreakerStrategyFactory != null,
                PoolingStrategyType.Default => true,
                _ => true
            };
        }

        /// <inheritdoc />
        public IPoolingStrategy GetDefaultStrategy()
        {
            return new DefaultPoolingStrategy(_loggingService, _profilerService, _alertService, _messageBusService);
        }

        private IPoolingStrategy CreateFixedSizeStrategy(PoolConfiguration configuration)
        {
            var fixedSize = configuration.InitialCapacity;
            
            // Use performance budget to determine optimization level
            if (configuration.PerformanceBudget?.TargetFrameRate >= 60)
                return _fixedSizeStrategyFactory.CreateForHighPerformance(fixedSize);
            else if (configuration.PerformanceBudget?.TargetFrameRate <= 30)
                return _fixedSizeStrategyFactory.CreateForMobile(fixedSize);
            else
                return _fixedSizeStrategyFactory.CreateDefault(fixedSize);
        }

        private IPoolingStrategy CreateDynamicStrategy(PoolConfiguration configuration)
        {
            var strategyConfig = configuration.StrategyConfig ?? CreateDefaultStrategyConfig(configuration);
            return _dynamicSizeStrategyFactory.Create(strategyConfig);
        }

        private IPoolingStrategy CreateHighPerformanceStrategy(PoolConfiguration configuration)
        {
            var strategyConfig = configuration.StrategyConfig ?? CreateHighPerformanceStrategyConfig(configuration);
            return _highPerformanceStrategyFactory.Create(strategyConfig);
        }

        private IPoolingStrategy CreateAdaptiveNetworkStrategy(PoolConfiguration configuration)
        {
            var strategyConfig = configuration.StrategyConfig ?? CreateNetworkStrategyConfig(configuration);
            var spikeThreshold = 0.8; // 80% capacity threshold
            var burstWindow = TimeSpan.FromSeconds(5);
            var maxBurstAllocations = configuration.MaxCapacity / 4; // 25% of max capacity
            
            return _adaptiveNetworkStrategyFactory.CreateWithNetworkParameters(
                strategyConfig, 
                spikeThreshold, 
                0.2, // preemptiveAllocationRatio 
                burstWindow, 
                maxBurstAllocations);
        }

        private IPoolingStrategy CreateCircuitBreakerStrategy(PoolConfiguration configuration)
        {
            var strategyConfig = configuration.StrategyConfig ?? CreateCircuitBreakerStrategyConfig(configuration);
            var failureThreshold = 5;
            var recoveryTime = TimeSpan.FromMinutes(1);
            
            // Circuit breaker strategy wraps another strategy - use default as inner strategy
            var innerStrategy = GetDefaultStrategy();
            
            return _circuitBreakerStrategyFactory.CreateWithCustomParameters(
                innerStrategy,
                strategyConfig, 
                failureThreshold, 
                recoveryTime);
        }

        private PoolingStrategyConfig CreateDefaultStrategyConfig(PoolConfiguration configuration)
        {
            return new PoolingStrategyConfig
            {
                Name = $"{configuration.Name}Strategy",
                PerformanceBudget = configuration.PerformanceBudget ?? PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = false,
                EnableHealthMonitoring = configuration.EnableStatistics,
                EnableDetailedMetrics = configuration.EnableStatistics,
                DefaultCapacity = configuration.InitialCapacity,
                MaxCapacity = configuration.MaxCapacity,
                ValidationIntervalSeconds = (int)configuration.ValidationInterval.TotalSeconds
            };
        }

        private PoolingStrategyConfig CreateHighPerformanceStrategyConfig(PoolConfiguration configuration)
        {
            return new PoolingStrategyConfig
            {
                Name = $"{configuration.Name}HighPerfStrategy",
                PerformanceBudget = configuration.PerformanceBudget ?? PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(15),
                EnableHealthMonitoring = true,
                EnableDetailedMetrics = false, // Reduced overhead
                EnableUnityOptimizations = true,
                DefaultCapacity = Math.Max(configuration.InitialCapacity, 25),
                MaxCapacity = configuration.MaxCapacity,
                ValidationIntervalSeconds = Math.Max((int)configuration.ValidationInterval.TotalSeconds, 30)
            };
        }

        private PoolingStrategyConfig CreateNetworkStrategyConfig(PoolConfiguration configuration)
        {
            return new PoolingStrategyConfig
            {
                Name = $"{configuration.Name}NetworkStrategy",
                PerformanceBudget = configuration.PerformanceBudget ?? PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(30),
                EnableHealthMonitoring = true,
                EnableDetailedMetrics = true,
                EnableNetworkOptimizations = true,
                DefaultCapacity = configuration.InitialCapacity,
                MaxCapacity = configuration.MaxCapacity,
                ExpansionSize = Math.Max(10, configuration.InitialCapacity / 2),
                ValidationIntervalSeconds = Math.Min((int)configuration.ValidationInterval.TotalSeconds, 120)
            };
        }

        private PoolingStrategyConfig CreateCircuitBreakerStrategyConfig(PoolConfiguration configuration)
        {
            return new PoolingStrategyConfig
            {
                Name = $"{configuration.Name}CircuitBreakerStrategy",
                PerformanceBudget = configuration.PerformanceBudget ?? PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerRecoveryTime = TimeSpan.FromMinutes(1),
                EnableHealthMonitoring = true,
                EnableDetailedMetrics = true,
                DefaultCapacity = configuration.InitialCapacity,
                MaxCapacity = configuration.MaxCapacity,
                ValidationIntervalSeconds = (int)configuration.ValidationInterval.TotalSeconds
            };
        }
    }
}