using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ZLinq;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Extensions;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.HealthChecking;

/// <summary>
/// Orchestration-focused health check service designed for Unity game development.
/// Coordinates domain health check registrars and provides centralized health monitoring
/// without directly managing individual health checks.
/// Follows CLAUDE.md patterns with 60+ FPS performance targets.
/// </summary>
public sealed class HealthCheckService : IHealthCheckService, IDisposable
{
    private readonly HealthCheckServiceConfig _config;
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IProfilerService _profilerService;
    private readonly IMessageBusService _messageBus;
    
    // Orchestration components
    private readonly HealthCheckRegistrationManager _registrationManager;
    private readonly ConcurrentDictionary<FixedString64Bytes, IHealthCheck> _registeredHealthChecks;
    private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration> _healthCheckConfigs;
    private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckResult> _lastResults;
    
    // Performance monitoring
    private readonly ProfilerMarker _executeHealthCheckMarker = new ProfilerMarker("HealthCheckService.ExecuteHealthCheck");
    private readonly ProfilerMarker _executeAllHealthChecksMarker = new ProfilerMarker("HealthCheckService.ExecuteAllHealthChecks");
    private readonly ProfilerMarker _orchestrationMarker = new ProfilerMarker("HealthCheckService.Orchestration");
    
    // State management
    private readonly Guid _serviceId;
    private readonly CancellationTokenSource _serviceCancellationSource;
    private CancellationTokenSource _automaticCheckCancellationSource;
    private UniTask _automaticCheckTask;
    private OverallHealthStatus _overallStatus;
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the orchestration-focused HealthCheckService
    /// </summary>
    /// <param name="config">Health check service configuration</param>
    /// <param name="logger">Logging service for health check operations</param>
    /// <param name="alertService">Alert service for health notifications</param>
    /// <param name="profilerService">Profiler service for performance monitoring</param>
    /// <param name="messageBus">Message bus for health check events</param>
    public HealthCheckService(
        HealthCheckServiceConfig config,
        ILoggingService logger,
        IAlertService alertService,
        IProfilerService profilerService,
        IMessageBusService messageBus)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

        _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckService");
        _serviceCancellationSource = new CancellationTokenSource();
        
        // Initialize orchestration components
        _registrationManager = new HealthCheckRegistrationManager(this, logger);
        _registeredHealthChecks = new ConcurrentDictionary<FixedString64Bytes, IHealthCheck>();
        _healthCheckConfigs = new ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration>();
        _lastResults = new ConcurrentDictionary<FixedString64Bytes, HealthCheckResult>();
        
        _overallStatus = OverallHealthStatus.Unknown;

        // Initialize automatic check cancellation source
        _automaticCheckCancellationSource = new CancellationTokenSource();

