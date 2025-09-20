using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Consolidated implementation of health check operations combining execution and scheduling.
    /// Uses IProfilerService directly for performance tracking per CLAUDE.md patterns.
    /// Does not re-implement functionality available in core systems.
    /// </summary>
    public sealed class HealthCheckOperationService : IHealthCheckOperationService
    {
        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;
        private readonly HealthCheckServiceConfig _config;
        private readonly Guid _serviceId;

        // Scheduling state
        private CancellationTokenSource _scheduleCts;
        private UniTask? _schedulingTask;
        private DateTime? _nextExecutionTime;
        private DateTime? _lastExecutionTime;
        private TimeSpan _currentInterval;
        private Func<CancellationToken, UniTask> _executeCallback;
        private readonly object _stateLock = new();

        private bool _isRunning;
        private bool _isPaused;
        private bool _disposed;

        /// <inheritdoc />
        public bool IsRunning => _isRunning && !_disposed;

        /// <inheritdoc />
        public bool IsPaused => _isPaused;

        /// <inheritdoc />
        public DateTime? NextExecutionTime => _nextExecutionTime;

        /// <inheritdoc />
        public DateTime? LastExecutionTime => _lastExecutionTime;

        /// <summary>
        /// Initializes a new instance of the HealthCheckOperationService.
        /// </summary>
        /// <param name="logger">Logging service for operation tracking</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="config">Health check service configuration</param>
        public HealthCheckOperationService(
            ILoggingService logger,
            IProfilerService profilerService,
            HealthCheckServiceConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckOperationService");

            _logger.LogDebug("HealthCheckOperationService initialized with ID: {ServiceId}", _serviceId, "HealthCheckOperationService");
        }

        /// <inheritdoc />
        public async UniTask<HealthCheckResult> ExecuteHealthCheckAsync(
            IHealthCheck healthCheck,
            HealthCheckConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Use IProfilerService directly for performance tracking
            var tag = ProfilerTag.CreateMethodTag("HealthCheck", healthCheck.Name.ToString());
            using (_profilerService.BeginScope(tag))
            {
                var stopwatch = Stopwatch.StartNew();
                HealthCheckResult result;

                try
                {
                    var timeout = configuration.Timeout == TimeSpan.Zero ? _config.DefaultTimeout : configuration.Timeout;
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(timeout);

                    // Execute the health check
                    result = await healthCheck.CheckHealthAsync(timeoutCts.Token);
                    result = result with { Duration = stopwatch.Elapsed };

                    // Record metrics directly in IProfilerService
                    _profilerService.RecordSample(tag, (float)stopwatch.ElapsedMilliseconds, "ms");
                    _profilerService.IncrementCounter("healthcheck.executions.success");

                    _logger.LogDebug($"Health check '{healthCheck.Name}' completed with status {result.Status} in {stopwatch.ElapsedMilliseconds}ms", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    result = HealthCheckResult.Unhealthy(
                        healthCheck.Name.ToString(),
                        $"Health check '{healthCheck.Name}' was cancelled",
                        duration: stopwatch.Elapsed);

                    _profilerService.IncrementCounter("healthcheck.executions.cancelled");

                    _logger.LogWarning($"Health check '{healthCheck.Name}' was cancelled after {stopwatch.ElapsedMilliseconds}ms", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                }
                catch (OperationCanceledException)
                {
                    result = HealthCheckResult.Unhealthy(
                        healthCheck.Name.ToString(),
                        $"Health check '{healthCheck.Name}' timed out after {stopwatch.Elapsed}",
                        duration: stopwatch.Elapsed);

                    _profilerService.IncrementCounter("healthcheck.executions.timeout");

                    _logger.LogWarning($"Health check '{healthCheck.Name}' timed out after {stopwatch.ElapsedMilliseconds}ms", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                }
                catch (Exception ex)
                {
                    result = HealthCheckResult.Unhealthy(
                        healthCheck.Name.ToString(),
                        $"Health check '{healthCheck.Name}' threw exception: {ex.Message}",
                        duration: stopwatch.Elapsed,
                        exception: ex);

                    _profilerService.IncrementCounter("healthcheck.executions.failed");

                    _logger.LogException($"Health check '{healthCheck.Name}' failed with exception after {stopwatch.ElapsedMilliseconds}ms", ex, correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                }
                finally
                {
                    stopwatch.Stop();

                    // Check for slow execution
                    if (_config.EnableProfiling && stopwatch.ElapsedMilliseconds > _config.SlowHealthCheckThreshold)
                    {
                        _profilerService.IncrementCounter("healthcheck.executions.slow");
                        _logger.LogWarning($"Slow health check detected: {healthCheck.Name} took {stopwatch.ElapsedMilliseconds}ms", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                    }
                }

                return result;
            }
        }

        /// <inheritdoc />
        public async UniTask<Dictionary<string, HealthCheckResult>> ExecuteBatchAsync(
            Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks,
            int maxConcurrency,
            CancellationToken cancellationToken = default)
        {
            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));
            if (maxConcurrency < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Must be at least 1");

            // Use IProfilerService directly for batch operation tracking
            using (_profilerService.BeginScope("HealthCheck.Batch"))
            {
                var results = new Dictionary<string, HealthCheckResult>();
                var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

                // Use ZLinq for zero-allocation operations
                var tasks = healthChecks.AsValueEnumerable().Select(kvp =>
                {
                    var (healthCheck, configuration) = kvp;
                    return ProcessHealthCheckAsync(healthCheck, configuration, semaphore, cancellationToken);
                }).ToArray();

                var completedResults = await UniTask.WhenAll(tasks);

                foreach (var (name, result) in completedResults)
                {
                    results[name] = result;
                }

                async UniTask<(string name, HealthCheckResult result)> ProcessHealthCheckAsync(
                    IHealthCheck healthCheck,
                    HealthCheckConfiguration configuration,
                    SemaphoreSlim semaphore,
                    CancellationToken ct)
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var result = await ExecuteHealthCheckAsync(healthCheck, configuration, ct);
                        return (healthCheck.Name.ToString(), result);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                // Record batch metrics in IProfilerService
                _profilerService.RecordMetric("healthcheck.batch.size", results.Count);
                _profilerService.RecordMetric("healthcheck.batch.concurrency", maxConcurrency);

                _logger.LogDebug($"Executed {results.Count} health checks with max concurrency {maxConcurrency}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");

                return results;
            }
        }

        /// <inheritdoc />
        public async UniTask<HealthCheckResult> ExecuteWithRetryAsync(
            IHealthCheck healthCheck,
            HealthCheckConfiguration configuration,
            int maxRetries,
            TimeSpan retryDelay,
            CancellationToken cancellationToken = default)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Must be non-negative");

            var tag = ProfilerTag.CreateMethodTag("HealthCheck.Retry", healthCheck.Name.ToString());
            using (_profilerService.BeginScope(tag))
            {
                HealthCheckResult result = null;
                Exception lastException = null;

                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    if (attempt > 0)
                    {
                        _logger.LogDebug($"Retrying health check '{healthCheck.Name}', attempt {attempt}/{maxRetries}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");

                        await UniTask.Delay(retryDelay, cancellationToken: cancellationToken);
                        _profilerService.IncrementCounter("healthcheck.retries");
                    }

                    try
                    {
                        result = await ExecuteHealthCheckAsync(healthCheck, configuration, cancellationToken);

                        // If healthy or degraded, consider it successful
                        if (result.Status == HealthStatus.Healthy ||
                            result.Status == HealthStatus.Degraded ||
                            result.Status == HealthStatus.Warning)
                        {
                            if (attempt > 0)
                            {
                                _logger.LogInfo($"Health check '{healthCheck.Name}' succeeded after {attempt + 1} attempts", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                            }
                            return result;
                        }

                        lastException = result.Exception;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogException($"Health check '{healthCheck.Name}' failed on attempt {attempt + 1}", ex, correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                    }
                }

                // All retries exhausted
                _profilerService.IncrementCounter("healthcheck.retries.exhausted");
                _logger.LogError($"Health check '{healthCheck.Name}' failed after {maxRetries + 1} retries", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");

                return result ?? HealthCheckResult.Unhealthy(
                    healthCheck.Name.ToString(),
                    $"Health check '{healthCheck.Name}' failed after {maxRetries + 1} attempts",
                    exception: lastException);
            }
        }

        /// <inheritdoc />
        public async UniTask StartScheduledExecutionAsync(
            TimeSpan interval,
            Func<CancellationToken, UniTask> executeCallback,
            CancellationToken cancellationToken = default)
        {
            if (executeCallback == null)
                throw new ArgumentNullException(nameof(executeCallback));
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive");

            lock (_stateLock)
            {
                if (_isRunning)
                    throw new InvalidOperationException("Scheduled execution is already running");

                _isRunning = true;
                _isPaused = false;
                _currentInterval = interval;
                _executeCallback = executeCallback;
                _scheduleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            _logger.LogInfo($"Starting scheduled health check execution with interval: {interval}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");

            // Start the scheduling loop
            _schedulingTask = RunSchedulingLoopAsync(_scheduleCts.Token);
            await _schedulingTask.Value.SuppressCancellationThrow();
        }

        /// <inheritdoc />
        public async UniTask StopScheduledExecutionAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _isPaused = false;
                _scheduleCts?.Cancel();
            }

            if (_schedulingTask.HasValue)
            {
                await _schedulingTask.Value.SuppressCancellationThrow();
            }

            _logger.LogInfo("Stopped scheduled health check execution", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
        }

        /// <inheritdoc />
        public void PauseScheduledExecution()
        {
            lock (_stateLock)
            {
                if (!_isRunning || _isPaused)
                    return;

                _isPaused = true;
            }

            _logger.LogInfo("Paused scheduled health check execution", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
        }

        /// <inheritdoc />
        public void ResumeScheduledExecution()
        {
            lock (_stateLock)
            {
                if (!_isRunning || !_isPaused)
                    return;

                _isPaused = false;
            }

            _logger.LogInfo("Resumed scheduled health check execution", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
        }

        /// <inheritdoc />
        public async UniTask TriggerManualExecutionAsync(CancellationToken cancellationToken = default)
        {
            if (_executeCallback == null)
                throw new InvalidOperationException("No execution callback configured");

            _profilerService.IncrementCounter("healthcheck.executions.manual");

            using (_profilerService.BeginScope("HealthCheck.ManualExecution"))
            {
                await _executeCallback(cancellationToken);
            }

            _logger.LogInfo("Manual health check execution completed", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
        }

        /// <inheritdoc />
        public void UpdateScheduleInterval(TimeSpan newInterval)
        {
            if (newInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(newInterval), "Interval must be positive");

            lock (_stateLock)
            {
                _currentInterval = newInterval;
                if (_nextExecutionTime.HasValue)
                {
                    _nextExecutionTime = DateTime.UtcNow.Add(newInterval);
                }
            }

            _logger.LogInfo($"Updated schedule interval to: {newInterval}", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
        }

        /// <inheritdoc />
        public bool CanExecute(IHealthCheck healthCheck, HealthCheckConfiguration configuration)
        {
            if (healthCheck == null || configuration == null)
                return false;

            // Check if the health check is enabled
            if (!configuration.Enabled)
            {
                _logger.LogDebug($"Health check '{healthCheck.Name}' is disabled", correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetOperationStatistics()
        {
            // Get statistics directly from IProfilerService instead of duplicating
            var profilerStats = _profilerService.GetStatistics();
            var healthCheckMetrics = _profilerService.GetAllMetrics()
                .AsValueEnumerable()
                .Where(kvp => kvp.Key.StartsWith("HealthCheck"))
                .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

            return new Dictionary<string, object>
            {
                ["IsRunning"] = _isRunning,
                ["IsPaused"] = _isPaused,
                ["NextExecutionTime"] = _nextExecutionTime,
                ["LastExecutionTime"] = _lastExecutionTime,
                ["CurrentInterval"] = _currentInterval.TotalSeconds,
                ["ProfilerMetrics"] = healthCheckMetrics
            };
        }

        private async UniTask RunSchedulingLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    _nextExecutionTime = DateTime.UtcNow.Add(_currentInterval);

                    await UniTask.Delay(_currentInterval, cancellationToken: cancellationToken);

                    if (_isPaused)
                    {
                        await UniTask.WaitWhile(() => _isPaused, cancellationToken: cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested && _isRunning && !_isPaused)
                    {
                        _lastExecutionTime = DateTime.UtcNow;

                        using (_profilerService.BeginScope("HealthCheck.ScheduledExecution"))
                        {
                            await _executeCallback(cancellationToken);
                        }

                        _profilerService.IncrementCounter("healthcheck.executions.scheduled");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogException("Error in scheduled health check execution", ex, correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
                    _profilerService.IncrementCounter("healthcheck.executions.scheduled.failed");
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger.LogInfo("Disposing HealthCheckOperationService: {ServiceId}", _serviceId, "HealthCheckOperationService");

                _isRunning = false;
                _scheduleCts?.Cancel();
                _scheduleCts?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException("Error during HealthCheckOperationService disposal", ex, correlationId: default(FixedString64Bytes), sourceContext: "HealthCheckOperationService");
            }
            finally
            {
                _disposed = true;
                _logger.LogDebug("HealthCheckOperationService disposed: {ServiceId}", _serviceId, "HealthCheckOperationService");
            }
        }
    }
}