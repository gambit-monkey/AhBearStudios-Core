using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.HealthCheck.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Alerts;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Profiler;
using AhBearStudios.Core.Profiling;
using IHealthCheck = AhBearStudios.Core.HealthCheck.Checks.IHealthCheck;

namespace AhBearStudios.Core.HealthCheck
{
    /// <summary>
    /// Production-ready health check service providing comprehensive system health monitoring,
    /// circuit breaker protection, and graceful degradation capabilities
    /// </summary>
    public sealed class HealthCheckService : IHealthCheckService, IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly HealthCheckServiceConfig _config;

        private readonly ConcurrentDictionary<FixedString64Bytes, IHealthCheck> _healthChecks = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration> _healthCheckConfigs = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, ICircuitBreaker> _circuitBreakers = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, List<HealthCheckResult>> _healthCheckHistory = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, bool> _healthCheckEnabledStates = new();

        private readonly SemaphoreSlim _executionSemaphore;
        private readonly Timer _automaticCheckTimer;
        private readonly object _degradationLock = new();

        private volatile bool _automaticChecksRunning;
        private volatile DegradationLevel _currentDegradationLevel = DegradationLevel.None;
        private volatile bool _disposed;
        private HealthStatus _lastOverallStatus = HealthStatus.Unknown;

        /// <summary>
        /// Event triggered when overall health status changes
        /// </summary>
        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        /// <summary>
        /// Event triggered when circuit breaker state changes
        /// </summary>
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;

        /// <summary>
        /// Event triggered when system degradation level changes
        /// </summary>
        public event EventHandler<DegradationStatusChangedEventArgs> DegradationStatusChanged;

        /// <summary>
        /// Initializes a new instance of the HealthCheckService class
        /// </summary>
        /// <param name="config">Service configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="alertService">Alert service</param>
        /// <param name="profilerService">Profiler service (optional)</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public HealthCheckService(
            HealthCheckServiceConfig config,
            ILoggingService logger,
            IAlertService alertService,
            IProfilerService profilerService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _profilerService = profilerService;

            // Validate configuration
            var validationErrors = _config.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid HealthCheckService configuration: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            _executionSemaphore = new SemaphoreSlim(_config.MaxConcurrentChecks, _config.MaxConcurrentChecks);
            _automaticCheckTimer = new Timer(ExecuteAutomaticChecks, null, Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);

            _logger.LogInfo($"HealthCheckService initialized with config: " +
                            $"MaxConcurrentChecks={_config.MaxConcurrentChecks}, " +
                            $"DefaultTimeout={_config.DefaultTimeout}, " +
                            $"AutomaticChecks={_config.EnableAutomaticChecks}");

