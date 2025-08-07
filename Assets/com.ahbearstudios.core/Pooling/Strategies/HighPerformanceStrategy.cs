using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// High-performance pooling strategy optimized for 60+ FPS gameplay.
    /// Minimizes allocations, garbage collection pressure, and frame time variance.
    /// Designed for Unity Jobs System compatibility and Burst compiler optimization.
    /// </summary>
    public class HighPerformanceStrategy : IPoolingStrategy
    {
        private readonly PoolingStrategyConfig _configuration;
        private readonly PerformanceBudget _performanceBudget;
        
        // Core system integration
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        
        // Performance-optimized settings
        private readonly int _preAllocationSize;
        private readonly double _aggressiveExpansionThreshold;
        private readonly double _conservativeContractionThreshold;
        private readonly TimeSpan _maxFrameTime;
        
        // Performance profiling tags (minimal for high performance)
        private static readonly ProfilerTag HighPerfOperationTag = new ProfilerTag("HighPerf.Operation");
        
        // Alert source identifier
        private static readonly FixedString64Bytes AlertSource = "HighPerformanceStrategy";
        
        // Minimal performance monitoring (reduced overhead)
        private int _errorCount;
        private int _budgetViolations;
        private int _gcPressureEvents;
        private TimeSpan _maxRecordedOperationTime;
        private readonly object _metricsLock = new object();

        /// <summary>
        /// Initializes a new instance of the HighPerformanceStrategy.
        /// This constructor should be called by the HighPerformanceStrategyFactory.
        /// </summary>
        /// <param name="configuration">Strategy configuration</param>
        /// <param name="loggingService">The logging service for system integration</param>
        /// <param name="profilerService">The profiler service for performance monitoring</param>
        /// <param name="alertService">The alert service for critical error notifications</param>
        /// <param name="messageBusService">The message bus service for event publishing</param>
        /// <param name="preAllocationSize">Size to pre-allocate at startup (default: 50)</param>
        /// <param name="aggressiveExpansionThreshold">Threshold for aggressive expansion (default: 0.9)</param>
        /// <param name="conservativeContractionThreshold">Threshold for conservative contraction (default: 0.1)</param>
        public HighPerformanceStrategy(
            PoolingStrategyConfig configuration,
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService,
            int preAllocationSize = 50,
            double aggressiveExpansionThreshold = 0.9,
            double conservativeContractionThreshold = 0.1)
        {
            // Validate dependencies
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For60FPS();
            
            _preAllocationSize = Math.Max(1, preAllocationSize);
            _aggressiveExpansionThreshold = Math.Clamp(aggressiveExpansionThreshold, 0.5, 1.0);
            _conservativeContractionThreshold = Math.Clamp(conservativeContractionThreshold, 0.0, 0.3);
            _maxFrameTime = TimeSpan.FromMilliseconds(1000.0 / 60.0); // 60 FPS frame time
            
            // Subscribe to profiler threshold events for performance monitoring
            _profilerService.ThresholdExceeded += OnPerformanceThresholdExceeded;
            
            _loggingService.LogInfo($"HighPerformanceStrategy initialized - Config: {_configuration.Name}, " +
                $"Pre-allocation: {_preAllocationSize}, Expansion: {_aggressiveExpansionThreshold}");
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "HighPerformance";

        /// <summary>
        /// Calculates target size with performance-first approach.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>Target pool size optimized for performance</returns>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            if (statistics == null) return _preAllocationSize;

            var utilization = statistics.Utilization / 100.0;
            var currentSize = statistics.TotalCount;

            // Always maintain minimum size for performance
            var minSize = Math.Max(_preAllocationSize, statistics.InitialCapacity);
            
            // Aggressive expansion to prevent allocation stalls
            if (utilization >= _aggressiveExpansionThreshold)
            {
                // Double the size or add 25% more, whichever is larger
                var doubledSize = currentSize * 2;
                var incrementalSize = currentSize + (currentSize / 4);
                var targetSize = Math.Max(doubledSize, incrementalSize);
                return Math.Min(targetSize, statistics.MaxCapacity);
            }

            // Conservative contraction to maintain performance buffer
            if (utilization <= _conservativeContractionThreshold && currentSize > minSize)
            {
                // Only contract by 10% to maintain performance buffer
                var contractedSize = (int)(currentSize * 0.9);
                return Math.Max(contractedSize, minSize);
            }

            // Maintain current size in the middle range
            return Math.Max(currentSize, minSize);
        }

        /// <summary>
        /// Determines expansion with performance priority.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if pool should expand for performance</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            if (statistics == null) return true;

            var utilization = statistics.Utilization / 100.0;
            
            // Expand aggressively to prevent performance degradation
            return utilization >= _aggressiveExpansionThreshold || 
                   statistics.AvailableCount <= 2; // Always keep at least 2 objects available
        }

        /// <summary>
        /// Determines contraction with performance stability in mind.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if pool can safely contract</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            var minSize = Math.Max(_preAllocationSize, statistics.InitialCapacity);
            
            // Very conservative contraction to maintain performance
            return utilization <= _conservativeContractionThreshold && 
                   statistics.TotalCount > minSize * 2 && // Only contract if significantly above minimum
                   statistics.AvailableCount > statistics.ActiveCount * 3; // Lots of unused capacity
        }

        /// <summary>
        /// Determines object creation with frame-time considerations.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if new object should be created</returns>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            if (statistics == null) return true;

            // Always create if pool is empty or very low
            if (statistics.AvailableCount <= 1)
                return true;

            // Create based on utilization threshold
            var utilization = statistics.Utilization / 100.0;
            return utilization >= _aggressiveExpansionThreshold && 
                   statistics.TotalCount < statistics.MaxCapacity;
        }

        /// <summary>
        /// Determines object destruction with minimal impact.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if objects can be safely destroyed</returns>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            var minSize = Math.Max(_preAllocationSize, statistics.InitialCapacity);
            
            // Only destroy if we have significant excess and very low utilization
            return utilization <= 0.05 && // Very low utilization
                   statistics.TotalCount > minSize * 3 && // Well above minimum
                   statistics.AvailableCount > statistics.ActiveCount * 5; // Excessive unused capacity
        }

        /// <summary>
        /// Gets optimized validation interval for high performance.
        /// </summary>
        /// <returns>Optimized validation interval</returns>
        public TimeSpan GetValidationInterval()
        {
            // Longer intervals to reduce validation overhead
            return TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Validates configuration for high-performance requirements.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <returns>True if configuration supports high performance</returns>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            if (config == null) return false;

            // High-performance pools need sufficient capacity
            if (config.MaxCapacity < _preAllocationSize * 2) return false;
            if (config.InitialCapacity < _preAllocationSize / 2) return false;
            if (config.Factory == null) return false;

            // Validation should not be too frequent
            if (config.ValidationInterval < TimeSpan.FromMinutes(1)) return false;

            return true;
        }

        /// <summary>
        /// Determines circuit breaker trigger with performance focus.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if circuit breaker should trigger</returns>
        public bool ShouldTriggerCircuitBreaker(PoolStatistics statistics)
        {
            if (!_configuration.EnableCircuitBreaker || statistics == null)
                return false;

            // Trigger on excessive performance budget violations
            if (_budgetViolations > _configuration.CircuitBreakerFailureThreshold * 3)
            {
                _loggingService.LogError($"Circuit breaker triggered due to budget violations: {_budgetViolations}");
                _alertService.RaiseAlert(
                    message: "High-performance circuit breaker triggered due to budget violations",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "CircuitBreakerBudgetViolations"
                );
                return true;
            }

            // Trigger on GC pressure events
            if (_gcPressureEvents > _configuration.CircuitBreakerFailureThreshold)
            {
                _loggingService.LogError($"Circuit breaker triggered due to GC pressure: {_gcPressureEvents} events");
                _alertService.RaiseAlert(
                    message: "High-performance circuit breaker triggered due to GC pressure",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "CircuitBreakerGCPressure"
                );
                return true;
            }

            // Trigger on excessive errors
            var errorRate = statistics.TotalCount > 0 ? (double)_errorCount / statistics.TotalCount : 0.0;
            if (errorRate > 0.02) // 2% error rate for high-performance scenarios
            {
                _loggingService.LogError($"Circuit breaker triggered due to high error rate: {errorRate:P}");
                _alertService.RaiseAlert(
                    message: "High-performance circuit breaker triggered due to high error rate",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "CircuitBreakerHighErrors"
                );
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Gets performance budget optimized for 60+ FPS.
        /// </summary>
        /// <returns>High-performance budget configuration</returns>
        public PerformanceBudget GetPerformanceBudget()
        {
            return _performanceBudget;
        }

        /// <summary>
        /// Gets health status focused on performance metrics.
        /// </summary>
        /// <returns>Performance-focused health status</returns>
        public StrategyHealthStatus GetHealthStatus()
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            // Check performance budget violations
            if (_budgetViolations > 0)
            {
                warnings.Add($"Performance budget violations: {_budgetViolations}");
            }

            // Check GC pressure
            if (_gcPressureEvents > 0)
            {
                warnings.Add($"GC pressure events detected: {_gcPressureEvents}");
            }

            // Check maximum operation time
            if (_maxRecordedOperationTime > _performanceBudget.MaxOperationTime)
            {
                warnings.Add($"Max operation time ({_maxRecordedOperationTime.TotalMilliseconds:F2}ms) exceeds budget");
            }

            // Check error count
            if (_errorCount > 0)
            {
                if (_errorCount > 10)
                    errors.Add($"High error count: {_errorCount}");
                else
                    warnings.Add($"Errors encountered: {_errorCount}");
            }

            var status = errors.Count > 0 ? StrategyHealth.Unhealthy :
                        warnings.Count > 0 ? StrategyHealth.Degraded :
                        StrategyHealth.Healthy;

            return new StrategyHealthStatus
            {
                Status = status,
                Description = GetHealthDescription(status),
                Timestamp = DateTime.UtcNow,
                Warnings = warnings,
                Errors = errors,
                ErrorCount = _errorCount,
                MaxOperationTime = _maxRecordedOperationTime,
                Metrics = new Dictionary<string, object>
                {
                    ["PreAllocationSize"] = _preAllocationSize,
                    ["BudgetViolations"] = _budgetViolations,
                    ["GCPressureEvents"] = _gcPressureEvents,
                    ["MaxFrameTime"] = _maxFrameTime.TotalMilliseconds,
                    ["TargetFPS"] = 60,
                    ["OptimizedForBurst"] = true,
                    ["JobSystemCompatible"] = true,
                    ["ZeroAllocationGoal"] = true
                }
            };
        }

        /// <summary>
        /// Called when pool operation starts (minimal overhead).
        /// </summary>
        public void OnPoolOperationStart()
        {
            // Minimal overhead for high performance
        }

        /// <summary>
        /// Called when pool operation completes (performance monitoring).
        /// </summary>
        /// <param name="duration">Operation duration</param>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            // Check performance budget
            if (duration > _performanceBudget.MaxOperationTime)
            {
                _budgetViolations++;
                
                // Log critical performance violations in high-perf scenarios
                if (_budgetViolations % 10 == 0)
                {
                    _loggingService.LogWarning($"High-performance budget violations: {_budgetViolations} (Latest: {duration.TotalMilliseconds}ms)");
                }
            }

            // Track maximum operation time
            if (duration > _maxRecordedOperationTime)
            {
                _maxRecordedOperationTime = duration;
                
                // Alert on new maximum if it's significantly high
                if (duration > _performanceBudget.MaxOperationTime.Multiply(3))
                {
                    _alertService.RaiseAlert(
                        message: $"New maximum operation time recorded: {duration.TotalMilliseconds}ms",
                        severity: AlertSeverity.Warning,
                        source: AlertSource,
                        tag: "PerformanceDegradation"
                    );
                }
            }

            // Check for potential GC pressure (operations taking unusually long)
            if (duration > _maxFrameTime)
            {
                _gcPressureEvents++;
                
                if (_gcPressureEvents % 50 == 0)
                {
                    _loggingService.LogWarning($"Potential GC pressure detected - Events: {_gcPressureEvents}");
                }
            }
        }

        /// <summary>
        /// Called when pool operation encounters an error.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        public void OnPoolError(Exception error)
        {
            _errorCount++;
            _loggingService.LogException($"High-performance pool error #{_errorCount}", error);
            
            // High-performance strategies should have very low error tolerance
            if (_errorCount > 3)
            {
                _alertService.RaiseAlert(
                    message: "Multiple errors in high-performance strategy - performance may be compromised",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "HighPerformanceCompromised"
                );
            }
        }

        /// <summary>
        /// Gets network metrics (not applicable for high-performance strategy).
        /// </summary>
        /// <returns>Null - high-performance strategy is not network-specific</returns>
        public NetworkPoolingMetrics GetNetworkMetrics()
        {
            // High-performance strategy is not network-specific
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
        /// Gets the pre-allocation size used by this strategy.
        /// </summary>
        /// <returns>Pre-allocation size</returns>
        public int GetPreAllocationSize()
        {
            return _preAllocationSize;
        }

        /// <summary>
        /// Gets the number of performance budget violations.
        /// </summary>
        /// <returns>Budget violation count</returns>
        public int GetBudgetViolations()
        {
            return _budgetViolations;
        }

        /// <summary>
        /// Gets the number of GC pressure events detected.
        /// </summary>
        /// <returns>GC pressure event count</returns>
        public int GetGCPressureEvents()
        {
            return _gcPressureEvents;
        }

        /// <summary>
        /// Resets performance monitoring counters.
        /// </summary>
        public void ResetPerformanceCounters()
        {
            lock (_metricsLock)
            {
                _budgetViolations = 0;
                _gcPressureEvents = 0;
                _errorCount = 0;
                _maxRecordedOperationTime = TimeSpan.Zero;
            }
        }

        #region Private Helper Methods

        private string GetHealthDescription(StrategyHealth status)
        {
            return status switch
            {
                StrategyHealth.Healthy => "High-performance strategy maintaining 60+ FPS targets",
                StrategyHealth.Degraded => "High-performance strategy experiencing performance degradation",
                StrategyHealth.Unhealthy => "High-performance strategy has critical performance issues",
                _ => "High-performance strategy status unknown"
            };
        }

        #endregion

        /// <summary>
        /// Handles performance threshold exceeded events from the profiler service.
        /// </summary>
        /// <param name="tag">The profiler tag that exceeded threshold</param>
        /// <param name="value">The measured value</param>
        /// <param name="unit">The unit of measurement</param>
        private void OnPerformanceThresholdExceeded(ProfilerTag tag, double value, string unit)
        {
            _loggingService.LogWarning($"HighPerformance threshold exceeded for {tag.Name}: {value}{unit} - This is critical for high-performance scenarios");
            
            // High-performance operations should be extremely fast
            if (value > 2.0) // > 2ms is critical for high-performance operations
            {
                _alertService.RaiseAlert(
                    message: $"Critical performance degradation in {Name}: {tag.Name} took {value}{unit}",
                    severity: AlertSeverity.Critical,
                    source: AlertSource,
                    tag: "HighPerformanceCriticalDegradation"
                );
            }
        }
    }
}