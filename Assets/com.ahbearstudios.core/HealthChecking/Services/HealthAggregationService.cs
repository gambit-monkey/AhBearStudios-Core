using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Messaging;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Service responsible for aggregating health check results and calculating overall system health status
    /// </summary>
    /// <remarks>
    /// Implements advanced health aggregation algorithms including weighted calculations, trend analysis,
    /// and configurable threshold-based status determination with support for real-time aggregation
    /// </remarks>
    public sealed class HealthAggregationService : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly HealthThresholds _healthThresholds;
        private readonly DegradationThresholds _degradationThresholds;
        private readonly AggregationConfig _aggregationConfig;

        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckResult> _latestResults;
        private readonly ConcurrentDictionary<HealthCheckCategory, double> _categoryHealthScores;
        private readonly ConcurrentDictionary<FixedString64Bytes, List<HealthCheckResult>> _resultHistory;
        private readonly object _aggregationLock = new object();

        private HealthStatus _lastOverallStatus = HealthStatus.Unknown;
        private DateTime _lastStatusChange = DateTime.UtcNow;
        private Timer _aggregationTimer;
        private bool _disposed;

        /// <summary>
        /// Occurs when the overall health status changes
        /// </summary>
        public event EventHandler<HealthStatusChangedEventArgs> OverallHealthStatusChanged;

        /// <summary>
        /// Occurs when category health scores are updated
        /// </summary>
        public event EventHandler<CategoryHealthUpdatedEventArgs> CategoryHealthUpdated;

        /// <summary>
        /// Occurs when aggregation metrics are calculated
        /// </summary>
        public event EventHandler<AggregationMetricsUpdatedEventArgs> AggregationMetricsUpdated;

        /// <summary>
        /// Initializes the health aggregation service with required dependencies
        /// </summary>
        /// <param name="logger">Logging service for aggregation operations</param>
        /// <param name="alertService">Alert service for status change notifications</param>
        /// <param name="messageBusService">Message bus for publishing aggregation events</param>
        /// <param name="healthThresholds">Health calculation thresholds configuration</param>
        /// <param name="degradationThresholds">Degradation calculation thresholds</param>
        /// <param name="aggregationConfig">Aggregation behavior configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public HealthAggregationService(
            ILoggingService logger,
            IAlertService alertService,
            IMessageBusService messageBusService,
            HealthThresholds healthThresholds,
            DegradationThresholds degradationThresholds,
            AggregationConfig aggregationConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _healthThresholds = healthThresholds ?? throw new ArgumentNullException(nameof(healthThresholds));
            _degradationThresholds =
                degradationThresholds ?? throw new ArgumentNullException(nameof(degradationThresholds));
            _aggregationConfig = aggregationConfig ?? throw new ArgumentNullException(nameof(aggregationConfig));

            _latestResults = new ConcurrentDictionary<FixedString64Bytes, HealthCheckResult>();
            _categoryHealthScores = new ConcurrentDictionary<HealthCheckCategory, double>();
            _resultHistory = new ConcurrentDictionary<FixedString64Bytes, List<HealthCheckResult>>();

            ValidateConfigurationOrThrow();
            InitializeAggregationTimer();

            _logger.LogInfo("HealthAggregationService initialized with aggregation-based health calculation");
        }

        /// <summary>
        /// Updates the aggregation with a new health check result
        /// </summary>
        /// <param name="result">Health check result to include in aggregation</param>
        /// <exception cref="ArgumentNullException">Thrown when result is null</exception>
        public void UpdateHealthCheckResult(HealthCheckResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            ThrowIfDisposed();

            try
            {
                var checkName = new FixedString64Bytes(result.Name);

                // Update latest result
                _latestResults.AddOrUpdate(checkName, result, (_, _) => result);

                // Add to history
                UpdateResultHistory(checkName, result);

                // Recalculate aggregations
                RecalculateAggregatedHealth();

                _logger.LogDebug($"Updated health aggregation with result from {result.Name}: {result.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to update health check result for {result.Name}");
                throw;
            }
        }

        /// <summary>
        /// Removes a health check from aggregation calculations
        /// </summary>
        /// <param name="checkName">Name of the health check to remove</param>
        /// <returns>True if the health check was found and removed</returns>
        public bool RemoveHealthCheck(FixedString64Bytes checkName)
        {
            ThrowIfDisposed();

            try
            {
                var removed = _latestResults.TryRemove(checkName, out var removedResult);
                _resultHistory.TryRemove(checkName, out _);

                if (removed)
                {
                    RecalculateAggregatedHealth();
                    _logger.LogInfo($"Removed health check '{checkName}' from aggregation");
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to remove health check '{checkName}' from aggregation");
                return false;
            }
        }

        /// <summary>
        /// Calculates the current overall health status using configured thresholds and weights
        /// </summary>
        /// <returns>Calculated overall health status</returns>
        public HealthStatus CalculateOverallHealthStatus()
        {
            ThrowIfDisposed();

            lock (_aggregationLock)
            {
                try
                {
                    if (_latestResults.IsEmpty)
                    {
                        _logger.LogWarning("No health check results available for aggregation");
                        return HealthStatus.Unknown;
                    }

                    return _healthThresholds.CalculationMethod switch
                    {
                        HealthCalculationMethod.Simple => CalculateSimpleHealth(),
                        HealthCalculationMethod.WeightedAverage => CalculateWeightedAverageHealth(),
                        HealthCalculationMethod.MajorityVoting => CalculateMajorityVotingHealth(),
                        HealthCalculationMethod.WorstCase => CalculateWorstCaseHealth(),
                        HealthCalculationMethod.BestCase => CalculateBestCaseHealth(),
                        HealthCalculationMethod.TrendBased => CalculateTrendBasedHealth(),
                        HealthCalculationMethod.Custom => CalculateCustomHealth(),
                        _ => CalculateWeightedAverageHealth()
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, "Failed to calculate overall health status");
                    return HealthStatus.Unknown;
                }
            }
        }

        /// <summary>
        /// Gets health scores for each category
        /// </summary>
        /// <returns>Dictionary of category health scores (0.0 to 1.0)</returns>
        public Dictionary<HealthCheckCategory, double> GetCategoryHealthScores()
        {
            ThrowIfDisposed();

            return new Dictionary<HealthCheckCategory, double>(_categoryHealthScores);
        }

        /// <summary>
        /// Gets aggregated health statistics for the specified time window
        /// </summary>
        /// <param name="timeWindow">Time window for statistics calculation</param>
        /// <returns>Comprehensive health statistics</returns>
        public HealthAggregationStatistics GetAggregationStatistics(TimeSpan timeWindow)
        {
            ThrowIfDisposed();

            try
            {
                var cutoffTime = DateTime.UtcNow - timeWindow;
                var windowResults = GetResultsInTimeWindow(cutoffTime);

                return new HealthAggregationStatistics
                {
                    TimeWindow = timeWindow,
                    TotalChecks = windowResults.Count,
                    HealthyCount = windowResults.Count(r => r.Status == HealthStatus.Healthy),
                    DegradedCount = windowResults.Count(r => r.Status == HealthStatus.Degraded),
                    UnhealthyCount = windowResults.Count(r => r.Status == HealthStatus.Unhealthy),
                    UnknownCount = windowResults.Count(r => r.Status == HealthStatus.Unknown),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(windowResults.Any()
                        ? windowResults.Average(r => r.Duration.TotalMilliseconds)
                        : 0),
                    CategoryScores = new Dictionary<HealthCheckCategory, double>(_categoryHealthScores),
                    OverallHealthScore = CalculateOverallHealthScore(),
                    LastStatusChange = _lastStatusChange,
                    CurrentOverallStatus = _lastOverallStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to calculate aggregation statistics");
                return new HealthAggregationStatistics
                {
                    TimeWindow = timeWindow,
                    CurrentOverallStatus = HealthStatus.Unknown
                };
            }
        }

        /// <summary>
        /// Calculates degradation level based on current health aggregation
        /// </summary>
        /// <returns>Recommended degradation level</returns>
        public DegradationLevel CalculateDegradationLevel()
        {
            ThrowIfDisposed();

            try
            {
                var overallScore = CalculateOverallHealthScore();
                var unhealthyPercentage = 1.0 - overallScore;

                if (unhealthyPercentage >= _degradationThresholds.DisabledThreshold)
                    return DegradationLevel.Disabled;
                if (unhealthyPercentage >= _degradationThresholds.SevereThreshold)
                    return DegradationLevel.Severe;
                if (unhealthyPercentage >= _degradationThresholds.ModerateThreshold)
                    return DegradationLevel.Moderate;
                if (unhealthyPercentage >= _degradationThresholds.MinorThreshold)
                    return DegradationLevel.Minor;

                return DegradationLevel.None;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to calculate degradation level");
                return DegradationLevel.None;
            }
        }

        /// <summary>
        /// Forces immediate recalculation of all aggregated health metrics
        /// </summary>
        public void RecalculateAggregatedHealth()
        {
            ThrowIfDisposed();

            lock (_aggregationLock)
            {
                try
                {
                    // Update category health scores
                    UpdateCategoryHealthScores();

                    // Calculate new overall status
                    var newOverallStatus = CalculateOverallHealthStatus();
                    var previousStatus = _lastOverallStatus;

                    if (newOverallStatus != previousStatus)
                    {
                        _lastOverallStatus = newOverallStatus;
                        _lastStatusChange = DateTime.UtcNow;

                        // Trigger status change events
                        OnOverallHealthStatusChanged(previousStatus, newOverallStatus);

                        // Publish message bus event
                        PublishHealthStatusChange(previousStatus, newOverallStatus);

                        // Generate alerts if necessary
                        HandleStatusChangeAlert(previousStatus, newOverallStatus);
                    }

                    // Calculate and publish aggregation metrics
                    var metrics = CalculateAggregationMetrics();
                    OnAggregationMetricsUpdated(metrics);

                    _logger.LogDebug($"Recalculated aggregated health: {newOverallStatus} (was {previousStatus})");
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, "Failed to recalculate aggregated health");
                }
            }
        }

        #region Private Implementation

        private void ValidateConfigurationOrThrow()
        {
            var healthErrors = _healthThresholds.Validate();
            var degradationErrors = _degradationThresholds.Validate();
            var aggregationErrors = _aggregationConfig.Validate();

            var allErrors = healthErrors.Concat(degradationErrors).Concat(aggregationErrors).ToList();

            if (allErrors.Count > 0)
            {
                var errorMessage = $"Invalid aggregation configuration: {string.Join(", ", allErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private void InitializeAggregationTimer()
        {
            if (_aggregationConfig.Enabled && _aggregationConfig.AggregationInterval > TimeSpan.Zero)
            {
                _aggregationTimer = new Timer(
                    PerformScheduledAggregation,
                    null,
                    _aggregationConfig.AggregationInterval,
                    _aggregationConfig.AggregationInterval);

                _logger.LogDebug(
                    $"Initialized aggregation timer with interval: {_aggregationConfig.AggregationInterval}");
            }
        }

        private void PerformScheduledAggregation(object state)
        {
            try
            {
                RecalculateAggregatedHealth();
                CleanupOldHistoryEntries();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during scheduled aggregation");
            }
        }

        private void UpdateResultHistory(FixedString64Bytes checkName, HealthCheckResult result)
        {
            var history = _resultHistory.GetOrAdd(checkName, _ => new List<HealthCheckResult>());

            lock (history)
            {
                history.Add(result);

                // Maintain history size limit
                const int maxHistorySize = 100;
                if (history.Count > maxHistorySize)
                {
                    history.RemoveRange(0, history.Count - maxHistorySize);
                }
            }
        }

        private void UpdateCategoryHealthScores()
        {
            var categorizedResults = _latestResults.Values
                .Where(r => _healthThresholds.CategoryWeights.ContainsKey(r.Category))
                .GroupBy(r => r.Category);

            foreach (var categoryGroup in categorizedResults)
            {
                var results = categoryGroup.ToList();
                var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
                var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
                var totalCount = results.Count;

                var healthScore = totalCount > 0
                    ? (healthyCount + (degradedCount * _healthThresholds.DegradedWeight)) / totalCount
                    : 1.0;

                _categoryHealthScores.AddOrUpdate(categoryGroup.Key, healthScore, (_, _) => healthScore);
            }

            OnCategoryHealthUpdated();
        }

        private HealthStatus CalculateSimpleHealth()
        {
            var results = _latestResults.Values.ToList();
            var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
            var totalCount = results.Count;

            if (totalCount == 0) return HealthStatus.Unknown;

            var healthyPercentage = (double)healthyCount / totalCount;

            return healthyPercentage >= _healthThresholds.HealthyThreshold
                ? HealthStatus.Healthy
                : (healthyPercentage >= (1.0 - _healthThresholds.UnhealthyThreshold)
                    ? HealthStatus.Degraded
                    : HealthStatus.Unhealthy);
        }

        private HealthStatus CalculateWeightedAverageHealth()
        {
            var weightedScore = 0.0;
            var totalWeight = 0.0;

            foreach (var result in _latestResults.Values)
            {
                if (!_healthThresholds.CategoryWeights.TryGetValue(result.Category, out var weight))
                    weight = 0.5; // Default weight for unknown categories

                var resultScore = result.Status switch
                {
                    HealthStatus.Healthy => 1.0,
                    HealthStatus.Degraded => _healthThresholds.DegradedWeight,
                    HealthStatus.Unhealthy => 0.0,
                    HealthStatus.Unknown => 0.5,
                    _ => 0.0
                };

                weightedScore += resultScore * weight;
                totalWeight += weight;
            }

            if (totalWeight == 0) return HealthStatus.Unknown;

            var overallScore = weightedScore / totalWeight;

            return overallScore >= _healthThresholds.HealthyThreshold
                ? HealthStatus.Healthy
                : (overallScore >= (1.0 - _healthThresholds.UnhealthyThreshold)
                    ? HealthStatus.Degraded
                    : HealthStatus.Unhealthy);
        }

        private HealthStatus CalculateMajorityVotingHealth()
        {
            var results = _latestResults.Values.ToList();
            var statusCounts = results.GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var majorityThreshold = results.Count / 2.0;

            if (statusCounts.GetValueOrDefault(HealthStatus.Healthy, 0) > majorityThreshold)
                return HealthStatus.Healthy;
            if (statusCounts.GetValueOrDefault(HealthStatus.Unhealthy, 0) > majorityThreshold)
                return HealthStatus.Unhealthy;

            return HealthStatus.Degraded;
        }

        private HealthStatus CalculateWorstCaseHealth()
        {
            var results = _latestResults.Values.ToList();

            if (results.Any(r => r.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;
            if (results.Any(r => r.Status == HealthStatus.Degraded))
                return HealthStatus.Degraded;
            if (results.Any(r => r.Status == HealthStatus.Unknown))
                return HealthStatus.Unknown;

            return HealthStatus.Healthy;
        }

        private HealthStatus CalculateBestCaseHealth()
        {
            var results = _latestResults.Values.ToList();

            if (results.Any(r => r.Status == HealthStatus.Healthy))
                return HealthStatus.Healthy;
            if (results.Any(r => r.Status == HealthStatus.Degraded))
                return HealthStatus.Degraded;
            if (results.Any(r => r.Status == HealthStatus.Unknown))
                return HealthStatus.Unknown;

            return HealthStatus.Unhealthy;
        }

        private HealthStatus CalculateTrendBasedHealth()
        {
            // Implementation would analyze historical trends
            // For now, fall back to weighted average
            return CalculateWeightedAverageHealth();
        }

        private HealthStatus CalculateCustomHealth()
        {
            // Implementation would use advanced rules from configuration
            // For now, fall back to weighted average
            return CalculateWeightedAverageHealth();
        }

        private double CalculateOverallHealthScore()
        {
            if (_healthThresholds.UseWeightedCalculation)
            {
                var weightedScore = 0.0;
                var totalWeight = 0.0;

                foreach (var result in _latestResults.Values)
                {
                    if (!_healthThresholds.CategoryWeights.TryGetValue(result.Category, out var weight))
                        weight = 0.5;

                    var resultScore = result.Status switch
                    {
                        HealthStatus.Healthy => 1.0,
                        HealthStatus.Degraded => _healthThresholds.DegradedWeight,
                        HealthStatus.Unhealthy => 0.0,
                        HealthStatus.Unknown => 0.5,
                        _ => 0.0
                    };

                    weightedScore += resultScore * weight;
                    totalWeight += weight;
                }

                return totalWeight > 0 ? weightedScore / totalWeight : 0.0;
            }
            else
            {
                var results = _latestResults.Values.ToList();
                var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
                var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);

                return results.Count > 0
                    ? (healthyCount + (degradedCount * _healthThresholds.DegradedWeight)) / results.Count
                    : 0.0;
            }
        }

        private List<HealthCheckResult> GetResultsInTimeWindow(DateTime cutoffTime)
        {
            var windowResults = new List<HealthCheckResult>();

            foreach (var history in _resultHistory.Values)
            {
                lock (history)
                {
                    windowResults.AddRange(history.Where(r => r.Timestamp >= cutoffTime));
                }
            }

            return windowResults;
        }

        private AggregationMetrics CalculateAggregationMetrics()
        {
            return new AggregationMetrics
            {
                Timestamp = DateTime.UtcNow,
                TotalHealthChecks = _latestResults.Count,
                OverallHealthScore = CalculateOverallHealthScore(),
                CategoryScores = new Dictionary<HealthCheckCategory, double>(_categoryHealthScores),
                OverallStatus = _lastOverallStatus,
                LastStatusChange = _lastStatusChange,
                DegradationLevel = CalculateDegradationLevel()
            };
        }

        private void CleanupOldHistoryEntries()
        {
            var cutoffTime = DateTime.UtcNow - _healthThresholds.EvaluationWindow;

            foreach (var history in _resultHistory.Values)
            {
                lock (history)
                {
                    history.RemoveAll(r => r.Timestamp < cutoffTime);
                }
            }
        }

        private void OnOverallHealthStatusChanged(HealthStatus oldStatus, HealthStatus newStatus)
        {
            try
            {
                var eventArgs = new HealthStatusChangedEventArgs
                {
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Timestamp = DateTime.UtcNow,
                    Source = "HealthAggregationService"
                };

                OverallHealthStatusChanged?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking OverallHealthStatusChanged event");
            }
        }

        private void OnCategoryHealthUpdated()
        {
            try
            {
                var eventArgs = new CategoryHealthUpdatedEventArgs
                {
                    CategoryScores = new Dictionary<HealthCheckCategory, double>(_categoryHealthScores),
                    Timestamp = DateTime.UtcNow
                };

                CategoryHealthUpdated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking CategoryHealthUpdated event");
            }
        }

        private void OnAggregationMetricsUpdated(AggregationMetrics metrics)
        {
            try
            {
                var eventArgs = new AggregationMetricsUpdatedEventArgs
                {
                    Metrics = metrics
                };

                AggregationMetricsUpdated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking AggregationMetricsUpdated event");
            }
        }

        private void PublishHealthStatusChange(HealthStatus oldStatus, HealthStatus newStatus)
        {
            try
            {
                var message = new HealthStatusChangeMessage
                {
                    Source = "HealthAggregationService",
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Timestamp = DateTime.UtcNow,
                    CategoryScores = new Dictionary<HealthCheckCategory, double>(_categoryHealthScores)
                };

                var publisher = _messageBusService.GetPublisher<HealthStatusChangeMessage>();
                publisher.PublishMessage(message);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to publish health status change message");
            }
        }

        private void HandleStatusChangeAlert(HealthStatus oldStatus, HealthStatus newStatus)
        {
            var severity = newStatus switch
            {
                HealthStatus.Unhealthy => AlertSeverity.High,
                HealthStatus.Degraded => AlertSeverity.Medium,
                HealthStatus.Unknown => AlertSeverity.Medium,
                HealthStatus.Healthy when oldStatus != HealthStatus.Healthy => AlertSeverity.Low,
                _ => (AlertSeverity?)null
            };

            if (severity.HasValue)
            {
                _alertService.RaiseAlert(
                    new FixedString64Bytes("HealthAggregation.StatusChange"),
                    severity.Value,
                    new FixedString512Bytes($"Overall health status changed: {oldStatus} -> {newStatus}"));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthAggregationService));
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _aggregationTimer?.Dispose();
                _aggregationTimer = null;

                _latestResults.Clear();
                _categoryHealthScores.Clear();
                _resultHistory.Clear();

                _logger.LogInfo("HealthAggregationService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during HealthAggregationService disposal");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}