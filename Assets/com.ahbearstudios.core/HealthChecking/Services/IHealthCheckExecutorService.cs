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
    /// Specialized service responsible for executing health checks.
    /// Handles individual and batch health check execution with proper timeout and cancellation support.
    /// Follows CLAUDE.md patterns for specialized service delegation.
    /// </summary>
    public interface IHealthCheckExecutorService
    {
        /// <summary>
        /// Executes a specific health check with timeout and cancellation support.
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
        /// </summary>
        /// <param name="healthChecks">Dictionary of health checks with their configurations</param>
        /// <param name="maxConcurrency">Maximum number of concurrent executions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of health check results keyed by check name</returns>
        UniTask<Dictionary<string, HealthCheckResult>> ExecuteHealthChecksAsync(
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
        /// Validates if a health check can be executed based on current conditions.
        /// </summary>
        /// <param name="healthCheck">The health check to validate</param>
        /// <param name="configuration">Configuration for the health check</param>
        /// <returns>True if the health check can be executed, false otherwise</returns>
        bool CanExecuteHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration configuration);

        /// <summary>
        /// Gets execution statistics for performance monitoring.
        /// </summary>
        /// <returns>Dictionary of execution metrics</returns>
        Dictionary<string, object> GetExecutionStatistics();

        /// <summary>
        /// Clears execution statistics and resets counters.
        /// </summary>
        void ResetExecutionStatistics();
    }
}