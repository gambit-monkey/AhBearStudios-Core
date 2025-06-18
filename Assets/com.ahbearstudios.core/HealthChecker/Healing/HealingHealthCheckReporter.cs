using System.Collections.Generic;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Healing
{
    /// <summary>
    /// Decorator that triggers remediation on failure.
    /// </summary>
    public sealed class HealingHealthCheckReporter : IHealthCheckReporter
    {
        private readonly IHealthCheckReporter _inner;
        private readonly Dictionary<FixedString64Bytes, IHealthRemediation> _healers = 
            new Dictionary<FixedString64Bytes, IHealthRemediation>();

        public HealingHealthCheckReporter(IHealthCheckReporter inner)
        {
            _inner = inner;
        }

        public void RegisterRemediation(FixedString64Bytes checkId, IHealthRemediation healer)
            => _healers[checkId] = healer;

        public void AddTarget(IHealthCheckTarget target) 
            => _inner.AddTarget(target);

        public void RemoveTarget(IHealthCheckTarget target) 
            => _inner.RemoveTarget(target);

        public bool HasTarget(FixedString64Bytes id) 
            => _inner.HasTarget(id);

        public void Report(in HealthCheckResult result)
        {
            _inner.Report(result);

            if (result.Status != HealthStatus.Healthy &&
                _healers.TryGetValue(result.Name, out var healer) &&
                healer.CanRemediate(result))
            {
                healer.AttemptRemediation(result);
            }
        }
    }
}