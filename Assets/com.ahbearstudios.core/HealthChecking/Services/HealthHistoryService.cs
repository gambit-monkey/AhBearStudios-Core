using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Service responsible for tracking and managing historical health check data
    /// </summary>
    /// <remarks>
    /// Provides comprehensive historical data management including trend analysis, 
    /// data retention policies, and performance-optimized storage with pooling support
    /// </remarks>
    public sealed class HealthHistoryService : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingService _poolingService;
        private readonly HistoryConfig _historyConfig;

        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckHistoryEntry> _healthCheckHistories;
        private readonly ConcurrentDictionary<FixedString64Bytes, HealthTrendData> _trendData;
        private readonly ConcurrentQueue<HistoryRetentionTask> _retentionTasks;
        private readonly ReaderWriterLockSlim _historyLock;

        private Timer _retentionTimer;
        private Timer _compactionTimer;
        private Timer _trendAnalysisTimer;
        private bool _disposed;

        /// <summary>
        /// Occurs when historical data is updated
        /// </summary>
        public event EventHandler<HistoryUpdatedEventArgs> HistoryUpdated;

        /// <summary>
        /// Occurs when trend analysis is completed
        /// </summary>
        public event EventHandler<TrendAnalysisCompletedEventArgs> TrendAnalysisCompleted;

        /// <summary>
        /// Occurs when data retention cleanup is performed
        /// </summary>
        public event EventHandler<DataRetentionEventArgs> DataRetentionPerformed;

        /// <summary>
        /// Initializes the health history service with required dependencies
        /// </summary>
        /// <param name="logger">Logging service for history operations</param>
        /// <param name="alertService">Alert service for history-related notifications</param>
        /// <param name="messageBusService">Message bus for publishing history events</param>
        /// <param name="poolingService">Optional pooling service for performance optimization</param>
        /// <param name="historyConfig">History management configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public HealthHistoryService(
            ILoggingService logger,
            IAlertService alertService,
            IMessageBusService messageBusService,
            IPoolingService poolingService,
            HistoryConfig historyConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _poolingService = poolingService; // Optional
            _historyConfig = historyConfig ?? throw new ArgumentNullException(nameof(historyConfig));

            _healthCheckHistories = new ConcurrentDictionary<FixedString64Bytes, HealthCheckHistoryEntry>();
            _trendData = new ConcurrentDictionary<FixedString64Bytes, HealthTrendData>();
            _retentionTasks = new ConcurrentQueue<HistoryRetentionTask>();
            _historyLock = new ReaderWriterLockSlim();

            ValidateConfigurationOrThrow();
            InitializeTimers();

            _logger.LogInfo("HealthHistoryService initialized with comprehensive historical tracking");
        }

        /// <summary>
        /// Records a health check result in the historical data
        /// </summary>
        /// <param name="result">Health check result to record</param>
        /// <exception cref="ArgumentNullException">Thrown when result is null</exception>
        public void RecordHealthCheckResult(HealthCheckResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            ThrowIfDisposed();

            try
            {
                var checkName = new FixedString64Bytes(result.Name);

                _historyLock.EnterWriteLock();
                try
                {
                    // Get or create history entry
                    var historyEntry = _healthCheckHistories.GetOrAdd(checkName, _ => CreateNewHistoryEntry(checkName));

                    // Add result to history with pooling optimization
                    AddResultToHistory(historyEntry, result);

                    // Update trend data
                    UpdateTrendData(checkName, result);

                    // Schedule retention cleanup if needed
                    ScheduleRetentionCleanup(checkName);
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }

                // Trigger events
                OnHistoryUpdated(checkName, result);

                _logger.LogDebug($"Recorded health check result for {result.Name} in history");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to record health check result for {result.Name}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves health check history for the specified time period
        /// </summary>
        /// <param name="checkName">Name of the health check</param>
        /// <param name="startTime">Start of the time period</param>
        /// <param name="endTime">End of the time period</param>
        /// <returns>Collection of health check results in the specified period</returns>
        public IEnumerable<HealthCheckResult> GetHealthCheckHistory(
            FixedString64Bytes checkName,
            DateTime startTime,
            DateTime endTime)
        {
            ThrowIfDisposed();

            if (startTime > endTime)
                throw new ArgumentException("Start time must be before end time");

            try
            {
                _historyLock.EnterReadLock();
                try
                {
                    if (!_healthCheckHistories.TryGetValue(checkName, out var historyEntry))
                    {
                        return Enumerable.Empty<HealthCheckResult>();
                    }

                    return historyEntry.GetResultsInTimeRange(startTime, endTime).ToList();
                }
                finally
                {
                    _historyLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to retrieve history for {checkName}");
                return Enumerable.Empty<HealthCheckResult>();
            }
        }

        /// <summary>
        /// Retrieves health check history for the specified time period from now
        /// </summary>
        /// <param name="checkName">Name of the health check</param>
        /// <param name="timePeriod">Time period to look back</param>
        /// <returns>Collection of health check results in the specified period</returns>
        public IEnumerable<HealthCheckResult> GetHealthCheckHistory(
            FixedString64Bytes checkName,
            TimeSpan timePeriod)
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime - timePeriod;
            return GetHealthCheckHistory(checkName, startTime, endTime);
        }

        /// <summary>
        /// Gets historical statistics for a health check
        /// </summary>
        /// <param name="checkName">Name of the health check</param>
        /// <param name="timePeriod">Time period for statistics calculation</param>
        /// <returns>Historical statistics</returns>
        public HealthCheckHistoryStatistics GetHealthCheckStatistics(
            FixedString64Bytes checkName,
            TimeSpan timePeriod)
        {
            ThrowIfDisposed();

            try
            {
                var results = GetHealthCheckHistory(checkName, timePeriod).ToList();

                if (!results.Any())
                {
                    return new HealthCheckHistoryStatistics
                    {
                        CheckName = checkName.ToString(),
                        TimePeriod = timePeriod,
                        TotalExecutions = 0
                    };
                }

                return CalculateHistoryStatistics(checkName.ToString(), results, timePeriod);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to calculate statistics for {checkName}");
                return new HealthCheckHistoryStatistics
                {
                    CheckName = checkName.ToString(),
                    TimePeriod = timePeriod,
                    TotalExecutions = 0
                };
            }
        }

        /// <summary>
        /// Gets trend analysis for a health check
        /// </summary>
        /// <param name="checkName">Name of the health check</param>
        /// <returns>Trend analysis data</returns>
        public HealthTrendAnalysis GetTrendAnalysis(FixedString64Bytes checkName)
        {
            ThrowIfDisposed();

            try
            {
                _historyLock.EnterReadLock();
                try
                {
                    if (_trendData.TryGetValue(checkName, out var trendData))
                    {
                        return CalculateTrendAnalysis(checkName, trendData);
                    }

                    return new HealthTrendAnalysis
                    {
                        CheckName = checkName.ToString(),
                        TrendDirection = TrendDirection.Stable,
                        Confidence = 0.0,
                        LastUpdated = DateTime.UtcNow
                    };
                }
                finally
                {
                    _historyLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to get trend analysis for {checkName}");
                return new HealthTrendAnalysis
                {
                    CheckName = checkName.ToString(),
                    TrendDirection = TrendDirection.Unknown,
                    Confidence = 0.0,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets comprehensive history statistics for all health checks
        /// </summary>
        /// <param name="timePeriod">Time period for statistics calculation</param>
        /// <returns>Overall history statistics</returns>
        public OverallHistoryStatistics GetOverallStatistics(TimeSpan timePeriod)
        {
            ThrowIfDisposed();

            try
            {
                _historyLock.EnterReadLock();
                try
                {
                    var statistics = new OverallHistoryStatistics
                    {
                        TimePeriod = timePeriod,
                        GeneratedAt = DateTime.UtcNow,
                        TotalHealthChecks = _healthCheckHistories.Count,
                        CheckStatistics = new Dictionary<string, HealthCheckHistoryStatistics>()
                    };

                    var totalExecutions = 0;
                    var totalUptime = TimeSpan.Zero;
                    var overallHealthyCount = 0;
                    var overallDegradedCount = 0;
                    var overallUnhealthyCount = 0;

                    foreach (var kvp in _healthCheckHistories)
                    {
                        var checkStats = GetHealthCheckStatistics(kvp.Key, timePeriod);
                        statistics.CheckStatistics[kvp.Key.ToString()] = checkStats;

                        totalExecutions += checkStats.TotalExecutions;
                        totalUptime += checkStats.TotalUptime;
                        overallHealthyCount += checkStats.HealthyCount;
                        overallDegradedCount += checkStats.DegradedCount;
                        overallUnhealthyCount += checkStats.UnhealthyCount;
                    }

                    statistics.TotalExecutions = totalExecutions;
                    statistics.AverageUptime = _healthCheckHistories.Count > 0
                        ? TimeSpan.FromTicks(totalUptime.Ticks / _healthCheckHistories.Count)
                        : TimeSpan.Zero;
                    statistics.OverallHealthyPercentage = totalExecutions > 0
                        ? (double)overallHealthyCount / totalExecutions
                        : 0.0;
                    statistics.OverallDegradedPercentage = totalExecutions > 0
                        ? (double)overallDegradedCount / totalExecutions
                        : 0.0;
                    statistics.OverallUnhealthyPercentage = totalExecutions > 0
                        ? (double)overallUnhealthyCount / totalExecutions
                        : 0.0;

                    return statistics;
                }
                finally
                {
                    _historyLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to calculate overall statistics");
                return new OverallHistoryStatistics
                {
                    TimePeriod = timePeriod,
                    GeneratedAt = DateTime.UtcNow,
                    TotalHealthChecks = 0
                };
            }
        }

        /// <summary>
        /// Clears history for a specific health check
        /// </summary>
        /// <param name="checkName">Name of the health check</param>
        /// <returns>True if history was found and cleared</returns>
        public bool ClearHealthCheckHistory(FixedString64Bytes checkName)
        {
            ThrowIfDisposed();

            try
            {
                _historyLock.EnterWriteLock();
                try
                {
                    var removed = _healthCheckHistories.TryRemove(checkName, out var historyEntry);
                    if (removed)
                    {
                        historyEntry?.Dispose();
                        _trendData.TryRemove(checkName, out _);

                        _logger.LogInfo($"Cleared history for health check: {checkName}");
                    }

                    return removed;
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to clear history for {checkName}");
                return false;
            }
        }

        /// <summary>
        /// Clears all historical data
        /// </summary>
        public void ClearAllHistory()
        {
            ThrowIfDisposed();

            try
            {
                _historyLock.EnterWriteLock();
                try
                {
                    foreach (var historyEntry in _healthCheckHistories.Values)
                    {
                        historyEntry?.Dispose();
                    }

                    _healthCheckHistories.Clear();
                    _trendData.Clear();

                    _logger.LogInfo("Cleared all health check history");
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to clear all history");
            }
        }

        /// <summary>
        /// Performs manual data compaction to optimize storage
        /// </summary>
        public void CompactHistoryData()
        {
            ThrowIfDisposed();

            try
            {
                _historyLock.EnterWriteLock();
                try
                {
                    var compactedCount = 0;
                    foreach (var historyEntry in _healthCheckHistories.Values)
                    {
                        if (historyEntry.CompactData())
                        {
                            compactedCount++;
                        }
                    }

                    _logger.LogInfo($"Compacted history data for {compactedCount} health checks");
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to compact history data");
            }
        }

        #region Private Implementation

        private void ValidateConfigurationOrThrow()
        {
            var validationErrors = _historyConfig.Validate();

            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid history configuration: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private void InitializeTimers()
        {
            // Data retention cleanup timer
            if (_historyConfig.RetentionPolicy.Enabled)
            {
                _retentionTimer = new Timer(
                    PerformRetentionCleanup,
                    null,
                    _historyConfig.RetentionPolicy.CleanupInterval,
                    _historyConfig.RetentionPolicy.CleanupInterval);
            }

            // Data compaction timer
            if (_historyConfig.CompactionPolicy.Enabled)
            {
                _compactionTimer = new Timer(
                    PerformDataCompaction,
                    null,
                    _historyConfig.CompactionPolicy.CompactionInterval,
                    _historyConfig.CompactionPolicy.CompactionInterval);
            }

            // Trend analysis timer
            if (_historyConfig.TrendAnalysis.Enabled)
            {
                _trendAnalysisTimer = new Timer(
                    PerformTrendAnalysis,
                    null,
                    _historyConfig.TrendAnalysis.AnalysisInterval,
                    _historyConfig.TrendAnalysis.AnalysisInterval);
            }

            _logger.LogDebug("Initialized history service timers");
        }

        private HealthCheckHistoryEntry CreateNewHistoryEntry(FixedString64Bytes checkName)
        {
            return new HealthCheckHistoryEntry(
                checkName,
                _historyConfig.MaxHistorySize,
                _poolingService,
                _logger);
        }

        private void AddResultToHistory(HealthCheckHistoryEntry historyEntry, HealthCheckResult result)
        {
            historyEntry.AddResult(result);

            // Check if we need to trigger retention cleanup
            if (historyEntry.Count > _historyConfig.MaxHistorySize)
            {
                historyEntry.TrimToSize(_historyConfig.MaxHistorySize);
            }
        }

        private void UpdateTrendData(FixedString64Bytes checkName, HealthCheckResult result)
        {
            var trendData = _trendData.GetOrAdd(checkName, _ => new HealthTrendData(checkName));
            trendData.AddDataPoint(result);
        }

        private void ScheduleRetentionCleanup(FixedString64Bytes checkName)
        {
            if (_historyConfig.RetentionPolicy.Enabled)
            {
                var task = new HistoryRetentionTask
                {
                    CheckName = checkName,
                    ScheduledTime = DateTime.UtcNow + _historyConfig.RetentionPolicy.CleanupDelay
                };

                _retentionTasks.Enqueue(task);
            }
        }

        private void PerformRetentionCleanup(object state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var cleanupTasks = new List<HistoryRetentionTask>();

                // Collect tasks that are due
                while (_retentionTasks.TryDequeue(out var task))
                {
                    if (task.ScheduledTime <= now)
                    {
                        cleanupTasks.Add(task);
                    }
                    else
                    {
                        // Put it back if not due yet
                        _retentionTasks.Enqueue(task);
                        break;
                    }
                }

                // Perform cleanup for due tasks
                var cleanedUpCount = 0;
                foreach (var task in cleanupTasks)
                {
                    if (PerformRetentionCleanupForCheck(task.CheckName))
                    {
                        cleanedUpCount++;
                    }
                }

                if (cleanedUpCount > 0)
                {
                    OnDataRetentionPerformed(cleanedUpCount);
                    _logger.LogDebug($"Performed retention cleanup for {cleanedUpCount} health checks");
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during retention cleanup");
            }
        }

        private bool PerformRetentionCleanupForCheck(FixedString64Bytes checkName)
        {
            try
            {
                _historyLock.EnterWriteLock();
                try
                {
                    if (_healthCheckHistories.TryGetValue(checkName, out var historyEntry))
                    {
                        var cutoffTime = DateTime.UtcNow - _historyConfig.RetentionPolicy.RetentionPeriod;
                        return historyEntry.RemoveResultsOlderThan(cutoffTime);
                    }
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error cleaning up retention for {checkName}");
            }

            return false;
        }

        private void PerformDataCompaction(object state)
        {
            try
            {
                CompactHistoryData();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during data compaction");
            }
        }

        private void PerformTrendAnalysis(object state)
        {
            try
            {
                _historyLock.EnterReadLock();
                try
                {
                    var analysisResults = new List<HealthTrendAnalysis>();

                    foreach (var kvp in _trendData)
                    {
                        var analysis = CalculateTrendAnalysis(kvp.Key, kvp.Value);
                        analysisResults.Add(analysis);
                    }

                    OnTrendAnalysisCompleted(analysisResults);
                }
                finally
                {
                    _historyLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during trend analysis");
            }
        }

        private HealthCheckHistoryStatistics CalculateHistoryStatistics(
            string checkName,
            List<HealthCheckResult> results,
            TimeSpan timePeriod)
        {
            var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
            var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
            var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
            var unknownCount = results.Count(r => r.Status == HealthStatus.Unknown);

            var totalExecutionTime = results.Aggregate(TimeSpan.Zero, (sum, r) => sum + r.Duration);
            var averageExecutionTime = results.Count > 0
                ? TimeSpan.FromTicks(totalExecutionTime.Ticks / results.Count)
                : TimeSpan.Zero;

            var uptime = CalculateUptime(results, timePeriod);

            return new HealthCheckHistoryStatistics
            {
                CheckName = checkName,
                TimePeriod = timePeriod,
                TotalExecutions = results.Count,
                HealthyCount = healthyCount,
                DegradedCount = degradedCount,
                UnhealthyCount = unhealthyCount,
                UnknownCount = unknownCount,
                HealthyPercentage = results.Count > 0 ? (double)healthyCount / results.Count : 0.0,
                DegradedPercentage = results.Count > 0 ? (double)degradedCount / results.Count : 0.0,
                UnhealthyPercentage = results.Count > 0 ? (double)unhealthyCount / results.Count : 0.0,
                UnknownPercentage = results.Count > 0 ? (double)unknownCount / results.Count : 0.0,
                AverageExecutionTime = averageExecutionTime,
                TotalExecutionTime = totalExecutionTime,
                TotalUptime = uptime,
                UptimePercentage = CalculateUptimePercentage(uptime, timePeriod),
                FirstResult = results.OrderBy(r => r.Timestamp).FirstOrDefault(),
                LastResult = results.OrderByDescending(r => r.Timestamp).FirstOrDefault()
            };
        }

        private HealthTrendAnalysis CalculateTrendAnalysis(FixedString64Bytes checkName, HealthTrendData trendData)
        {
            var dataPoints = trendData.GetRecentDataPoints(_historyConfig.TrendAnalysis.AnalysisWindow);

            if (dataPoints.Count < 2)
            {
                return new HealthTrendAnalysis
                {
                    CheckName = checkName.ToString(),
                    TrendDirection = TrendDirection.Stable,
                    Confidence = 0.0,
                    LastUpdated = DateTime.UtcNow
                };
            }

            // Simple trend analysis based on health status changes
            var healthyTrend = CalculateStatusTrend(dataPoints, HealthStatus.Healthy);
            var unhealthyTrend = CalculateStatusTrend(dataPoints, HealthStatus.Unhealthy);

            var trendDirection = DetermineTrendDirection(healthyTrend, unhealthyTrend);
            var confidence = CalculateTrendConfidence(dataPoints, trendDirection);

            return new HealthTrendAnalysis
            {
                CheckName = checkName.ToString(),
                TrendDirection = trendDirection,
                Confidence = confidence,
                HealthyTrend = healthyTrend,
                UnhealthyTrend = unhealthyTrend,
                DataPointCount = dataPoints.Count,
                AnalysisWindow = _historyConfig.TrendAnalysis.AnalysisWindow,
                LastUpdated = DateTime.UtcNow
            };
        }

        private TimeSpan CalculateUptime(List<HealthCheckResult> results, TimeSpan timePeriod)
        {
            // Simplified uptime calculation based on healthy/degraded states
            var healthyDuration = TimeSpan.Zero;
            var orderedResults = results.OrderBy(r => r.Timestamp).ToList();

            for (int i = 0; i < orderedResults.Count - 1; i++)
            {
                var current = orderedResults[i];
                var next = orderedResults[i + 1];

                if (current.Status == HealthStatus.Healthy || current.Status == HealthStatus.Degraded)
                {
                    healthyDuration += next.Timestamp - current.Timestamp;
                }
            }

            return healthyDuration;
        }

        private double CalculateUptimePercentage(TimeSpan uptime, TimeSpan totalPeriod)
        {
            return totalPeriod.TotalMilliseconds > 0
                ? uptime.TotalMilliseconds / totalPeriod.TotalMilliseconds
                : 0.0;
        }

        private double CalculateStatusTrend(List<HealthTrendDataPoint> dataPoints, HealthStatus targetStatus)
        {
            var recentPoints = dataPoints.TakeLast(10).ToList();
            var olderPoints = dataPoints.Count > 10
                ? dataPoints.Take(dataPoints.Count - 10).ToList()
                : new List<HealthTrendDataPoint>();

            if (!olderPoints.Any()) return 0.0;

            var recentPercentage = recentPoints.Count(p => p.Status == targetStatus) / (double)recentPoints.Count;
            var olderPercentage = olderPoints.Count(p => p.Status == targetStatus) / (double)olderPoints.Count;

            return recentPercentage - olderPercentage;
        }

        private TrendDirection DetermineTrendDirection(double healthyTrend, double unhealthyTrend)
        {
            const double trendThreshold = 0.1;

            if (healthyTrend > trendThreshold && unhealthyTrend < -trendThreshold)
                return TrendDirection.Improving;
            if (healthyTrend < -trendThreshold && unhealthyTrend > trendThreshold)
                return TrendDirection.Degrading;
            if (Math.Abs(healthyTrend) < trendThreshold && Math.Abs(unhealthyTrend) < trendThreshold)
                return TrendDirection.Stable;

            return TrendDirection.Unknown;
        }

        private double CalculateTrendConfidence(List<HealthTrendDataPoint> dataPoints, TrendDirection direction)
        {
            // Simple confidence calculation based on data consistency
            if (dataPoints.Count < 5) return 0.2;
            if (dataPoints.Count < 10) return 0.5;
            if (dataPoints.Count < 20) return 0.7;

            return 0.9;
        }

        private void OnHistoryUpdated(FixedString64Bytes checkName, HealthCheckResult result)
        {
            try
            {
                var eventArgs = new HistoryUpdatedEventArgs
                {
                    CheckName = checkName.ToString(),
                    Result = result,
                    Timestamp = DateTime.UtcNow
                };

                HistoryUpdated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking HistoryUpdated event");
            }
        }

        private void OnTrendAnalysisCompleted(List<HealthTrendAnalysis> analyses)
        {
            try
            {
                var eventArgs = new TrendAnalysisCompletedEventArgs
                {
                    Analyses = analyses,
                    Timestamp = DateTime.UtcNow
                };

                TrendAnalysisCompleted?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking TrendAnalysisCompleted event");
            }
        }

        private void OnDataRetentionPerformed(int cleanedUpCount)
        {
            try
            {
                var eventArgs = new DataRetentionEventArgs
                {
                    CleanedUpCount = cleanedUpCount,
                    Timestamp = DateTime.UtcNow
                };

                DataRetentionPerformed?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking DataRetentionPerformed event");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthHistoryService));
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _retentionTimer?.Dispose();
                _retentionTimer = null;

                _compactionTimer?.Dispose();
                _compactionTimer = null;

                _trendAnalysisTimer?.Dispose();
                _trendAnalysisTimer = null;

                _historyLock.EnterWriteLock();
                try
                {
                    foreach (var historyEntry in _healthCheckHistories.Values)
                    {
                        historyEntry?.Dispose();
                    }

                    _healthCheckHistories.Clear();
                    _trendData.Clear();
                }
                finally
                {
                    _historyLock.ExitWriteLock();
                }

                _historyLock?.Dispose();

                _logger.LogInfo("HealthHistoryService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during HealthHistoryService disposal");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}