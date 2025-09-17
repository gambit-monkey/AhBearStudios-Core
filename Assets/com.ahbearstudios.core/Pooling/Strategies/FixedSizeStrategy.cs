using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Profiling.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Fixed-size pooling strategy that maintains a constant pool size.
    /// Provides predictable memory footprint and guaranteed performance characteristics.
    /// Ideal for mobile platforms and memory-constrained environments.
    /// </summary>
    public class FixedSizeStrategy : IPoolingStrategy
    {
        private readonly int _fixedSize;
        private readonly PoolingStrategyConfig _configuration;
        private readonly PerformanceBudget _performanceBudget;
        
        // Core system integration
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        
        // Performance profiling tags
        private static readonly ProfilerTag FixedSizeOperationTag = new ProfilerTag("FixedSize.Operation");
        
        // Alert source identifier
        private static readonly FixedString64Bytes AlertSource = "FixedSizeStrategy";
        
        // Performance monitoring
        private readonly List<TimeSpan> _recentOperationTimes = new();
        private readonly object _metricsLock = new object();
        private int _errorCount;
        private int _rejectedRequests; // Requests rejected due to size limit
        private int _circuitBreakerTriggerCount;
        private DateTime _lastHealthCheck = DateTime.UtcNow;
        private IDisposable _thresholdExceededSubscription;

        /// <summary>
        /// Initializes a new instance of the FixedSizeStrategy.
        /// This constructor should be called by the FixedSizeStrategyFactory.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool</param>
        /// <param name="configuration">Strategy configuration</param>
        /// <param name="loggingService">The logging service for system integration</param>
        /// <param name="profilerService">The profiler service for performance monitoring</param>
        /// <param name="alertService">The alert service for critical error notifications</param>
        /// <param name="messageBusService">The message bus service for event publishing</param>
        public FixedSizeStrategy(
            int fixedSize, 
            PoolingStrategyConfig configuration,
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));

            // Validate dependencies
            _fixedSize = fixedSize;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For30FPS();

            // Subscribe to profiler threshold exceeded messages for performance monitoring
            _thresholdExceededSubscription = _messageBusService.SubscribeToMessage<ProfilerThresholdExceededMessage>(OnPerformanceThresholdExceeded);
            
            _loggingService.LogInfo($"FixedSizeStrategy initialized - Size: {_fixedSize}, Config: {_configuration.Name}");
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => $"FixedSize({_fixedSize})";

        /// <summary>
        /// Calculates the target size (always returns the fixed size).
        /// </summary>
        /// <param name="statistics">Current pool statistics (ignored)</param>
        /// <returns>The fixed pool size</returns>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            return _fixedSize;
        }

        /// <summary>
        /// Determines if the pool should expand (only if below fixed size).
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if current size is below fixed size</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            if (statistics == null) return true;
            return statistics.TotalCount < _fixedSize;
        }

        /// <summary>
        /// Determines if the pool should contract (only if above fixed size).
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if current size is above fixed size</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            if (statistics == null) return false;
            return statistics.TotalCount > _fixedSize;
        }

        /// <summary>
        /// Determines if a new object should be created (only if below fixed size).
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if current size is below fixed size</returns>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            if (statistics == null) return true;
            
            if (statistics.TotalCount >= _fixedSize)
            {
                _rejectedRequests++;
                _loggingService.LogDebug($"Request rejected due to fixed size limit - Total rejections: {_rejectedRequests}");
                
                // Alert if rejection rate becomes high
                if (_rejectedRequests > 0 && _rejectedRequests % 100 == 0)
                {
                    _alertService.RaiseAlert(
                        message: $"High rejection rate in FixedSizeStrategy - {_rejectedRequests} requests rejected",
                        severity: AlertSeverity.Warning,
                        source: AlertSource,
                        tag: "HighRejectionRate"
                    );
                }
                
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Determines if objects should be destroyed (only if above fixed size).
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if current size is above fixed size</returns>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            if (statistics == null) return false;
            return statistics.TotalCount > _fixedSize;
        }

        /// <summary>
        /// Gets the validation interval (longer for fixed-size pools).
        /// </summary>
        /// <returns>Validation interval</returns>
        public TimeSpan GetValidationInterval()
        {
            // Fixed-size pools need less frequent validation
            return TimeSpan.FromMinutes(2);
        }

        /// <summary>
        /// Validates the pool configuration for fixed-size strategy.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            if (config == null) return false;

            // For fixed-size strategy, ensure the fixed size is within bounds
            if (config.MaxCapacity > 0 && _fixedSize > config.MaxCapacity)
                return false;

            if (_fixedSize < config.InitialCapacity)
                return false;

            if (config.Factory == null)
                return false;

            return true;
        }

        /// <summary>
        /// Determines if the circuit breaker should be triggered.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if circuit breaker should be triggered</returns>
        public bool ShouldTriggerCircuitBreaker(PoolStatistics statistics)
        {
            if (!_configuration.EnableCircuitBreaker || statistics == null)
                return false;

            // Trigger on excessive rejected requests (indicates size is too small)
            var rejectionRate = statistics.TotalRequestCount > 0 ? 
                (double)_rejectedRequests / statistics.TotalRequestCount : 0.0;
                
            if (rejectionRate > 0.5) // 50% rejection rate
            {
                _circuitBreakerTriggerCount++;
                _loggingService.LogWarning($"High rejection rate: {rejectionRate:P} - Circuit breaker trigger count: {_circuitBreakerTriggerCount}");
                
                if (_circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold)
                {
                    _alertService.RaiseAlert(
                        message: "FixedSize strategy circuit breaker triggered due to high rejection rate",
                        severity: AlertSeverity.Critical,
                        source: AlertSource,
                        tag: "CircuitBreakerTriggered"
                    );
                    return true;
                }
            }

            // Trigger on excessive errors
            var errorRate = statistics.TotalCount > 0 ? (double)_errorCount / statistics.TotalCount : 0.0;
            if (errorRate > 0.1) // 10% error rate
            {
                _circuitBreakerTriggerCount++;
                _loggingService.LogWarning($"High error rate: {errorRate:P} - Circuit breaker trigger count: {_circuitBreakerTriggerCount}");
                
                if (_circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold)
                {
                    _alertService.RaiseAlert(
                        message: "FixedSize strategy circuit breaker triggered due to high error rate",
                        severity: AlertSeverity.Critical,
                        source: AlertSource,
                        tag: "HighErrorRate"
                    );
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the performance budget for fixed-size operations.
        /// </summary>
        /// <returns>Performance budget configuration</returns>
        public PerformanceBudget GetPerformanceBudget()
        {
            return _performanceBudget;
        }

        /// <summary>
        /// Gets the current health status of the fixed-size strategy.
        /// </summary>
        /// <returns>Strategy health status</returns>
        public StrategyHealthStatus GetHealthStatus()
        {
            var now = DateTime.UtcNow;
            var warnings = new List<string>();
            var errors = new List<string>();

            // Check rejection rate
            if (_rejectedRequests > 0)
            {
                warnings.Add($"Fixed size too small: {_rejectedRequests} requests rejected");
            }

            // Check error rate
            if (_errorCount > 0)
            {
                warnings.Add($"Strategy has encountered {_errorCount} errors");
            }

            // Check performance
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count > 0)
                {
                    var averageTime = GetAverageOperationTime();
                    if (averageTime > _performanceBudget.MaxOperationTime)
                    {
                        warnings.Add($"Average operation time ({averageTime.TotalMilliseconds:F2}ms) exceeds budget");
                    }
                }
            }

            // Check circuit breaker
            if (_circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold)
            {
                errors.Add("Circuit breaker threshold reached - fixed size may be inadequate");
            }

            var status = errors.Count > 0 ? StrategyHealth.Unhealthy :
                        warnings.Count > 0 ? StrategyHealth.Degraded :
                        StrategyHealth.Healthy;

            return new StrategyHealthStatus
            {
                Status = status,
                Description = GetHealthDescription(status, warnings.Count, errors.Count),
                Timestamp = now,
                Warnings = warnings,
                Errors = errors,
                IsCircuitBreakerOpen = _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold,
                OperationCount = _recentOperationTimes.Count,
                ErrorCount = _errorCount,
                AverageOperationTime = GetAverageOperationTime(),
                MaxOperationTime = GetMaxOperationTime(),
                Metrics = new Dictionary<string, object>
                {
                    ["FixedSize"] = _fixedSize,
                    ["RejectedRequests"] = _rejectedRequests,
                    ["MemoryFootprintPredictable"] = true,
                    ["ZeroRuntimeAllocations"] = true
                }
            };
        }

        /// <summary>
        /// Called when a pool operation starts.
        /// </summary>
        public void OnPoolOperationStart()
        {
            // Publish operation started message if message bus is available
            if (_messageBusService != null)
            {
                try
                {
                    var message = PoolOperationStartedMessage.Create(
                        poolName: "FixedSizePool",
                        strategyName: Name,
                        operationType: "FixedSizeOperation",
                        poolSizeAtStart: _fixedSize, // Fixed size is constant
                        activeObjectsAtStart: 0 // Fixed-size strategy doesn't track specific active counts
                    );
                    
                    _messageBusService.PublishMessageAsync(message);
                }
                catch
                {
                    // Swallow exceptions to avoid disrupting pool operations
                }
            }
        }

        /// <summary>
        /// Called when a pool operation completes.
        /// </summary>
        /// <param name="duration">Duration of the operation</param>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            if (_configuration.EnableDetailedMetrics)
            {
                lock (_metricsLock)
                {
                    _recentOperationTimes.Add(duration);
                    
                    // Keep limited history for memory efficiency
                    if (_recentOperationTimes.Count > _configuration.MaxMetricsSamples)
                    {
                        _recentOperationTimes.RemoveAt(0);
                    }
                }
            }
            
            // Check performance budget
            if (duration > _performanceBudget.MaxOperationTime)
            {
                _loggingService.LogWarning($"Fixed-size pool operation exceeded performance budget: {duration.TotalMilliseconds}ms > {_performanceBudget.MaxOperationTime.TotalMilliseconds}ms");
            }
            
            // Publish operation completed message if message bus is available
            if (_messageBusService != null)
            {
                try
                {
                    var message = PoolOperationCompletedMessage.Create(
                        poolName: "FixedSizePool",
                        strategyName: Name,
                        operationType: "FixedSizeOperation",
                        duration: duration,
                        poolSizeAfter: _fixedSize, // Fixed size is constant
                        activeObjectsAfter: 0, // Fixed-size strategy doesn't track specific active counts
                        isSuccessful: true
                    );
                    
                    _messageBusService.PublishMessageAsync(message);
                }
                catch
                {
                    // Swallow exceptions to avoid disrupting pool operations
                }
            }
        }

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        public void OnPoolError(Exception error)
        {
            _errorCount++;
            _loggingService.LogException($"Pool operation error in {Name} strategy - Error count: {_errorCount}", error);
            
            if (_errorCount > 5)
            {
                _alertService.RaiseAlert(
                    message: "Multiple errors in FixedSizeStrategy",
                    severity: AlertSeverity.Warning,
                    source: AlertSource,
                    tag: "MultipleErrors"
                );
            }
            
            // Publish operation failed message if message bus is available
            if (_messageBusService != null)
            {
                try
                {
                    var message = PoolOperationFailedMessage.Create(
                        poolName: "FixedSizePool",
                        strategyName: Name,
                        operationType: "FixedSizeOperation",
                        error: error,
                        errorCount: _errorCount,
                        poolSizeAtFailure: _fixedSize, // Fixed size is constant
                        activeObjectsAtFailure: 0 // Fixed-size strategy doesn't track specific active counts
                    );
                    
                    _messageBusService.PublishMessageAsync(message);
                }
                catch
                {
                    // Swallow exceptions to avoid disrupting pool operations
                }
            }
        }

        /// <summary>
        /// Gets network-specific metrics (not supported by fixed-size strategy).
        /// </summary>
        /// <returns>Null - fixed-size strategy doesn't support network metrics</returns>
        public NetworkPoolingMetrics GetNetworkMetrics()
        {
            // Fixed-size strategy doesn't have network-specific optimizations
            return null;
        }

        /// <summary>
        /// Gets the strategy configuration.
        /// </summary>
        /// <returns>Strategy configuration</returns>
        public PoolingStrategyConfig GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Gets the fixed size of this strategy.
        /// </summary>
        /// <returns>The fixed pool size</returns>
        public int GetFixedSize()
        {
            return _fixedSize;
        }

        /// <summary>
        /// Gets the number of requests rejected due to size limit.
        /// </summary>
        /// <returns>Number of rejected requests</returns>
        public int GetRejectedRequestCount()
        {
            return _rejectedRequests;
        }

        /// <summary>
        /// Resets the rejected request counter.
        /// </summary>
        public void ResetRejectedRequestCount()
        {
            _rejectedRequests = 0;
        }

        #region Private Helper Methods

        private TimeSpan GetAverageOperationTime()
        {
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count == 0)
                    return TimeSpan.Zero;

                var total = TimeSpan.Zero;
                foreach (var time in _recentOperationTimes)
                    total = total.Add(time);
                return TimeSpan.FromTicks(total.Ticks / _recentOperationTimes.Count);
            }
        }

        private TimeSpan GetMaxOperationTime()
        {
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count == 0)
                    return TimeSpan.Zero;

                var max = TimeSpan.Zero;
                foreach (var time in _recentOperationTimes)
                {
                    if (time > max)
                        max = time;
                }
                return max;
            }
        }

        private string GetHealthDescription(StrategyHealth status, int warningCount, int errorCount)
        {
            return status switch
            {
                StrategyHealth.Healthy => $"Fixed-size strategy maintaining {_fixedSize} objects efficiently",
                StrategyHealth.Degraded => $"Fixed-size strategy has {warningCount} performance warnings",
                StrategyHealth.Unhealthy => $"Fixed-size strategy has {errorCount} critical errors",
                _ => "Fixed-size strategy status unknown"
            };
        }

        #endregion

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
                    var message = PoolStrategyHealthStatusMessage.Create(
                        strategyName: Name,
                        isHealthy: healthStatus.Status == StrategyHealth.Healthy,
                        errorCount: (int)Math.Min(healthStatus.ErrorCount, int.MaxValue),
                        lastHealthCheck: healthStatus.Timestamp,
                        statusMessage: healthStatus.Description,
                        averageOperationDurationMs: healthStatus.AverageOperationTime.TotalMilliseconds,
                        totalOperations: healthStatus.OperationCount,
                        successRatePercentage: CalculateSuccessRate()
                    );
                    
                    _messageBusService.PublishMessageAsync(message);
                }
                catch
                {
                    // Swallow exceptions to avoid disrupting pool operations
                }
            }
        }

        /// <summary>
        /// Calculates the current success rate percentage.
        /// </summary>
        /// <returns>Success rate as a percentage (0-100)</returns>
        private double CalculateSuccessRate()
        {
            lock (_metricsLock)
            {
                var totalOperations = _recentOperationTimes.Count + _errorCount;
                if (totalOperations == 0) return 100.0;
                
                var successfulOperations = _recentOperationTimes.Count;
                return (double)successfulOperations / totalOperations * 100.0;
            }
        }

        /// <summary>
        /// Handles performance threshold exceeded messages from the profiler service.
        /// </summary>
        /// <param name="message">The profiler threshold exceeded message</param>
        private void OnPerformanceThresholdExceeded(ProfilerThresholdExceededMessage message)
        {
            _loggingService.LogWarning($"FixedSize performance threshold exceeded for {message.Tag.Name}: {message.ElapsedMs}{message.Unit}");

            // Fixed size operations should be very fast
            if (message.ElapsedMs > 5.0) // > 5ms is significant for fixed-size operations
            {
                _alertService.RaiseAlert(
                    message: $"Performance degradation in {Name}: {message.Tag.Name} took {message.ElapsedMs}{message.Unit}",
                    severity: AlertSeverity.Warning,
                    source: AlertSource,
                    tag: "FixedSizePerformanceDegradation"
                );
            }
        }

        /// <summary>
        /// Disposes the strategy and cleans up subscriptions.
        /// </summary>
        public void Dispose()
        {
            _thresholdExceededSubscription?.Dispose();
        }
    }
}