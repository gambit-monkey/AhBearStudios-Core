using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
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

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckService");
            _serviceCancellationSource = new CancellationTokenSource();
            _overallStatus = OverallHealthStatus.Unknown;

            _logger.LogInfo("HealthCheckService initialized in orchestration mode with ID: {ServiceId}", _serviceId);
        }

        #region IHealthCheckService Implementation (Delegated to Specialized Services)

        /// <inheritdoc />
        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            ThrowIfDisposed();
            _registryService.RegisterHealthCheck(healthCheck, config);

            _logger.LogDebug("Delegated health check registration to registry service: {HealthCheckName}",
                healthCheck?.Name ?? "Unknown");
        }

        /// <inheritdoc />
        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            ThrowIfDisposed();
            _registryService.RegisterHealthChecks(healthChecks);

            _logger.LogInfo("Delegated bulk health check registration to registry service: {Count} checks",
                healthChecks?.Count ?? 0);
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

            var healthCheck = _registryService.GetHealthCheck(name);
            if (healthCheck == null)
            {
                throw new InvalidOperationException($"Health check '{name}' is not registered");
            }

            var configuration = _registryService.GetHealthCheckConfiguration(name) ??
                               HealthCheckConfiguration.Create(name.ToString());

            // Check circuit breaker before execution
            if (!_resilienceService.CanExecuteOperation(name))
            {
                var result = HealthCheckResult.Unhealthy(
                    $"Health check '{name}' circuit breaker is open");

                // Publish directly via IMessageBusService - no wrapper
                var message = HealthCheckCompletedWithResultsMessage.Create(
                    name.ToString(), "HealthCheck", result.Status, result.Message,
                    result.Duration.TotalMilliseconds, true, false,
                    "HealthCheckService", DeterministicIdGenerator.GenerateCorrelationId("HealthCheck", name.ToString()));
                await _messageBus.PublishMessageAsync(message, cancellationToken);

                return result;
            }

            try
            {
                var result = await _operationService.ExecuteHealthCheckAsync(healthCheck, configuration, cancellationToken);

                // Record circuit breaker result
                if (result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Warning)
                {
                    _resilienceService.RecordSuccess(name, result.Duration);
                }
                else
                {
                    _resilienceService.RecordFailure(name, result.Exception, result.Duration);
                }

                // Use event service for complex lifecycle coordination
                await _eventService.PublishHealthCheckLifecycleEventsAsync(
                    name.ToString(), result, HealthStatus.Unknown,
                    DeterministicIdGenerator.GenerateCorrelationId("HealthCheck", name.ToString()), cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _resilienceService.RecordFailure(name, ex, TimeSpan.Zero);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (_profilerService.BeginScope("HealthCheckService.ExecuteAllHealthChecks"))
            {
                var allHealthChecks = _registryService.GetAllHealthChecks();

                // Use ZLinq for zero-allocation operations
                var enabledChecks = allHealthChecks.AsValueEnumerable()
                    .Where(kvp => kvp.Value.Enabled)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (enabledChecks.Count == 0)
                {
                    return new HealthReport
                    {
                        OverallStatus = OverallHealthStatus.Unknown,
                        CheckResults = new Dictionary<string, HealthCheckResult>(),
                        GeneratedAt = DateTime.UtcNow,
                        TotalChecks = 0,
                        HealthyChecks = 0,
                        UnhealthyChecks = 0,
                        DegradedChecks = 0
                    };
                }

                // Execute health checks using consolidated operation service
                var results = await _operationService.ExecuteBatchAsync(
                    enabledChecks, _config.MaxConcurrentHealthChecks, cancellationToken);

                // Update overall status and degradation
                await UpdateOverallHealthStatusAsync(results);
                await _resilienceService.UpdateDegradationFromHealthStatusAsync(results, _overallStatus);

                // Use event service for complex batch coordination
                await _eventService.CoordinateBatchResultsAsync(results, _overallStatus, _serviceId, cancellationToken);

                _logger.LogDebug("Executed {Count} health checks via simplified orchestration", results.Count);

                return new HealthReport
                {
                    OverallStatus = _overallStatus,
                    CheckResults = results,
                    GeneratedAt = DateTime.UtcNow,
                    TotalChecks = results.Count,
                    HealthyChecks = results.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Healthy),
                    UnhealthyChecks = results.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Unhealthy),
                    DegradedChecks = results.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Degraded)
                };
            }
        }

        /// <inheritdoc />
        public HealthReport GetHealthReport()
        {
            ThrowIfDisposed();

            // This would be delegated to a statistics service in a full implementation
            return new HealthReport
            {
                OverallStatus = _overallStatus,
                CheckResults = new Dictionary<string, HealthCheckResult>(),
                GeneratedAt = DateTime.UtcNow,
                TotalChecks = _registryService.GetHealthCheckCount(),
                HealthyChecks = 0,
                UnhealthyChecks = 0,
                DegradedChecks = 0
            };
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
            // This would be delegated to a statistics service in a full implementation
            return new List<HealthCheckResult>();
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
            _logger.LogInfo("Degradation level change requested to {Level}: {Reason}", level, reason);
            _ = _resilienceService.SetDegradationLevelAsync(level, reason);
        }

        /// <inheritdoc />
        public HealthStatistics GetHealthStatistics()
        {
            ThrowIfDisposed();
            // This would be delegated to a statistics service in a full implementation
            return new HealthStatistics
            {
                TotalHealthChecks = _registryService.GetHealthCheckCount(),
                HealthyChecks = 0,
                UnhealthyChecks = 0,
                DegradedChecks = 0,
                OverallStatus = _overallStatus,
                LastUpdated = DateTime.UtcNow
            };
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

        private async UniTask UpdateOverallHealthStatusAsync(Dictionary<string, HealthCheckResult> results)
        {
            var previousStatus = _overallStatus;

            // Use ZLinq for zero-allocation operations
            var hasUnhealthy = results.Values.AsValueEnumerable().Any(r => r.Status == HealthStatus.Unhealthy);
            var hasDegraded = results.Values.AsValueEnumerable().Any(r => r.Status == HealthStatus.Degraded);
            var hasWarning = results.Values.AsValueEnumerable().Any(r => r.Status == HealthStatus.Warning);

            if (hasUnhealthy)
                _overallStatus = OverallHealthStatus.Unhealthy;
            else if (hasDegraded)
                _overallStatus = OverallHealthStatus.Degraded;
            else if (hasWarning)
                _overallStatus = OverallHealthStatus.Warning;
            else if (results.Count > 0)
                _overallStatus = OverallHealthStatus.Healthy;
            else
                _overallStatus = OverallHealthStatus.Unknown;

            // Fire event if status changed
            if (_overallStatus != previousStatus)
            {
                _logger.LogInfo("Overall health status changed from {PreviousStatus} to {NewStatus}",
                    previousStatus, _overallStatus);

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
                    Guid.NewGuid());

                // Send alert if configured
                if (_config.EnableHealthAlerts)
                {
                    await SendHealthStatusAlertAsync(_overallStatus, previousStatus);
                }
            }
        }

        private async UniTask SendHealthStatusAlertAsync(OverallHealthStatus newStatus, OverallHealthStatus previousStatus)
        {
            try
            {
                var severity = newStatus switch
                {
                    OverallHealthStatus.Healthy => AlertSeverity.Info,
                    OverallHealthStatus.Warning => AlertSeverity.Warning,
                    OverallHealthStatus.Degraded => AlertSeverity.Warning,
                    OverallHealthStatus.Unhealthy => AlertSeverity.Critical,
                    _ => AlertSeverity.Info
                };

                var message = $"Overall health status changed from {previousStatus} to {newStatus}";

                await _alertService.SendAlertAsync(
                    title: "Health Status Change",
                    message: message,
                    severity: severity,
                    tags: _config.AlertTags?.ToArray() ?? Array.Empty<FixedString64Bytes>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send health status alert");
            }
        }

        private double CalculateOverallHealthScore(Dictionary<string, HealthCheckResult> results)
        {
            if (results.Count == 0)
                return 0.0;

            var totalScore = 0.0;
            foreach (var result in results.Values)
            {
                var score = result.Status switch
                {
                    HealthStatus.Healthy => 1.0,
                    HealthStatus.Warning => 0.8,
                    HealthStatus.Degraded => 0.5,
                    HealthStatus.Unhealthy => 0.0,
                    _ => 0.0
                };
                totalScore += score;
            }

            return totalScore / results.Count;
        }

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
                _logger.LogInfo("Disposing HealthCheckService: {ServiceId}", _serviceId);

                _isRunning = false;
                _serviceCancellationSource?.Cancel();

                // Dispose consolidated services
                _operationService?.Dispose();
                _eventService?.Dispose();
                _resilienceService?.Dispose();

                _serviceCancellationSource?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HealthCheckService disposal");
            }
            finally
            {
                _isDisposed = true;
                _logger.LogDebug("HealthCheckService disposed: {ServiceId}", _serviceId);
            }
        }

        #endregion
    }
}