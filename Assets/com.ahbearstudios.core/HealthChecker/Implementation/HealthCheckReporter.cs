using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.HealthCheck.Implementation
{
    /// <summary>
    /// Default reporter that forwards results to targets.
    /// </summary>
    public sealed class HealthCheckReporter : IHealthCheckReporter
    {
        private FixedList128Bytes<IHealthCheckTarget> _targets;

        public void AddTarget(IHealthCheckTarget target)
        {
            if (!_targets.Contains(target))
                _targets.Add(target);
        }

        public void RemoveTarget(IHealthCheckTarget target)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] == target)
                {
                    _targets.RemoveAt(i);
                    return;
                }
            }
        }

        public bool HasTarget(FixedString64Bytes id)
        {
            for (int i = 0; i < _targets.Length; i++)
                if (_targets[i].GetType().Name.Equals(id.ToString()))
                    return true;
            return false;
        }

        public void Report(in HealthCheckResult result)
        {
            for (int i = 0; i < _targets.Length; i++)
                _targets[i].Handle(result);
        }
    }
}