using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs
{
    /// <summary>
    /// Stub implementation of IHealthCheckService for TDD testing.
    /// Returns configurable responses without implementing actual health check logic.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class StubHealthCheckService : IHealthCheckService
    {
        private readonly Dictionary<FixedString64Bytes, HealthCheckResult> _configuredResults = new();
        private readonly List<FixedString64Bytes> _registeredChecks = new();
        private readonly object _lockObject = new();

        #region Test Configuration Properties

        /// <summary>
        /// Gets or sets the overall health status returned by the stub.
        /// </summary>
        public HealthStatus OverallHealthStatus { get; set; } = HealthStatus.Healthy;

        /// <summary>
        /// Gets or sets the degradation level returned by the stub.
        /// </summary>
        public DegradationLevel CurrentDegradationLevel { get; set; } = DegradationLevel.None;

        /// <summary>
        /// Gets or sets whether automatic checks are running.
        /// </summary>
        public bool AutomaticChecksRunning { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the service is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets the count of registered health checks.
        /// </summary>
        public int RegisteredCheckCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _registeredChecks.Count;
                }
            }
        }

        /// <summary>
        /// Configures a specific result for a health check.
        /// </summary>
        public void ConfigureResult(FixedString64Bytes name, HealthCheckResult result)
        {
            lock (_lockObject)
            {
                _configuredResults[name] = result;
            }
        }

        /// <summary>
        /// Configures a healthy result for a health check.
        /// </summary>
        public void ConfigureHealthyResult(FixedString64Bytes name, string message = "Healthy")
        {
            ConfigureResult(name, HealthCheckResult.Healthy(name.ToString(), message));
        }

        /// <summary>
        /// Configures an unhealthy result for a health check.
        /// </summary>
        public void ConfigureUnhealthyResult(FixedString64Bytes name, string message = "Unhealthy")
        {
            ConfigureResult(name, HealthCheckResult.Unhealthy(name.ToString(), message));
        }

        /// <summary>
        /// Clears all configured results and registered checks.
        /// </summary>
        public void ClearConfiguration()
        {
            lock (_lockObject)
            {
                _configuredResults.Clear();
                _registeredChecks.Clear();
            }
        }

        #endregion

        #region IHealthCheckService Implementation - Stub Behavior

        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            lock (_lockObject)
            {
                if (_registeredChecks.Contains(healthCheck.Name))
                    throw new InvalidOperationException($"Health check with name '{healthCheck.Name}' is already registered");

                _registeredChecks.Add(healthCheck.Name);

                // If no specific result is configured, default to healthy
                if (!_configuredResults.ContainsKey(healthCheck.Name))
                {
                    _configuredResults[healthCheck.Name] = HealthCheckResult.Healthy(
                        name: healthCheck.Name.ToString(),
                        message: "Stub health check result");
                }
            }
        }

        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));

            foreach (var kvp in healthChecks)
            {
                RegisterHealthCheck(kvp.Key, kvp.Value);
            }
        }

        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            lock (_lockObject)
            {
                var removed = _registeredChecks.Remove(name);
                if (removed)
                {
                    _configuredResults.Remove(name);
                }
                return removed;
            }
        }

        public async UniTask<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            lock (_lockObject)
            {
                if (!_registeredChecks.Contains(name))
                    throw new ArgumentException($"Health check '{name}' is not registered", nameof(name));

                if (_configuredResults.TryGetValue(name, out var result))
                {
                    return result;
                }

                // Default to healthy if no specific result configured
                return HealthCheckResult.Healthy(
                    name: name.ToString(),
                    message: "Default stub health check result");
            }
        }

        public async UniTask<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            var results = new List<HealthCheckResult>();
            var startTime = DateTime.UtcNow;

            lock (_lockObject)
            {
                foreach (var checkName in _registeredChecks)
                {
                    if (_configuredResults.TryGetValue(checkName, out var result))
                    {
                        results.Add(result);
                    }
                    else
                    {
                        results.Add(HealthCheckResult.Healthy(
                            name: checkName.ToString(),
                            message: "Default stub health check result"));
                    }
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var overallStatus = DetermineOverallStatus(results);

            return HealthReport.Create(
                status: overallStatus,
                results: results,
                duration: duration,
                correlationId: Guid.NewGuid(),
                degradationLevel: CurrentDegradationLevel);
        }

        public async UniTask<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return OverallHealthStatus;
        }

        public DegradationLevel GetCurrentDegradationLevel()
        {
            return CurrentDegradationLevel;
        }

        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            return CircuitBreakerState.Closed; // Always closed for stub
        }

        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            return new Dictionary<FixedString64Bytes, CircuitBreakerState>();
        }

        public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
        {
            // Stub doesn't maintain history, return current result only
            lock (_lockObject)
            {
                if (_configuredResults.TryGetValue(name, out var result))
                {
                    return new List<HealthCheckResult> { result };
                }
                return new List<HealthCheckResult>();
            }
        }

        public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            lock (_lockObject)
            {
                return _registeredChecks.ToList();
            }
        }

        public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            return new Dictionary<string, object>
            {
                ["IsStub"] = true,
                ["HealthCheckName"] = name.ToString()
            };
        }

        public void StartAutomaticChecks()
        {
            AutomaticChecksRunning = true;
        }

        public void StopAutomaticChecks()
        {
            AutomaticChecksRunning = false;
        }

        public bool IsAutomaticChecksRunning()
        {
            return AutomaticChecksRunning;
        }

        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            // No-op: stub doesn't manage circuit breakers
        }

        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            // No-op: stub doesn't manage circuit breakers
        }

        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            CurrentDegradationLevel = level;
        }

        public HealthStatistics GetHealthStatistics()
        {
            lock (_lockObject)
            {
                var totalChecks = _registeredChecks.Count;
                var healthyCount = _configuredResults.Values.Count(r => r.Status == HealthStatus.Healthy);
                var unhealthyCount = _configuredResults.Values.Count(r => r.Status == HealthStatus.Unhealthy);

                return HealthStatistics.Create(
                    serviceUptime: TimeSpan.FromMinutes(1), // Stub uptime
                    totalHealthChecks: totalChecks,
                    successfulHealthChecks: healthyCount,
                    failedHealthChecks: unhealthyCount,
                    registeredHealthCheckCount: totalChecks,
                    currentDegradationLevel: CurrentDegradationLevel,
                    lastOverallStatus: OverallHealthStatus,
                    averageExecutionTime: TimeSpan.FromMilliseconds(1), // Stub execution time
                    openCircuitBreakers: 0,
                    activeCircuitBreakers: 0);
            }
        }

        public bool IsHealthCheckEnabled(FixedString64Bytes name)
        {
            lock (_lockObject)
            {
                return _registeredChecks.Contains(name);
            }
        }

        public void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
        {
            // No-op: stub doesn't manage enabled states
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            ClearConfiguration();
        }

        #endregion

        #region Private Helper Methods

        private HealthStatus DetermineOverallStatus(List<HealthCheckResult> results)
        {
            if (results.Count == 0)
                return OverallHealthStatus;

            var hasCritical = results.Any(r => r.Status == HealthStatus.Critical);
            var hasUnhealthy = results.Any(r => r.Status == HealthStatus.Unhealthy);
            var hasDegraded = results.Any(r => r.Status == HealthStatus.Degraded);
            var hasWarning = results.Any(r => r.Status == HealthStatus.Warning);

            if (hasCritical) return HealthStatus.Critical;
            if (hasUnhealthy) return HealthStatus.Unhealthy;
            if (hasDegraded) return HealthStatus.Degraded;
            if (hasWarning) return HealthStatus.Warning;

            return HealthStatus.Healthy;
        }

        #endregion
    }
}