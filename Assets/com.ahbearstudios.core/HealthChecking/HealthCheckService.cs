using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.HealthChecking
{
    /// <summary>
    /// Refactored orchestration-focused health check service following PoolingService pattern.
    /// Delegates complex operations to specialized services for improved maintainability and testability.
    /// Follows CLAUDE.md patterns with Builder → Config → Factory → Service design flow.
    /// </summary>
    public sealed class HealthCheckService : IHealthCheckService, IDisposable
    {
        #region Consolidated Services (Following Simplified Architecture)

        private readonly IHealthCheckOperationService _operationService;
        private readonly IHealthCheckRegistryService _registryService;
        private readonly IHealthCheckEventService _eventService;
        private readonly IHealthCheckResilienceService _resilienceService;

        #endregion

        #region Core Dependencies

        private readonly HealthCheckServiceConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IMessageBusService _messageBus;

        #endregion

        #region State Management

        private readonly Guid _serviceId;
        private readonly CancellationTokenSource _serviceCancellationSource;
        private OverallHealthStatus _overallStatus;
        private bool _isRunning;
        private bool _isDisposed;

        #endregion

        #region Statistics Tracking

        private readonly DateTime _serviceStartTime;
        private long _totalHealthChecks;
        private long _successfulHealthChecks;
        private long _failedHealthChecks;
        private readonly ConcurrentQueue<TimeSpan> _executionTimes;
        private const int MaxExecutionTimesStored = 1000;

        #endregion

        /// <summary>
        /// Initializes a new instance of the simplified HealthCheckService.
        /// Uses consolidated services and core systems directly per CLAUDE.md patterns.
        /// </summary>
        /// <param name="config">Health check service configuration</param>
        /// <param name="operationService">Service for health check operations and scheduling</param>
        /// <param name="registryService">Service for managing health check registration</param>
        /// <param name="eventService">Service for complex event coordination</param>
        /// <param name="resilienceService">Service for circuit breakers and degradation</param>
        /// <param name="logger">Logging service for health check operations</param>
        /// <param name="alertService">Alert service for health notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="messageBus">Message bus for health check events</param>
        public HealthCheckService(
            HealthCheckServiceConfig config,
            IHealthCheckOperationService operationService,
            IHealthCheckRegistryService registryService,
            IHealthCheckEventService eventService,
            IHealthCheckResilienceService resilienceService,
            ILoggingService logger,
            IAlertService alertService,
            IProfilerService profilerService,
            IMessageBusService messageBus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
            _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _resilienceService = resilienceService ?? throw new ArgumentNullException(nameof(resilienceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

            // Validate configuration
            if (_config.MaxConcurrentHealthChecks <= 0)
                throw new ArgumentException("MaxConcurrentHealthChecks must be greater than 0", nameof(config));
            if (_config.AutomaticCheckInterval <= TimeSpan.Zero)
                throw new ArgumentException("AutomaticCheckInterval must be greater than zero", nameof(config));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckService");
            _serviceCancellationSource = new CancellationTokenSource();
            _overallStatus = OverallHealthStatus.Unknown;

            // Initialize statistics tracking
            _serviceStartTime = DateTime.UtcNow;
            _totalHealthChecks = 0;
            _successfulHealthChecks = 0;
            _failedHealthChecks = 0;
            _executionTimes = new ConcurrentQueue<TimeSpan>();

            _logger.LogInfo($"HealthCheckService initialized in orchestration mode with ID: {_serviceId}",
                correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
        }

        #region IHealthCheckService Implementation (Delegated to Specialized Services)

        /// <inheritdoc />
        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            ThrowIfDisposed();

            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            try
            {
                _registryService.RegisterHealthCheck(healthCheck, config);

                _logger.LogDebug($"Successfully registered health check: {healthCheck.Name}",
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);

                _profilerService.RecordMetric("healthcheck.registrations", 1);
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to register health check '{healthCheck.Name}'", ex,
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                throw;
            }
        }

        /// <inheritdoc />
        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            ThrowIfDisposed();

            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));

            if (healthChecks.Count == 0)
            {
                _logger.LogWarning("Attempted to register empty health check collection",
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                return;
            }

            try
            {
                _registryService.RegisterHealthChecks(healthChecks);

                _logger.LogInfo($"Successfully registered {healthChecks.Count} health checks in bulk operation",
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);

                _profilerService.RecordMetric("healthcheck.bulk_registrations", healthChecks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to register {healthChecks.Count} health checks in bulk operation", ex,
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                throw;
            }
        }

        /// <inheritdoc />
        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            ThrowIfDisposed();
            return _registryService.UnregisterHealthCheck(name);
        }

        /// <inheritdoc />
        public async UniTask<HealthCheckResult> ExecuteHealthCheckAsync(
            FixedString64Bytes name,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (_profilerService.BeginScope($"HealthCheckService.ExecuteHealthCheck.{name}"))
            {
                var healthCheck = _registryService.GetHealthCheck(name);
                if (healthCheck == null)
                {
                    throw new InvalidOperationException($"Health check '{name}' is not registered");
                }

                var configuration = _registryService.GetHealthCheckConfiguration(name) ??
                                   HealthCheckConfiguration.Create(name.ToString());

                // Increment total health checks counter
                Interlocked.Increment(ref _totalHealthChecks);

                // Check circuit breaker before execution
                if (!_resilienceService.CanExecuteOperation(name))
                {
                    var result = HealthCheckResult.Unhealthy(
                        name.ToString(),
                        $"Health check '{name}' circuit breaker is open");

                    // Track failed execution due to circuit breaker
                    Interlocked.Increment(ref _failedHealthChecks);
                    TrackExecutionTime(result.Duration);
                    _profilerService.RecordMetric($"healthcheck.{name}.circuit_breaker_blocked", 1);

                    // Publish via IMessageBusService with proper error handling
                    var message = HealthCheckCompletedWithResultsMessage.Create(
                        name.ToString(), "HealthCheck", result.Status, result.Message,
                        result.Duration.TotalMilliseconds, true, false,
                        "HealthCheckService", DeterministicIdGenerator.GenerateCorrelationId("HealthCheck", name.ToString()));

                    try
                    {
                        await _messageBus.PublishMessageAsync(message, cancellationToken);
                    }
                    catch (Exception publishEx)
                    {
                        _logger.LogException("Failed to publish health check circuit breaker message", publishEx,
                            correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                    }

                    return result;
                }

                try
                {
                    var result = await _operationService.ExecuteHealthCheckAsync(healthCheck, configuration, cancellationToken);

                    // Track execution statistics based on result
                    if (result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Warning)
                    {
                        Interlocked.Increment(ref _successfulHealthChecks);
                        _resilienceService.RecordSuccess(name, result.Duration);
                        _profilerService.RecordMetric($"healthcheck.{name}.success", 1);
                    }
                    else
                    {
                        Interlocked.Increment(ref _failedHealthChecks);
                        _resilienceService.RecordFailure(name, result.Exception, result.Duration);
                        _profilerService.RecordMetric($"healthcheck.{name}.failure", 1);
                    }

                    TrackExecutionTime(result.Duration);
                    _profilerService.RecordMetric($"healthcheck.{name}.duration_ms", result.Duration.TotalMilliseconds);

                    // Record result in history for future retrieval
                    _registryService.RecordHealthCheckHistory(result);

                    // Use event service for complex lifecycle coordination
                    await _eventService.PublishHealthCheckLifecycleEventsAsync(
                        name.ToString(), result, HealthStatus.Unknown,
                        DeterministicIdGenerator.GenerateCorrelationId("HealthCheck", name.ToString()), cancellationToken);

                    return result;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _failedHealthChecks);
                    _resilienceService.RecordFailure(name, ex, TimeSpan.Zero);
                    _profilerService.RecordMetric($"healthcheck.{name}.exception", 1);

                    _logger.LogException($"Health check '{name}' execution failed", ex,
                        correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async UniTask<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (_profilerService.BeginScope("HealthCheckService.ExecuteAllHealthChecks"))
            {
                var batchStartTime = DateTime.UtcNow;
                var allHealthChecks = _registryService.GetAllHealthChecks();

                // Use ZLinq for zero-allocation operations
                var enabledChecks = allHealthChecks.AsValueEnumerable()
                    .Where(kvp => kvp.Value.Enabled)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (enabledChecks.Count == 0)
                {
                    return HealthReport.Create(
                        status: HealthStatus.Unknown,
                        results: new List<HealthCheckResult>(),
                        duration: TimeSpan.Zero,
                        correlationId: DeterministicIdGenerator.GenerateCorrelationId("HealthReport", "EmptyReport"),
                        data: new Dictionary<string, object>(),
                        degradationLevel: DegradationLevel.None);
                }

                // Execute health checks using consolidated operation service
                var results = await _operationService.ExecuteBatchAsync(
                    enabledChecks, _config.MaxConcurrentHealthChecks, cancellationToken);

                // Record all results in history
                foreach (var result in results.Values)
                {
                    _registryService.RecordHealthCheckHistory(result);
                }

                // Track batch execution statistics
                TrackBatchExecutionStatistics(results);

                // Update overall status and degradation
                await UpdateOverallHealthStatusAsync(results);
                await _resilienceService.UpdateDegradationFromHealthStatusAsync(results, _overallStatus);

                // Use event service for complex batch coordination
                await _eventService.CoordinateBatchResultsAsync(results, _overallStatus, _serviceId, cancellationToken);

                // Calculate actual batch execution duration
                var batchDuration = DateTime.UtcNow - batchStartTime;

                // Record batch execution metrics
                _profilerService.RecordMetric("healthcheck.batch.total_checks", results.Count);
                _profilerService.RecordMetric("healthcheck.batch.duration_ms", batchDuration.TotalMilliseconds);
                _profilerService.RecordMetric("healthcheck.batch.enabled_checks", enabledChecks.Count);

                _logger.LogDebug($"Executed {results.Count} health checks via simplified orchestration in {batchDuration.TotalMilliseconds:F1}ms",
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);

                return HealthReport.Create(
                    status: ConvertToHealthStatus(_overallStatus),
                    results: results.Values,
                    duration: batchDuration,
                    correlationId: DeterministicIdGenerator.GenerateCorrelationId("HealthReport", "BatchExecution"),
                    data: new Dictionary<string, object> { ["ExecutionCount"] = results.Count },
                    degradationLevel: _resilienceService.GetCurrentDegradationLevel());
            }
        }

        /// <inheritdoc />
        public HealthReport GetHealthReport()
        {
            ThrowIfDisposed();

            // Delegate to registry service for comprehensive statistics
            var statistics = _registryService.GetHealthStatistics();

            return HealthReport.Create(
                status: ConvertToHealthStatus(_overallStatus),
                results: new List<HealthCheckResult>(),
                duration: TimeSpan.Zero,
                correlationId: DeterministicIdGenerator.GenerateCorrelationId("HealthReport", "StatusOnly"),
                data: new Dictionary<string, object>
                {
                    ["RegisteredCount"] = statistics.RegisteredHealthCheckCount,
                    ["TotalExecutions"] = statistics.TotalHealthChecks,
                    ["SuccessRate"] = statistics.SuccessRate,
                    ["AverageExecutionTime"] = statistics.AverageExecutionTime.TotalMilliseconds
                },
                degradationLevel: _resilienceService.GetCurrentDegradationLevel());
        }

        /// <inheritdoc />
        public bool IsHealthCheckRegistered(string name)
        {
            ThrowIfDisposed();
            return _registryService.IsHealthCheckRegistered(new FixedString64Bytes(name ?? string.Empty));
        }

        /// <inheritdoc />
        public OverallHealthStatus GetOverallHealthStatus()
        {
            ThrowIfDisposed();
            return _overallStatus;
        }

        /// <inheritdoc />
        public async UniTask<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return ConvertToHealthStatus(_overallStatus);
        }

        /// <inheritdoc />
        public DegradationLevel GetCurrentDegradationLevel()
        {
            ThrowIfDisposed();
            return _resilienceService.GetCurrentDegradationLevel();
        }

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            ThrowIfDisposed();
            return _resilienceService.GetCircuitBreakerState(operationName);
        }

        /// <inheritdoc />
        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            ThrowIfDisposed();
            return _resilienceService.GetAllCircuitBreakerStates();
        }

        /// <inheritdoc />
        public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
        {
            ThrowIfDisposed();
            return _registryService.GetHealthCheckHistory(name, maxResults);
        }

        /// <inheritdoc />
        public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            ThrowIfDisposed();
            return _registryService.GetRegisteredHealthCheckNames();
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            ThrowIfDisposed();
            return _registryService.GetHealthCheckMetadata(name);
        }

        /// <inheritdoc />
        public void StartAutomaticChecks()
        {
            ThrowIfDisposed();

            if (_isRunning)
            {
                _logger.LogWarning("HealthCheckService is already running");
                return;
            }

            _isRunning = true;

            // Delegate to operation service for scheduling
            _ = _operationService.StartScheduledExecutionAsync(
                _config.AutomaticCheckInterval,
                async (cancellationToken) =>
                {
                    // Execute all health checks when scheduled
                    await ExecuteAllHealthChecksAsync(cancellationToken);
                },
                _serviceCancellationSource.Token);

            _logger.LogInfo("HealthCheckService started with simplified orchestration");
        }

        /// <inheritdoc />
        public void StopAutomaticChecks()
        {
            ThrowIfDisposed();

            if (!_isRunning)
            {
                _logger.LogWarning("HealthCheckService is not running");
                return;
            }

            _isRunning = false;

            // Delegate to operation service for stopping
            _ = _operationService.StopScheduledExecutionAsync(_serviceCancellationSource.Token);

            _logger.LogInfo("HealthCheckService stopped");
        }

        /// <inheritdoc />
        public bool IsAutomaticChecksRunning()
        {
            ThrowIfDisposed();
            return _isRunning && _operationService.IsRunning;
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            ThrowIfDisposed();
            _resilienceService.ForceCircuitBreakerOpen(operationName, reason);
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            ThrowIfDisposed();
            _resilienceService.ForceCircuitBreakerClosed(operationName, reason);
        }

        /// <inheritdoc />
        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            ThrowIfDisposed();
            _logger.LogInfo($"Degradation level change requested to {level}: {reason}", correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
            _ = _resilienceService.SetDegradationLevelAsync(level, reason);
        }

        /// <inheritdoc />
        public HealthStatistics GetHealthStatistics()
        {
            ThrowIfDisposed();

            // Calculate service uptime
            var serviceUptime = DateTime.UtcNow - _serviceStartTime;

            // Get current statistics values (thread-safe reads)
            var totalChecks = Interlocked.Read(ref _totalHealthChecks);
            var successfulChecks = Interlocked.Read(ref _successfulHealthChecks);
            var failedChecks = Interlocked.Read(ref _failedHealthChecks);

            // Calculate average execution time from tracked execution times
            var averageExecutionTime = CalculateAverageExecutionTime();

            // Get circuit breaker statistics from resilience service
            var circuitBreakerStates = _resilienceService.GetAllCircuitBreakerStates();
            var circuitBreakerStats = GetCircuitBreakerStatistics(circuitBreakerStates);

            // Count open and active circuit breakers
            var openCircuitBreakers = circuitBreakerStates.Values.Count(state => state == CircuitBreakerState.Open);
            var activeCircuitBreakers = circuitBreakerStates.Count;

            return HealthStatistics.Create(
                serviceUptime: serviceUptime,
                totalHealthChecks: totalChecks,
                successfulHealthChecks: successfulChecks,
                failedHealthChecks: failedChecks,
                registeredHealthCheckCount: _registryService.GetHealthCheckCount(),
                currentDegradationLevel: _resilienceService.GetCurrentDegradationLevel(),
                lastOverallStatus: ConvertToHealthStatus(_overallStatus),
                circuitBreakerStatistics: circuitBreakerStats,
                averageExecutionTime: averageExecutionTime,
                openCircuitBreakers: openCircuitBreakers,
                activeCircuitBreakers: activeCircuitBreakers);
        }

        /// <inheritdoc />
        public bool IsHealthCheckEnabled(FixedString64Bytes name)
        {
            ThrowIfDisposed();
            return _registryService.IsHealthCheckEnabled(name);
        }

        /// <inheritdoc />
        public void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
        {
            ThrowIfDisposed();
            _registryService.SetHealthCheckEnabled(name, enabled);
        }

        #endregion

        #region Private Helper Methods

        private void TrackExecutionTime(TimeSpan executionTime)
        {
            _executionTimes.Enqueue(executionTime);

            // Maintain a rolling window of execution times
            while (_executionTimes.Count > MaxExecutionTimesStored)
            {
                _executionTimes.TryDequeue(out _);
            }
        }

        private void TrackBatchExecutionStatistics(Dictionary<string, HealthCheckResult> results)
        {
            if (results?.Count == 0)
                return;

            // Use ZLinq for zero-allocation grouping and counting
            var successCount = 0;
            var failureCount = 0;

            foreach (var result in results.Values)
            {
                // Increment total counter for each health check in batch
                Interlocked.Increment(ref _totalHealthChecks);

                // Track success/failure based on result status
                if (IsSuccessfulResult(result.Status))
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }

                // Track execution time for each result
                TrackExecutionTime(result.Duration);
            }

            // Batch update counters for better performance
            Interlocked.Add(ref _successfulHealthChecks, successCount);
            Interlocked.Add(ref _failedHealthChecks, failureCount);

            // Record batch metrics
            _profilerService.RecordMetric("healthcheck.batch.success_count", successCount);
            _profilerService.RecordMetric("healthcheck.batch.failure_count", failureCount);
        }

        private static bool IsSuccessfulResult(HealthStatus status) =>
            status == HealthStatus.Healthy || status == HealthStatus.Warning;

        private TimeSpan CalculateAverageExecutionTime()
        {
            var executionTimes = _executionTimes.ToArray();
            if (executionTimes.Length == 0)
                return TimeSpan.Zero;

            var totalTicks = executionTimes.Sum(t => t.Ticks);
            var averageTicks = totalTicks / executionTimes.Length;
            return new TimeSpan(averageTicks);
        }

        private Dictionary<FixedString64Bytes, CircuitBreakerStatistics> GetCircuitBreakerStatistics(
            Dictionary<FixedString64Bytes, CircuitBreakerState> circuitBreakerStates)
        {
            var statistics = new Dictionary<FixedString64Bytes, CircuitBreakerStatistics>();

            // Get resilience statistics which should contain circuit breaker metrics
            var resilienceStats = _resilienceService.GetResilienceStatistics();

            foreach (var kvp in circuitBreakerStates)
            {
                var name = kvp.Key;
                var state = kvp.Value;

                // Create basic circuit breaker statistics
                // In a full implementation, these would come from the resilience service
                var stats = new CircuitBreakerStatistics
                {
                    Name = name,
                    State = state,
                    TotalExecutions = 0, // Would be tracked by resilience service
                    TotalFailures = 0,   // Would be tracked by resilience service
                    TotalSuccesses = 0,  // Would be tracked by resilience service
                    LastStateChange = DateTime.UtcNow // Would be tracked by resilience service
                };

                statistics[name] = stats;
            }

            return statistics;
        }

        private async UniTask UpdateOverallHealthStatusAsync(Dictionary<string, HealthCheckResult> results)
        {
            using (_profilerService.BeginScope("HealthCheckService.UpdateOverallStatus"))
            {
                var previousStatus = _overallStatus;
                _overallStatus = CalculateOverallHealthStatus(results);

                // Record status change metrics
                _profilerService.RecordMetric("healthcheck.overall_status_updates", 1);
                _profilerService.RecordMetric($"healthcheck.overall_status.{_overallStatus.ToString().ToLowerInvariant()}", 1);

                // Fire event if status changed
                if (_overallStatus != previousStatus)
                {
                    _logger.LogInfo($"Overall health status changed from {previousStatus} to {_overallStatus}",
                        correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);

                    await PublishStatusChangeEventsAsync(previousStatus, results);
                }
            }
        }

        private OverallHealthStatus CalculateOverallHealthStatus(Dictionary<string, HealthCheckResult> results)
        {
            if (results?.Count == 0)
                return OverallHealthStatus.Unknown;

            // Use ZLinq for zero-allocation operations
            var hasUnhealthy = results.Values.AsValueEnumerable().Any(r => r.Status == HealthStatus.Unhealthy);
            var hasDegraded = results.Values.AsValueEnumerable().Any(r => r.Status == HealthStatus.Degraded);
            var hasWarning = results.Values.AsValueEnumerable().Any(r => r.Status == HealthStatus.Warning);

            return hasUnhealthy ? OverallHealthStatus.Unhealthy :
                   hasDegraded ? OverallHealthStatus.Degraded :
                   hasWarning ? OverallHealthStatus.Warning :
                   results.Count > 0 ? OverallHealthStatus.Healthy :
                   OverallHealthStatus.Unknown;
        }

        private async UniTask PublishStatusChangeEventsAsync(OverallHealthStatus previousStatus, Dictionary<string, HealthCheckResult> results)
        {
            try
            {
                // Coordinate complex event publishing using event service
                await _eventService.PublishHealthCheckLifecycleEventsAsync(
                    "OverallHealthStatus",
                    new HealthCheckResult
                    {
                        Status = ConvertToHealthStatus(_overallStatus),
                        Description = $"Overall status changed from {previousStatus} to {_overallStatus}",
                        Data = new Dictionary<string, object> { ["Score"] = CalculateOverallHealthScore(results) },
                        Timestamp = DateTime.UtcNow
                    },
                    ConvertToHealthStatus(previousStatus),
                    DeterministicIdGenerator.GenerateCorrelationId("OverallHealthStatusChange", _serviceId.ToString()));

                // Send alert if configured
                if (_config.EnableHealthAlerts)
                {
                    await SendHealthStatusAlertAsync(_overallStatus, previousStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to publish overall health status change events", ex,
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
            }
        }

        private async UniTask SendHealthStatusAlertAsync(OverallHealthStatus newStatus, OverallHealthStatus previousStatus)
        {
            using (_profilerService.BeginScope("HealthCheckService.SendHealthStatusAlert"))
            {
                try
                {
                    var severity = DetermineAlertSeverity(newStatus);
                    var message = $"Overall health status changed from {previousStatus} to {newStatus}";

                    await _alertService.RaiseAlertAsync(
                        message: $"Health Status Change: {message}",
                        severity: severity,
                        source: nameof(HealthCheckService),
                        tag: "HealthStatusChange",
                        correlationId: DeterministicIdGenerator.GenerateCorrelationId("HealthAlert", "StatusChange"));

                    _profilerService.RecordMetric("healthcheck.alerts_sent", 1);
                    _profilerService.RecordMetric($"healthcheck.alerts.{severity.ToString().ToLowerInvariant()}", 1);

                    _logger.LogDebug($"Health status alert sent: {severity} - {message}",
                        correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                }
                catch (Exception ex)
                {
                    _profilerService.RecordMetric("healthcheck.alert_failures", 1);
                    _logger.LogException("Failed to send health status alert", ex,
                        correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
                }
            }
        }

        private static AlertSeverity DetermineAlertSeverity(OverallHealthStatus status) => status switch
        {
            OverallHealthStatus.Healthy => AlertSeverity.Info,
            OverallHealthStatus.Warning => AlertSeverity.Warning,
            OverallHealthStatus.Degraded => AlertSeverity.Warning,
            OverallHealthStatus.Unhealthy => AlertSeverity.Critical,
            _ => AlertSeverity.Info
        };

        private double CalculateOverallHealthScore(Dictionary<string, HealthCheckResult> results)
        {
            if (results?.Count == 0)
                return 0.0;

            // Use ZLinq for zero-allocation calculation with mapping
            var totalScore = results.Values.AsValueEnumerable()
                .Select(result => GetHealthStatusScore(result.Status))
                .Sum();

            return totalScore / results.Count;
        }

        private static double GetHealthStatusScore(HealthStatus status) => status switch
        {
            HealthStatus.Healthy => 1.0,
            HealthStatus.Warning => 0.8,
            HealthStatus.Degraded => 0.5,
            HealthStatus.Unhealthy => 0.0,
            _ => 0.0
        };

        private static HealthStatus ConvertToHealthStatus(OverallHealthStatus overallStatus)
        {
            return overallStatus switch
            {
                OverallHealthStatus.Healthy => HealthStatus.Healthy,
                OverallHealthStatus.Warning => HealthStatus.Warning,
                OverallHealthStatus.Degraded => HealthStatus.Degraded,
                OverallHealthStatus.Unhealthy => HealthStatus.Unhealthy,
                _ => HealthStatus.Unknown
            };
        }

        private static DegradationLevel ConvertToDegradationLevel(OverallHealthStatus overallStatus)
        {
            return overallStatus switch
            {
                OverallHealthStatus.Healthy => DegradationLevel.None,
                OverallHealthStatus.Warning => DegradationLevel.Minor,
                OverallHealthStatus.Degraded => DegradationLevel.Moderate,
                OverallHealthStatus.Unhealthy => DegradationLevel.Severe,
                _ => DegradationLevel.None
            };
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HealthCheckService));
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                _logger.LogInfo($"Disposing HealthCheckService: {_serviceId}",
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);

                _isRunning = false;
                _serviceCancellationSource?.Cancel();

                // Dispose consolidated services that implement IDisposable
                _operationService?.Dispose();

                _serviceCancellationSource?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException("Error during HealthCheckService disposal", ex, correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
            }
            finally
            {
                _isDisposed = true;
                _logger.LogDebug($"HealthCheckService disposed: {_serviceId}",
                    correlationId: default(FixedString64Bytes), sourceContext: nameof(HealthCheckService), properties: null);
            }
        }

        #endregion
    }
}