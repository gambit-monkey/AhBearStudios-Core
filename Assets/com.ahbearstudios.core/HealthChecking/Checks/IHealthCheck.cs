using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Checks
{
    /// <summary>
    /// Interface for implementing health checks with comprehensive monitoring capabilities
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// Unique name identifying this health check
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Human-readable description of what this health check validates
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category of this health check for organization and filtering
        /// </summary>
        HealthCheckCategory Category { get; }

        /// <summary>
        /// Maximum time this health check should be allowed to run
        /// </summary>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Configuration for this health check
        /// </summary>
        HealthCheckConfiguration Configuration { get; }

        /// <summary>
        /// Dependencies that must be healthy for this check to run
        /// </summary>
        IEnumerable<FixedString64Bytes> Dependencies { get; }

        /// <summary>
        /// Executes the health check and returns the result
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Health check result indicating the current health status</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled</exception>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures the health check with new configuration settings
        /// </summary>
        /// <param name="configuration">Configuration to apply</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        void Configure(HealthCheckConfiguration configuration);

        /// <summary>
        /// Gets metadata about this health check for introspection and monitoring
        /// </summary>
        /// <returns>Dictionary containing metadata about the health check</returns>
        Dictionary<string, object> GetMetadata();
    }
}