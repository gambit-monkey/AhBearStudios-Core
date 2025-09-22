using System;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Mocks
{
    public sealed class MockHealthCheckService : IHealthCheckService
    {
        private readonly Dictionary<string, HealthCheckResult> _healthCheckResults = new Dictionary<string, HealthCheckResult>();
        private readonly Dictionary<string, IHealthCheck> _registeredChecks = new Dictionary<string, IHealthCheck>();

        public bool IsEnabled { get; set; } = true;
        public bool OverallHealthStatus { get; set; } = true;
        public int CheckCount => _healthCheckResults.Count;
        public int RegisteredCheckCount => _registeredChecks.Count;
        public bool ShouldThrowOnCheck { get; set; }
        public TimeSpan CheckDelay { get; set; } = TimeSpan.Zero;

        public async UniTask<HealthCheckResult> PerformHealthCheckAsync(string checkName, Guid correlationId = default)
        {
            if (CheckDelay > TimeSpan.Zero)
                await UniTask.Delay(CheckDelay);

            if (ShouldThrowOnCheck)
                throw new InvalidOperationException($"Mock health check error for {checkName}");

            if (_registeredChecks.TryGetValue(checkName, out var healthCheck))
            {
                try
                {
                    var result = await healthCheck.CheckHealthAsync(correlationId);
                    _healthCheckResults[checkName] = result;
                    return result;
                }
                catch (Exception ex)
                {
                    var failureResult = HealthCheckResult.Failure(
                        checkName: checkName,
                        error: ex.Message,
                        correlationId: correlationId);
                    _healthCheckResults[checkName] = failureResult;
                    return failureResult;
                }
            }

            // Return a default successful result for unregistered checks
            var defaultResult = HealthCheckResult.Success(
                checkName: checkName,
                correlationId: correlationId);
            _healthCheckResults[checkName] = defaultResult;
            return defaultResult;
        }

        public async UniTask<SystemHealthReport> PerformAllHealthChecksAsync(Guid correlationId = default)
        {
            if (CheckDelay > TimeSpan.Zero)
                await UniTask.Delay(CheckDelay);

            if (ShouldThrowOnCheck)
                throw new InvalidOperationException("Mock health check error for all checks");

            var results = new List<HealthCheckResult>();

            foreach (var checkName in _registeredChecks.Keys)
            {
                var result = await PerformHealthCheckAsync(checkName, correlationId);
                results.Add(result);
            }

            var overallStatus = OverallHealthStatus && results.AsValueEnumerable().All(r => r.IsHealthy);

            return SystemHealthReport.Create(
                overallStatus: overallStatus,
                checkResults: results,
                correlationId: correlationId);
        }

        public void RegisterHealthCheck(IHealthCheck healthCheck)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            _registeredChecks[healthCheck.Name] = healthCheck;
        }

        public bool UnregisterHealthCheck(string checkName)
        {
            if (string.IsNullOrEmpty(checkName))
                return false;

            var removed = _registeredChecks.Remove(checkName);
            if (removed)
            {
                _healthCheckResults.Remove(checkName);
            }
            return removed;
        }

        public IEnumerable<string> GetRegisteredHealthCheckNames()
        {
            return _registeredChecks.Keys.AsValueEnumerable().ToList();
        }

        public HealthCheckResult GetLastResult(string checkName)
        {
            return _healthCheckResults.TryGetValue(checkName, out var result) ? result : null;
        }

        public IEnumerable<HealthCheckResult> GetAllLastResults()
        {
            return _healthCheckResults.Values.AsValueEnumerable().ToList();
        }

        public bool IsHealthCheckRegistered(string checkName)
        {
            return _registeredChecks.ContainsKey(checkName);
        }

        public void SetHealthCheckResult(string checkName, HealthCheckResult result)
        {
            _healthCheckResults[checkName] = result;
        }

        public void SetOverallHealth(bool isHealthy)
        {
            OverallHealthStatus = isHealthy;
        }

        public void Clear()
        {
            _healthCheckResults.Clear();
            _registeredChecks.Clear();
            OverallHealthStatus = true;
        }

        public HealthCheckStatistics GetStatistics()
        {
            var successCount = _healthCheckResults.Values.AsValueEnumerable().Count(r => r.IsHealthy);
            var failureCount = _healthCheckResults.Values.AsValueEnumerable().Count(r => !r.IsHealthy);

            return HealthCheckStatistics.Create(
                totalChecks: _healthCheckResults.Count,
                successfulChecks: successCount,
                failedChecks: failureCount,
                averageResponseTime: TimeSpan.FromMilliseconds(10), // Mock average
                lastCheckTime: DateTime.UtcNow);
        }

        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success("MockHealthCheckService");
        }

        public void Dispose()
        {
            Clear();
        }

        public async UniTask StartAsync()
        {
            IsEnabled = true;
            await UniTask.CompletedTask;
        }

        public async UniTask StopAsync()
        {
            IsEnabled = false;
            await UniTask.CompletedTask;
        }
    }
}