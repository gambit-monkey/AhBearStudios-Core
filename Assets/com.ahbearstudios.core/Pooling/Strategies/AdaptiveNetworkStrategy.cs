using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling.Models;
using Unity.Collections;

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
        
        // Core system integration
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        
        // Network-specific thresholds
        private readonly double _spikeDetectionThreshold;
        private readonly double _preemptiveAllocationRatio;
        private readonly TimeSpan _burstWindow;
        private readonly int _maxBurstAllocations;
        
        // Performance profiling tags
        private static readonly ProfilerTag CalculateTargetTag = new ProfilerTag("AdaptiveNetwork.CalculateTarget");
        private static readonly ProfilerTag ShouldExpandTag = new ProfilerTag("AdaptiveNetwork.ShouldExpand");
        private static readonly ProfilerTag ShouldContractTag = new ProfilerTag("AdaptiveNetwork.ShouldContract");
        private static readonly ProfilerTag NetworkSpikeDetectionTag = new ProfilerTag("AdaptiveNetwork.SpikeDetection");
        
        // Alert source identifier
        private static readonly FixedString64Bytes AlertSource = "AdaptiveNetworkStrategy";
        
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
        /// This constructor should be called by the AdaptiveNetworkStrategyFactory.
        /// </summary>
        /// <param name="configuration">Strategy configuration</param>
        /// <param name="loggingService">The logging service for system integration</param>
        /// <param name="profilerService">The profiler service for performance monitoring</param>
        /// <param name="alertService">The alert service for critical error notifications</param>
        /// <param name="messageBusService">The message bus service for event publishing</param>
        /// <param name="spikeDetectionThreshold">Threshold for detecting network spikes (default: 0.8)</param>
        /// <param name="preemptiveAllocationRatio">Ratio of preemptive allocations (default: 0.2)</param>
        /// <param name="burstWindow">Time window for burst detection (default: 5 seconds)</param>
        /// <param name="maxBurstAllocations">Maximum allocations during burst (default: 50)</param>
        public AdaptiveNetworkStrategy(
            PoolingStrategyConfig configuration,
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService,
            double spikeDetectionThreshold = 0.8,
            double preemptiveAllocationRatio = 0.2,
            TimeSpan? burstWindow = null,
            int maxBurstAllocations = 50)
        {
            // Validate dependencies
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For60FPS();
            
            _spikeDetectionThreshold = Math.Clamp(spikeDetectionThreshold, 0.1, 1.0);
            _preemptiveAllocationRatio = Math.Clamp(preemptiveAllocationRatio, 0.0, 0.5);
            _burstWindow = burstWindow ?? TimeSpan.FromSeconds(5);
            _maxBurstAllocations = Math.Max(1, maxBurstAllocations);
            
            // Subscribe to profiler threshold events for performance monitoring
            _profilerService.ThresholdExceeded += OnPerformanceThresholdExceeded;
            
            _loggingService.LogInfo($"AdaptiveNetworkStrategy initialized - Config: {_configuration.Name}, " +
                $"Spike Threshold: {_spikeDetectionThreshold}, Preemptive Ratio: {_preemptiveAllocationRatio}");
            
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
            if (statistics == null)
            {
                _loggingService.LogWarning("CalculateTargetSize called with null statistics");
                return 0;
            }

            using var profileSession = _profilerService.BeginScope(CalculateTargetTag);
            
            // Record performance sample for monitoring
            var startTime = DateTime.UtcNow;
            
            var currentSize = statistics.TotalCount;
            var utilization = statistics.Utilization / 100.0;
            
            _loggingService.LogDebug($"Calculating target size - Current: {currentSize}, Utilization: {utilization:P}");
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            _profilerService.RecordSample(CalculateTargetTag, (float)duration.TotalMilliseconds, "ms");
            
            // Detect network spikes and adjust accordingly
            if (IsNetworkSpikeDetected())
            {
                // During network spikes, expand more aggressively
                var spikeMultiplier = 1.5 + (_spikeDetectionThreshold * 0.5);
                var targetSize = (int)Math.Ceiling(currentSize * spikeMultiplier);
                _spikeTriggeredAllocations += targetSize - currentSize;
                
                _loggingService.LogInfo($"Network spike detected - Expanding pool from {currentSize} to {targetSize}");
                _messageBusService.PublishMessage(PoolExpansionMessage.Create(
                    strategyName: Name,
                    oldSize: currentSize,
                    newSize: targetSize,
                    reason: "NetworkSpike",
                    source: AlertSource
                ));
                
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
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should expand</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            if (statistics == null)
            {
                _loggingService.LogWarning("ShouldExpand called with null statistics");
                return false;
            }

            using var profileSession = _profilerService.BeginScope(ShouldExpandTag);
            var utilization = statistics.Utilization / 100.0;
            
            // Always expand if no buffers available (critical for network responsiveness)
            if (statistics.AvailableCount == 0)
            {
                _bufferExhaustionEvents++;
                _loggingService.LogWarning($"Buffer exhaustion detected - Event count: {_bufferExhaustionEvents}");
                
                if (_bufferExhaustionEvents > 5)
                {
                    _alertService.RaiseAlert(
                        message: "Frequent buffer exhaustion events detected",
                        severity: AlertSeverity.Warning,
                        source: AlertSource,
                        tag: "BufferExhaustion"
                    );
                }
                
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
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should contract</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            if (statistics == null)
            {
                _loggingService.LogWarning("ShouldContract called with null statistics");
                return false;
            }

            using var profileSession = _profilerService.BeginScope(ShouldContractTag);
            var utilization = statistics.Utilization / 100.0;
            
            // Never contract during network spikes or high utilization
            if (IsNetworkSpikeDetected() || utilization >= 0.5)
            {
                _loggingService.LogDebug("Contract prevented due to network spike or high utilization");
                return false;
            }

            // Contract if utilization is consistently low
            var hasLowUtilization = utilization <= 0.2;
            var hasExcessCapacity = statistics.AvailableCount > statistics.ActiveCount * 2;
            var isAboveMinimum = statistics.TotalCount > Math.Max(statistics.InitialCapacity, 5);
            
            return hasLowUtilization && hasExcessCapacity && isAboveMinimum;
        }

        /// <summary>
        /// Determines if a new object should be created based on network demand.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
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
        /// <param name="statistics">Current pool statistics</param>
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
        /// <param name="config">Pool configuration to validate</param>
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
        /// <param name="statistics">Current pool statistics</param>
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
                warnings.Add($"Buffer exhaustion events: {_bufferExhaustionEvents}");
            }

            // Check error rate
            if (_errorCount > 0)
            {
                warnings.Add($"Network operation errors: {_errorCount}");
            }

            // Check network spikes
            var recentSpikes = GetRecentNetworkSpikes();
            if (recentSpikes.Count > 10)
            {
                warnings.Add($"High network spike frequency: {recentSpikes.Count} in recent window");
            }

            // Check circuit breaker
            if (_circuitBreakerTriggerCount >= _configuration.CircuitBreakerFailureThreshold)
            {
                errors.Add("Network circuit breaker threshold reached");
                
                // Alert on circuit breaker activation
                _alertService.RaiseAlert(
                    message: "Network pooling circuit breaker activated",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "CircuitBreakerActivated"
                );
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
                    ["PacketsProcessed"] = _packetsProcessed,
                    ["BytesProcessed"] = _bytesProcessed,
                    ["NetworkSpikes"] = _networkSpikes.Count,
                    ["BufferExhaustionEvents"] = _bufferExhaustionEvents,
                    ["SpikeTriggeredAllocations"] = _spikeTriggeredAllocations,
                    ["PreemptiveAllocations"] = _preemptiveAllocations
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
        /// <param name="duration">Duration of the operation</param>
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

            // Check for performance budget violations
            if (duration > _performanceBudget.MaxOperationTime)
            {
                _loggingService.LogWarning($"Pool operation exceeded performance budget: {duration.TotalMilliseconds}ms > {_performanceBudget.MaxOperationTime.TotalMilliseconds}ms");
            }

            // Detect network spikes based on operation frequency
            if (_recentOperationTimes.Count > 10)
            {
                var recentOpsPerSecond = _recentOperationTimes.Count / 10.0; // Approximate
                if (recentOpsPerSecond > 50) // High frequency threshold
                {
                    _networkSpikes.Add(DateTime.UtcNow);
                    _loggingService.LogInfo($"Network spike detected - Operations per second: {recentOpsPerSecond:F1}");
                    
                    _messageBusService.PublishMessage(PoolNetworkSpikeDetectedMessage.Create(
                        strategyName: Name,
                        operationsPerSecond: recentOpsPerSecond,
                        source: AlertSource
                    ));
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
            
            if (_errorCount > 10)
            {
                _alertService.RaiseAlert(
                    message: "High error rate in AdaptiveNetworkStrategy",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "HighErrorRate"
                );
            }
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
            using var profileSession = _profilerService.BeginScope(NetworkSpikeDetectionTag);
            
            var startTime = DateTime.UtcNow;
            var recentSpikes = GetRecentNetworkSpikes();
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            _profilerService.RecordSample(NetworkSpikeDetectionTag, (float)duration.TotalMilliseconds, "ms");
            
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
                StrategyHealth.Healthy => "Network strategy operating optimally",
                StrategyHealth.Degraded => $"Network strategy has {warningCount} performance warnings",
                StrategyHealth.Unhealthy => $"Network strategy has {errorCount} critical errors",
                _ => "Network strategy status unknown"
            };
        }

        /// <summary>
        /// Handles performance threshold exceeded events from the profiler service.
        /// </summary>
        /// <param name="tag">The profiler tag that exceeded threshold</param>
        /// <param name="value">The measured value</param>
        /// <param name="unit">The unit of measurement</param>
        private void OnPerformanceThresholdExceeded(ProfilerTag tag, double value, string unit)
        {
            _loggingService.LogWarning($"Performance threshold exceeded for {tag.Name}: {value}{unit}");
            
            // Raise alert for significant performance degradation
            if (value > 10.0) // > 10ms is significant for pooling operations
            {
                _alertService.RaiseAlert(
                    message: $"Severe performance degradation in {Name}: {tag.Name} took {value}{unit}",
                    severity: AlertSeverity.Warning,
                    source: AlertSource,
                    tag: "PerformanceDegradation"
                );
            }
        }

        #endregion
    }
}