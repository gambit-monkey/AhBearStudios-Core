using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Network-optimized pooling strategy that adapts to network traffic patterns.
    /// Designed specifically for FishNet + MemoryPack integration with frame-budget awareness.
    /// Handles network spikes, burst traffic, and maintains 60+ FPS performance.
    /// </summary>
    public class AdaptiveNetworkStrategy : IPoolingStrategy
    {
        private readonly PoolingStrategyConfig _configuration;
        private readonly PerformanceBudget _performanceBudget;
        private readonly NetworkPoolingMetrics _networkMetrics;
        
        // Network-specific thresholds
        private readonly double _spikeDetectionThreshold;
        private readonly double _preemptiveAllocationRatio;
        private readonly TimeSpan _burstWindow;
        private readonly int _maxBurstAllocations;
        
        // Performance monitoring
        private readonly List<TimeSpan> _recentOperationTimes = new();
        private readonly List<DateTime> _networkSpikes = new();
        private readonly object _metricsLock = new object();
        private int _errorCount;
        private int _circuitBreakerTriggerCount;
        private long _packetsProcessed;
        private long _bytesProcessed;
        private long _spikeTriggeredAllocations;
        private long _preemptiveAllocations;
        private int _bufferExhaustionEvents;
        private DateTime _lastNetworkMetricsUpdate = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the AdaptiveNetworkStrategy.
        /// </summary>
        /// <param name="configuration">Strategy configuration</param>
        /// <param name="spikeDetectionThreshold">Threshold for detecting network spikes (default: 0.8)</param>
        /// <param name="preemptiveAllocationRatio">Ratio of preemptive allocations (default: 0.2)</param>
        /// <param name="burstWindow">Time window for burst detection (default: 5 seconds)</param>
        /// <param name="maxBurstAllocations">Maximum allocations during burst (default: 50)</param>
        public AdaptiveNetworkStrategy(
            PoolingStrategyConfig configuration = null,
            double spikeDetectionThreshold = 0.8,
            double preemptiveAllocationRatio = 0.2,
            TimeSpan? burstWindow = null,
            int maxBurstAllocations = 50)
        {
            _configuration = configuration ?? PoolingStrategyConfig.NetworkOptimized("AdaptiveNetwork");
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For60FPS();
            
            _spikeDetectionThreshold = Math.Clamp(spikeDetectionThreshold, 0.1, 1.0);
            _preemptiveAllocationRatio = Math.Clamp(preemptiveAllocationRatio, 0.0, 0.5);
            _burstWindow = burstWindow ?? TimeSpan.FromSeconds(5);
            _maxBurstAllocations = Math.Max(1, maxBurstAllocations);
            
            _networkMetrics = new NetworkPoolingMetrics
            {
                CapturedAt = DateTime.UtcNow,
                CollectionPeriod = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "AdaptiveNetwork";

        /// <summary>
        /// Calculates the target size for the pool based on network traffic patterns.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>Target pool size</returns>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            if (statistics == null) return 0;

            var currentSize = statistics.TotalCount;
            var utilization = statistics.Utilization / 100.0;
            
            // Detect network spikes and adjust accordingly
            if (IsNetworkSpikeDetected())
            {
                // During network spikes, expand more aggressively
                var spikeMultiplier = 1.5 + (_spikeDetectionThreshold * 0.5);
                var targetSize = (int)Math.Ceiling(currentSize * spikeMultiplier);
                _spikeTriggeredAllocations += targetSize - currentSize;
                return Math.Min(targetSize, statistics.MaxCapacity);
            }

            // Normal network traffic - adaptive sizing
            if (utilization >= 0.9) // Very high utilization
            {
                return Math.Min(currentSize + Math.Max(5, currentSize / 4), statistics.MaxCapacity);
            }
            else if (utilization >= 0.7) // High utilization
            {
                var preemptiveAllocations = (int)(currentSize * _preemptiveAllocationRatio);
                _preemptiveAllocations += preemptiveAllocations;
                return Math.Min(currentSize + preemptiveAllocations, statistics.MaxCapacity);
            }
            else if (utilization <= 0.3 && currentSize > statistics.InitialCapacity) // Low utilization
            {
                // Contract gradually, but maintain minimum for network responsiveness
                var minSize = Math.Max(statistics.InitialCapacity, statistics.MaxCapacity / 4);
                return Math.Max(minSize, (int)(currentSize * 0.8));
            }

            return currentSize;
        }

        /// <summary>
        /// Determines if the pool should expand based on network conditions.
        /// </summary>
        /// <param name=\"statistics\">Current pool statistics</param>
        /// <returns>True if the pool should expand</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            
            // Always expand if no buffers available (critical for network responsiveness)
            if (statistics.AvailableCount == 0)
            {
                _bufferExhaustionEvents++;
                return true;
            }

            // Expand during network spikes
            if (IsNetworkSpikeDetected())
                return true;

            // Expand if utilization is high
            return utilization >= _spikeDetectionThreshold;
        }

        /// <summary>
        /// Determines if the pool should contract based on network patterns.
        /// </summary>
        /// <param name=\"statistics\">Current pool statistics</param>
        /// <returns>True if the pool should contract</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            
            // Never contract during network spikes or high utilization
            if (IsNetworkSpikeDetected() || utilization >= 0.5)
                return false;

            // Contract if utilization is consistently low
            var hasLowUtilization = utilization <= 0.2;
            var hasExcessCapacity = statistics.AvailableCount > statistics.ActiveCount * 2;
            var isAboveMinimum = statistics.TotalCount > Math.Max(statistics.InitialCapacity, 5);
            
            return hasLowUtilization && hasExcessCapacity && isAboveMinimum;
        }

        /// <summary>
        /// Determines if a new object should be created based on network demand.
        /// </summary>
        /// <param name=\"statistics\">Current pool statistics</param>
        /// <returns>True if a new object should be created</returns>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            if (statistics == null) return true;

            // Always create if pool is empty (critical for network)
            if (statistics.AvailableCount == 0)
                return true;

            // Create during network spikes with burst protection
            if (IsNetworkSpikeDetected())
            {
                var recentSpikes = GetRecentNetworkSpikes();
                return recentSpikes.Count < _maxBurstAllocations;
            }

            // Create based on utilization and available capacity
            var utilization = statistics.Utilization / 100.0;
            return utilization >= _spikeDetectionThreshold && statistics.TotalCount < statistics.MaxCapacity;
        }

        /// <summary>
        /// Determines if objects should be destroyed to free memory.
        /// </summary>
        /// <param name=\"statistics\">Current pool statistics</param>
        /// <returns>True if objects should be destroyed</returns>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            // Never destroy during network activity
            if (IsNetworkSpikeDetected())
                return false;

            var utilization = statistics.Utilization / 100.0;
            var hasExcessCapacity = statistics.AvailableCount > statistics.ActiveCount * 3;
            var isLowUtilization = utilization <= 0.1;
            var isAboveMinimum = statistics.TotalCount > Math.Max(statistics.InitialCapacity, 10);
            
            return hasExcessCapacity && isLowUtilization && isAboveMinimum;
        }

        /// <summary>
        /// Gets the interval between validation checks (more frequent for network strategy).
        /// </summary>
        /// <returns>Validation interval</returns>
        public TimeSpan GetValidationInterval()
        {
            // More frequent validation for network responsiveness
            return TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Validates the pool configuration for network optimizations.
        /// </summary>
        /// <param name=\"config\">Pool configuration to validate</param>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            if (config == null) return false;

            // Network pools need higher capacity and faster validation
            if (config.MaxCapacity < 20) return false;
            if (config.ValidationInterval > TimeSpan.FromMinutes(1)) return false;
            if (config.Factory == null) return false;

            return true;
        }

        /// <summary>
        /// Determines if the circuit breaker should be triggered.
        /// </summary>
        /// <param name=\"statistics\">Current pool statistics</param>
        /// <returns>True if circuit breaker should be triggered</returns>
        public bool ShouldTriggerCircuitBreaker(PoolStatistics statistics)
        {
            if (!_configuration.EnableCircuitBreaker || statistics == null)
                return false;

            // Trigger on excessive buffer exhaustion events
            if (_bufferExhaustionEvents > _configuration.CircuitBreakerFailureThreshold * 2)
            {
                _circuitBreakerTriggerCount++;
                return _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold;
            }

            // Trigger on excessive errors
            var errorRate = statistics.TotalCount > 0 ? (double)_errorCount / statistics.TotalCount : 0.0;
            if (errorRate > 0.05) // 5% error rate for network operations
            {
                _circuitBreakerTriggerCount++;
                return _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold;
            }

            return false;
        }

        /// <summary>
        /// Gets the performance budget for network operations.
        /// </summary>
        /// <returns>Performance budget configuration</returns>
        public PerformanceBudget GetPerformanceBudget()
        {
            return _performanceBudget;
        }

        /// <summary>
        /// Gets the current health status of the network strategy.
        /// </summary>
        /// <returns>Strategy health status</returns>
        public StrategyHealthStatus GetHealthStatus()
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            // Check buffer exhaustion events
            if (_bufferExhaustionEvents > 0)
            {
                warnings.Add($\"Buffer exhaustion events: {_bufferExhaustionEvents}\");
            }

            // Check error rate
            if (_errorCount > 0)
            {
                warnings.Add($\"Network operation errors: {_errorCount}\");
            }

            // Check network spikes
            var recentSpikes = GetRecentNetworkSpikes();
            if (recentSpikes.Count > 10)
            {
                warnings.Add($\"High network spike frequency: {recentSpikes.Count} in recent window\");
            }

            // Check circuit breaker
            if (_circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold)
            {
                errors.Add(\"Network circuit breaker threshold reached\");
            }

            var status = errors.Count > 0 ? StrategyHealth.Unhealthy :
                        warnings.Count > 0 ? StrategyHealth.Degraded :
                        StrategyHealth.Healthy;

            return new StrategyHealthStatus
            {
                Status = status,
                Description = GetHealthDescription(status, warnings.Count, errors.Count),
                Timestamp = DateTime.UtcNow,
                Warnings = warnings,
                Errors = errors,
                IsCircuitBreakerOpen = _circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold,
                OperationCount = _recentOperationTimes.Count,
                ErrorCount = _errorCount,
                AverageOperationTime = GetAverageOperationTime(),
                MaxOperationTime = GetMaxOperationTime(),
                Metrics = new Dictionary<string, object>
                {
                    [\"PacketsProcessed\"] = _packetsProcessed,
                    [\"BytesProcessed\"] = _bytesProcessed,
                    [\"NetworkSpikes\"] = _networkSpikes.Count,
                    [\"BufferExhaustionEvents\"] = _bufferExhaustionEvents,
                    [\"SpikeTriggeredAllocations\"] = _spikeTriggeredAllocations,
                    [\"PreemptiveAllocations\"] = _preemptiveAllocations
                }
            };
        }

        /// <summary>
        /// Called when a pool operation starts.
        /// </summary>
        public void OnPoolOperationStart()
        {
            // Network operations can update packet/byte counters here
        }

        /// <summary>
        /// Called when a pool operation completes.
        /// </summary>
        /// <param name=\"duration\">Duration of the operation</param>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            if (!_configuration.EnableDetailedMetrics)
                return;

            lock (_metricsLock)
            {
                _recentOperationTimes.Add(duration);
                
                if (_recentOperationTimes.Count > _configuration.MaxMetricsSamples)
                {
                    _recentOperationTimes.RemoveAt(0);
                }
            }

            // Detect network spikes based on operation frequency
            if (_recentOperationTimes.Count > 10)
            {
                var recentOpsPerSecond = _recentOperationTimes.Count / 10.0; // Approximate
                if (recentOpsPerSecond > 50) // High frequency threshold
                {
                    _networkSpikes.Add(DateTime.UtcNow);
                }
            }
        }

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        /// <param name=\"error\">The error that occurred</param>
        public void OnPoolError(Exception error)
        {
            _errorCount++;
        }

        /// <summary>
        /// Gets network-specific metrics for this strategy.
        /// </summary>
        /// <returns>Network pooling metrics</returns>
        public NetworkPoolingMetrics GetNetworkMetrics()
        {
            var now = DateTime.UtcNow;
            var collectionPeriod = now - _lastNetworkMetricsUpdate;
            
            var recentSpikes = GetRecentNetworkSpikes();
            
            var metrics = new NetworkPoolingMetrics
            {
                PacketsProcessed = _packetsProcessed,
                BytesProcessed = _bytesProcessed,
                NetworkSpikesDetected = _networkSpikes.Count,
                SpikeTriggeredAllocations = _spikeTriggeredAllocations,
                PreemptiveAllocations = _preemptiveAllocations,
                BufferExhaustionEvents = _bufferExhaustionEvents,
                CapturedAt = now,
                CollectionPeriod = collectionPeriod
            };
            
            _lastNetworkMetricsUpdate = now;
            return metrics;
        }

        /// <summary>
        /// Gets the strategy configuration.
        /// </summary>
        /// <returns>Strategy configuration</returns>
        public PoolingStrategyConfig GetConfiguration()
        {
            return _configuration;
        }

        #region Private Helper Methods

        private bool IsNetworkSpikeDetected()
        {
            var recentSpikes = GetRecentNetworkSpikes();
            return recentSpikes.Count > 0;
        }

        private List<DateTime> GetRecentNetworkSpikes()
        {
            var cutoff = DateTime.UtcNow - _burstWindow;
            var recentSpikes = new List<DateTime>();
            
            foreach (var spike in _networkSpikes)
            {
                if (spike >= cutoff)
                    recentSpikes.Add(spike);
            }
            
            return recentSpikes;
        }

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
                StrategyHealth.Healthy => \"Network strategy operating optimally\",
                StrategyHealth.Degraded => $\"Network strategy has {warningCount} performance warnings\",
                StrategyHealth.Unhealthy => $\"Network strategy has {errorCount} critical errors\",
                _ => \"Network strategy status unknown\"
            };
        }

        #endregion
    }
}