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
    public sealed class HealthCheckServiceRefactored : IHealthCheckService, IDisposable
    {
        #region Specialized Services (Following PoolingService Pattern)

        private readonly IHealthCheckExecutorService _executorService;
        private readonly IHealthCheckRegistryService _registryService;
        private readonly IHealthCheckMessagePublisherService _messagePublisherService;
        private readonly IHealthCheckCircuitBreakerService _circuitBreakerService;
        private readonly IHealthCheckSchedulerService _schedulerService;

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
        /// Initializes a new instance of the orchestration-focused HealthCheckService.
        /// Delegates to specialized services following the PoolingService architecture pattern.
        /// </summary>
        /// <param name="config">Health check service configuration</param>
        /// <param name="executorService">Service for executing health checks</param>
        /// <param name="registryService">Service for managing health check registration</param>
        /// <param name="messagePublisherService">Service for publishing health check messages</param>
        /// <param name="circuitBreakerService">Service for managing circuit breaker states</param>
        /// <param name="schedulerService">Service for managing automatic scheduling</param>
        /// <param name="logger">Logging service for health check operations</param>
        /// <param name="alertService">Alert service for health notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="messageBus">Message bus for health check events</param>
        public HealthCheckServiceRefactored(
            HealthCheckServiceConfig config,
            IHealthCheckExecutorService executorService,
            IHealthCheckRegistryService registryService,
            IHealthCheckMessagePublisherService messagePublisherService,
            IHealthCheckCircuitBreakerService circuitBreakerService,
            IHealthCheckSchedulerService schedulerService,
            ILoggingService logger,
            IAlertService alertService,
            IProfilerService profilerService,
            IMessageBusService messageBus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _executorService = executorService ?? throw new ArgumentNullException(nameof(executorService));
            _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
            _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));
            _circuitBreakerService = circuitBreakerService ?? throw new ArgumentNullException(nameof(circuitBreakerService));
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
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
            if (!_circuitBreakerService.CanExecuteOperation(name))
            {
                var result = HealthCheckResult.Unhealthy(
                    $"Health check '{name}' circuit breaker is open");

                await _messagePublisherService.PublishHealthCheckCompletedAsync(
                    name.ToString(), result.Status, result.Message, result.Duration, _serviceId, cancellationToken);

                return result;
            }

            try
            {
                var result = await _executorService.ExecuteHealthCheckAsync(healthCheck, configuration, cancellationToken);

                // Record circuit breaker result
                if (result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Warning)
                {
                    _circuitBreakerService.RecordSuccess(name, result.Duration);
                }
                else
                {
                    _circuitBreakerService.RecordFailure(name, result.Exception, result.Duration);
                }

                // Publish completion message
                await _messagePublisherService.PublishHealthCheckCompletedAsync(
                    name.ToString(), result.Status, result.Message, result.Duration, _serviceId, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _circuitBreakerService.RecordFailure(name, ex, TimeSpan.Zero);
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

                // Execute health checks using specialized executor service
                var results = await _executorService.ExecuteHealthChecksAsync(
                    enabledChecks, _config.MaxConcurrentHealthChecks, cancellationToken);

                // Update overall status
                await UpdateOverallHealthStatusAsync(results);

                // Publish batch results
                await _messagePublisherService.PublishBatchHealthCheckResultsAsync(
                    results, _overallStatus, _serviceId, cancellationToken);

                _logger.LogDebug("Executed {Count} health checks via orchestration", results.Count);

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
            return ConvertToDegradationLevel(_overallStatus);
        }

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            ThrowIfDisposed();
            return _circuitBreakerService.GetCircuitBreakerState(operationName);
        }

        /// <inheritdoc />
        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            ThrowIfDisposed();
            return _circuitBreakerService.GetAllCircuitBreakerStates();
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

            // Delegate to scheduler service
            _ = _schedulerService.StartAsync(_config.AutomaticCheckInterval, _serviceCancellationSource.Token);

            _logger.LogInfo("HealthCheckService started in orchestration mode");
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

            // Delegate to scheduler service
            _ = _schedulerService.StopAsync(_serviceCancellationSource.Token);

            _logger.LogInfo("HealthCheckService stopped");
        }

        /// <inheritdoc />
        public bool IsAutomaticChecksRunning()
        {
            ThrowIfDisposed();
            return _isRunning && _schedulerService.IsRunning;
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            ThrowIfDisposed();
            _circuitBreakerService.ForceCircuitBreakerOpen(operationName, reason);
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            ThrowIfDisposed();
            _circuitBreakerService.ForceCircuitBreakerClosed(operationName, reason);
        }

        /// <inheritdoc />
        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            ThrowIfDisposed();
            _logger.LogInfo("Degradation level change requested to {Level}: {Reason}", level, reason);
            // This would be delegated to a degradation service in a full implementation
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

                // Publish health status change using specialized message publisher
                await _messagePublisherService.PublishHealthStatusChangedAsync(
                    ConvertToHealthStatus(previousStatus),
                    ConvertToHealthStatus(_overallStatus),
                    CalculateOverallHealthScore(results),
                    _serviceId);

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
                throw new ObjectDisposedException(nameof(HealthCheckServiceRefactored));
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

                // Dispose specialized services
                _schedulerService?.Dispose();

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