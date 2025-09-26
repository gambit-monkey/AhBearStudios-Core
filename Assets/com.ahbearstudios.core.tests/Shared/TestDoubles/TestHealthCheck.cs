using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles
{
    /// <summary>
    /// Test implementation of IHealthCheck for verification tests.
    /// Provides complete interface implementation following TDD patterns.
    /// </summary>
    public class TestHealthCheck : IHealthCheck
    {
        private HealthCheckConfiguration _configuration;

        public TestHealthCheck(string name, string description = "Test health check")
        {
            Name = name;
            Description = description ?? "Test health check for verification";
            _configuration = new HealthCheckConfiguration
            {
                Name = Name,
                Timeout = TimeSpan.FromSeconds(5),
                Category = HealthCheckCategory.System
            };
        }

        #region IHealthCheck Implementation

        public FixedString64Bytes Name { get; }

        public string Description { get; }

        public HealthCheckCategory Category => HealthCheckCategory.System;

        public TimeSpan Timeout => _configuration?.Timeout ?? TimeSpan.FromSeconds(5);

        public HealthCheckConfiguration Configuration => _configuration;

        public IEnumerable<FixedString64Bytes> Dependencies { get; } = new List<FixedString64Bytes>();

        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            // Simulate some work
            await UniTask.Delay(TimeSpan.FromMilliseconds(10), cancellationToken: cancellationToken);

            // Return healthy result for test
            return HealthCheckResult.Healthy(Name.ToString(), "Test health check passed");
        }

        public void Configure(HealthCheckConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["IsTest"] = true,
                ["Name"] = Name.ToString(),
                ["Description"] = Description,
                ["Category"] = Category.ToString(),
                ["Timeout"] = Timeout.TotalSeconds,
                ["Version"] = "1.0.0-test",
                ["Implementation"] = nameof(TestHealthCheck),
                ["HasDependencies"] = false,
                ["ResourceRequirements"] = "Minimal",
                ["PerformanceCharacteristics"] = "Fast execution, low overhead"
            };
        }

        #endregion
    }
}