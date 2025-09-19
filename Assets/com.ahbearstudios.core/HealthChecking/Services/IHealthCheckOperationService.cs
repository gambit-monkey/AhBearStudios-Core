using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Consolidated service responsible for health check execution operations.
    /// Combines execution and scheduling functionality following CLAUDE.md patterns.
    /// Uses IProfilerService directly for performance tracking instead of reimplementing.
    /// </summary>
    public interface IHealthCheckOperationService : IDisposable
    {
        /// <summary>
        /// Gets whether automatic health checks are currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets whether the service is paused.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Gets the next scheduled execution time.
        /// </summary>
        DateTime? NextExecutionTime { get; }

        /// <summary>
        /// Gets the last execution time.
        /// </summary>
        DateTime? LastExecutionTime { get; }

        /// <summary>
        /// Executes a specific health check with timeout and cancellation support.
        /// Uses IProfilerService directly for performance tracking.
        /// </summary>
        /// <param name="healthCheck">The health check to execute</param>
        /// <param name="configuration">Configuration for the health check execution</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result with execution details</returns>
        UniTask<HealthCheckResult> ExecuteHealthCheckAsync(
            IHealthCheck healthCheck,
            HealthCheckConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes multiple health checks concurrently with configurable parallelism.
        /// Uses IProfilerService directly for batch performance tracking.
        /// </summary>
        /// <param name="healthChecks">Dictionary of health checks with their configurations</param>
        /// <param name="maxConcurrency">Maximum number of concurrent executions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of health check results keyed by check name</returns>
        UniTask<Dictionary<string, HealthCheckResult>> ExecuteBatchAsync(
            Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks,
            int maxConcurrency,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a health check with retry logic on failure.
        /// </summary>
        /// <param name="healthCheck">The health check to execute</param>
        /// <param name="configuration">Configuration for the health check execution</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="retryDelay">Delay between retry attempts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result after retries</returns>
        UniTask<HealthCheckResult> ExecuteWithRetryAsync(
            IHealthCheck healthCheck,
            HealthCheckConfiguration configuration,
            int maxRetries,
            TimeSpan retryDelay,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts automatic health check execution on a schedule.
        /// </summary>
        /// <param name="interval">Execution interval</param>
        /// <param name="executeCallback">Callback to execute health checks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the scheduling operation</returns>
        UniTask StartScheduledExecutionAsync(
            TimeSpan interval,
            Func<CancellationToken, UniTask> executeCallback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops automatic health check execution.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the stop operation</returns>
        UniTask StopScheduledExecutionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses scheduled execution without stopping.
        /// </summary>
        void PauseScheduledExecution();

        /// <summary>
        /// Resumes paused scheduled execution.
        /// </summary>
        void ResumeScheduledExecution();

        /// <summary>
        /// Triggers an immediate execution outside of the schedule.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the manual execution</returns>
        UniTask TriggerManualExecutionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the scheduling interval dynamically.
        /// </summary>
        /// <param name="newInterval">New execution interval</param>
        void UpdateScheduleInterval(TimeSpan newInterval);

        /// <summary>
        /// Validates if a health check can be executed based on current conditions.
        /// </summary>
        /// <param name="healthCheck">The health check to validate</param>
        /// <param name="configuration">Configuration for the health check</param>
        /// <returns>True if the health check can be executed, false otherwise</returns>
        bool CanExecute(IHealthCheck healthCheck, HealthCheckConfiguration configuration);

        /// <summary>
        /// Gets operation statistics.
        /// Statistics are collected via IProfilerService, not duplicated here.
        /// </summary>
        /// <returns>Dictionary of operation metrics from IProfilerService</returns>
        Dictionary<string, object> GetOperationStatistics();
    }
}