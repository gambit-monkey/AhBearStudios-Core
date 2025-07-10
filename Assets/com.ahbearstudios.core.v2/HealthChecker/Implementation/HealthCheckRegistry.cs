// File: AhBearStudios.Core.HealthCheck.Implementation/HealthCheckRegistry.cs
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.HealthCheck.Implementation
{
    /// <summary>
    /// Default implementation of IHealthCheckRegistry using UnsafeParallelHashMap.
    /// </summary>
    public sealed class HealthCheckRegistry : IHealthCheckRegistry, System.IDisposable
    {
        private UnsafeParallelHashMap<FixedString64Bytes, IHealthCheck> _checks;

        public HealthCheckRegistry(Allocator allocator)
        {
            _checks = new UnsafeParallelHashMap<FixedString64Bytes, IHealthCheck>(16, allocator);
        }

        public void Register(IHealthCheck check)
        {
            if (!_checks.ContainsKey(check.Name))
                _checks.Add(check.Name, check);
        }

        public void Unregister(FixedString64Bytes name) => _checks.Remove(name);

        public bool Contains(FixedString64Bytes name) => _checks.ContainsKey(name);

        public NativeList<HealthCheckResult> RunAllChecks(Allocator allocator, double timestampUtc)
        {
            var results = new NativeList<HealthCheckResult>(allocator);
            foreach (var kv in _checks)
                results.Add(kv.Value.Execute(timestampUtc));
            return results;
        }

        public void Dispose()
        {
            if (_checks.IsCreated)
                _checks.Dispose();
        }
    }
}