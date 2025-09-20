using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Implementation of complex health check event coordination.
    /// Uses IMessageBusService, IProfilerService, and IAlertService directly without wrapping.
    /// Only implements complex coordination logic that requires multiple service orchestration.
    /// </summary>
    public sealed class HealthCheckEventService : IHealthCheckEventService
    {
        private readonly IMessageBusService _messageBus;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly ILoggingService _logger;
        private readonly Guid _serviceId;

        // Cache for recent events (for correlation analysis)
        private readonly Queue<HealthCheckEvent> _recentEvents = new();
        private readonly int _maxEventCacheSize = 1000;
        private readonly object _eventCacheLock = new();

        /// <summary>
        /// Initializes a new instance of the HealthCheckEventService.
        /// </summary>
        /// <param name="messageBus">Message bus for event publishing (used directly, not wrapped)</param>
        /// <param name="profilerService">Profiler service for metrics (used directly, not wrapped)</param>
        /// <param name="alertService">Alert service for notifications (used directly, not wrapped)</param>
        /// <param name="logger">Logging service</param>
        public HealthCheckEventService(
            IMessageBusService messageBus,
            IProfilerService profilerService,
            IAlertService alertService,
            ILoggingService logger)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckEventService");

            _logger.LogDebug("HealthCheckEventService initialized with ID: {ServiceId}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckEventService", properties: new Dictionary<string, object> { ["ServiceId"] = _serviceId });
        }

        /// <inheritdoc />
        public async UniTask PublishHealthCheckLifecycleEventsAsync(
            string checkName,
            HealthCheckResult result,
            HealthStatus previousStatus,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            // Complex coordination of multiple events and systems
            var tag = ProfilerTag.CreateMethodTag("HealthCheckEvent", "Lifecycle");
            using (_profilerService.BeginScope(tag))
            {
                // Cache event for correlation analysis
                var completedEvent = HealthCheckEvent.Create(
                    "Completed",
                    checkName,
                    result.Status,
                    result.Message,
                    new Dictionary<string, object>
                    {
                        ["Duration"] = result.Duration.TotalMilliseconds,
                        ["CorrelationId"] = correlationId
                    });
                CacheHealthCheckEvent(completedEvent);

                // 1. Publish completion message directly via IMessageBusService
                var completionMessage = HealthCheckCompletedWithResultsMessage.Create(
                    checkName,
                    "HealthCheck",
                    result.Status,
                    result.Message,
                    result.Duration.TotalMilliseconds,
                    result.Status == HealthStatus.Unhealthy,
                    result.Status == HealthStatus.Warning,
                    "HealthCheckService",
                    correlationId);

                await _messageBus.PublishMessageAsync(completionMessage, cancellationToken);

                // 2. Record metrics directly via IProfilerService
                _profilerService.RecordMetric($"healthcheck.{checkName}.duration",
                    result.Duration.TotalMilliseconds, "ms");
                _profilerService.IncrementCounter($"healthcheck.{checkName}.{result.Status}");

                // 3. Handle status changes with coordinated alerts
                if (result.Status != previousStatus)
                {
                    await HandleStatusChangeAsync(checkName, previousStatus, result.Status, correlationId, cancellationToken);
                }

                // 4. Coordinate critical alerts if needed
                if (result.Status == HealthStatus.Critical || result.Status == HealthStatus.Unhealthy)
                {
                    await CoordinateCriticalAlertAsync(checkName, result, correlationId, cancellationToken);
                }

                _logger.LogDebug("Published lifecycle events for health check '{Name}' with status {Status}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckEventService", properties: new Dictionary<string, object> { ["Name"] = checkName, ["Status"] = result.Status });
            }
        }

        /// <inheritdoc />
        public HealthCheckPerformanceReport GeneratePerformanceReport(
            TimeSpan period,
            IEnumerable<string> healthCheckNames = null)
        {
            // Aggregate data from IProfilerService metrics, not collecting new ones
            var allMetrics = _profilerService.GetAllMetrics();
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(period);

            var averageExecutionTimes = new Dictionary<string, double>();
            var executionCounts = new Dictionary<string, long>();
            var failureCounts = new Dictionary<string, long>();
            var successRates = new Dictionary<string, double>();

            // Filter to health check metrics
            var healthCheckMetrics = allMetrics
                .Where(kvp => kvp.Key.StartsWith("healthcheck."))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Process metrics for each health check
            var checkNames = healthCheckNames?.ToList() ??
                healthCheckMetrics.Keys
                    .Where(k => k.Contains(".duration"))
                    .Select(k => k.Replace("healthcheck.", "").Replace(".duration", ""))
                    .Distinct()
                    .ToList();

            foreach (var checkName in checkNames)
            {
                // Get duration metrics
                if (healthCheckMetrics.TryGetValue($"healthcheck.{checkName}.duration", out var durationMetrics))
                {
                    var avgDuration = durationMetrics.AsValueEnumerable()
                        .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                        .Average(m => m.Value);
                    averageExecutionTimes[checkName] = avgDuration;
                }

                // Get counter metrics from profiler statistics
                var stats = _profilerService.GetStatistics();
                if (stats.TryGetValue($"healthcheck.{checkName}.Healthy", out var healthyCount))
                {
                    executionCounts[checkName] = Convert.ToInt64(healthyCount);
                }
                if (stats.TryGetValue($"healthcheck.{checkName}.Unhealthy", out var unhealthyCount))
                {
                    failureCounts[checkName] = Convert.ToInt64(unhealthyCount);
                }

                // Calculate success rate
                var total = executionCounts.GetValueOrDefault(checkName) +
                           failureCounts.GetValueOrDefault(checkName);
                if (total > 0)
                {
                    successRates[checkName] = (double)executionCounts.GetValueOrDefault(checkName) / total;
                }
            }

            // Calculate overall metrics
            double overallAverageTime = 0.0;
            string slowestHealthCheck = string.Empty;
            string mostFailedHealthCheck = string.Empty;

            if (averageExecutionTimes.Any())
            {
                overallAverageTime = averageExecutionTimes.Values.Average();
                slowestHealthCheck = averageExecutionTimes
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault().Key ?? string.Empty;
            }

            if (failureCounts.Any())
            {
                mostFailedHealthCheck = failureCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault().Key ?? string.Empty;
            }

            return HealthCheckPerformanceReport.Create(
                startTime,
                endTime,
                averageExecutionTimes,
                executionCounts,
                failureCounts,
                successRates,
                overallAverageTime,
                slowestHealthCheck,
                mostFailedHealthCheck);
        }

        /// <inheritdoc />
        public async UniTask<CorrelatedHealthEvents> GetCorrelatedEventsAsync(
            Guid correlationId,
            bool includeRelated = true,
            CancellationToken cancellationToken = default)
        {
            var events = new List<HealthCheckEvent>();
            var relatedEvents = new Dictionary<string, List<HealthCheckEvent>>();

            // Get events from cache
            lock (_eventCacheLock)
            {
                events = _recentEvents
                    .Where(e => e.Metadata != null &&
                               e.Metadata.TryGetValue("CorrelationId", out var id) &&
                               (Guid)id == correlationId)
                    .ToList();
            }

            var totalDuration = TimeSpan.Zero;
            var rootCause = string.Empty;

            if (events.Any())
            {
                var firstEvent = events.OrderBy(e => e.Timestamp).First();
                var lastEvent = events.OrderBy(e => e.Timestamp).Last();
                totalDuration = lastEvent.Timestamp - firstEvent.Timestamp;

                // Analyze for root cause
                var failedEvents = events.Where(e => e.Status == HealthStatus.Unhealthy);
                if (failedEvents.Any())
                {
                    rootCause = failedEvents.First().Message;
                }

                // Find related events if requested
                if (includeRelated)
                {
                    var healthCheckNames = events
                        .Select(e => e.HealthCheckName)
                        .Distinct()
                        .ToList();

                    foreach (var checkName in healthCheckNames)
                    {
                        lock (_eventCacheLock)
                        {
                            var related = _recentEvents
                                .Where(e => e.HealthCheckName == checkName &&
                                           e.Timestamp >= firstEvent.Timestamp.AddMinutes(-5) &&
                                           e.Timestamp <= lastEvent.Timestamp.AddMinutes(5))
                                .ToList();

                            if (related.Any())
                            {
                                relatedEvents[checkName] = related;
                            }
                        }
                    }
                }
            }

            return CorrelatedHealthEvents.Create(correlationId, events, relatedEvents, totalDuration, rootCause);
        }

        /// <inheritdoc />
        public async UniTask CoordinateAlertEscalationAsync(
            HealthReport healthReport,
            AlertEscalationRules escalationRules,
            CancellationToken cancellationToken = default)
        {
            // Complex escalation logic that coordinates multiple systems
            var unhealthyChecks = healthReport.Results
                .AsValueEnumerable()
                .Where(result => result.Status == HealthStatus.Unhealthy ||
                               result.Status == HealthStatus.Critical)
                .ToList();

            if (unhealthyChecks.Any())
            {
                // Determine escalation level based on rules
                var severity = unhealthyChecks.Any(c => c.Status == HealthStatus.Critical)
                    ? AlertSeverity.Critical
                    : AlertSeverity.Warning;

                // Check if we need to escalate based on duration
                if (escalationRules.EscalationDelays.TryGetValue(HealthStatus.Unhealthy, out var delay))
                {
                    // This would check historical data to see if the issue persists
                    // For now, we'll escalate immediately
                    severity = AlertSeverity.Emergency;
                }

                // Create coordinated alert with all unhealthy checks
                var message = $"Health Check Escalation: {unhealthyChecks.Count} checks unhealthy. " +
                             $"Affected: {string.Join(", ", unhealthyChecks.Select(c => c.Name))}";

                await _alertService.RaiseAlertAsync(
                    message,
                    severity,
                    "HealthCheckEventService",
                    "HealthCheckEscalation",
                    default(Guid),
                    cancellationToken);

                // Record escalation metrics
                _profilerService.IncrementCounter("healthcheck.escalations");
                _profilerService.RecordMetric("healthcheck.escalation.unhealthy_count", unhealthyChecks.Count);

                _logger.LogWarning("Alert escalation triggered for {Count} unhealthy checks", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckEventService", properties: new Dictionary<string, object> { ["Count"] = unhealthyChecks.Count });
            }
        }

        /// <inheritdoc />
        public Models.HealthCheckTrendAnalysis AnalyzeTrends(string checkName, TimeSpan period)
        {
            var dataPoints = new List<TrendPoint>();

            // Get historical data from cached events
            lock (_eventCacheLock)
            {
                var cutoffTime = DateTime.UtcNow.Subtract(period);
                var events = _recentEvents
                    .Where(e => e.HealthCheckName == checkName && e.Timestamp >= cutoffTime)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                foreach (var evt in events)
                {
                    if (evt.Metadata != null && evt.Metadata.TryGetValue("Duration", out var duration))
                    {
                        var trendPoint = TrendPoint.Create(
                            evt.Timestamp,
                            Convert.ToDouble(duration),
                            evt.Status);
                        dataPoints.Add(trendPoint);
                    }
                }
            }

            // Use the existing model from the Models namespace
            return new Models.HealthCheckTrendAnalysis
            {
                HealthCheckName = checkName,
                AnalysisPeriod = period,
                IsImproving = false, // Would need to calculate based on actual trends
                IsDegrading = false,
                ConfidenceLevel = 0.5,
                DailyChangeRate = 0.0,
                ExecutionTimeTrend = TrendDirection.Stable,
                SuccessRateTrend = TrendDirection.Stable,
                Prediction = new PerformancePrediction() // Would need proper prediction logic
            };
        }

        /// <inheritdoc />
        public CriticalEventSummary GetCriticalEventSummary(int maxEvents = 10, HealthStatus minSeverity = HealthStatus.Warning)
        {
            var criticalEvents = new List<CriticalEvent>();
            var eventCountsByType = new Dictionary<string, int>();

            lock (_eventCacheLock)
            {
                criticalEvents = _recentEvents
                    .Where(e => e.Status >= minSeverity)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(maxEvents)
                    .Select(e => CriticalEvent.Create(
                        e.HealthCheckName,
                        e.Status,
                        e.Message,
                        DetermineImpact(e.Status),
                        e.Timestamp))
                    .ToList();

                // Count events by type
                foreach (var evt in _recentEvents.Where(e => e.Status >= minSeverity))
                {
                    var key = $"{evt.HealthCheckName}:{evt.Status}";
                    eventCountsByType[key] = eventCountsByType.GetValueOrDefault(key) + 1;
                }
            }

            // Determine current overall status based on recent critical events
            var currentStatus = OverallHealthStatus.Healthy;
            if (criticalEvents.Any(e => e.Severity == HealthStatus.Critical))
                currentStatus = OverallHealthStatus.Critical;
            else if (criticalEvents.Any(e => e.Severity == HealthStatus.Unhealthy))
                currentStatus = OverallHealthStatus.Unhealthy;
            else if (criticalEvents.Any(e => e.Severity == HealthStatus.Warning))
                currentStatus = OverallHealthStatus.Warning;

            return CriticalEventSummary.Create(criticalEvents, eventCountsByType, currentStatus);
        }

        /// <inheritdoc />
        public async UniTask CoordinateBatchResultsAsync(
            Dictionary<string, HealthCheckResult> results,
            OverallHealthStatus overallStatus,
            Guid serviceId,
            CancellationToken cancellationToken = default)
        {
            // Complex batch coordination with deduplication and prioritization
            using (_profilerService.BeginScope("HealthCheckEvent.BatchCoordination"))
            {
                // Group results by status for efficient processing
                var groupedResults = results
                    .GroupBy(kvp => kvp.Value.Status)
                    .OrderByDescending(g => GetStatusPriority(g.Key))
                    .ToList();

                // Process critical/unhealthy results first
                foreach (var group in groupedResults)
                {
                    if (group.Key == HealthStatus.Critical || group.Key == HealthStatus.Unhealthy)
                    {
                        // Batch alert for multiple failures
                        if (group.Count() >= 3)
                        {
                            var checkNames = string.Join(", ", group.Select(kvp => kvp.Key));
                            await _alertService.RaiseAlertAsync(
                                $"Multiple Health Check Failures: {group.Count()} health checks are {group.Key}: {checkNames}",
                                AlertSeverity.Critical,
                                "HealthCheckEventService",
                                "MultipleFailures",
                                default(Guid),
                                cancellationToken);
                        }
                    }

                    // Cache events for correlation
                    foreach (var kvp in group)
                    {
                        var batchEvent = HealthCheckEvent.Create(
                            "BatchResult",
                            kvp.Key,
                            kvp.Value.Status,
                            kvp.Value.Message,
                            new Dictionary<string, object>
                            {
                                ["Duration"] = kvp.Value.Duration.TotalMilliseconds,
                                ["ServiceId"] = serviceId
                            });
                        CacheHealthCheckEvent(batchEvent);
                    }
                }

                // Record batch processing metrics
                _profilerService.RecordMetric("healthcheck.batch.size", results.Count);
                _profilerService.RecordMetric("healthcheck.batch.unhealthy",
                    results.Count(r => r.Value.Status == HealthStatus.Unhealthy));

                _logger.LogDebug("Coordinated batch results for {Count} health checks", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckEventService", properties: new Dictionary<string, object> { ["Count"] = results.Count });
            }
        }

        private async UniTask HandleStatusChangeAsync(
            string checkName,
            HealthStatus previousStatus,
            HealthStatus newStatus,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            // Publish status change message directly
            var statusChangeMessage = HealthCheckStatusChangedMessage.Create(
                previousStatus,
                newStatus,
                1.0, // Health score calculation would go here
                HealthCheckCategory.System,
                1.0,
                "HealthCheckService",
                correlationId);

            await _messageBus.PublishMessageAsync(statusChangeMessage, cancellationToken);

            // Record status change metrics
            _profilerService.IncrementCounter($"healthcheck.{checkName}.status_changes");
            _profilerService.RecordMetric($"healthcheck.{checkName}.status", (int)newStatus);

            _logger.LogInfo("Health check '{Name}' status changed from {Previous} to {New}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckEventService", properties: new Dictionary<string, object> { ["Name"] = checkName, ["Previous"] = previousStatus, ["New"] = newStatus });
        }

        private async UniTask CoordinateCriticalAlertAsync(
            string checkName,
            HealthCheckResult result,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            // Coordinate critical alert with enriched context
            var severity = result.Status == HealthStatus.Critical
                ? AlertSeverity.Emergency
                : AlertSeverity.Critical;

            var message = $"Health check '{checkName}' is {result.Status}: {result.Message}";
            if (result.Exception != null)
            {
                message += $" Exception: {result.Exception.Message}";
            }

            await _alertService.RaiseAlertAsync(
                message,
                severity,
                "HealthCheckEventService",
                $"HealthCheck:{checkName}",
                default(Guid),
                cancellationToken);

            // Record critical alert metrics
            _profilerService.IncrementCounter($"healthcheck.{checkName}.critical_alerts");
        }

        private void CacheHealthCheckEvent(HealthCheckEvent evt)
        {
            lock (_eventCacheLock)
            {
                _recentEvents.Enqueue(evt);
                while (_recentEvents.Count > _maxEventCacheSize)
                {
                    _recentEvents.Dequeue();
                }
            }
        }

        private static int GetStatusPriority(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Critical => 5,
                HealthStatus.Unhealthy => 4,
                HealthStatus.Degraded => 3,
                HealthStatus.Warning => 2,
                HealthStatus.Healthy => 1,
                _ => 0
            };
        }

        private static string DetermineImpact(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Critical => "Service unavailable",
                HealthStatus.Unhealthy => "Service degraded",
                HealthStatus.Degraded => "Performance impact",
                HealthStatus.Warning => "Potential issues",
                _ => "No impact"
            };
        }
    }
}