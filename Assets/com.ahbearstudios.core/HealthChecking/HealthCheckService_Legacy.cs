using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking
{
    /// <summary>
    /// Production-ready health check service providing comprehensive system health monitoring.
    /// Orchestrates specialized services for scheduling, degradation management, statistics, and circuit breakers.
    /// </summary>
    public sealed class HealthCheckService_Legacy : IHealthCheckService, IDisposable
    {
        private readonly HealthCheckServiceConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IMessageBusService _messageBus;
        
        // Specialized service dependencies
        private readonly IHealthCheckScheduler _scheduler;
        private readonly IHealthDegradationManager _degradationManager;
        private readonly IHealthStatisticsCollector _statisticsCollector;
        private readonly IHealthCircuitBreakerManager _circuitBreakerManager;
        
        // Core health check management
        private readonly ConcurrentDictionary<FixedString64Bytes, IHealthCheck> _healthChecks = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration> _healthCheckConfigs = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, bool> _healthCheckEnabledStatus = new();
        
        // Profiler markers
        private readonly ProfilerMarker _executeHealthCheckMarker = new("HealthCheckService.ExecuteHealthCheck");
        private readonly ProfilerMarker _executeAllHealthChecksMarker = new("HealthCheckService.ExecuteAllHealthChecks");
        
        // State management
        private readonly Guid _serviceId;
        private readonly object _stateLock = new();
        private HealthStatus _lastOverallStatus = HealthStatus.Unknown;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly DateTime _serviceStartTime = DateTime.UtcNow;
        private bool _disposed;

        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;
        public event EventHandler<DegradationStatusChangedEventArgs> DegradationStatusChanged;

        /// <summary>
        /// Initializes a new instance of the HealthCheckService with specialized service dependencies.
        /// </summary>
        /// <param name="config">Health check service configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="alertService">Alert service</param>
        /// <param name="messageBus">Message bus service</param>
        /// <param name="scheduler">Health check scheduler service</param>
        /// <param name="degradationManager">Degradation management service</param>
        /// <param name="statisticsCollector">Statistics collection service</param>
        /// <param name="circuitBreakerManager">Circuit breaker management service</param>
        /// <param name="profilerService">Profiler service (optional)</param>
        public HealthCheckService(
            HealthCheckServiceConfig config,
            ILoggingService logger,
            IAlertService alertService,
            IMessageBusService messageBus,
            IHealthCheckScheduler scheduler,
            IHealthDegradationManager degradationManager,
            IHealthStatisticsCollector statisticsCollector,
            IHealthCircuitBreakerManager circuitBreakerManager,
            IProfilerService profilerService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _degradationManager = degradationManager ?? throw new ArgumentNullException(nameof(degradationManager));
            _statisticsCollector = statisticsCollector ?? throw new ArgumentNullException(nameof(statisticsCollector));
            _circuitBreakerManager = circuitBreakerManager ?? throw new ArgumentNullException(nameof(circuitBreakerManager));
            _profilerService = profilerService;

            // Validate configuration
            _config.Validate();

            _serviceId = DeterministicIdGenerator.GenerateHealthCheckId("HealthCheckService", Environment.MachineName);

            // Wire up event handlers
            _circuitBreakerManager.StateChanged += OnCircuitBreakerStateChanged;
            _degradationManager.DegradationLevelChanged += OnDegradationLevelChanged;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckServiceInit", _serviceId.ToString());
            _logger.LogInfo("HealthCheckService initialized with specialized services", correlationId);
            
            // Initialize scheduler to execute our health checks
            InitializeScheduler();
        }

        /// <summary>
        /// Registers a health check with the service and all specialized services.
        /// </summary>
        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            ThrowIfDisposed();

            var name = healthCheck.Name;
            if (_healthChecks.ContainsKey(name))
                throw new InvalidOperationException($"Health check with name '{name}' is already registered");

            // Use provided config or create default
            var healthCheckConfig = config ?? new HealthCheckConfiguration
            {
                Name = name,
                Timeout = _config.DefaultTimeout,
                Interval = _config.AutomaticCheckInterval,
                EnableCircuitBreaker = _config.EnableCircuitBreaker,
                MaxRetries = _config.MaxRetries,
                RetryDelay = _config.RetryDelay
            };

            // Register circuit breaker if enabled
            if (healthCheckConfig.EnableCircuitBreaker)
            {
                _circuitBreakerManager.RegisterCircuitBreaker(
                    name,
                    healthCheckConfig.FailureThreshold,
                    healthCheckConfig.Timeout,
                    1 // halfOpenTestCount
                );
            }

            // Register the health check
            _healthChecks.TryAdd(name, healthCheck);
            _healthCheckConfigs.TryAdd(name, healthCheckConfig);
            _healthCheckEnabledStatus.TryAdd(name, true);

            // Configure the health check if it supports configuration
            if (healthCheck is IConfigurableHealthCheck configurableHealthCheck)
            {
                configurableHealthCheck.Configure(healthCheckConfig);
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckRegistered", name.ToString());
            _logger.LogInfo($"Health check '{name}' registered successfully", correlationId);
        }

        /// <summary>
        /// Registers multiple health checks in a single operation
        /// </summary>
        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));

            foreach (var kvp in healthChecks)
            {
                RegisterHealthCheck(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Unregisters a health check from the service and all specialized services.
        /// </summary>
        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            ThrowIfDisposed();

            var removed = _healthChecks.TryRemove(name, out var healthCheck);
            if (removed)
            {
                _healthCheckConfigs.TryRemove(name, out _);
                _healthCheckEnabledStatus.TryRemove(name, out _);

                // Unregister from circuit breaker manager
                _circuitBreakerManager.UnregisterCircuitBreaker(name);

                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckUnregistered", name.ToString());
                _logger.LogInfo($"Health check '{name}' unregistered successfully", correlationId);
            }

            return removed;
        }

        /// <summary>
        /// Executes a specific health check by name
        /// </summary>
        public async UniTask<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                throw new ArgumentException($"Health check '{name}' not found", nameof(name));

            if (!_healthCheckEnabledStatus.TryGetValue(name, out var isEnabled) || !isEnabled)
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("DisabledHealthCheck", name.ToString());
                return new HealthCheckResult
                {
                    Name = name,
                    Status = HealthStatus.Unknown,
                    Description = "Health check is disabled",
                    Duration = TimeSpan.Zero,
                    Data = new Dictionary<string, object> { ["Enabled"] = false }.AsReadOnly(),
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };
            }

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
            
            // Check circuit breaker status
            if (!_circuitBreakerManager.IsExecutionAllowed(name))
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerOpen", name.ToString());
                return new HealthCheckResult
                {
                    Name = name,
                    Status = HealthStatus.Unhealthy,
                    Description = "Circuit breaker is open - execution blocked",
                    Duration = TimeSpan.Zero,
                    Data = new Dictionary<string, object> { ["CircuitBreakerOpen"] = true }.AsReadOnly(),
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };
            }

            using (_executeHealthCheckMarker.Auto())
            {
                var result = await ExecuteHealthCheckInternalAsync(healthCheck, combinedCts.Token);
                
                // Record result in circuit breaker and statistics
                _circuitBreakerManager.RecordHealthCheckResult(result);
                _statisticsCollector.RecordHealthCheckExecution(result);
                
                return result;
            }
        }

        /// <summary>
        /// Executes all registered health checks
        /// </summary>
        public async UniTask<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var startTime = DateTime.UtcNow;
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ExecuteAllHealthChecks", _serviceId.ToString());

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
            var results = new List<HealthCheckResult>();

            using (_executeAllHealthChecksMarker.Auto())
            {
                // Execute all health checks in parallel with limited concurrency
                var semaphore = new SemaphoreSlim(_config.MaxConcurrentHealthChecks, _config.MaxConcurrentHealthChecks);
                var tasks = _healthChecks.Keys.AsValueEnumerable().Select(async name =>
                {
                    await semaphore.WaitAsync(combinedCts.Token);
                    try
                    {
                        return await ExecuteHealthCheckAsync(name, combinedCts.Token);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                try
                {
                    var allResults = await UniTask.WhenAll(tasks);
                    results.AddRange(allResults);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, "Error executing health checks", correlationId);
                    throw;
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var overallStatus = DetermineOverallStatus(results);
            
            // Create comprehensive report
            var report = new HealthReport
            {
                Status = overallStatus,
                Results = results.AsReadOnly(),
                Duration = duration,
                CorrelationId = correlationId,
                Timestamp = startTime,
                Data = new Dictionary<string, object>
                {
                    ["ExecutionTime"] = startTime,
                    ["TotalChecks"] = results.Count,
                    ["SuccessfulChecks"] = results.AsValueEnumerable().Count(r => r.Status == HealthStatus.Healthy),
                    ["WarningChecks"] = results.AsValueEnumerable().Count(r => r.Status == HealthStatus.Warning),
                    ["UnhealthyChecks"] = results.AsValueEnumerable().Count(r => r.Status == HealthStatus.Unhealthy),
                    ["DegradationLevel"] = _degradationManager.CurrentLevel
                }.AsReadOnly()
            };

            // Record health report in statistics and evaluate degradation
            _statisticsCollector.RecordHealthReport(report);
            _degradationManager.EvaluateAndUpdateDegradationLevel(report, "Scheduled health check execution");

            // Raise health status changed event if needed
            if (overallStatus != _lastOverallStatus)
            {
                OnHealthStatusChanged(_lastOverallStatus, overallStatus, "Health check execution completed");
                _lastOverallStatus = overallStatus;
            }

            return report;
        }

        /// <summary>
        /// Gets the overall health status of the system
        /// </summary>
        public async UniTask<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var report = await ExecuteAllHealthChecksAsync(cancellationToken);
            return report.Status;
        }

        /// <summary>
        /// Gets the current degradation level of the system
        /// </summary>
        public DegradationLevel GetCurrentDegradationLevel()
        {
            return _degradationManager.CurrentLevel;
        }

        /// <summary>
        /// Gets circuit breaker state for a specific operation
        /// </summary>
        public CircuitBreakerState? GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            return _circuitBreakerManager.GetCircuitBreakerState(operationName);
        }

        /// <summary>
        /// Gets all circuit breaker states
        /// </summary>
        public IReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics> GetAllCircuitBreakerStatistics()
        {
            return _circuitBreakerManager.GetAllCircuitBreakerStatistics();
        }

        /// <summary>
        /// Gets health check statistics for a specific check
        /// </summary>
        public IndividualHealthCheckStatistics GetHealthCheckStatistics(FixedString64Bytes name)
        {
            return _statisticsCollector.GetHealthCheckStatistics(name);
        }

        /// <summary>
        /// Gets names of all registered health checks
        /// </summary>
        public IEnumerable<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            return _healthChecks.Keys;
        }

        /// <summary>
        /// Gets metadata for a specific health check
        /// </summary>
        public IReadOnlyDictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                return new Dictionary<string, object>().AsReadOnly();

            return healthCheck.GetMetadata();
        }

        /// <summary>
        /// Starts automatic health check execution using the scheduler service.
        /// </summary>
        public async UniTask StartAutomaticChecksAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _scheduler.StartAsync(_config.AutomaticCheckInterval, cancellationToken);
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("AutomaticChecksStarted", _serviceId.ToString());
            _logger.LogInfo($"Automatic health checks started with interval {_config.AutomaticCheckInterval}", correlationId);
        }

        /// <summary>
        /// Stops automatic health check execution using the scheduler service.
        /// </summary>
        public async UniTask StopAutomaticChecksAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateLock)
            {
                _automaticCheckTimer?.Dispose();
                _automaticCheckTimer = null;
                _logger.LogInfo("Automatic health checks stopped");
            }
        }

        /// <summary>
        /// Checks if automatic health checks are running
        /// </summary>
        public bool IsAutomaticChecksRunning()
        {
            return _scheduler.IsRunning;
        }

        /// <summary>
        /// Forces circuit breaker to open state
        /// </summary>
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            _circuitBreakerManager.SetCircuitBreakerState(operationName, CircuitBreakerState.Open, reason ?? "Manually forced open");
        }

        /// <summary>
        /// Forces circuit breaker to closed state
        /// </summary>
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            _circuitBreakerManager.SetCircuitBreakerState(operationName, CircuitBreakerState.Closed, reason ?? "Manually forced closed");
        }

        /// <summary>
        /// Sets the system degradation level manually
        /// </summary>
        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            _degradationManager.SetDegradationLevel(level, reason ?? "Manual degradation level change");
        }

        /// <summary>
        /// Gets comprehensive system health statistics
        /// </summary>
        public HealthStatistics GetHealthStatistics()
        {
            return _statisticsCollector.GetHealthStatistics();
        }

        /// <summary>
        /// Checks if a health check is enabled
        /// </summary>
        public bool IsHealthCheckEnabled(FixedString64Bytes name)
        {
            return _healthCheckEnabledStatus.TryGetValue(name, out var enabled) && enabled;
        }

        /// <summary>
        /// Enables or disables a health check
        /// </summary>
        public void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
        {
            if (_healthCheckEnabledStatus.ContainsKey(name))
            {
                _healthCheckEnabledStatus[name] = enabled;
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckToggled", name.ToString());
                _logger.LogInfo($"Health check '{name}' {(enabled ? "enabled" : "disabled")}", correlationId);
            }
        }

        #region Private Methods

        private void InitializeScheduler()
        {
            // The scheduler will be configured to execute health checks through our ExecuteScheduledHealthChecksAsync method
            // This is handled by the HealthCheckServiceFactory when creating the scheduler
        }

        private async UniTask ExecuteScheduledHealthChecksAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ExecuteAllHealthChecksAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ScheduledHealthCheckError", _serviceId.ToString());
                _logger.LogException(ex, "Error during scheduled health check execution", correlationId);
            }
        }

        private async UniTask<HealthCheckResult> ExecuteHealthCheckInternalAsync(IHealthCheck healthCheck, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckExecution", healthCheck.Name.ToString());

            try
            {
                using var profilerSession = _profilerService?.StartSession($"HealthCheck.{healthCheck.Name}");
                
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                result.CorrelationId = correlationId; // Ensure correlation ID is set
                result.Timestamp = startTime;

                LogHealthCheckResult(healthCheck, result);
                RaiseHealthAlert(healthCheck, result);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                
                var result = new HealthCheckResult
                {
                    Name = healthCheck.Name,
                    Status = HealthStatus.Unhealthy,
                    Description = $"Health check failed: {ex.Message}",
                    Duration = duration,
                    Data = new Dictionary<string, object>
                    {
                        ["Exception"] = ex.GetType().Name,
                        ["ExceptionMessage"] = ex.Message
                    }.AsReadOnly(),
                    CorrelationId = correlationId,
                    Timestamp = startTime
                };

                LogHealthCheckResult(healthCheck, result);
                RaiseHealthAlert(healthCheck, result);

                return result;
            }
        }


        private void LogHealthCheckResult(IHealthCheck healthCheck, HealthCheckResult result)
        {
            var logData = new Dictionary<string, object>
            {
                ["HealthCheck"] = healthCheck.Name,
                ["Status"] = result.Status,
                ["Duration"] = result.Duration,
                ["CorrelationId"] = result.CorrelationId
            };

            switch (result.Status)
            {
                case HealthStatus.Healthy:
                    _logger.LogDebug($"Health check '{healthCheck.Name}' passed", logData);
                    break;
                case HealthStatus.Warning:
                    _logger.LogWarning($"Health check '{healthCheck.Name}' returned warning: {result.Message}", logData);
                    break;
                case HealthStatus.Unhealthy:
                    _logger.LogError($"Health check '{healthCheck.Name}' failed: {result.Message}", logData);
                    break;
            }
        }

        private void RaiseHealthAlert(IHealthCheck healthCheck, HealthCheckResult result)
        {
            if (result.Status == HealthStatus.Unhealthy || result.Status == HealthStatus.Warning)
            {
                var severity = result.Status == HealthStatus.Unhealthy 
                    ? AlertSeverity.Critical 
                    : AlertSeverity.Warning;

                _alertService.SendAlert(new Alert
                {
                    Severity = severity,
                    Source = "HealthCheckService",
                    Message = $"Health check '{healthCheck.Name}' {result.Status}: {result.Message}",
                    Tag = "HealthCheck",
                    CorrelationId = result.CorrelationId,
                    Data = result.Data
                });
            }
        }

        private HealthStatus DetermineOverallStatus(List<HealthCheckResult> results)
        {
            if (!results.Any())
                return HealthStatus.Unknown;

            if (results.Any(r => r.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            if (results.Any(r => r.Status == HealthStatus.Warning))
                return HealthStatus.Warning;

            return results.All(r => r.Status == HealthStatus.Healthy) 
                ? HealthStatus.Healthy 
                : HealthStatus.Unknown;
        }

        private void UpdateDegradationLevel(List<HealthCheckResult> results)
        {
            var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
            var warningCount = results.Count(r => r.Status == HealthStatus.Warning);
            var totalCount = results.Count;

            var oldLevel = _currentDegradationLevel;
            var newLevel = DegradationLevel.None;

            if (unhealthyCount > 0)
            {
                var unhealthyPercentage = (double)unhealthyCount / totalCount;
                if (unhealthyPercentage >= 0.5)
                    newLevel = DegradationLevel.Severe;
                else if (unhealthyPercentage >= 0.25)
                    newLevel = DegradationLevel.Moderate;
                else
                    newLevel = DegradationLevel.Minor;
            }
            else if (warningCount > 0)
            {
                var warningPercentage = (double)warningCount / totalCount;
                if (warningPercentage >= 0.5)
                    newLevel = DegradationLevel.Minor;
            }

            if (newLevel != oldLevel)
            {
                _currentDegradationLevel = newLevel;
                OnDegradationStatusChanged(oldLevel, newLevel, "Degradation level updated based on health check results");
            }
        }

        private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs e)
        {
            CircuitBreakerStateChanged?.Invoke(this, e);
        }

        private void OnHealthStatusChanged(HealthStatus oldStatus, HealthStatus newStatus, string reason)
        {
            HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs(oldStatus, newStatus, reason));
        }

        private void OnDegradationStatusChanged(DegradationLevel oldLevel, DegradationLevel newLevel, string reason)
        {
            DegradationStatusChanged?.Invoke(this, new DegradationStatusChangedEventArgs(oldLevel, newLevel, reason));
        }

        private HealthCheckResult CreateCircuitBreakerFallbackResult(FixedString64Bytes name, TimeSpan duration)
        {
            return new HealthCheckResult(
                name,
                HealthStatus.Unhealthy,
                "Circuit breaker is open - health check bypassed",
                duration,
                new Dictionary<string, object>
                {
                    ["CircuitBreakerState"] = "Open",
                    ["Fallback"] = true
                },
                GenerateCorrelationId()
            );
        }

        private FixedString64Bytes GenerateCorrelationId()
        {
            return $"hc_{DateTime.UtcNow:yyyyMMddHHmmss}_{DeterministicIdGenerator.GenerateHealthCheckCorrelationId("HealthCheckService").ToString("N")[..8]}";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthCheckService));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Stop automatic checks
            StopAutomaticChecks();

            // Cancel any ongoing operations
            _cancellationTokenSource?.Cancel();

            // Dispose circuit breakers
            foreach (var circuitBreaker in _circuitBreakers.Values)
            {
                if (circuitBreaker is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _circuitBreakers.Clear();
            _healthChecks.Clear();
            _healthCheckConfigs.Clear();
            _healthCheckHistory.Clear();
            _healthCheckEnabledStatus.Clear();

            _cancellationTokenSource?.Dispose();

            _logger.LogInfo("HealthCheckService disposed");
        }

        #endregion
    }
}