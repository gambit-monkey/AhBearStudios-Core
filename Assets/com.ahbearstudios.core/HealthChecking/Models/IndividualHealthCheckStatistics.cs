using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Individual health check statistics.
    /// </summary>
    public sealed record IndividualHealthCheckStatistics
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Gets the category of the health check.
        /// </summary>
        public HealthCheckCategory Category { get; init; }

        /// <summary>
        /// Gets the total number of executions.
        /// </summary>
        public long TotalExecutions { get; init; }

        /// <summary>
        /// Gets the total number of successful executions.
        /// </summary>
        public long SuccessfulExecutions { get; init; }

        /// <summary>
        /// Gets the total number of failed executions.
        /// </summary>
        public long FailedExecutions { get; init; }

        /// <summary>
        /// Gets the total number of timed out executions.
        /// </summary>
        public long TimedOutExecutions { get; init; }

        /// <summary>
        /// Gets the average execution time.
        /// </summary>
        public TimeSpan AverageExecutionTime { get; init; }

        /// <summary>
        /// Gets the minimum execution time.
        /// </summary>
        public TimeSpan MinimumExecutionTime { get; init; }

        /// <summary>
        /// Gets the maximum execution time.
        /// </summary>
        public TimeSpan MaximumExecutionTime { get; init; }

        /// <summary>
        /// Gets the last execution time.
        /// </summary>
        public TimeSpan LastExecutionTime { get; init; }

        /// <summary>
        /// Gets the current success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0.0;

        /// <summary>
        /// Gets the current failure rate (0.0 to 1.0).
        /// </summary>
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions : 0.0;

        /// <summary>
        /// Gets the timeout rate (0.0 to 1.0).
        /// </summary>
        public double TimeoutRate => TotalExecutions > 0 ? (double)TimedOutExecutions / TotalExecutions : 0.0;

        /// <summary>
        /// Gets the current status of the health check.
        /// </summary>
        public HealthStatus CurrentStatus { get; init; }

        /// <summary>
        /// Gets the timestamp of the last execution.
        /// </summary>
        public DateTime LastExecution { get; init; }

        /// <summary>
        /// Gets the timestamp of the last failure.
        /// </summary>
        public DateTime LastFailure { get; init; }

        /// <summary>
        /// Gets the last failure message.
        /// </summary>
        public string LastFailureMessage { get; init; }

        /// <summary>
        /// Gets whether the health check is currently enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets whether the health check has an associated circuit breaker.
        /// </summary>
        public bool HasCircuitBreaker { get; init; }

        /// <summary>
        /// Gets the first execution timestamp.
        /// </summary>
        public DateTime FirstExecution { get; init; }

        /// <summary>
        /// Gets additional metadata about the health check.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    }
}