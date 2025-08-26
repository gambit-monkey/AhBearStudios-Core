using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Default pooling strategy implementation with basic pool management.
    /// Provides simple, reliable pool sizing and maintenance suitable for most use cases.
    /// Designed for Unity game development with minimal overhead and predictable behavior.
    /// </summary>
    public class DefaultPoolingStrategy : IPoolingStrategy
    {
        private const int DefaultMaxSize = 1000;
        private const int DefaultExpansionSize = 10;
        
        // Core system integration
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        
        // Configuration and budgets
        private readonly PerformanceBudget _performanceBudget;
        private readonly PoolingStrategyConfig _configuration;
        
        // Performance profiling tags
        private static readonly ProfilerTag DefaultOperationTag = new ProfilerTag("DefaultStrategy.Operation");
        private static readonly ProfilerTag CalculateTargetTag = new ProfilerTag("DefaultStrategy.CalculateTarget");
        private static readonly ProfilerTag HealthStatusTag = new ProfilerTag("DefaultStrategy.HealthStatus");
        
        // Alert source identifier
        private static readonly FixedString64Bytes AlertSource = "DefaultPoolingStrategy";
        
        // Performance monitoring
        private readonly List<TimeSpan> _recentOperationTimes = new();
        private readonly object _metricsLock = new object();
        private int _errorCount = 0;
        private DateTime _lastHealthCheck = DateTime.UtcNow;
        private int _circuitBreakerTriggerCount;

        /// <summary>
        /// Initializes a new instance of the DefaultPoolingStrategy.
        /// This constructor should be called by the DefaultPoolingStrategyFactory.
        /// </summary>
        /// <param name="loggingService">Logging service for strategy operations</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="messageBusService">Optional message bus service for publishing lifecycle events</param>
        public DefaultPoolingStrategy(
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService; // Optional - can be null
            
            _performanceBudget = PerformanceBudget.For60FPS();
            
            _configuration = new PoolingStrategyConfig
            {
                Name = Name,
                PerformanceBudget = _performanceBudget,
                DefaultCapacity = 50,
                MaxCapacity = DefaultMaxSize,
                MinCapacity = 10,
                ExpansionSize = DefaultExpansionSize,
                ContractionSize = 5,
                ValidationIntervalSeconds = 60,
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerRecoveryTime = TimeSpan.FromMinutes(1),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromSeconds(30),
                EnableDetailedMetrics = false,
                MaxMetricsSamples = 1000,
                EnableNetworkOptimizations = false,
                EnableUnityOptimizations = true,
                EnableDebugLogging = false
            };
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "DefaultPoolingStrategy";

        /// <summary>
        /// Calculates the target size for the pool based on current statistics.
        /// </summary>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            using var profileSession = _profilerService?.BeginScope(CalculateTargetTag);
            
            // Simple algorithm: maintain 20% buffer above current active count
            var targetSize = Math.Max(statistics.ActiveCount * 1.2f, _configuration.MinCapacity);
            return (int)Math.Min(targetSize, _configuration.MaxCapacity);
        }

        /// <summary>
        /// Determines if the pool should expand.
        /// </summary>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            return statistics.AvailableCount == 0 && 
                   statistics.TotalCount < _configuration.MaxCapacity;
        }

        /// <summary>
        /// Determines if the pool should contract.
        /// </summary>
        public bool ShouldContract(PoolStatistics statistics)
        {
            return statistics.AvailableCount > statistics.ActiveCount * 2 &&
                   statistics.TotalCount > _configuration.MinCapacity;
        }

        /// <summary>
        /// Determines if a new object should be created.
        /// </summary>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            return statistics.AvailableCount == 0 && 
                   statistics.TotalCount < _configuration.MaxCapacity;
        }

        /// <summary>
        /// Determines if objects should be destroyed.
        /// </summary>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            return statistics.AvailableCount > statistics.ActiveCount * 2;
        }

        /// <summary>
        /// Gets the interval between validation checks.
        /// </summary>
        public TimeSpan GetValidationInterval()
        {
            return TimeSpan.FromSeconds(_configuration.ValidationIntervalSeconds);
        }

        /// <summary>
        /// Validates the pool configuration against this strategy.
        /// </summary>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            return config != null &&
                   config.InitialCapacity > 0 &&
                   config.MaxCapacity >= config.InitialCapacity &&
                   config.MaxCapacity <= DefaultMaxSize;
        }

        /// <summary>
        /// Determines if the circuit breaker should be triggered based on current statistics.
        /// </summary>
        public bool ShouldTriggerCircuitBreaker(PoolStatistics statistics)
        {
            // Trigger if error rate is too high (more than 50% of operations fail)
            var totalOperations = statistics.TotalGets + statistics.TotalReturns;
            if (totalOperations == 0) return false;
            
            var errorRate = (double)_errorCount / totalOperations;
            return errorRate > 0.5;
        }

        /// <summary>
        /// Gets the performance budget for this strategy.
        /// </summary>
        public PerformanceBudget GetPerformanceBudget()
        {
            return _performanceBudget;
        }

        /// <summary>
        /// Gets the current health status of this strategy.
        /// </summary>
        public StrategyHealthStatus GetHealthStatus()
        {
            using var profileSession = _profilerService?.BeginScope(HealthStatusTag);
            
            var timeSinceLastCheck = DateTime.UtcNow - _lastHealthCheck;
            var isHealthy = _errorCount < 5 && timeSinceLastCheck < TimeSpan.FromMinutes(5);
            
            if (!isHealthy)
            {
                if (_errorCount >= 10)
                {
                    return StrategyHealthStatus.Unhealthy(
                        "High error count detected", 
                        new[] { $"Error count: {_errorCount}" });
                }
                else
                {
                    return StrategyHealthStatus.Degraded(
                        "Performance degradation detected",
                        $"Recent errors: {_errorCount}",
                        $"Last health check: {timeSinceLastCheck.TotalMinutes:F1} minutes ago");
                }
            }
            
            var healthStatus = StrategyHealthStatus.Healthy("Operating normally");
            return healthStatus with
            {
                OperationCount = _recentOperationTimes.Count,
                ErrorCount = _errorCount,
                AverageOperationTime = CalculateAverageOperationTime(),
                MaxOperationTime = CalculateMaxOperationTime()
            };
        }

        /// <summary>
        /// Called when a pool operation starts (for performance monitoring).
        /// </summary>
        public void OnPoolOperationStart()
        {
            // Performance profiling is handled per-operation with using statements
            // in individual methods that need profiling
            
            // Publish operation started message if message bus is available
            if (_messageBusService != null)
            {
                try
                {
                    var message = PoolOperationStartedMessage.Create(
                        poolName: "DefaultPool", // Generic name since we don't have specific pool context
                        strategyName: Name,
                        operationType: "Operation",
                        poolSizeAtStart: 0, // Default strategy doesn't track specific pool metrics
                        activeObjectsAtStart: 0
                    );
                    
                    _messageBusService.PublishMessage(message);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to publish pool operation started message: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Called when a pool operation completes (for performance monitoring).
        /// </summary>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            // Record performance sample
            _profilerService?.RecordSample(DefaultOperationTag, (float)duration.TotalMilliseconds, "ms");
            
            // Track operation performance
            lock (_metricsLock)
            {
                _recentOperationTimes.Add(duration);
                
                // Keep only recent samples to manage memory
                if (_recentOperationTimes.Count > _configuration.MaxMetricsSamples)
                {
                    _recentOperationTimes.RemoveAt(0);
                }
            }
            
            // Check performance budget violation
            if (duration > _performanceBudget.MaxOperationTime)
            {
                _loggingService?.LogWarning($"Pool operation exceeded performance budget: {duration.TotalMilliseconds:F2}ms");
                
                if (_alertService != null)
                {
                    _alertService.RaiseAlert(
                        message: $"Pool Performance Budget Exceeded: Operation took {duration.TotalMilliseconds:F2}ms, budget is {_performanceBudget.MaxOperationTime.TotalMilliseconds:F2}ms",
                        severity: AlertSeverity.Warning,
                        source: AlertSource,
                        tag: "PerformanceBudget"
                    );
                }
            }
            
            // Update health check timestamp if operation completed successfully
            _lastHealthCheck = DateTime.UtcNow;
            
            // Publish operation completed message if message bus is available
            if (_messageBusService != null)
            {
                try
                {
                    var message = PoolOperationCompletedMessage.Create(
                        poolName: "DefaultPool",
                        strategyName: Name,
                        operationType: "Operation",
                        duration: duration,
                        poolSizeAfter: 0, // Default strategy doesn't track specific pool metrics
                        activeObjectsAfter: 0,
                        isSuccessful: true
                    );
                    
                    _messageBusService.PublishMessage(message);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to publish pool operation completed message: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        public void OnPoolError(Exception error)
        {
            _errorCount++;
            
            // Log the error
            _loggingService?.LogError($"Pool operation error in {Name}: {error.Message}. Exception: {error}");
            
            // Raise alert for critical errors
            if (_alertService != null)
            {
                var severity = _errorCount > 5 ? AlertSeverity.Critical : AlertSeverity.Emergency;
                _alertService.RaiseAlert(
                    message: $"Pool Operation Error: Error in {Name}: {error.Message}. Error count: {_errorCount}",
                    severity: severity,
                    source: AlertSource,
                    tag: "OperationError"
                );
            }
            
            // Check if circuit breaker should trigger
            if (ShouldTriggerCircuitBreaker(null))
            {
                _circuitBreakerTriggerCount++;
                _loggingService?.LogCritical($"Circuit breaker triggered for {Name} due to high error rate");
            }
            
            // Publish operation failed message if message bus is available
            if (_messageBusService != null)
            {
                try
                {
                    var message = PoolOperationFailedMessage.Create(
                        poolName: "DefaultPool",
                        strategyName: Name,
                        operationType: "Operation",
                        error: error,
                        errorCount: _errorCount,
                        poolSizeAtFailure: 0, // Default strategy doesn't track specific pool metrics
                        activeObjectsAtFailure: 0
                    );
                    
                    _messageBusService.PublishMessage(message);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to publish pool operation failed message: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets network-specific metrics if this strategy supports network optimizations.
        /// </summary>
        public NetworkPoolingMetrics GetNetworkMetrics()
        {
            // Default strategy doesn't provide network-specific optimizations
            return null;
        }

        /// <summary>
        /// Gets the strategy configuration.
        /// </summary>
        public PoolingStrategyConfig GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Publishes a health status message if message bus is available.
        /// Can be called periodically for health monitoring.
        /// </summary>
        public void PublishHealthStatus()
        {
            if (_messageBusService != null)
            {
                try
                {
                    var healthStatus = GetHealthStatus();
                    var totalOperations = _recentOperationTimes.Count;
                    var successRate = totalOperations > 0 ? Math.Max(0, 100.0 - ((_errorCount * 100.0) / totalOperations)) : 100.0;
                    
                    var message = PoolStrategyHealthStatusMessage.Create(
                        strategyName: Name,
                        isHealthy: healthStatus.IsHealthy,
                        errorCount: (int)healthStatus.ErrorCount,
                        lastHealthCheck: _lastHealthCheck,
                        statusMessage: healthStatus.Description,
                        totalOperations: totalOperations,
                        successRatePercentage: successRate
                    );
                    
                    _messageBusService.PublishMessage(message);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to publish health status message: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Calculates the average operation time from recent samples.
        /// </summary>
        /// <returns>Average operation time</returns>
        private TimeSpan CalculateAverageOperationTime()
        {
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count == 0)
                    return TimeSpan.Zero;
                    
                var totalTicks = 0L;
                foreach (var time in _recentOperationTimes)
                    totalTicks += time.Ticks;
                    
                return new TimeSpan(totalTicks / _recentOperationTimes.Count);
            }
        }
        
        /// <summary>
        /// Calculates the maximum operation time from recent samples.
        /// </summary>
        /// <returns>Maximum operation time</returns>
        private TimeSpan CalculateMaxOperationTime()
        {
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count == 0)
                    return TimeSpan.Zero;
                    
                var maxTicks = 0L;
                foreach (var time in _recentOperationTimes)
                {
                    if (time.Ticks > maxTicks)
                        maxTicks = time.Ticks;
                }
                    
                return new TimeSpan(maxTicks);
            }
        }

    }
}