        _logger.LogInfo("HealthCheckService initialized in orchestration mode with ID: {ServiceId}", _serviceId);
    }

    #region IHealthCheckService Implementation

    /// <inheritdoc />
    public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
    {
        ThrowIfDisposed();
        
        if (healthCheck == null)
            throw new ArgumentNullException(nameof(healthCheck));

        var effectiveConfig = config ?? HealthCheckConfiguration.Create(healthCheck.Name.ToString());
        
        if (!_registeredHealthChecks.TryAdd(healthCheck.Name, healthCheck))
        {
            throw new InvalidOperationException($"Health check '{healthCheck.Name}' is already registered");
        }

        _healthCheckConfigs.TryAdd(healthCheck.Name, effectiveConfig);
        
        _logger.LogDebug("Registered health check: {HealthCheckName} (Category: {Category})", 
            healthCheck.Name, healthCheck.Category);
    }

    /// <inheritdoc />
    public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
    {
        ThrowIfDisposed();
        
        if (healthChecks == null)
            throw new ArgumentNullException(nameof(healthChecks));

        foreach (var (healthCheck, config) in healthChecks)
        {
            RegisterHealthCheck(healthCheck, config);
        }
        
        _logger.LogInfo("Bulk registered {Count} health checks", healthChecks.Count);
    }

    /// <inheritdoc />
    public bool UnregisterHealthCheck(FixedString64Bytes name)
    {
        ThrowIfDisposed();
        
        if (name.IsEmpty)
            return false;
        
        var removed = _registeredHealthChecks.TryRemove(name, out var healthCheck);
        if (removed)
        {
            _healthCheckConfigs.TryRemove(name, out _);
            _lastResults.TryRemove(name, out _);
            
            _logger.LogDebug("Unregistered health check: {HealthCheckName}", name);
        }
        
        return removed;
    }

    /// <inheritdoc />
    public async UniTask<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (name.IsEmpty)
            throw new ArgumentException("Health check name cannot be null or empty", nameof(name));

        if (!_registeredHealthChecks.TryGetValue(name, out var healthCheck))
        {
            throw new InvalidOperationException($"Health check '{name}' is not registered");
        }

        using (_executeHealthCheckMarker.Auto())
        {
            return await ExecuteHealthCheckInternalAsync(healthCheck, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async UniTask<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        using (_executeAllHealthChecksMarker.Auto())
        {
            var results = new Dictionary<string, HealthCheckResult>();
            var healthChecks = _registeredHealthChecks.Values.ToArray();
            
            // Execute health checks with concurrency limit
            var semaphore = new SemaphoreSlim(_config.MaxConcurrentHealthChecks, _config.MaxConcurrentHealthChecks);
            var tasks = healthChecks.AsValueEnumerable().Select(async healthCheck =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await ExecuteHealthCheckInternalAsync(healthCheck, cancellationToken);
                    return (healthCheck.Name.ToString(), result);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            var completedResults = await UniTask.WhenAll(tasks);
            
            foreach (var (name, result) in completedResults)
            {
                results[name] = result;
            }
            
            // Update overall status based on results
            await UpdateOverallHealthStatusAsync(results);

            _logger.LogDebug("Executed {Count} health checks", results.Count);

            // Return HealthReport instead of raw dictionary
            return new HealthReport
            {
                OverallStatus = _overallStatus,
                CheckResults = results,
                GeneratedAt = DateTime.UtcNow,
                TotalChecks = _registeredHealthChecks.Count,
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
        
        var lastResults = _lastResults.AsValueEnumerable()
            .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);

        return new HealthReport
        {
            OverallStatus = _overallStatus,
            CheckResults = lastResults,
            GeneratedAt = DateTime.UtcNow,
            TotalChecks = _registeredHealthChecks.Count,
            HealthyChecks = lastResults.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Healthy),
            UnhealthyChecks = lastResults.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Unhealthy),
            DegradedChecks = lastResults.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Degraded)
        };
    }

    /// <inheritdoc />
    public bool IsHealthCheckRegistered(string name)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(name))
            return false;

        return _registeredHealthChecks.ContainsKey(new FixedString64Bytes(name));
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

        // Convert OverallHealthStatus to HealthStatus
        return _overallStatus switch
        {
            OverallHealthStatus.Healthy => HealthStatus.Healthy,
            OverallHealthStatus.Warning => HealthStatus.Warning,
            OverallHealthStatus.Degraded => HealthStatus.Degraded,
            OverallHealthStatus.Unhealthy => HealthStatus.Unhealthy,
            _ => HealthStatus.Unknown
        };
    }

    /// <inheritdoc />
    public DegradationLevel GetCurrentDegradationLevel()
    {
        ThrowIfDisposed();

        // For now return based on overall health status
        // In a full implementation this would come from the degradation manager
        return _overallStatus switch
        {
            OverallHealthStatus.Healthy => DegradationLevel.None,
            OverallHealthStatus.Warning => DegradationLevel.Minor,
            OverallHealthStatus.Degraded => DegradationLevel.Moderate,
            OverallHealthStatus.Unhealthy => DegradationLevel.Severe,
            _ => DegradationLevel.None
        };
    }

    /// <inheritdoc />
    public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
    {
        ThrowIfDisposed();

        // For now return Closed - in full implementation this would query the circuit breaker manager
        return CircuitBreakerState.Closed;
    }

    /// <inheritdoc />
    public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
    {
        ThrowIfDisposed();

        // For now return empty dictionary - in full implementation this would query the circuit breaker manager
        return new Dictionary<FixedString64Bytes, CircuitBreakerState>();
    }

    /// <inheritdoc />
    public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
    {
        ThrowIfDisposed();

        // For now return just the last result if available
        if (_lastResults.TryGetValue(name, out var lastResult))
        {
            return new List<HealthCheckResult> { lastResult };
        }

        return new List<HealthCheckResult>();
    }

    /// <inheritdoc />
    public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
    {
        ThrowIfDisposed();

        return _registeredHealthChecks.Keys.AsValueEnumerable().ToList();
    }

    /// <inheritdoc />
    public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
    {
        ThrowIfDisposed();

        if (!_registeredHealthChecks.TryGetValue(name, out var healthCheck))
        {
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>
        {
            ["Name"] = healthCheck.Name.ToString(),
            ["Category"] = healthCheck.Category.ToString(),
            ["Registered"] = DateTime.UtcNow // In full implementation this would be the actual registration time
        };
    }

    /// <inheritdoc />
    public void StartAutomaticChecks()
    {
        ThrowIfDisposed();

        // For now use the existing Start method
        Start();
    }

    /// <inheritdoc />
    public void StopAutomaticChecks()
    {
        ThrowIfDisposed();

        // For now use the existing Stop method
        Stop();
    }

    /// <inheritdoc />
    public bool IsAutomaticChecksRunning()
    {
        ThrowIfDisposed();

        return _isRunning;
    }

    /// <inheritdoc />
    public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
    {
        ThrowIfDisposed();

        _logger.LogWarning("Circuit breaker force open requested for {Operation}: {Reason}", operationName, reason);
        // In full implementation this would interact with the circuit breaker manager
    }

    /// <inheritdoc />
    public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
    {
        ThrowIfDisposed();

        _logger.LogInfo("Circuit breaker force closed requested for {Operation}: {Reason}", operationName, reason);
        // In full implementation this would interact with the circuit breaker manager
    }

    /// <inheritdoc />
    public void SetDegradationLevel(DegradationLevel level, string reason)
    {
        ThrowIfDisposed();

        _logger.LogInfo("Degradation level change requested to {Level}: {Reason}", level, reason);
        // In full implementation this would interact with the degradation manager
    }

    /// <inheritdoc />
    public HealthStatistics GetHealthStatistics()
    {
        ThrowIfDisposed();

        // Create basic statistics based on current state
        var healthyCount = _lastResults.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Healthy);
        var unhealthyCount = _lastResults.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Unhealthy);
        var degradedCount = _lastResults.Values.AsValueEnumerable().Count(r => r.Status == HealthStatus.Degraded);

        return new HealthStatistics
        {
            TotalHealthChecks = _registeredHealthChecks.Count,
            HealthyChecks = healthyCount,
            UnhealthyChecks = unhealthyCount,
            DegradedChecks = degradedCount,
            OverallStatus = _overallStatus,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public bool IsHealthCheckEnabled(FixedString64Bytes name)
    {
        ThrowIfDisposed();

        // For now assume all registered health checks are enabled
        return _registeredHealthChecks.ContainsKey(name);
    }

    /// <inheritdoc />
    public void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
    {
        ThrowIfDisposed();

        _logger.LogInfo("Health check {Name} enable state change requested to {Enabled}", name, enabled);
        // In full implementation this would track enabled/disabled state
    }

    #endregion

    #region Orchestration Methods

    /// <summary>
    /// Initializes the service with all core domain health checks.
    /// This is the primary method for setting up the orchestration.
    /// </summary>
    /// <returns>Task representing the initialization operation</returns>
    public async UniTask InitializeWithCoreDomainsAsync()
    {
        using (_orchestrationMarker.Auto())
        {
            try
            {
                _logger.LogInfo("Initializing HealthCheckService with core domain health checks");

                // Register all core domain health checks using extension method
                _registrationManager.Dispose(); // Clean up the default one
                var newManager = this.RegisterAllCoreDomainHealthChecks(_logger, _config);

                // Store reference to the new manager (we'll need to dispose it later)
                // Note: In a real implementation, you'd want to properly manage this lifecycle

                _isRunning = true;
                _logger.LogInfo("HealthCheckService initialization complete. Registered {Count} health checks", 
                    _registeredHealthChecks.Count);

                // Perform initial health check
                await ExecuteAllHealthChecksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize HealthCheckService with core domains");
                throw;
            }
        }
    }

    /// <summary>
    /// Starts the health check service with automatic monitoring
    /// </summary>
    public void Start()
    {
        ThrowIfDisposed();

        if (_isRunning)
        {
            _logger.LogWarning("HealthCheckService is already running");
            return;
        }

        _isRunning = true;

        // Start automatic health checks if enabled
        if (_config.EnableAutomaticChecks)
        {
            _automaticCheckCancellationSource?.Cancel();
            _automaticCheckCancellationSource?.Dispose();
            _automaticCheckCancellationSource = new CancellationTokenSource();

            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                _serviceCancellationSource.Token,
                _automaticCheckCancellationSource.Token).Token;

            _automaticCheckTask = ExecuteAutomaticHealthChecksLoopAsync(combinedToken);
        }

        _logger.LogInfo("HealthCheckService started in orchestration mode");
    }

    /// <summary>
    /// Stops the health check service
    /// </summary>
    public void Stop()
    {
        ThrowIfDisposed();

        if (!_isRunning)
        {
            _logger.LogWarning("HealthCheckService is not running");
            return;
        }

        _isRunning = false;

        // Stop automatic health checks
        _automaticCheckCancellationSource?.Cancel();

        _logger.LogInfo("HealthCheckService stopped");
    }

    #endregion

    #region Private Methods

    private async UniTask<HealthCheckResult> ExecuteHealthCheckInternalAsync(
        IHealthCheck healthCheck, 
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        HealthCheckResult result;

        try
        {
            // Get timeout from configuration or use default
            var timeout = _healthCheckConfigs.TryGetValue(healthCheck.Name, out var config) 
                ? config.Timeout 
                : _config.DefaultTimeout;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            result = await healthCheck.CheckAsync(timeoutCts.Token);
            result = result with { Duration = stopwatch.Elapsed };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = HealthCheckResult.Unhealthy(
                $"Health check '{healthCheck.Name}' was cancelled",
                duration: stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            result = HealthCheckResult.Unhealthy(
                $"Health check '{healthCheck.Name}' timed out after {stopwatch.Elapsed}",
                duration: stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            result = HealthCheckResult.Unhealthy(
                $"Health check '{healthCheck.Name}' threw an exception: {ex.Message}",
                ex,
                stopwatch.Elapsed);
        }
        finally
        {
            stopwatch.Stop();
        }

        // Store the result
        _lastResults.TryAdd(healthCheck.Name, result);
        _lastResults.TryUpdate(healthCheck.Name, result, _lastResults[healthCheck.Name]);

        // Publish individual health check completion message
        try
        {
            var completionMessage = HealthCheckCompletedMessage.Create(
                healthCheckName: healthCheck.Name.ToString(),
                status: result.Status,
                duration: result.Duration,
                source: "HealthCheckService",
                correlationId: _serviceId);
                
            _messageBus.PublishMessage(completionMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish health check completion message for {HealthCheckName}", healthCheck.Name);
        }

        // Profile slow health checks using IProfilerService
        if (_config.EnableProfiling && stopwatch.ElapsedMilliseconds > _config.SlowHealthCheckThreshold)
        {
            _logger.LogWarning("Slow health check detected: {HealthCheckName} took {Duration}ms", 
                healthCheck.Name, stopwatch.ElapsedMilliseconds);
            
            // Record performance metrics with profiler service
            _profilerService.RecordCustomMetric(
                $"HealthCheck.{healthCheck.Name}.ExecutionTime", 
                (double)stopwatch.ElapsedMilliseconds);
        }
        else if (_config.EnableProfiling)
        {
            // Record all health check execution times for analysis
            _profilerService.RecordCustomMetric(
                $"HealthCheck.{healthCheck.Name}.ExecutionTime", 
                (double)stopwatch.ElapsedMilliseconds);
        }

        return result;
    }

    private async UniTask UpdateOverallHealthStatusAsync(Dictionary<string, HealthCheckResult> results)
    {
        var previousStatus = _overallStatus;
        
        // Determine overall status based on individual results
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

            // Health status changes are published via message bus below

            // Publish health status change message to message bus
            try
            {
                var healthScore = CalculateOverallHealthScore(results);
                var healthStatusMessage = HealthCheckStatusChangedMessage.Create(
                    oldStatus: ConvertToHealthStatus(previousStatus),
                    newStatus: ConvertToHealthStatus(_overallStatus),
                    overallHealthScore: healthScore,
                    source: "HealthCheckService",
                    correlationId: _serviceId);
                    
                _messageBus.PublishMessage(healthStatusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish health status change message");
            }

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

    private async UniTask ExecuteAutomaticHealthChecksLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning && !_isDisposed)
        {
            try
            {
                await ExecuteAllHealthChecksAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic health check execution");
            }

            try
            {
                await UniTask.Delay(_config.AutomaticCheckInterval, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when cancellation is requested
                break;
            }
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
            _automaticCheckCancellationSource?.Cancel();
            _automaticCheckCancellationSource?.Dispose();
            _registrationManager?.Dispose();
            _serviceCancellationSource?.Dispose();

            _registeredHealthChecks.Clear();
            _healthCheckConfigs.Clear();
            _lastResults.Clear();

            _executeHealthCheckMarker.Dispose();
            _executeAllHealthChecksMarker.Dispose();
            _orchestrationMarker.Dispose();
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