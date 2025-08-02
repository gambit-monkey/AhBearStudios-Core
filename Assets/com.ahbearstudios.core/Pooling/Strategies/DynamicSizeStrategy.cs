using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies.Models;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Dynamic pool strategy that adjusts pool size based on usage patterns.
    /// Expands when utilization is high and contracts when objects are idle.
    /// Enhanced with production-ready features for Unity game development.
    /// </summary>
    public class DynamicSizeStrategy : IPoolingStrategy
    {
        private readonly double _expandThreshold;
        private readonly double _contractThreshold;
        private readonly double _maxUtilization;
        private readonly TimeSpan _validationInterval;
        private readonly TimeSpan _idleTimeThreshold;
        private readonly PoolingStrategyConfig _configuration;
        private readonly PerformanceBudget _performanceBudget;
        
        // Performance monitoring
        private readonly List<TimeSpan> _recentOperationTimes = new();
        private readonly object _metricsLock = new object();
        private int _errorCount;
        private int _circuitBreakerTriggerCount;
        private DateTime _lastHealthCheck = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the DynamicSizeStrategy.
        /// </summary>
        /// <param name="expandThreshold">Utilization threshold to trigger expansion (0.0-1.0)</param>
        /// <param name="contractThreshold">Utilization threshold to trigger contraction (0.0-1.0)</param>
        /// <param name="maxUtilization">Maximum allowed utilization before forcing expansion (0.0-1.0)</param>
        /// <param name="validationInterval">Interval between validation checks</param>
        /// <param name="idleTimeThreshold">Time threshold for considering objects idle</param>
        /// <param name="configuration">Strategy configuration (optional)</param>
        public DynamicSizeStrategy(
            double expandThreshold = 0.8,
            double contractThreshold = 0.3,
            double maxUtilization = 0.95,
            TimeSpan? validationInterval = null,
            TimeSpan? idleTimeThreshold = null,
            PoolingStrategyConfig configuration = null)
        {
            _expandThreshold = Math.Clamp(expandThreshold, 0.0, 1.0);
            _contractThreshold = Math.Clamp(contractThreshold, 0.0, 1.0);
            _maxUtilization = Math.Clamp(maxUtilization, 0.0, 1.0);
            _validationInterval = validationInterval ?? TimeSpan.FromMinutes(5);
            _idleTimeThreshold = idleTimeThreshold ?? TimeSpan.FromMinutes(10);
            _configuration = configuration ?? PoolingStrategyConfig.Default("DynamicSize");
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For60FPS();

            if (_contractThreshold >= _expandThreshold)
                throw new ArgumentException("Contract threshold must be less than expand threshold");
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "DynamicSize";

        /// <summary>
        /// Calculates the target size for the pool based on current statistics.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>Target pool size</returns>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            if (statistics == null) return 0;

            var currentUtilization = statistics.Utilization / 100.0;
            var currentSize = statistics.TotalCount;

            // If utilization is very high, expand aggressively
            if (currentUtilization >= _maxUtilization)
            {
                return Math.Max(currentSize * 2, currentSize + 10);
            }

            // If utilization is above expand threshold, grow gradually
            if (currentUtilization >= _expandThreshold)
            {
                var growthFactor = 1.0 + (currentUtilization - _expandThreshold) / (1.0 - _expandThreshold) * 0.5;
                return (int)Math.Ceiling(currentSize * growthFactor);
            }

            // If utilization is below contract threshold, shrink gradually
            if (currentUtilization <= _contractThreshold && currentSize > 1)
            {
                var shrinkFactor = 0.8 + (_contractThreshold - currentUtilization) / _contractThreshold * 0.2;
                return Math.Max(1, (int)Math.Floor(currentSize * shrinkFactor));
            }

            // Maintain current size
            return currentSize;
        }

        /// <summary>
        /// Determines if the pool should expand.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should expand</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            return utilization >= _expandThreshold || statistics.AvailableCount == 0;
        }

        /// <summary>
        /// Determines if the pool should contract.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should contract</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            var hasIdleTime = statistics.AverageIdleTimeMinutes > _idleTimeThreshold.TotalMinutes;
            
            return utilization <= _contractThreshold && hasIdleTime && statistics.TotalCount > 1;
        }

        /// <summary>
        /// Determines if a new object should be created.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if a new object should be created</returns>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            if (statistics == null) return true;

            // Create new if no objects available
            if (statistics.AvailableCount == 0)
                return true;

            // Create new if utilization is very high
            var utilization = statistics.Utilization / 100.0;
            return utilization >= _maxUtilization;
        }

        /// <summary>
        /// Determines if objects should be destroyed.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if objects should be destroyed</returns>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            // Only destroy if we have excess capacity and low utilization
            var utilization = statistics.Utilization / 100.0;
            var hasExcess = statistics.AvailableCount > statistics.ActiveCount;
            var hasIdleTime = statistics.AverageIdleTimeMinutes > _idleTimeThreshold.TotalMinutes * 2;
            
            return utilization <= _contractThreshold && hasExcess && hasIdleTime;
        }

        /// <summary>
        /// Gets the interval between validation checks.
        /// </summary>
        /// <returns>Validation interval</returns>
        public TimeSpan GetValidationInterval()
        {
            return _validationInterval;
        }

        /// <summary>
        /// Validates the pool configuration against this strategy.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            if (config == null) return false;

            // Validate basic requirements
            if (config.InitialCapacity < 0 || config.MaxCapacity < 1)
                return false;

            if (config.InitialCapacity > config.MaxCapacity)
                return false;

            if (config.Factory == null)
                return false;

            // Validate timeouts
            if (config.MaxIdleTime <= TimeSpan.Zero)
                return false;

            if (config.ValidationInterval <= TimeSpan.Zero)
                return false;

            return true;
        }

        #region Production-Ready Enhancements

        /// <summary>
        /// Determines if the circuit breaker should be triggered based on current statistics.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if circuit breaker should be triggered</returns>
        public bool ShouldTriggerCircuitBreaker(PoolStatistics statistics)
        {
            if (!_configuration.EnableCircuitBreaker || statistics == null)
                return false;

            // Trigger circuit breaker if error rate is too high
            var errorRate = statistics.TotalCount > 0 ? (double)_errorCount / statistics.TotalCount : 0.0;
            if (errorRate > 0.1) // 10% error rate
            {
                _circuitBreakerTriggerCount++;
                return _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold;
            }

            // Trigger if performance is severely degraded
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count > 10)
                {
                    var averageTime = TimeSpan.Zero;
                    foreach (var time in _recentOperationTimes)
                        averageTime = averageTime.Add(time);
                    averageTime = TimeSpan.FromTicks(averageTime.Ticks / _recentOperationTimes.Count);

                    if (averageTime > _performanceBudget.MaxOperationTime.Multiply(5)) // 5x over budget
                    {
                        _circuitBreakerTriggerCount++;
                        return _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the performance budget for this strategy.
        /// </summary>
        /// <returns>Performance budget configuration</returns>
        public PerformanceBudget GetPerformanceBudget()
        {
            return _performanceBudget;
        }

        /// <summary>
        /// Gets the current health status of this strategy.
        /// </summary>
        /// <returns>Strategy health status</returns>
        public StrategyHealthStatus GetHealthStatus()
        {
            var now = DateTime.UtcNow;
            var warnings = new List<string>();
            var errors = new List<string>();

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
                    var averageTime = TimeSpan.Zero;
                    foreach (var time in _recentOperationTimes)
                        averageTime = averageTime.Add(time);
                    averageTime = TimeSpan.FromTicks(averageTime.Ticks / _recentOperationTimes.Count);

                    if (averageTime > _performanceBudget.MaxOperationTime)
                    {
                        warnings.Add($"Average operation time ({averageTime.TotalMilliseconds:F2}ms) exceeds budget");
                    }
                }
            }

            // Check circuit breaker
            if (_circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold)
            {
                errors.Add("Circuit breaker threshold reached");
            }

            var status = errors.Count > 0 ? StrategyHealth.Unhealthy :
                        warnings.Count > 0 ? StrategyHealth.Degraded :
                        StrategyHealth.Healthy;

            return new StrategyHealthStatus
            {
                Status = status,
                Description = status == StrategyHealth.Healthy ? "Strategy operating normally" : 
                             $"Strategy has {warnings.Count} warnings and {errors.Count} errors",
                Timestamp = now,
                Warnings = warnings,
                Errors = errors,
                CircuitBreakerOpen = _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold,
                OperationCount = _recentOperationTimes.Count,
                ErrorCount = _errorCount,
                AverageOperationTime = GetAverageOperationTime(),
                MaxOperationTime = GetMaxOperationTime()
            };
        }

        /// <summary>
        /// Called when a pool operation starts (for performance monitoring).
        /// </summary>
        public void OnPoolOperationStart()
        {
            // Currently no pre-operation setup needed
        }

        /// <summary>
        /// Called when a pool operation completes (for performance monitoring).
        /// </summary>
        /// <param name="duration">Duration of the operation</param>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            if (!_configuration.EnableDetailedMetrics)
                return;

            lock (_metricsLock)
            {
                _recentOperationTimes.Add(duration);
                
                // Keep only recent samples to avoid memory growth
                if (_recentOperationTimes.Count > _configuration.MaxMetricsSamples)
                {
                    _recentOperationTimes.RemoveAt(0);
                }
            }

            // Log performance warnings if enabled
            if (_performanceBudget.LogPerformanceWarnings && duration > _performanceBudget.MaxOperationTime)
            {
                // Would log warning here if logging service was available
                // For now, just track in metrics
            }
        }

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        public void OnPoolError(Exception error)
        {
            _errorCount++;
            // Could log error details here if logging service was available
        }

        /// <summary>
        /// Gets network-specific metrics if this strategy supports network optimizations.
        /// </summary>
        /// <returns>Network pooling metrics, or null if not supported</returns>
        public NetworkPoolingMetrics GetNetworkMetrics()
        {
            // DynamicSizeStrategy doesn't have network-specific optimizations
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

        #endregion

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

        #endregion
    }
}