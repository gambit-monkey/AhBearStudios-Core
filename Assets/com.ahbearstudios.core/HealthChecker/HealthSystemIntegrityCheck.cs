using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.HealthCheck
{
    /// <summary>
    /// Checks that the health system itself is configured and running correctly.
    /// </summary>
    public sealed class HealthSystemIntegrityCheck : IHealthCheck
    {
        private readonly IHealthCheckRegistry _registry;
        private readonly IHealthCheckReporter _reporter;
        private readonly FixedList64Bytes<FixedString64Bytes> _requiredChecks;
        private readonly FixedList64Bytes<FixedString64Bytes> _requiredTargets;

        public FixedString64Bytes Name => "Health.SystemIntegrity";
        public FixedString64Bytes Category => HealthCheckCategory.Core;

        public HealthSystemIntegrityCheck(
            IHealthCheckRegistry registry,
            IHealthCheckReporter reporter,
            FixedList64Bytes<FixedString64Bytes> requiredChecks,
            FixedList64Bytes<FixedString64Bytes> requiredTargets)
        {
            _registry = registry;
            _reporter = reporter;
            _requiredChecks = requiredChecks;
            _requiredTargets = requiredTargets;
        }

        public HealthCheckResult Execute(in double timestampUtc)
        {
            int missingChecks = 0, missingTargets = 0;
            foreach (var id in _requiredChecks)
                if (!_registry.Contains(id)) missingChecks++;
            foreach (var t in _requiredTargets)
                if (!_reporter.HasTarget(t)) missingTargets++;

            var message = $"MissingChecks={missingChecks}, MissingTargets={missingTargets}";
            var status = (missingChecks > 0 || missingTargets > 0)
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

            return new HealthCheckResult
            {
                Name = Name,
                Category = Category,
                SourceSystem = "HealthSystem",
                Status = status,
                Message = message,
                TimestampUtc = timestampUtc,
                CorrelationId = default
            };
        }
    }
}