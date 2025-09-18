using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health check executor service.
    /// Handles individual and batch health check execution with proper timeout and cancellation support.
    /// Uses ZLinq for zero-allocation operations and follows CLAUDE.md patterns.
    /// </summary>
    public sealed class HealthCheckExecutorService : IHealthCheckExecutorService
    {
        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;
        private readonly HealthCheckServiceConfig _config;
        private readonly Guid _serviceId;

        // Profiler markers
        private readonly ProfilerMarker _executeMarker = new ProfilerMarker("HealthCheckExecutor.Execute");
        private readonly ProfilerMarker _executeBatchMarker = new ProfilerMarker("HealthCheckExecutor.ExecuteBatch");
        private readonly ProfilerMarker _retryMarker = new ProfilerMarker("HealthCheckExecutor.Retry");

        // Execution statistics
        private long _totalExecutions;
        private long _successfulExecutions;
        private long _failedExecutions;
        private long _timedOutExecutions;
        private long _cancelledExecutions;
        private long _totalRetries;
        private TimeSpan _totalExecutionTime;
        private TimeSpan _averageExecutionTime;
        private readonly object _statsLock = new object();

        /// <summary>
        /// Initializes a new instance of the HealthCheckExecutorService.
        /// </summary>
        /// <param name="logger">Logging service for executor operations</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="config">Health check service configuration</param>
        public HealthCheckExecutorService(
            ILoggingService logger,
            IProfilerService profilerService,
            HealthCheckServiceConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckExecutorService");

            _logger.LogDebug("HealthCheckExecutorService initialized with ID: {ServiceId}", _serviceId);
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

            using (_executeMarker.Auto())
            {
                var stopwatch = Stopwatch.StartNew();
                HealthCheckResult result;

                try
                {
                    // Determine timeout from configuration or use default
                    var timeout = configuration.Timeout ?? _config.DefaultTimeout;

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(timeout);

                    // Execute the health check
                    result = await healthCheck.CheckAsync(timeoutCts.Token);
                    result = result with { Duration = stopwatch.Elapsed };

                    UpdateStatistics(true, false, false, stopwatch.Elapsed);

                    _logger.LogDebug("Health check '{Name}' completed with status {Status} in {Duration}ms",
                        healthCheck.Name, result.Status, stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    result = HealthCheckResult.Unhealthy(
                        $"Health check '{healthCheck.Name}' was cancelled",
                        duration: stopwatch.Elapsed);

                    UpdateStatistics(false, false, true, stopwatch.Elapsed);

                    _logger.LogWarning("Health check '{Name}' was cancelled after {Duration}ms",
                        healthCheck.Name, stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException)
                {
                    result = HealthCheckResult.Unhealthy(
                        $"Health check '{healthCheck.Name}' timed out after {Elapsed}",
                        duration: stopwatch.Elapsed);

                    UpdateStatistics(false, true, false, stopwatch.Elapsed);

                    _logger.LogWarning("Health check '{Name}' timed out after {Duration}ms",
                        healthCheck.Name, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    result = HealthCheckResult.Unhealthy(
                        $"Health check '{healthCheck.Name}' threw exception: {ex.Message}",
                        ex,
                        stopwatch.Elapsed);

                    UpdateStatistics(false, false, false, stopwatch.Elapsed);

                    _logger.LogError(ex, "Health check '{Name}' failed with exception after {Duration}ms",
                        healthCheck.Name, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    stopwatch.Stop();

                    // Record performance metrics if profiling is enabled
                    if (_config.EnableProfiling)
                    {
                        _profilerService.RecordCustomMetric(
                            $"HealthCheck.{healthCheck.Name}.ExecutionTime",
                            (double)stopwatch.ElapsedMilliseconds);

                        if (stopwatch.ElapsedMilliseconds > _config.SlowHealthCheckThreshold)
                        {
                            _logger.LogWarning("Slow health check detected: {Name} took {Duration}ms",
                                healthCheck.Name, stopwatch.ElapsedMilliseconds);
                        }
                    }
                }

                return result;
            }
        }

        /// <inheritdoc />
        public async UniTask<Dictionary<string, HealthCheckResult>> ExecuteHealthChecksAsync(
            Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks,
            int maxConcurrency,
            CancellationToken cancellationToken = default)
        {
            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));
            if (maxConcurrency < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Must be at least 1");

            using (_executeBatchMarker.Auto())
            {
                var results = new Dictionary<string, HealthCheckResult>();
                var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

                // Use ZLinq for zero-allocation operations
                var tasks = healthChecks.AsValueEnumerable().Select(async kvp =>
                {
                    var (healthCheck, configuration) = kvp;

                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var result = await ExecuteHealthCheckAsync(healthCheck, configuration, cancellationToken);
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

                _logger.LogDebug("Executed {Count} health checks with max concurrency {MaxConcurrency}",
                    results.Count, maxConcurrency);

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

            using (_retryMarker.Auto())
            {
                HealthCheckResult result = null;
                Exception lastException = null;

                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    if (attempt > 0)
                    {
                        _logger.LogDebug("Retrying health check '{Name}', attempt {Attempt}/{MaxRetries}",
                            healthCheck.Name, attempt, maxRetries);

                        await UniTask.Delay(retryDelay, cancellationToken: cancellationToken);
                        IncrementRetryCount();
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
                                _logger.LogInfo("Health check '{Name}' succeeded after {Attempts} attempts",
                                    healthCheck.Name, attempt + 1);
                            }
                            return result;
                        }

                        lastException = result.Exception;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Health check '{Name}' failed on attempt {Attempt}",
                            healthCheck.Name, attempt + 1);
                    }
                }

                // All retries exhausted
                _logger.LogError("Health check '{Name}' failed after {MaxRetries} retries",
                    healthCheck.Name, maxRetries + 1);

                return result ?? HealthCheckResult.Unhealthy(
                    $"Health check '{healthCheck.Name}' failed after {maxRetries + 1} attempts",
                    lastException);
            }
        }

        /// <inheritdoc />
        public bool CanExecuteHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration configuration)
        {
            if (healthCheck == null || configuration == null)
                return false;

            // Check if the health check is enabled
            if (!configuration.Enabled)
            {
                _logger.LogDebug("Health check '{Name}' is disabled", healthCheck.Name);
                return false;
            }

            // Additional validation can be added here
            // For example, checking circuit breaker state, resource availability, etc.

            return true;
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetExecutionStatistics()
        {
            lock (_statsLock)
            {
                return new Dictionary<string, object>
                {
                    ["TotalExecutions"] = _totalExecutions,
                    ["SuccessfulExecutions"] = _successfulExecutions,
                    ["FailedExecutions"] = _failedExecutions,
                    ["TimedOutExecutions"] = _timedOutExecutions,
                    ["CancelledExecutions"] = _cancelledExecutions,
                    ["TotalRetries"] = _totalRetries,
                    ["TotalExecutionTime"] = _totalExecutionTime.TotalMilliseconds,
                    ["AverageExecutionTime"] = _averageExecutionTime.TotalMilliseconds,
                    ["SuccessRate"] = _totalExecutions > 0
                        ? (double)_successfulExecutions / _totalExecutions
                        : 0.0
                };
            }
        }

        /// <inheritdoc />
        public void ResetExecutionStatistics()
        {
            lock (_statsLock)
            {
                _totalExecutions = 0;
                _successfulExecutions = 0;
                _failedExecutions = 0;
                _timedOutExecutions = 0;
                _cancelledExecutions = 0;
                _totalRetries = 0;
                _totalExecutionTime = TimeSpan.Zero;
                _averageExecutionTime = TimeSpan.Zero;

                _logger.LogInfo("Execution statistics reset for HealthCheckExecutorService");
            }
        }

        private void UpdateStatistics(bool success, bool timedOut, bool cancelled, TimeSpan duration)
        {
            lock (_statsLock)
            {
                _totalExecutions++;
                _totalExecutionTime += duration;

                if (success)
                    _successfulExecutions++;
                else if (timedOut)
                    _timedOutExecutions++;
                else if (cancelled)
                    _cancelledExecutions++;
                else
                    _failedExecutions++;

                _averageExecutionTime = TimeSpan.FromMilliseconds(
                    _totalExecutionTime.TotalMilliseconds / _totalExecutions);
            }
        }

        private void IncrementRetryCount()
        {
            lock (_statsLock)
            {
                _totalRetries++;
            }
        }
    }
}