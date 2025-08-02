using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;

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
        
        // Performance monitoring
        private readonly List<TimeSpan> _recentOperationTimes = new();
        private readonly object _metricsLock = new object();
        private int _errorCount;
        private int _rejectedRequests; // Requests rejected due to size limit
        private int _circuitBreakerTriggerCount;
        private DateTime _lastHealthCheck = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the FixedSizeStrategy.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool</param>
        /// <param name="configuration">Strategy configuration (optional)</param>
        public FixedSizeStrategy(int fixedSize, PoolingStrategyConfig configuration = null)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));

            _fixedSize = fixedSize;
            _configuration = configuration ?? PoolingStrategyConfig.MemoryOptimized("FixedSize");
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For30FPS();
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
                return _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold;
            }

            // Trigger on excessive errors
            var errorRate = statistics.TotalCount > 0 ? (double)_errorCount / statistics.TotalCount : 0.0;
            if (errorRate > 0.1) // 10% error rate
            {
                _circuitBreakerTriggerCount++;
                return _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold;
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
            // Fixed-size strategy has minimal overhead
        }

        /// <summary>
        /// Called when a pool operation completes.
        /// </summary>
        /// <param name="duration">Duration of the operation</param>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            if (!_configuration.EnableDetailedMetrics)
                return;

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

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        public void OnPoolError(Exception error)
        {
            _errorCount++;
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
        /// Creates a fixed-size strategy optimized for mobile devices.
        /// </summary>
        /// <param name="size">Fixed pool size</param>
        /// <returns>Mobile-optimized fixed-size strategy</returns>
        public static FixedSizeStrategy ForMobile(int size)
        {
            var config = PoolingStrategyConfig.MemoryOptimized("FixedSizeMobile")
                .WithPerformanceBudget(PerformanceBudget.For30FPS())
                .WithAdditionalTags("mobile", "memory-constrained");
                
            return new FixedSizeStrategy(size, config);
        }

        /// <summary>
        /// Creates a fixed-size strategy for high-performance scenarios.
        /// </summary>
        /// <param name="size">Fixed pool size</param>
        /// <returns>High-performance fixed-size strategy</returns>
        public static FixedSizeStrategy ForHighPerformance(int size)
        {
            var config = PoolingStrategyConfig.HighPerformance("FixedSizeHighPerf")
                .WithPerformanceBudget(PerformanceBudget.For60FPS())
                .WithAdditionalTags("high-performance", "predictable");
                
            return new FixedSizeStrategy(size, config);
        }

        /// <summary>
        /// Creates a fixed-size strategy for testing and development.
        /// </summary>
        /// <param name="size">Fixed pool size</param>
        /// <returns>Development-optimized fixed-size strategy</returns>
        public static FixedSizeStrategy ForDevelopment(int size)
        {
            var config = PoolingStrategyConfig.Development("FixedSizeDev")
                .WithAdditionalTags("development", "testing");
                
            return new FixedSizeStrategy(size, config);
        }
    }
}