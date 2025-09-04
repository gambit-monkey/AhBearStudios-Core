using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZLinq;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health statistics collection and analysis.
    /// Provides comprehensive metrics tracking, trend analysis, and performance monitoring.
    /// </summary>
    public sealed class HealthStatisticsCollector : IHealthStatisticsCollector
    {
        private readonly ILoggingService _logger;
        private readonly IMessageBusService _messageBus;
        private readonly IProfilerService _profilerService;
        private readonly HealthCheckServiceConfig _config;

        private readonly ProfilerMarker _recordingMarker = new ProfilerMarker("HealthStatisticsCollector.Record");
        private readonly Guid _collectorId;
        private readonly object _stateLock = new();

        // Collection state
        private readonly DateTime _collectionStartTime;
        private bool _disposed;

        // Performance thresholds
        private TimeSpan _slowExecutionThreshold;
        private double _highFailureRateThreshold;

        // Automatic cleanup
        private bool _automaticCleanupEnabled;
        private TimeSpan _retentionPeriod;

        // Global statistics
        private long _totalHealthCheckExecutions;
        private long _totalSuccessfulExecutions;
        private long _totalFailedExecutions;
        private long _totalTimedOutExecutions;
        private DateTime _lastStatsReset;

        // Health check specific statistics
        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckStats> _healthCheckStats;
        
        // Circuit breaker statistics
        private readonly ConcurrentDictionary<FixedString64Bytes, CircuitBreakerStats> _circuitBreakerStats;

        // Time-series data for trend analysis
        private readonly ConcurrentQueue<HealthCheckExecutionRecord> _executionHistory;
        private readonly ConcurrentQueue<SystemHealthSnapshot> _systemHealthHistory;

        /// <summary>
        /// Event triggered when statistics are reset.
        /// </summary>
        public event EventHandler<StatisticsResetEventArgs> StatisticsReset;

        /// <summary>
        /// Event triggered when performance thresholds are exceeded.
        /// </summary>
        public event EventHandler<PerformanceThresholdEventArgs> PerformanceThresholdExceeded;

        /// <summary>
        /// Gets the timestamp when statistics collection started.
        /// </summary>
        public DateTime CollectionStartTime => _collectionStartTime;

        /// <summary>
        /// Gets the total uptime of the statistics collector.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _collectionStartTime;

        /// <summary>
        /// Initializes a new health statistics collector.
        /// </summary>
        /// <param name="config">Health check service configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="messageBus">Message bus</param>
        /// <param name="profilerService">Profiler service</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public HealthStatisticsCollector(
            HealthCheckServiceConfig config,
            ILoggingService logger,
            IMessageBusService messageBus,
            IProfilerService profilerService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));

            _collectorId = DeterministicIdGenerator.GenerateHealthCheckId("HealthStatisticsCollector", Environment.MachineName);
            _collectionStartTime = DateTime.UtcNow;
            _lastStatsReset = _collectionStartTime;

            // Initialize performance thresholds
            _slowExecutionThreshold = TimeSpan.FromMilliseconds(_config.SlowHealthCheckThreshold);
            _highFailureRateThreshold = 0.20; // 20% failure rate threshold

            // Initialize cleanup settings
            _automaticCleanupEnabled = true;
            _retentionPeriod = _config.MaxHistoryAge;

            // Initialize collections
            _healthCheckStats = new ConcurrentDictionary<FixedString64Bytes, HealthCheckStats>();
            _circuitBreakerStats = new ConcurrentDictionary<FixedString64Bytes, CircuitBreakerStats>();
            _executionHistory = new ConcurrentQueue<HealthCheckExecutionRecord>();
            _systemHealthHistory = new ConcurrentQueue<SystemHealthSnapshot>();

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("StatisticsCollectorInit", _collectorId.ToString());
            _logger.LogInfo("HealthStatisticsCollector initialized", correlationId);
        }

        /// <summary>
        /// Records the execution of a health check.
        /// </summary>
        /// <param name="result">Health check result to record</param>
        public void RecordHealthCheckExecution(HealthCheckResult result)
        {
            ThrowIfDisposed();

            if (result == null)
                return;

            using (_recordingMarker.Auto())
            {
                // Update global statistics
                Interlocked.Increment(ref _totalHealthCheckExecutions);

                if (result.Status == HealthStatus.Healthy)
                    Interlocked.Increment(ref _totalSuccessfulExecutions);
                else
                    Interlocked.Increment(ref _totalFailedExecutions);

                // Update health check specific statistics
                var stats = _healthCheckStats.AddOrUpdate(result.Name, 
                    _ => new HealthCheckStats(result.Name, result.Category),
                    (_, existing) => existing.RecordExecution(result));

                // Record in execution history for trend analysis
                var executionRecord = new HealthCheckExecutionRecord
                {
                    Timestamp = result.Timestamp,
                    HealthCheckName = result.Name,
                    Status = result.Status,
                    Duration = result.Duration,
                    Category = result.Category
                };

                _executionHistory.Enqueue(executionRecord);

                // Check performance thresholds
                CheckPerformanceThresholds(result, stats);

                // Cleanup old records if automatic cleanup is enabled
                if (_automaticCleanupEnabled)
                {
                    CleanupOldExecutionHistory();
                }
            }
        }

        /// <summary>
        /// Records a health report for system-wide metrics.
        /// </summary>
        /// <param name="report">Health report to record</param>
        public void RecordHealthReport(HealthReport report)
        {
            ThrowIfDisposed();

            if (report == null)
                return;

            // Create system health snapshot
            var snapshot = new SystemHealthSnapshot
            {
                Timestamp = report.Timestamp,
                OverallStatus = report.Status,
                TotalChecks = report.TotalChecks,
                HealthyCount = report.HealthyCount,
                DegradedCount = report.DegradedCount,
                UnhealthyCount = report.UnhealthyCount,
                AverageExecutionTime = report.GetAverageExecutionTime(),
                DegradationLevel = report.CurrentDegradationLevel
            };

            _systemHealthHistory.Enqueue(snapshot);

            // Cleanup old snapshots
            if (_automaticCleanupEnabled)
            {
                CleanupOldSystemHistory();
            }
        }

        /// <summary>
        /// Records a circuit breaker state change.
        /// </summary>
        /// <param name="name">Circuit breaker name</param>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="reason">Reason for state change</param>
        public void RecordCircuitBreakerStateChange(FixedString64Bytes name, CircuitBreakerState oldState, CircuitBreakerState newState, string reason)
        {
            ThrowIfDisposed();

            var stats = _circuitBreakerStats.AddOrUpdate(name,
                _ => new CircuitBreakerStats(name, newState),
                (_, existing) => existing.RecordStateChange(newState, reason));

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerStateChange", _collectorId.ToString());
            _logger.LogDebug($"Recorded circuit breaker state change: {name} {oldState} -> {newState}", correlationId);
        }

        /// <summary>
        /// Records a degradation level change.
        /// </summary>
        /// <param name="oldLevel">Previous degradation level</param>
        /// <param name="newLevel">New degradation level</param>
        /// <param name="reason">Reason for change</param>
        public void RecordDegradationLevelChange(DegradationLevel oldLevel, DegradationLevel newLevel, string reason)
        {
            ThrowIfDisposed();

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("DegradationLevelChange", _collectorId.ToString());
            _logger.LogInfo($"Recorded degradation level change: {oldLevel} -> {newLevel} ({reason})", correlationId);
        }

        /// <summary>
        /// Gets comprehensive health statistics.
        /// </summary>
        /// <returns>Current health statistics</returns>
        public HealthStatistics GetHealthStatistics()
        {
            ThrowIfDisposed();

            var uptime = Uptime;
            var averageExecutionTime = CalculateAverageExecutionTime();
            var circuitBreakerStatistics = GetCircuitBreakerStatistics();

            return HealthStatistics.Create(
                serviceUptime: uptime,
                totalHealthChecks: _totalHealthCheckExecutions,
                successfulHealthChecks: _totalSuccessfulExecutions,
                failedHealthChecks: _totalFailedExecutions,
                registeredHealthCheckCount: _healthCheckStats.Count,
                currentDegradationLevel: GetCurrentDegradationLevel(),
                lastOverallStatus: GetLastOverallStatus(),
                circuitBreakerStatistics: circuitBreakerStatistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                averageExecutionTime: averageExecutionTime,
                openCircuitBreakers: circuitBreakerStatistics.Values.Count(cb => cb.State == CircuitBreakerState.Open),
                activeCircuitBreakers: circuitBreakerStatistics.Count
            );
        }

        /// <summary>
        /// Gets statistics for a specific health check.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <returns>Individual health check statistics, or null if not found</returns>
        public IndividualHealthCheckStatistics GetHealthCheckStatistics(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            if (!_healthCheckStats.TryGetValue(healthCheckName, out var stats))
                return null;

            return stats.ToIndividualStatistics();
        }

        /// <summary>
        /// Gets statistics for all registered health checks.
        /// </summary>
        /// <returns>Dictionary of health check statistics</returns>
        public IReadOnlyDictionary<FixedString64Bytes, IndividualHealthCheckStatistics> GetAllHealthCheckStatistics()
        {
            ThrowIfDisposed();

            return _healthCheckStats.AsValueEnumerable()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToIndividualStatistics());
        }

        /// <summary>
        /// Gets circuit breaker statistics.
        /// </summary>
        /// <returns>Dictionary of circuit breaker statistics</returns>
        public IReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics> GetCircuitBreakerStatistics()
        {
            ThrowIfDisposed();

            return _circuitBreakerStats.AsValueEnumerable()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToCircuitBreakerStatistics());
        }

        /// <summary>
        /// Gets performance metrics over a specified time period.
        /// </summary>
        /// <param name="period">Time period to analyze</param>
        /// <returns>Performance metrics for the period</returns>
        public PerformanceMetrics GetPerformanceMetrics(TimeSpan period)
        {
            ThrowIfDisposed();

            var endTime = DateTime.UtcNow;
            var startTime = endTime - period;

            var relevantRecords = _executionHistory.AsValueEnumerable()
                .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                .ToList();

            if (!relevantRecords.Any())
            {
                return new PerformanceMetrics
                {
                    Period = period,
                    PeriodStart = startTime,
                    PeriodEnd = endTime
                };
            }

            var totalExecutions = relevantRecords.Count;
            var successfulExecutions = relevantRecords.Count(r => r.Status == HealthStatus.Healthy);
            var executionsPerMinute = totalExecutions / period.TotalMinutes;
            var overallSuccessRate = (double)successfulExecutions / totalExecutions;

            var executionTimes = relevantRecords.AsValueEnumerable().Select(r => r.Duration).OrderBy(d => d.Ticks).ToList();
            var averageExecutionTime = new TimeSpan((long)executionTimes.AsValueEnumerable().Select(t => t.Ticks).Average());
            var p95Index = (int)(executionTimes.Count * 0.95);
            var p99Index = (int)(executionTimes.Count * 0.99);

            return new PerformanceMetrics
            {
                Period = period,
                PeriodStart = startTime,
                PeriodEnd = endTime,
                TotalExecutions = totalExecutions,
                ExecutionsPerMinute = executionsPerMinute,
                OverallSuccessRate = overallSuccessRate,
                AverageExecutionTime = averageExecutionTime,
                P95ExecutionTime = executionTimes.Count > p95Index ? executionTimes[p95Index] : TimeSpan.Zero,
                P99ExecutionTime = executionTimes.Count > p99Index ? executionTimes[p99Index] : TimeSpan.Zero,
                PeakExecutionTime = executionTimes.LastOrDefault()
            };
        }

        /// <summary>
        /// Gets trend analysis for health check performance.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to analyze</param>
        /// <param name="period">Time period for analysis</param>
        /// <returns>Trend analysis results</returns>
        public HealthCheckTrendAnalysis GetTrendAnalysis(FixedString64Bytes healthCheckName, TimeSpan period)
        {
            ThrowIfDisposed();

            var endTime = DateTime.UtcNow;
            var startTime = endTime - period;

            var relevantRecords = _executionHistory.AsValueEnumerable()
                .Where(r => r.HealthCheckName == healthCheckName && r.Timestamp >= startTime)
                .OrderBy(r => r.Timestamp)
                .ToList();

            if (relevantRecords.Count < 10) // Insufficient data
            {
                return new HealthCheckTrendAnalysis
                {
                    HealthCheckName = healthCheckName,
                    AnalysisPeriod = period,
                    ExecutionTimeTrend = TrendDirection.InsufficientData,
                    SuccessRateTrend = TrendDirection.InsufficientData,
                    ConfidenceLevel = 0.0
                };
            }

            // Simple trend analysis using linear regression concepts
            var executionTimeTrend = CalculateExecutionTimeTrend(relevantRecords);
            var successRateTrend = CalculateSuccessRateTrend(relevantRecords);

            return new HealthCheckTrendAnalysis
            {
                HealthCheckName = healthCheckName,
                AnalysisPeriod = period,
                IsImproving = successRateTrend == TrendDirection.Improving || successRateTrend == TrendDirection.StronglyImproving,
                IsDegrading = successRateTrend == TrendDirection.Degrading || successRateTrend == TrendDirection.StronglyDegrading,
                ConfidenceLevel = Math.Min(relevantRecords.Count / 100.0, 1.0), // Simple confidence based on sample size
                ExecutionTimeTrend = executionTimeTrend,
                SuccessRateTrend = successRateTrend
            };
        }

        /// <summary>
        /// Gets system-wide trend analysis.
        /// </summary>
        /// <param name="period">Time period for analysis</param>
        /// <returns>System trend analysis</returns>
        public SystemTrendAnalysis GetSystemTrendAnalysis(TimeSpan period)
        {
            ThrowIfDisposed();

            var healthCheckTrends = _healthCheckStats.Keys.AsValueEnumerable()
                .Select(name => GetTrendAnalysis(name, period))
                .ToList();

            var concerningHealthChecks = healthCheckTrends.AsValueEnumerable()
                .Where(t => t.IsDegrading)
                .Select(t => t.HealthCheckName)
                .ToList();

            var improvingHealthChecks = healthCheckTrends.AsValueEnumerable()
                .Where(t => t.IsImproving)
                .Select(t => t.HealthCheckName)
                .ToList();

            // Determine overall trends based on individual health check trends
            var overallHealthTrend = DetermineOverallTrend(healthCheckTrends.AsValueEnumerable().Select(t => t.SuccessRateTrend).ToList());
            var performanceTrend = DetermineOverallTrend(healthCheckTrends.AsValueEnumerable().Select(t => t.ExecutionTimeTrend).ToList());

            return new SystemTrendAnalysis
            {
                AnalysisPeriod = period,
                OverallHealthTrend = overallHealthTrend,
                PerformanceTrend = performanceTrend,
                AvailabilityTrend = overallHealthTrend, // Simplified - same as health trend
                HealthCheckTrends = healthCheckTrends,
                ConcerningHealthChecks = concerningHealthChecks,
                ImprovingHealthChecks = improvingHealthChecks
            };
        }

        /// <summary>
        /// Resets all statistics to initial values.
        /// </summary>
        /// <param name="reason">Reason for the reset</param>
        public void ResetStatistics(string reason = null)
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                _totalHealthCheckExecutions = 0;
                _totalSuccessfulExecutions = 0;
                _totalFailedExecutions = 0;
                _totalTimedOutExecutions = 0;
                _lastStatsReset = DateTime.UtcNow;

                _healthCheckStats.Clear();
                _circuitBreakerStats.Clear();

                // Clear history
                while (_executionHistory.TryDequeue(out _)) { }
                while (_systemHealthHistory.TryDequeue(out _)) { }
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("StatisticsReset", _collectorId.ToString());
            _logger.LogInfo($"All statistics reset: {reason ?? "No reason provided"}", correlationId);

            var eventArgs = new StatisticsResetEventArgs
            {
                Reason = reason,
                CorrelationId = correlationId,
                IsFullReset = true
            };

            StatisticsReset?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Resets statistics for a specific health check.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to reset</param>
        /// <param name="reason">Reason for the reset</param>
        /// <returns>True if statistics were found and reset</returns>
        public bool ResetHealthCheckStatistics(FixedString64Bytes healthCheckName, string reason = null)
        {
            ThrowIfDisposed();

            var removed = _healthCheckStats.TryRemove(healthCheckName, out _);
            
            if (removed)
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckStatsReset", _collectorId.ToString());
                _logger.LogInfo($"Statistics reset for health check '{healthCheckName}': {reason ?? "No reason provided"}", correlationId);

                var eventArgs = new StatisticsResetEventArgs
                {
                    Reason = reason,
                    CorrelationId = correlationId,
                    IsFullReset = false,
                    HealthCheckName = healthCheckName
                };

                StatisticsReset?.Invoke(this, eventArgs);
            }

            return removed;
        }

        /// <summary>
        /// Sets performance thresholds for monitoring.
        /// </summary>
        /// <param name="slowExecutionThreshold">Threshold for slow execution alerts</param>
        /// <param name="highFailureRateThreshold">Threshold for high failure rate alerts</param>
        public void SetPerformanceThresholds(TimeSpan slowExecutionThreshold, double highFailureRateThreshold)
        {
            ThrowIfDisposed();

            _slowExecutionThreshold = slowExecutionThreshold;
            _highFailureRateThreshold = highFailureRateThreshold;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ThresholdUpdate", _collectorId.ToString());
            _logger.LogInfo($"Performance thresholds updated: execution={slowExecutionThreshold.TotalMilliseconds}ms, failure={highFailureRateThreshold:P1}", correlationId);
        }

        /// <summary>
        /// Enables or disables automatic cleanup of old statistics.
        /// </summary>
        /// <param name="enabled">Whether to enable automatic cleanup</param>
        /// <param name="retentionPeriod">How long to retain statistics</param>
        public void SetAutomaticCleanup(bool enabled, TimeSpan retentionPeriod)
        {
            ThrowIfDisposed();

            _automaticCleanupEnabled = enabled;
            _retentionPeriod = retentionPeriod;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CleanupConfigUpdate", _collectorId.ToString());
            _logger.LogInfo($"Automatic cleanup {(enabled ? "enabled" : "disabled")}, retention: {retentionPeriod}", correlationId);
        }

        /// <summary>
        /// Manually triggers cleanup of old statistics.
        /// </summary>
        /// <param name="cutoffTime">Remove statistics older than this time</param>
        /// <returns>Number of records cleaned up</returns>
        public int CleanupOldStatistics(DateTime cutoffTime)
        {
            ThrowIfDisposed();

            var cleanedUpCount = 0;

            // Cleanup execution history
            var tempQueue = new ConcurrentQueue<HealthCheckExecutionRecord>();
            while (_executionHistory.TryDequeue(out var record))
            {
                if (record.Timestamp >= cutoffTime)
                {
                    tempQueue.Enqueue(record);
                }
                else
                {
                    cleanedUpCount++;
                }
            }

            // Re-enqueue the records we want to keep
            while (tempQueue.TryDequeue(out var record))
            {
                _executionHistory.Enqueue(record);
            }

            // Cleanup system health history
            var tempSystemQueue = new ConcurrentQueue<SystemHealthSnapshot>();
            while (_systemHealthHistory.TryDequeue(out var snapshot))
            {
                if (snapshot.Timestamp >= cutoffTime)
                {
                    tempSystemQueue.Enqueue(snapshot);
                }
                else
                {
                    cleanedUpCount++;
                }
            }

            // Re-enqueue the snapshots we want to keep
            while (tempSystemQueue.TryDequeue(out var snapshot))
            {
                _systemHealthHistory.Enqueue(snapshot);
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("StatisticsCleanup", _collectorId.ToString());
            _logger.LogDebug($"Cleaned up {cleanedUpCount} old statistics records", correlationId);

            return cleanedUpCount;
        }

        private void CheckPerformanceThresholds(HealthCheckResult result, HealthCheckStats stats)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ThresholdCheck", _collectorId.ToString());

            // Check slow execution threshold
            if (result.Duration > _slowExecutionThreshold)
            {
                var eventArgs = new PerformanceThresholdEventArgs
                {
                    ThresholdType = PerformanceThresholdType.SlowExecution,
                    HealthCheckName = result.Name,
                    ActualValue = result.Duration.TotalMilliseconds,
                    ThresholdValue = _slowExecutionThreshold.TotalMilliseconds,
                    CorrelationId = correlationId,
                    Context = $"Health check '{result.Name}' took {result.Duration.TotalMilliseconds:F1}ms"
                };

                PerformanceThresholdExceeded?.Invoke(this, eventArgs);
            }

            // Check failure rate threshold
            if (stats.TotalExecutions > 10 && stats.FailureRate > _highFailureRateThreshold)
            {
                var eventArgs = new PerformanceThresholdEventArgs
                {
                    ThresholdType = PerformanceThresholdType.HighFailureRate,
                    HealthCheckName = result.Name,
                    ActualValue = stats.FailureRate,
                    ThresholdValue = _highFailureRateThreshold,
                    CorrelationId = correlationId,
                    Context = $"Health check '{result.Name}' has {stats.FailureRate:P1} failure rate"
                };

                PerformanceThresholdExceeded?.Invoke(this, eventArgs);
            }
        }

        private void CleanupOldExecutionHistory()
        {
            if (_executionHistory.Count <= _config.MaxHistorySize)
                return;

            var cutoffTime = DateTime.UtcNow - _retentionPeriod;
            CleanupOldStatistics(cutoffTime);
        }

        private void CleanupOldSystemHistory()
        {
            if (_systemHealthHistory.Count <= _config.MaxHistorySize)
                return;

            var cutoffTime = DateTime.UtcNow - _retentionPeriod;

            var tempQueue = new ConcurrentQueue<SystemHealthSnapshot>();
            while (_systemHealthHistory.TryDequeue(out var snapshot))
            {
                if (snapshot.Timestamp >= cutoffTime)
                {
                    tempQueue.Enqueue(snapshot);
                }
            }

            while (tempQueue.TryDequeue(out var snapshot))
            {
                _systemHealthHistory.Enqueue(snapshot);
            }
        }

        private TimeSpan CalculateAverageExecutionTime()
        {
            var recentRecords = _executionHistory.AsValueEnumerable()
                .Where(r => r.Timestamp > DateTime.UtcNow.AddMinutes(-10))
                .ToList();

            if (!recentRecords.Any())
                return TimeSpan.Zero;

            var totalTicks = recentRecords.AsValueEnumerable().Select(r => r.Duration.Ticks).Sum();
            return new TimeSpan(totalTicks / recentRecords.Count);
        }

        private DegradationLevel GetCurrentDegradationLevel()
        {
            var latestSnapshot = _systemHealthHistory.AsValueEnumerable().LastOrDefault();
            return latestSnapshot?.DegradationLevel ?? DegradationLevel.None;
        }

        private HealthStatus GetLastOverallStatus()
        {
            var latestSnapshot = _systemHealthHistory.AsValueEnumerable().LastOrDefault();
            return latestSnapshot?.OverallStatus ?? HealthStatus.Unknown;
        }

        private static TrendDirection CalculateExecutionTimeTrend(List<HealthCheckExecutionRecord> records)
        {
            if (records.Count < 5)
                return TrendDirection.InsufficientData;

            var midPoint = records.Count / 2;
            var firstHalf = records.Take(midPoint).AsValueEnumerable().Select(r => r.Duration.TotalMilliseconds).Average();
            var secondHalf = records.Skip(midPoint).AsValueEnumerable().Select(r => r.Duration.TotalMilliseconds).Average();

            var changePercent = (secondHalf - firstHalf) / firstHalf;

            return changePercent switch
            {
                > 0.20 => TrendDirection.StronglyDegrading,
                > 0.10 => TrendDirection.Degrading,
                < -0.20 => TrendDirection.StronglyImproving,
                < -0.10 => TrendDirection.Improving,
                _ => TrendDirection.Stable
            };
        }

        private static TrendDirection CalculateSuccessRateTrend(List<HealthCheckExecutionRecord> records)
        {
            if (records.Count < 10)
                return TrendDirection.InsufficientData;

            var midPoint = records.Count / 2;
            var firstHalfSuccessRate = records.Take(midPoint).AsValueEnumerable().Count(r => r.Status == HealthStatus.Healthy) / (double)midPoint;
            var secondHalfSuccessRate = records.Skip(midPoint).AsValueEnumerable().Count(r => r.Status == HealthStatus.Healthy) / (double)(records.Count - midPoint);

            var changePercent = (secondHalfSuccessRate - firstHalfSuccessRate) / firstHalfSuccessRate;

            return changePercent switch
            {
                > 0.10 => TrendDirection.StronglyImproving,
                > 0.05 => TrendDirection.Improving,
                < -0.10 => TrendDirection.StronglyDegrading,
                < -0.05 => TrendDirection.Degrading,
                _ => TrendDirection.Stable
            };
        }

        private static TrendDirection DetermineOverallTrend(IEnumerable<TrendDirection> individualTrends)
        {
            var trends = individualTrends.ToList();
            if (!trends.Any())
                return TrendDirection.InsufficientData;

            var improving = trends.Count(t => t == TrendDirection.Improving || t == TrendDirection.StronglyImproving);
            var degrading = trends.Count(t => t == TrendDirection.Degrading || t == TrendDirection.StronglyDegrading);

            if (improving > degrading * 2)
                return TrendDirection.Improving;
            if (degrading > improving * 2)
                return TrendDirection.Degrading;

            return TrendDirection.Stable;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthStatisticsCollector));
        }

        /// <summary>
        /// Disposes the health statistics collector.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("StatisticsCollectorDispose", _collectorId.ToString());
            _logger.LogInfo("HealthStatisticsCollector disposed", correlationId);
        }

        #region Internal Data Structures

        private sealed record HealthCheckExecutionRecord
        {
            public DateTime Timestamp { get; init; }
            public FixedString64Bytes HealthCheckName { get; init; }
            public HealthStatus Status { get; init; }
            public TimeSpan Duration { get; init; }
            public HealthCheckCategory Category { get; init; }
        }

        private sealed record SystemHealthSnapshot
        {
            public DateTime Timestamp { get; init; }
            public HealthStatus OverallStatus { get; init; }
            public int TotalChecks { get; init; }
            public int HealthyCount { get; init; }
            public int DegradedCount { get; init; }
            public int UnhealthyCount { get; init; }
            public TimeSpan AverageExecutionTime { get; init; }
            public DegradationLevel DegradationLevel { get; init; }
        }

        private sealed class HealthCheckStats
        {
            private readonly object _lock = new();

            public FixedString64Bytes Name { get; }
            public HealthCheckCategory Category { get; }
            public long TotalExecutions { get; private set; }
            public long SuccessfulExecutions { get; private set; }
            public long FailedExecutions { get; private set; }
            public TimeSpan TotalExecutionTime { get; private set; }
            public TimeSpan MinExecutionTime { get; private set; } = TimeSpan.MaxValue;
            public TimeSpan MaxExecutionTime { get; private set; }
            public TimeSpan LastExecutionTime { get; private set; }
            public HealthStatus CurrentStatus { get; private set; }
            public DateTime LastExecution { get; private set; }
            public DateTime LastFailure { get; private set; }
            public string LastFailureMessage { get; private set; }
            public DateTime FirstExecution { get; private set; }

            public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0.0;
            public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions : 0.0;

            public HealthCheckStats(FixedString64Bytes name, HealthCheckCategory category)
            {
                Name = name;
                Category = category;
                FirstExecution = DateTime.UtcNow;
            }

            public HealthCheckStats RecordExecution(HealthCheckResult result)
            {
                lock (_lock)
                {
                    TotalExecutions++;
                    LastExecution = result.Timestamp;
                    LastExecutionTime = result.Duration;
                    CurrentStatus = result.Status;
                    TotalExecutionTime = TotalExecutionTime.Add(result.Duration);

                    if (result.Duration < MinExecutionTime)
                        MinExecutionTime = result.Duration;

                    if (result.Duration > MaxExecutionTime)
                        MaxExecutionTime = result.Duration;

                    if (result.Status == HealthStatus.Healthy)
                    {
                        SuccessfulExecutions++;
                    }
                    else
                    {
                        FailedExecutions++;
                        LastFailure = result.Timestamp;
                        LastFailureMessage = result.Message;
                    }

                    return this;
                }
            }

            public IndividualHealthCheckStatistics ToIndividualStatistics()
            {
                lock (_lock)
                {
                    var avgExecutionTime = TotalExecutions > 0 
                        ? new TimeSpan(TotalExecutionTime.Ticks / TotalExecutions)
                        : TimeSpan.Zero;

                    return new IndividualHealthCheckStatistics
                    {
                        Name = Name,
                        Category = Category,
                        TotalExecutions = TotalExecutions,
                        SuccessfulExecutions = SuccessfulExecutions,
                        FailedExecutions = FailedExecutions,
                        AverageExecutionTime = avgExecutionTime,
                        MinimumExecutionTime = MinExecutionTime == TimeSpan.MaxValue ? TimeSpan.Zero : MinExecutionTime,
                        MaximumExecutionTime = MaxExecutionTime,
                        LastExecutionTime = LastExecutionTime,
                        CurrentStatus = CurrentStatus,
                        LastExecution = LastExecution,
                        LastFailure = LastFailure,
                        LastFailureMessage = LastFailureMessage,
                        FirstExecution = FirstExecution,
                        IsEnabled = true, // Simplified - assume enabled if we have stats
                        HasCircuitBreaker = false // Would need to be passed in or determined elsewhere
                    };
                }
            }
        }

        private sealed class CircuitBreakerStats
        {
            private readonly object _lock = new();

            public FixedString64Bytes Name { get; }
            public CircuitBreakerState State { get; private set; }
            public DateTime LastStateChange { get; private set; }
            public int StateTransitions { get; private set; }

            public CircuitBreakerStats(FixedString64Bytes name, CircuitBreakerState initialState)
            {
                Name = name;
                State = initialState;
                LastStateChange = DateTime.UtcNow;
            }

            public CircuitBreakerStats RecordStateChange(CircuitBreakerState newState, string reason)
            {
                lock (_lock)
                {
                    if (State != newState)
                    {
                        State = newState;
                        LastStateChange = DateTime.UtcNow;
                        StateTransitions++;
                    }
                    return this;
                }
            }

            public CircuitBreakerStatistics ToCircuitBreakerStatistics()
            {
                lock (_lock)
                {
                    return new CircuitBreakerStatistics
                    {
                        Name = Name,
                        State = State,
                        LastStateChange = LastStateChange
                    };
                }
            }
        }

        #endregion
    }
}