            if (_config.EnableAutomaticChecks)
            {
                StartAutomaticChecks();
            }
        }

        /// <summary>
        /// Registers a health check with the service
        /// </summary>
        /// <param name="healthCheck">The health check to register</param>
        /// <param name="config">Optional configuration for the health check</param>
        /// <exception cref="ArgumentNullException">Thrown when healthCheck is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when a health check with the same name already exists</exception>
        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            ThrowIfDisposed();

            if (!_healthChecks.TryAdd(healthCheck.Name, healthCheck))
            {
                throw new InvalidOperationException(
                    $"Health check with name '{healthCheck.Name}' is already registered");
            }

            // Store configuration
            var healthCheckConfig = config ?? new HealthCheckConfiguration();
            _healthCheckConfigs.TryAdd(healthCheck.Name, healthCheckConfig);

            // Initialize enabled state
            _healthCheckEnabledStates.TryAdd(healthCheck.Name, true);

            // Initialize history
            _healthCheckHistory.TryAdd(healthCheck.Name, new List<HealthCheckResult>());

            // Create circuit breaker if enabled
            if (_config.EnableCircuitBreakers && healthCheckConfig.EnableCircuitBreaker)
            {
                var circuitBreakerConfig =
                    healthCheckConfig.CircuitBreakerConfig ?? _config.DefaultCircuitBreakerConfig;
                var circuitBreaker = new CircuitBreaker(healthCheck.Name, circuitBreakerConfig, _logger);
                circuitBreaker.StateChanged += OnCircuitBreakerStateChanged;
                _circuitBreakers.TryAdd(healthCheck.Name, circuitBreaker);
            }

            _logger.LogInfo($"Health check '{healthCheck.Name}' registered successfully " +
                            $"(Category: {healthCheck.Category}, Timeout: {healthCheck.Timeout})");
        }

        /// <summary>
        /// Registers multiple health checks in a single operation
        /// </summary>
        /// <param name="healthChecks">Dictionary of health checks with their configurations</param>
        /// <exception cref="ArgumentNullException">Thrown when healthChecks is null</exception>
        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));

            ThrowIfDisposed();

            foreach (var kvp in healthChecks)
            {
                RegisterHealthCheck(kvp.Key, kvp.Value);
            }

            _logger.LogInfo($"Registered {healthChecks.Count} health checks in batch operation");
        }

        /// <summary>
        /// Unregisters a health check from the service
        /// </summary>
        /// <param name="name">Name of the health check to unregister</param>
        /// <returns>True if the health check was found and removed, false otherwise</returns>
        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            ThrowIfDisposed();

            var removed = _healthChecks.TryRemove(name, out var healthCheck);
            if (removed)
            {
                _healthCheckConfigs.TryRemove(name, out _);
                _healthCheckEnabledStates.TryRemove(name, out _);
                _healthCheckHistory.TryRemove(name, out _);

                // Dispose circuit breaker if it exists
                if (_circuitBreakers.TryRemove(name, out var circuitBreaker))
                {
                    circuitBreaker.StateChanged -= OnCircuitBreakerStateChanged;
                    circuitBreaker.Dispose();
                }

                _logger.LogInfo($"Health check '{name}' unregistered successfully");
            }

            return removed;
        }

        /// <summary>
        /// Executes a specific health check by name
        /// </summary>
        /// <param name="name">Name of the health check to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        /// <exception cref="ArgumentException">Thrown when health check name is not found</exception>
        public async Task<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_healthChecks.TryGetValue(name, out var healthCheck))
            {
                throw new ArgumentException($"Health check '{name}' not found", nameof(name));
            }

            if (!IsHealthCheckEnabled(name))
            {
                return HealthCheckResult.Unknown(
                    name.ToString(),
                    "Health check is disabled",
                    TimeSpan.Zero);
            }

            return await ExecuteHealthCheckInternalAsync(healthCheck, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes all registered health checks
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive health report</returns>
        public async Task<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var correlationId = GenerateCorrelationId();
            using var scope = _profilerService?.BeginScope("ExecuteAllHealthChecks");

            _logger.LogInfo($"Executing all health checks (CorrelationId: {correlationId})");

            var results = new List<HealthCheckResult>();
            var enabledHealthChecks = _healthChecks.Where(kvp => IsHealthCheckEnabled(kvp.Key)).ToList();

            if (enabledHealthChecks.Count == 0)
            {
                _logger.LogWarning("No enabled health checks found");
                return new HealthReport
                {
                    Results = results,
                    OverallStatus = HealthStatus.Unknown,
                    TotalDuration = TimeSpan.Zero,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId
                };
            }

            var startTime = DateTime.UtcNow;
            var tasks = enabledHealthChecks.Select(kvp =>
                ExecuteHealthCheckInternalAsync(kvp.Value, cancellationToken)).ToArray();

            try
            {
                results.AddRange(await Task.WhenAll(tasks).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error executing health checks (CorrelationId: {correlationId})");

                // Add failed results for any that didn't complete
                for (int i = 0; i < tasks.Length; i++)
                {
                    if (tasks[i].IsFaulted && results.Count <= i)
                    {
                        var healthCheck = enabledHealthChecks[i].Value;
                        results.Add(HealthCheckResult.Unhealthy(
                            healthCheck.Name.ToString(),
                            $"Health check failed: {tasks[i].Exception?.GetBaseException().Message}",
                            TimeSpan.Zero,
                            exception: tasks[i].Exception?.GetBaseException()));
                    }
                }
            }

            var totalDuration = DateTime.UtcNow - startTime;
            var overallStatus = DetermineOverallStatus(results);

            // Check if overall status changed and raise event
            if (overallStatus != _lastOverallStatus)
            {
                var oldStatus = _lastOverallStatus;
                _lastOverallStatus = overallStatus;
                OnHealthStatusChanged(oldStatus, overallStatus,
                    "Overall health status changed after executing all checks");
            }

            // Update degradation level if enabled
            if (_config.EnableGracefulDegradation)
            {
                UpdateDegradationLevel(results);
            }

            var report = new HealthReport
            {
                Results = results,
                OverallStatus = overallStatus,
                TotalDuration = totalDuration,
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId,
                DegradationLevel = _currentDegradationLevel
            };

            _logger.LogInfo($"Health check execution completed: {results.Count} checks, " +
                            $"Overall: {overallStatus}, Duration: {totalDuration.TotalMilliseconds:F0}ms " +
                            $"(CorrelationId: {correlationId})");

            return report;
        }

        /// <summary>
        /// Gets the overall health status of the system
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Overall system health status</returns>
        public async Task<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var report = await ExecuteAllHealthChecksAsync(cancellationToken).ConfigureAwait(false);
            return report.OverallStatus;
        }

        /// <summary>
        /// Gets the current degradation level of the system
        /// </summary>
        /// <returns>Current degradation level</returns>
        public DegradationLevel GetCurrentDegradationLevel()
        {
            return _currentDegradationLevel;
        }

        /// <summary>
        /// Gets circuit breaker state for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Circuit breaker state</returns>
        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            return _circuitBreakers.TryGetValue(operationName, out var circuitBreaker)
                ? circuitBreaker.State
                : CircuitBreakerState.Closed;
        }

        /// <summary>
        /// Gets all circuit breaker states
        /// </summary>
        /// <returns>Dictionary of operation names to circuit breaker states</returns>
        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            return _circuitBreakers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.State);
        }

        /// <summary>
        /// Gets health check history for a specific check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of historical health check results</returns>
        public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
        {
            if (!_healthCheckHistory.TryGetValue(name, out var history))
                return new List<HealthCheckResult>();

            lock (history)
            {
                return history.TakeLast(Math.Min(maxResults, history.Count)).ToList();
            }
        }

        /// <summary>
        /// Gets names of all registered health checks
        /// </summary>
        /// <returns>List of health check names</returns>
        public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            return _healthChecks.Keys.ToList();
        }

        /// <summary>
        /// Gets metadata for a specific health check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>Health check metadata</returns>
        public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                return new Dictionary<string, object>();

            var metadata = healthCheck.GetMetadata();
            metadata["Enabled"] = IsHealthCheckEnabled(name);
            metadata["CircuitBreakerState"] = GetCircuitBreakerState(name);

            if (_healthCheckHistory.TryGetValue(name, out var history))
            {
                lock (history)
                {
                    metadata["HistoryCount"] = history.Count;
                    metadata["LastExecution"] = history.LastOrDefault()?.Timestamp;
                }
            }

            return metadata;
        }

        /// <summary>
        /// Starts automatic health check execution with configured intervals
        /// </summary>
        public void StartAutomaticChecks()
        {
            ThrowIfDisposed();

            if (_automaticChecksRunning)
                return;

            _automaticChecksRunning = true;
            _automaticCheckTimer.Change(_config.DefaultCheckInterval, _config.DefaultCheckInterval);

            _logger.LogInfo($"Automatic health checks started with interval: {_config.DefaultCheckInterval}");
        }

        /// <summary>
        /// Stops automatic health check execution
        /// </summary>
        public void StopAutomaticChecks()
        {
            if (!_automaticChecksRunning)
                return;

            _automaticChecksRunning = false;
            _automaticCheckTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            _logger.LogInfo("Automatic health checks stopped");
        }

        /// <summary>
        /// Checks if automatic health checks are currently running
        /// </summary>
        /// <returns>True if automatic checks are running, false otherwise</returns>
        public bool IsAutomaticChecksRunning()
        {
            return _automaticChecksRunning;
        }

        /// <summary>
        /// Forces circuit breaker to open state for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing the circuit breaker open</param>
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                circuitBreaker.Open(reason);
                _logger.LogWarning($"Circuit breaker '{operationName}' forced open. Reason: {reason}");
            }
        }

        /// <summary>
        /// Forces circuit breaker to closed state for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing the circuit breaker closed</param>
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                circuitBreaker.Close(reason);
                _logger.LogInfo($"Circuit breaker '{operationName}' forced closed. Reason: {reason}");
            }
        }

        /// <summary>
        /// Sets the system degradation level manually
        /// </summary>
        /// <param name="level">Degradation level to set</param>
        /// <param name="reason">Reason for the degradation level change</param>
        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            lock (_degradationLock)
            {
                if (_currentDegradationLevel != level)
                {
                    var oldLevel = _currentDegradationLevel;
                    _currentDegradationLevel = level;

                    _logger.LogWarning(
                        $"System degradation level changed from {oldLevel} to {level}. Reason: {reason}");
                    OnDegradationStatusChanged(oldLevel, level, reason);
                }
            }
        }

        /// <summary>
        /// Gets comprehensive system health statistics
        /// </summary>
        /// <returns>System health statistics</returns>
        public HealthStatistics GetHealthStatistics()
        {
            var totalChecks = _healthChecks.Count;
            var enabledChecks = _healthCheckEnabledStates.Values.Count(enabled => enabled);
            var disabledChecks = totalChecks - enabledChecks;

            var circuitBreakerStats = _circuitBreakers.Values.Select(cb => cb.GetStatistics()).ToList();
            var openCircuitBreakers = circuitBreakerStats.Count(stats => stats.State == CircuitBreakerState.Open);

            var allHistory = _healthCheckHistory.Values.SelectMany(history =>
            {
                lock (history)
                {
                    return history.ToList();
                }
            }).ToList();

            var healthyCount = allHistory.Count(r => r.Status == HealthStatus.Healthy);
            var degradedCount = allHistory.Count(r => r.Status == HealthStatus.Degraded);
            var unhealthyCount = allHistory.Count(r => r.Status == HealthStatus.Unhealthy);
            var unknownCount = allHistory.Count(r => r.Status == HealthStatus.Unknown);

            return new HealthStatistics
            {
                TotalHealthChecks = totalChecks,
                EnabledHealthChecks = enabledChecks,
                DisabledHealthChecks = disabledChecks,
                TotalCircuitBreakers = _circuitBreakers.Count,
                OpenCircuitBreakers = openCircuitBreakers,
                CurrentDegradationLevel = _currentDegradationLevel,
                OverallHealthStatus = _lastOverallStatus,
                HealthyResultsCount = healthyCount,
                DegradedResultsCount = degradedCount,
                UnhealthyResultsCount = unhealthyCount,
                UnknownResultsCount = unknownCount,
                TotalHistoryEntries = allHistory.Count,
                AutomaticChecksRunning = _automaticChecksRunning,
                LastExecutionTime = allHistory.OrderByDescending(r => r.Timestamp).FirstOrDefault()?.Timestamp,
                CircuitBreakerStatistics = circuitBreakerStats
            };
        }

        /// <summary>
        /// Checks if a specific health check is currently enabled
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>True if enabled, false otherwise</returns>
        public bool IsHealthCheckEnabled(FixedString64Bytes name)
        {
            return _healthCheckEnabledStates.TryGetValue(name, out var enabled) && enabled;
        }

        /// <summary>
        /// Enables or disables a specific health check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="enabled">Whether to enable or disable the check</param>
        public void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
        {
            if (_healthCheckEnabledStates.TryGetValue(name, out var currentState) && currentState != enabled)
            {
                _healthCheckEnabledStates.TryUpdate(name, enabled, currentState);
                _logger.LogInfo($"Health check '{name}' {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Executes automatic health checks (called by timer)
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private async void ExecuteAutomaticChecks(object state)
        {
            if (!_automaticChecksRunning || _disposed)
                return;

            try
            {
                await ExecuteAllHealthChecksAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during automatic health check execution");
            }
        }

        /// <summary>
        /// Executes a single health check with proper error handling and circuit breaker protection
        /// </summary>
        /// <param name="healthCheck">Health check to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        private async Task<HealthCheckResult> ExecuteHealthCheckInternalAsync(IHealthCheck healthCheck,
            CancellationToken cancellationToken)
        {
            var correlationId = GenerateCorrelationId();
            var startTime = DateTime.UtcNow;

            await _executionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var scope = _profilerService?.BeginScope($"HealthCheck_{healthCheck.Name}");
                using var timeoutCts = new CancellationTokenSource(healthCheck.Timeout);
                using var combinedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                HealthCheckResult result;

                // Execute with circuit breaker protection if available
                if (_circuitBreakers.TryGetValue(healthCheck.Name, out var circuitBreaker))
                {
                    try
                    {
                        result = await circuitBreaker
                            .ExecuteAsync(
                                async ct => { return await healthCheck.CheckHealthAsync(ct).ConfigureAwait(false); },
                                combinedCts.Token).ConfigureAwait(false);
                    }
                    catch (CircuitBreakerOpenException)
                    {
                        result = HealthCheckResult.Unhealthy(
                            healthCheck.Name.ToString(),
                            "Circuit breaker is open",
                            DateTime.UtcNow - startTime);
                    }
                }
                else
                {
                    result = await healthCheck.CheckHealthAsync(combinedCts.Token).ConfigureAwait(false);
                }

                // Add correlation ID and timing information
                result = result with
                {
                    CorrelationId = correlationId,
                    Duration = DateTime.UtcNow - startTime,
                    Category = healthCheck.Category
                };

                // Store in history
                StoreHealthCheckResult(healthCheck.Name, result);

                // Log result
                if (_config.EnableHealthCheckLogging)
                {
                    LogHealthCheckResult(healthCheck, result);
                }

                // Raise alerts if needed
                if (_config.EnableHealthAlerts)
                {
                    RaiseHealthAlert(healthCheck, result);
                }

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                var result = HealthCheckResult.Unhealthy(
                    healthCheck.Name.ToString(),
                    "Health check was cancelled",
                    DateTime.UtcNow - startTime);

                StoreHealthCheckResult(healthCheck.Name, result);
                return result;
            }
            catch (Exception ex)
            {
                var result = HealthCheckResult.Unhealthy(
                    healthCheck.Name.ToString(),
                    $"Health check failed: {ex.Message}",
                    DateTime.UtcNow - startTime,
                    exception: ex,
                    correlationId: correlationId);

                StoreHealthCheckResult(healthCheck.Name, result);

                _logger.LogException(ex, $"Health check '{healthCheck.Name}' failed (CorrelationId: {correlationId})");
                return result;
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        /// <summary>
        /// Stores health check result in history with size limits
        /// </summary>
        /// <param name="name">Health check name</param>
        /// <param name="result">Health check result</param>
        private void StoreHealthCheckResult(FixedString64Bytes name, HealthCheckResult result)
        {
            if (_healthCheckHistory.TryGetValue(name, out var history))
            {
                lock (history)
                {
                    history.Add(result);

                    // Trim history if it exceeds maximum size
                    while (history.Count > _config.MaxHistoryPerCheck)
                    {
                        history.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Logs health check result based on configuration
        /// </summary>
        /// <param name="healthCheck">Health check that was executed</param>
        /// <param name="result">Health check result</param>
        private void LogHealthCheckResult(IHealthCheck healthCheck, HealthCheckResult result)
        {
            var message = $"Health check '{healthCheck.Name}' completed: {result.Status} - {result.Message} " +
                          $"[{result.Duration.TotalMilliseconds:F0}ms] (CorrelationId: {result.CorrelationId})";

            switch (_config.HealthCheckLogLevel)
            {
                case LogLevel.Debug:
                    _logger.LogDebug(message);
                    break;
                case LogLevel.Info:
                    _logger.LogInfo(message);
                    break;
                case LogLevel.Warning when result.Status != HealthStatus.Healthy:
                    _logger.LogWarning(message);
                    break;
                case LogLevel.Error when result.Status == HealthStatus.Unhealthy:
                    _logger.LogError(message);
                    break;
            }

            // Log slow health checks
            if (_config.EnableProfiling && result.Duration.TotalMilliseconds > _config.SlowHealthCheckThreshold)
            {
                _logger.LogWarning(
                    $"Slow health check detected: '{healthCheck.Name}' took {result.Duration.TotalMilliseconds:F0}ms " +
                    $"(threshold: {_config.SlowHealthCheckThreshold}ms)");
            }
        }

        /// <summary>
        /// Raises health alerts based on configuration and result status
        /// </summary>
        /// <param name="healthCheck">Health check that was executed</param>
        /// <param name="result">Health check result</param>
        private void RaiseHealthAlert(IHealthCheck healthCheck, HealthCheckResult result)
        {
            if (!_config.AlertSeverities.TryGetValue(result.Status, out var severity))
                return;

            // Only alert on status changes or critical issues
            var shouldAlert = result.Status == HealthStatus.Unhealthy ||
                              (result.Status == HealthStatus.Degraded && severity >= AlertSeverity.Warning);

            if (shouldAlert)
            {
                try
                {
                    _alertService.RaiseAlert(
                        $"Health check '{healthCheck.Name}' reported {result.Status}",
                        severity,
                        healthCheck.Category.ToString(),
                        healthCheck.Name,
                        new Dictionary<string, object>
                        {
                            ["HealthCheckName"] = healthCheck.Name.ToString(),
                            ["Status"] = result.Status.ToString(),
                            ["Message"] = result.Message,
                            ["Duration"] = result.Duration.TotalMilliseconds,
                            ["Category"] = healthCheck.Category.ToString(),
                            ["CorrelationId"] = result.CorrelationId.ToString()
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, $"Failed to raise health alert for '{healthCheck.Name}'");
                }
            }
        }

        /// <summary>
        /// Determines overall system health status from individual check results
        /// </summary>
        /// <param name="results">Individual health check results</param>
        /// <returns>Overall health status</returns>
        private HealthStatus DetermineOverallStatus(List<HealthCheckResult> results)
        {
            if (results.Count == 0)
                return HealthStatus.Unknown;

            var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
            var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
            var totalCount = results.Count;

            var unhealthyPercentage = (double)unhealthyCount / totalCount;
            var degradedPercentage = (double)(unhealthyCount + degradedCount) / totalCount;

            if (unhealthyPercentage >= _config.HealthThresholds.UnhealthyThreshold)
                return HealthStatus.Unhealthy;

            if (degradedPercentage >= _config.HealthThresholds.DegradedThreshold)
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }

        /// <summary>
        /// Updates system degradation level based on health check results
        /// </summary>
        /// <param name="results">Health check results</param>
        private void UpdateDegradationLevel(List<HealthCheckResult> results)
        {
            if (results.Count == 0)
                return;

            var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
            var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
            var totalCount = results.Count;

            var unhealthyPercentage = (double)unhealthyCount / totalCount;
            var degradedPercentage = (double)(unhealthyCount + degradedCount) / totalCount;

            var newLevel = DegradationLevel.None;

            if (unhealthyPercentage >= _config.DegradationThresholds.DisabledThreshold)
                newLevel = DegradationLevel.Disabled;
            else if (unhealthyPercentage >= _config.DegradationThresholds.SevereThreshold)
                newLevel = DegradationLevel.Severe;
            else if (degradedPercentage >= _config.DegradationThresholds.ModerateThreshold)
                newLevel = DegradationLevel.Moderate;
            else if (degradedPercentage >= _config.DegradationThresholds.MinorThreshold)
                newLevel = DegradationLevel.Minor;

            SetDegradationLevel(newLevel,
                $"Auto-updated based on health check results: {unhealthyCount} unhealthy, {degradedCount} degraded out of {totalCount}");
        }

        /// <summary>
        /// Handles circuit breaker state changes
        /// </summary>
        /// <param name="sender">Circuit breaker that changed state</param>
        /// <param name="e">State change event arguments</param>
        private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs e)
        {
            try
            {
                CircuitBreakerStateChanged?.Invoke(this, e);

                if (_config.EnableCircuitBreakerAlerts)
                {
                    var severity = e.NewState == CircuitBreakerState.Open ? AlertSeverity.Critical : AlertSeverity.Info;
                    _alertService.RaiseAlert(
                        $"Circuit breaker '{e.CircuitBreakerName}' changed state to {e.NewState}",
                        severity,
                        "CircuitBreaker",
                        e.CircuitBreakerName,
                        new Dictionary<string, object>
                        {
                            ["CircuitBreakerName"] = e.CircuitBreakerName.ToString(),
                            ["OldState"] = e.OldState.ToString(),
                            ["NewState"] = e.NewState.ToString(),
                            ["Reason"] = e.Reason,
                            ["Timestamp"] = e.Timestamp
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error handling circuit breaker state change for '{e.CircuitBreakerName}'");
            }
        }

        /// <summary>
        /// Raises health status changed event
        /// </summary>
        /// <param name="oldStatus">Previous health status</param>
        /// <param name="newStatus">New health status</param>
        /// <param name="reason">Reason for status change</param>
        private void OnHealthStatusChanged(HealthStatus oldStatus, HealthStatus newStatus, string reason)
        {
            try
            {
                var eventArgs = new HealthStatusChangedEventArgs
                {
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                };

                HealthStatusChanged?.Invoke(this, eventArgs);

                if (_config.EnableHealthAlerts && _config.AlertSeverities.TryGetValue(newStatus, out var severity))
                {
                    _alertService.RaiseAlert(
                        $"System health status changed from {oldStatus} to {newStatus}",
                        severity,
                        "SystemHealth",
                        new FixedString64Bytes("OverallHealth"),
                        new Dictionary<string, object>
                        {
                            ["OldStatus"] = oldStatus.ToString(),
                            ["NewStatus"] = newStatus.ToString(),
                            ["Reason"] = reason,
                            ["Timestamp"] = eventArgs.Timestamp
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error raising health status changed event: {oldStatus} -> {newStatus}");
            }
        }

        /// <summary>
        /// Raises degradation status changed event
        /// </summary>
        /// <param name="oldLevel">Previous degradation level</param>
        /// <param name="newLevel">New degradation level</param>
        /// <param name="reason">Reason for level change</param>
        private void OnDegradationStatusChanged(DegradationLevel oldLevel, DegradationLevel newLevel, string reason)
        {
            try
            {
                var eventArgs = new DegradationStatusChangedEventArgs
                {
                    OldLevel = oldLevel,
                    NewLevel = newLevel,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                };

                DegradationStatusChanged?.Invoke(this, eventArgs);

                if (_config.EnableDegradationAlerts)
                {
                    var severity = newLevel >= DegradationLevel.Moderate
                        ? AlertSeverity.Critical
                        : AlertSeverity.Warning;
                    _alertService.RaiseAlert(
                        $"System degradation level changed from {oldLevel} to {newLevel}",
                        severity,
                        "SystemDegradation",
                        new FixedString64Bytes("DegradationLevel"),
                        new Dictionary<string, object>
                        {
                            ["OldLevel"] = oldLevel.ToString(),
                            ["NewLevel"] = newLevel.ToString(),
                            ["Reason"] = reason,
                            ["Timestamp"] = eventArgs.Timestamp
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Error raising degradation status changed event: {oldLevel} -> {newLevel}");
            }
        }

        /// <summary>
        /// Generates a unique correlation ID for tracing
        /// </summary>
        /// <returns>Unique correlation ID</returns>
        private static FixedString64Bytes GenerateCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthCheckService));
        }

        /// <summary>
        /// Disposes the health check service and its resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                StopAutomaticChecks();
                _automaticCheckTimer?.Dispose();
                _executionSemaphore?.Dispose();

                // Dispose all circuit breakers
                foreach (var circuitBreaker in _circuitBreakers.Values)
                {
                    circuitBreaker.StateChanged -= OnCircuitBreakerStateChanged;
                    circuitBreaker.Dispose();
                }

                _circuitBreakers.Clear();

                _logger.LogInfo("HealthCheckService disposed");
            }
        }
    }
}

    

    