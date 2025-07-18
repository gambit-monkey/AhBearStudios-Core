using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking
{
    /// <summary>
    /// Production-ready health check service providing comprehensive system health monitoring,
    /// circuit breaker protection, and graceful degradation capabilities.
    /// </summary>
    public sealed class HealthCheckService : IHealthCheckService, IDisposable
    {
        private readonly HealthCheckServiceConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        
        // Thread-safe collections for health checks and circuit breakers
        private readonly ConcurrentDictionary<FixedString64Bytes, IHealthCheck> _healthChecks = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration> _healthCheckConfigs = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, ICircuitBreaker> _circuitBreakers = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, List<HealthCheckResult>> _healthCheckHistory = new();
        private readonly ConcurrentDictionary<FixedString64Bytes, bool> _healthCheckEnabledStatus = new();
        
        // State management
        private readonly object _stateLock = new();
        private Timer _automaticCheckTimer;
        private HealthStatus _lastOverallStatus = HealthStatus.Unknown;
        private DegradationLevel _currentDegradationLevel = DegradationLevel.None;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed;

        // Statistics tracking
        private long _totalHealthChecks;
        private long _successfulHealthChecks;
        private long _failedHealthChecks;
        private DateTime _serviceStartTime = DateTime.UtcNow;

        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;
        public event EventHandler<DegradationStatusChangedEventArgs> DegradationStatusChanged;

        /// <summary>
        /// Initializes a new instance of the HealthCheckService
        /// </summary>
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
            _config.Validate();

            _logger.LogInfo("HealthCheckService initialized with configuration", new Dictionary<string, object>
            {
                ["AutomaticCheckInterval"] = _config.AutomaticCheckInterval,
                ["MaxHistorySize"] = _config.MaxHistorySize,
                ["EnableCircuitBreaker"] = _config.EnableCircuitBreaker,
                ["DefaultTimeout"] = _config.DefaultTimeout
            });
        }

        /// <summary>
        /// Registers a health check with the service
        /// </summary>
        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            ThrowIfDisposed();

            var name = healthCheck.Name;
            if (_healthChecks.ContainsKey(name))
                throw new InvalidOperationException($"Health check with name '{name}' is already registered");

            // Use provided configSo or create default
            var healthCheckConfig = config ?? new HealthCheckConfiguration
            {
                Name = name,
                Timeout = _config.DefaultTimeout,
                Interval = _config.AutomaticCheckInterval,
                EnableCircuitBreaker = _config.EnableCircuitBreaker,
                MaxRetries = _config.MaxRetries,
                RetryDelay = _config.RetryDelay
            };

            // Create circuit breaker if enabled
            if (healthCheckConfig.EnableCircuitBreaker)
            {
                var circuitBreakerConfig = new CircuitBreakerConfig
                {
                    FailureThreshold = healthCheckConfig.FailureThreshold,
                    Timeout = healthCheckConfig.Timeout,
                    SamplingDuration = TimeSpan.FromMinutes(5),
                    MinimumThroughput = 5
                };

                var circuitBreaker = new CircuitBreaker(name, circuitBreakerConfig, _logger);
                circuitBreaker.StateChanged += OnCircuitBreakerStateChanged;
                _circuitBreakers.TryAdd(name, circuitBreaker);
            }

            // Register the health check
            _healthChecks.TryAdd(name, healthCheck);
            _healthCheckConfigs.TryAdd(name, healthCheckConfig);
            _healthCheckEnabledStatus.TryAdd(name, true);
            _healthCheckHistory.TryAdd(name, new List<HealthCheckResult>());

            // Configure the health check if it supports configuration
            if (healthCheck is IConfigurableHealthCheck configurableHealthCheck)
            {
                configurableHealthCheck.Configure(healthCheckConfig);
            }

            _logger.LogInfo($"Health check '{name}' registered successfully", new Dictionary<string, object>
            {
                ["Category"] = healthCheck.Category,
                ["Timeout"] = healthCheckConfig.Timeout,
                ["CircuitBreakerEnabled"] = healthCheckConfig.EnableCircuitBreaker
            });
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
        /// Unregisters a health check from the service
        /// </summary>
        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            ThrowIfDisposed();

            var removed = _healthChecks.TryRemove(name, out var healthCheck);
            if (removed)
            {
                _healthCheckConfigs.TryRemove(name, out _);
                _healthCheckEnabledStatus.TryRemove(name, out _);
                _healthCheckHistory.TryRemove(name, out _);

                // Dispose circuit breaker if it exists
                if (_circuitBreakers.TryRemove(name, out var circuitBreaker))
                {
                    circuitBreaker.StateChanged -= OnCircuitBreakerStateChanged;
                    if (circuitBreaker is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _logger.LogInfo($"Health check '{name}' unregistered successfully");
            }

            return removed;
        }

        /// <summary>
        /// Executes a specific health check by name
        /// </summary>
        public async Task<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                throw new ArgumentException($"Health check '{name}' not found", nameof(name));

            if (!_healthCheckEnabledStatus.TryGetValue(name, out var isEnabled) || !isEnabled)
            {
                return new HealthCheckResult(
                    name,
                    HealthStatus.Unknown,
                    "Health check is disabled",
                    TimeSpan.Zero,
                    new Dictionary<string, object> { ["Enabled"] = false },
                    GenerateCorrelationId()
                );
            }

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
            
            // Execute with circuit breaker protection if available
            if (_circuitBreakers.TryGetValue(name, out var circuitBreaker))
            {
                try
                {
                    return await circuitBreaker.ExecuteAsync(async ct => 
                        await ExecuteHealthCheckInternalAsync(healthCheck, ct), combinedCts.Token);
                }
                catch (CircuitBreakerOpenException)
                {
                    return CreateCircuitBreakerFallbackResult(name, TimeSpan.Zero);
                }
            }

            return await ExecuteHealthCheckInternalAsync(healthCheck, combinedCts.Token);
        }

        /// <summary>
        /// Executes all registered health checks
        /// </summary>
        public async Task<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var startTime = DateTime.UtcNow;
            var results = new List<HealthCheckResult>();
            var correlationId = GenerateCorrelationId();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            // Execute all health checks in parallel with limited concurrency
            var semaphore = new SemaphoreSlim(_config.MaxConcurrentHealthChecks, _config.MaxConcurrentHealthChecks);
            var tasks = _healthChecks.Keys.Select(async name =>
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
            });

            try
            {
                var allResults = await Task.WhenAll(tasks);
                results.AddRange(allResults);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error executing health checks", ex);
                throw;
            }

            var duration = DateTime.UtcNow - startTime;
            var overallStatus = DetermineOverallStatus(results);
            
            // Update degradation level based on results
            UpdateDegradationLevel(results);

            // Create comprehensive report
            var report = new HealthReport(
                overallStatus,
                results,
                duration,
                correlationId,
                new Dictionary<string, object>
                {
                    ["ExecutionTime"] = startTime,
                    ["TotalChecks"] = results.Count,
                    ["SuccessfulChecks"] = results.Count(r => r.Status == HealthStatus.Healthy),
                    ["WarningChecks"] = results.Count(r => r.Status == HealthStatus.Warning),
                    ["UnhealthyChecks"] = results.Count(r => r.Status == HealthStatus.Unhealthy),
                    ["DegradationLevel"] = _currentDegradationLevel
                }
            );

            // Store results in history
            foreach (var result in results)
            {
                StoreHealthCheckResult(result.Name, result);
            }

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
        public async Task<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var report = await ExecuteAllHealthChecksAsync(cancellationToken);
            return report.Status;
        }

        /// <summary>
        /// Gets the current degradation level of the system
        /// </summary>
        public DegradationLevel GetCurrentDegradationLevel()
        {
            return _currentDegradationLevel;
        }

        /// <summary>
        /// Gets circuit breaker state for a specific operation
        /// </summary>
        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            return _circuitBreakers.TryGetValue(operationName, out var circuitBreaker) 
                ? circuitBreaker.State 
                : CircuitBreakerState.Closed;
        }

        /// <summary>
        /// Gets all circuit breaker states
        /// </summary>
        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            return _circuitBreakers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.State);
        }

        /// <summary>
        /// Gets health check history for a specific check
        /// </summary>
        public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
        {
            if (!_healthCheckHistory.TryGetValue(name, out var history))
                return new List<HealthCheckResult>();

            lock (history)
            {
                return history.TakeLast(maxResults).ToList();
            }
        }

        /// <summary>
        /// Gets names of all registered health checks
        /// </summary>
        public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            return _healthChecks.Keys.ToList();
        }

        /// <summary>
        /// Gets metadata for a specific health check
        /// </summary>
        public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                return new Dictionary<string, object>();

            return healthCheck.GetMetadata();
        }

        /// <summary>
        /// Starts automatic health check execution
        /// </summary>
        public void StartAutomaticChecks()
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (_automaticCheckTimer != null)
                    return;

                _automaticCheckTimer = new Timer(ExecuteAutomaticChecks, null, TimeSpan.Zero, _config.AutomaticCheckInterval);
                _logger.LogInfo("Automatic health checks started", new Dictionary<string, object>
                {
                    ["Interval"] = _config.AutomaticCheckInterval
                });
            }
        }

        /// <summary>
        /// Stops automatic health check execution
        /// </summary>
        public void StopAutomaticChecks()
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
            lock (_stateLock)
            {
                return _automaticCheckTimer != null;
            }
        }

        /// <summary>
        /// Forces circuit breaker to open state
        /// </summary>
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                circuitBreaker.Open(reason);
            }
        }

        /// <summary>
        /// Forces circuit breaker to closed state
        /// </summary>
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                circuitBreaker.Close(reason);
            }
        }

        /// <summary>
        /// Sets the system degradation level manually
        /// </summary>
        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            var oldLevel = _currentDegradationLevel;
            _currentDegradationLevel = level;
            OnDegradationStatusChanged(oldLevel, level, reason);
        }

        /// <summary>
        /// Gets comprehensive system health statistics
        /// </summary>
        public HealthStatistics GetHealthStatistics()
        {
            var uptime = DateTime.UtcNow - _serviceStartTime;
            var circuitBreakerStats = _circuitBreakers.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.GetStatistics()
            );

            return new HealthStatistics
            {
                ServiceUptime = uptime,
                TotalHealthChecks = _totalHealthChecks,
                SuccessfulHealthChecks = _successfulHealthChecks,
                FailedHealthChecks = _failedHealthChecks,
                RegisteredHealthCheckCount = _healthChecks.Count,
                CurrentDegradationLevel = _currentDegradationLevel,
                CircuitBreakerStatistics = circuitBreakerStats,
                LastOverallStatus = _lastOverallStatus
            };
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
                _logger.LogInfo($"Health check '{name}' {(enabled ? "enabled" : "disabled")}");
            }
        }

        #region Private Methods

        private async void ExecuteAutomaticChecks(object state)
        {
            try
            {
                await ExecuteAllHealthChecksAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during automatic health check execution", ex);
            }
        }

        private async Task<HealthCheckResult> ExecuteHealthCheckInternalAsync(IHealthCheck healthCheck, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var correlationId = GenerateCorrelationId();

            try
            {
                Interlocked.Increment(ref _totalHealthChecks);

                using var profilerSession = _profilerService?.StartSession($"HealthCheck.{healthCheck.Name}");
                
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                
                if (result.Status == HealthStatus.Healthy)
                {
                    Interlocked.Increment(ref _successfulHealthChecks);
                }
                else
                {
                    Interlocked.Increment(ref _failedHealthChecks);
                }

                LogHealthCheckResult(healthCheck, result);
                RaiseHealthAlert(healthCheck, result);

                return result;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedHealthChecks);
                var duration = DateTime.UtcNow - startTime;
                
                var result = new HealthCheckResult(
                    healthCheck.Name,
                    HealthStatus.Unhealthy,
                    $"Health check failed: {ex.Message}",
                    duration,
                    new Dictionary<string, object>
                    {
                        ["Exception"] = ex.GetType().Name,
                        ["ExceptionMessage"] = ex.Message
                    },
                    correlationId
                );

                LogHealthCheckResult(healthCheck, result);
                RaiseHealthAlert(healthCheck, result);

                return result;
            }
        }

        private void StoreHealthCheckResult(FixedString64Bytes name, HealthCheckResult result)
        {
            if (_healthCheckHistory.TryGetValue(name, out var history))
            {
                lock (history)
                {
                    history.Add(result);
                    if (history.Count > _config.MaxHistorySize)
                    {
                        history.RemoveAt(0);
                    }
                }
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
            return $"hc_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
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