using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Implementation
{
    /// <summary>
    /// Schedules and runs health checks at a fixed interval.
    /// </summary>
    public sealed class HealthCheckScheduler
    {
        private readonly IHealthCheckRegistry _registry;
        private readonly IHealthCheckReporter _reporter;
        private double _lastRunTime;
        private readonly double _interval;

        public HealthCheckScheduler(
            IHealthCheckRegistry registry,
            IHealthCheckReporter reporter,
            double intervalSeconds)
        {
            _registry = registry;
            _reporter = reporter;
            _interval = intervalSeconds;
        }

        public void Tick(double currentTimeUtc)
        {
            if (currentTimeUtc - _lastRunTime < _interval)
                return;

            _lastRunTime = currentTimeUtc;
            using var results = _registry.RunAllChecks(Allocator.Temp);
            for (int i = 0; i < results.Length; i++)
                _reporter.Report(results[i]);
        }
    }
}