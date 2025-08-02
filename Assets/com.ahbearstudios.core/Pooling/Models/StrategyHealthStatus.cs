using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Represents the health status of a pooling strategy.
    /// Provides detailed information about strategy performance and potential issues.
    /// </summary>
    public sealed class StrategyHealthStatus
    {
        /// <summary>
        /// Overall health status of the strategy.
        /// </summary>
        public StrategyHealth Status { get; init; }

        /// <summary>
        /// Human-readable description of the current health status.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Timestamp when this health status was generated.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Performance metrics for the strategy.
        /// </summary>
        public Dictionary<string, object> Metrics { get; init; } = new();

        /// <summary>
        /// List of current warnings (non-critical issues).
        /// </summary>
        public List<string> Warnings { get; init; } = new();

        /// <summary>
        /// List of current errors (critical issues).
        /// </summary>
        public List<string> Errors { get; init; } = new();

        /// <summary>
        /// Exception that caused the health issue, if any.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Whether the circuit breaker is currently open.
        /// </summary>
        public bool IsCircuitBreakerOpen { get; init; }

        /// <summary>
        /// Number of recent operations that exceeded performance budget.
        /// </summary>
        public int PerformanceBudgetViolations { get; init; }

        /// <summary>
        /// Average operation time over recent operations.
        /// </summary>
        public TimeSpan AverageOperationTime { get; init; }

        /// <summary>
        /// Maximum operation time recorded recently.
        /// </summary>
        public TimeSpan MaxOperationTime { get; init; }

        /// <summary>
        /// Number of operations processed since last health check.
        /// </summary>
        public long OperationCount { get; init; }

        /// <summary>
        /// Number of errors encountered since last health check.
        /// </summary>
        public long ErrorCount { get; init; }

        /// <summary>
        /// Creates a healthy status.
        /// </summary>
        /// <param name="description">Description of the healthy state</param>
        /// <returns>Healthy strategy status</returns>
        public static StrategyHealthStatus Healthy(string description = "Strategy operating normally")
        {
            return new StrategyHealthStatus
            {
                Status = StrategyHealth.Healthy,
                Description = description,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a degraded status with warnings.
        /// </summary>
        /// <param name="description">Description of the degraded state</param>
        /// <param name="warnings">List of warnings</param>
        /// <returns>Degraded strategy status</returns>
        public static StrategyHealthStatus Degraded(string description, params string[] warnings)
        {
            return new StrategyHealthStatus
            {
                Status = StrategyHealth.Degraded,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Warnings = new List<string>(warnings)
            };
        }

        /// <summary>
        /// Creates an unhealthy status with errors.
        /// </summary>
        /// <param name="description">Description of the unhealthy state</param>
        /// <param name="errors">List of errors</param>
        /// <param name="exception">Exception that caused the issue</param>
        /// <returns>Unhealthy strategy status</returns>
        public static StrategyHealthStatus Unhealthy(string description, string[] errors = null, Exception exception = null)
        {
            return new StrategyHealthStatus
            {
                Status = StrategyHealth.Unhealthy,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Errors = new List<string>(errors ?? Array.Empty<string>()),
                Exception = exception
            };
        }

        /// <summary>
        /// Creates a circuit breaker open status.
        /// </summary>
        /// <param name="description">Description of why circuit breaker opened</param>
        /// <returns>Circuit breaker open status</returns>
        public static StrategyHealthStatus CircuitBreakerOpen(string description)
        {
            return new StrategyHealthStatus
            {
                Status = StrategyHealth.CircuitBreakerOpen,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IsCircuitBreakerOpen = true
            };
        }

        /// <summary>
        /// Gets whether the strategy is in a healthy state.
        /// </summary>
        public bool IsHealthy => Status == StrategyHealth.Healthy;

        /// <summary>
        /// Gets whether the strategy has any issues (warnings or errors).
        /// </summary>
        public bool HasIssues => Warnings.Count > 0 || Errors.Count > 0 || Exception != null;

        /// <summary>
        /// Gets whether the strategy is in a critical state requiring immediate attention.
        /// </summary>
        public bool IsCritical => Status == StrategyHealth.Unhealthy || Status == StrategyHealth.CircuitBreakerOpen;
    }

    /// <summary>
    /// Enumeration of possible strategy health states.
    /// </summary>
    public enum StrategyHealth
    {
        /// <summary>
        /// Strategy is operating normally with no issues.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// Strategy is operating but with some performance degradation or warnings.
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// Strategy is not operating properly and has critical issues.
        /// </summary>
        Unhealthy = 2,

        /// <summary>
        /// Circuit breaker is open, strategy is temporarily disabled.
        /// </summary>
        CircuitBreakerOpen = 3,

        /// <summary>
        /// Strategy health status is unknown.
        /// </summary>
        Unknown = 4
    }
}