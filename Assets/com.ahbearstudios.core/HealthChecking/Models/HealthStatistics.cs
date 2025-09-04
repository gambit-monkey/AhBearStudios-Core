using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Comprehensive statistics for the health check system.
    /// Provides detailed metrics for monitoring and optimization.
    /// </summary>
    public sealed record HealthStatistics
    {
        /// <summary>
        /// Gets the total time the health check service has been running.
        /// </summary>
        public TimeSpan ServiceUptime { get; init; }

        /// <summary>
        /// Gets the total number of health checks executed.
        /// </summary>
        public long TotalHealthChecks { get; init; }

        /// <summary>
        /// Gets the total number of successful health checks.
        /// </summary>
        public long SuccessfulHealthChecks { get; init; }

        /// <summary>
        /// Gets the total number of failed health checks.
        /// </summary>
        public long FailedHealthChecks { get; init; }

        /// <summary>
        /// Gets the number of currently registered health checks.
        /// </summary>
        public int RegisteredHealthCheckCount { get; init; }

        /// <summary>
        /// Gets the current system degradation level.
        /// </summary>
        public DegradationLevel CurrentDegradationLevel { get; init; }

        /// <summary>
        /// Gets the current overall health status.
        /// </summary>
        public HealthStatus LastOverallStatus { get; init; }

        /// <summary>
        /// Gets circuit breaker statistics for all registered circuit breakers.
        /// </summary>
        public IReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics> CircuitBreakerStatistics { get; init; } 
            = new Dictionary<FixedString64Bytes, CircuitBreakerStatistics>();

        /// <summary>
        /// Gets the average execution time for health checks.
        /// </summary>
        public TimeSpan AverageExecutionTime { get; init; }

        /// <summary>
        /// Gets the timestamp when statistics were last reset.
        /// </summary>
        public DateTime LastStatsReset { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the current error rate (0.0 to 1.0).
        /// </summary>
        public double ErrorRate => TotalHealthChecks > 0 ? (double)FailedHealthChecks / TotalHealthChecks : 0.0;

        /// <summary>
        /// Gets the current success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalHealthChecks > 0 ? (double)SuccessfulHealthChecks / TotalHealthChecks : 0.0;

        /// <summary>
        /// Gets the number of circuit breakers currently in open state.
        /// </summary>
        public int OpenCircuitBreakers { get; init; }

        /// <summary>
        /// Gets the total number of active circuit breakers.
        /// </summary>
        public int ActiveCircuitBreakers { get; init; }

        /// <summary>
        /// Creates a new HealthStatistics instance with the specified values.
        /// </summary>
        /// <param name="serviceUptime">Service uptime</param>
        /// <param name="totalHealthChecks">Total health checks executed</param>
        /// <param name="successfulHealthChecks">Successful health checks</param>
        /// <param name="failedHealthChecks">Failed health checks</param>
        /// <param name="registeredHealthCheckCount">Number of registered checks</param>
        /// <param name="currentDegradationLevel">Current degradation level</param>
        /// <param name="lastOverallStatus">Last overall status</param>
        /// <param name="circuitBreakerStatistics">Circuit breaker statistics</param>
        /// <param name="averageExecutionTime">Average execution time</param>
        /// <param name="openCircuitBreakers">Number of open circuit breakers</param>
        /// <param name="activeCircuitBreakers">Number of active circuit breakers</param>
        /// <returns>New HealthStatistics instance</returns>
        public static HealthStatistics Create(
            TimeSpan serviceUptime,
            long totalHealthChecks,
            long successfulHealthChecks,
            long failedHealthChecks,
            int registeredHealthCheckCount,
            DegradationLevel currentDegradationLevel,
            HealthStatus lastOverallStatus,
            Dictionary<FixedString64Bytes, CircuitBreakerStatistics> circuitBreakerStatistics = null,
            TimeSpan averageExecutionTime = default,
            int openCircuitBreakers = 0,
            int activeCircuitBreakers = 0)
        {
            return new HealthStatistics
            {
                ServiceUptime = serviceUptime,
                TotalHealthChecks = totalHealthChecks,
                SuccessfulHealthChecks = successfulHealthChecks,
                FailedHealthChecks = failedHealthChecks,
                RegisteredHealthCheckCount = registeredHealthCheckCount,
                CurrentDegradationLevel = currentDegradationLevel,
                LastOverallStatus = lastOverallStatus,
                CircuitBreakerStatistics = circuitBreakerStatistics != null ? 
                    new ReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics>(circuitBreakerStatistics) :
                    new ReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics>(new Dictionary<FixedString64Bytes, CircuitBreakerStatistics>()),
                AverageExecutionTime = averageExecutionTime,
                OpenCircuitBreakers = openCircuitBreakers,
                ActiveCircuitBreakers = activeCircuitBreakers
            };
        }

        /// <summary>
        /// Returns a string representation of the health statistics.
        /// </summary>
        /// <returns>Statistics summary string</returns>
        public override string ToString()
        {
            return $"HealthStats: {RegisteredHealthCheckCount} checks, {SuccessRate:P1} success rate, {CurrentDegradationLevel} degradation";
        }
    }

}