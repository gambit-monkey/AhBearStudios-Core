using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Checks
{
    /// <summary>
    /// Enhanced interface for implementing health checks with comprehensive monitoring capabilities
    /// </summary>
    /// <remarks>
    /// Provides the foundation for all health check implementations with support for:
    /// - Asynchronous execution with cancellation support
    /// - Configurable timeouts and retry policies
    /// - Dependency tracking and metadata introspection
    /// - Category-based organization for filtering and aggregation
    /// - Circuit breaker and degradation integration
    /// </remarks>
    public interface IHealthCheck
    {
        /// <summary>
        /// Unique name identifying this health check using high-performance FixedString
        /// </summary>
        /// <remarks>
        /// Must be unique across all health checks in the system. Used for registration,
        /// tracking, and correlation with circuit breakers and degradation policies
        /// </remarks>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Human-readable description of what this health check validates
        /// </summary>
        /// <remarks>
        /// Should clearly describe the functionality being tested and what a failure indicates.
        /// Used in health reports, logs, and alert messages for operational clarity
        /// </remarks>
        string Description { get; }

        /// <summary>
        /// Category of this health check for organization, filtering, and weighted aggregation
        /// </summary>
        /// <remarks>
        /// Categories enable grouped health reporting, priority-based aggregation weights,
        /// and category-specific degradation policies
        /// </remarks>
        HealthCheckCategory Category { get; }

        /// <summary>
        /// Maximum time this health check should be allowed to run before timing out
        /// </summary>
        /// <remarks>
        /// Used by the scheduling service to enforce execution timeouts and detect stuck checks.
        /// Should be set appropriately for the operation being performed
        /// </remarks>
        TimeSpan Timeout { get; }

        /// <summary>
        /// Current configuration for this health check instance
        /// </summary>
        /// <remarks>
        /// Provides access to the active configuration including thresholds, retry policies,
        /// circuit breaker settings, and degradation impact configuration
        /// </remarks>
        HealthCheckConfiguration Configuration { get; }

        /// <summary>
        /// Names of other health checks that must be healthy for this check to execute
        /// </summary>
        /// <remarks>
        /// Enables dependency-based health check ordering and prevents cascade failures.
        /// Dependencies are validated before execution and included in health reports
        /// </remarks>
        IEnumerable<FixedString64Bytes> Dependencies { get; }

        /// <summary>
        /// Executes the health check and returns a comprehensive result
        /// </summary>
        /// <param name="cancellationToken">Token to observe for cancellation requests</param>
        /// <returns>
        /// Task representing the asynchronous health check operation.
        /// Result contains status, timing information, diagnostic data, and any exceptions
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the health check is not properly configured
        /// </exception>
        /// <remarks>
        /// Implementations should:
        /// - Handle cancellation gracefully and promptly
        /// - Provide detailed diagnostic information in the result
        /// - Use appropriate timeouts for external dependencies
        /// - Return degraded status for performance issues
        /// - Include relevant metrics and context data
        /// </remarks>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures the health check with new configuration settings
        /// </summary>
        /// <param name="configuration">Configuration to apply to this health check</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configuration is invalid or conflicts with current state
        /// </exception>
        /// <remarks>
        /// Allows runtime reconfiguration of health check behavior including:
        /// - Execution intervals and timeouts
        /// - Threshold values for status determination
        /// - Circuit breaker and retry policies
        /// - Alert and degradation settings
        /// </remarks>
        void Configure(HealthCheckConfiguration configuration);

        /// <summary>
        /// Gets comprehensive metadata about this health check for introspection and monitoring
        /// </summary>
        /// <returns>
        /// Dictionary containing metadata about the health check including:
        /// - Implementation details and version information
        /// - Resource requirements and performance characteristics
        /// - Configuration capabilities and current settings
        /// - Integration points and external dependencies
        /// </returns>
        /// <remarks>
        /// Metadata is used by:
        /// - Health check management interfaces for configuration
        /// - Monitoring systems for operational dashboards
        /// - Diagnostic tools for troubleshooting
        /// - Documentation generators for system inventories
        /// </remarks>
        Dictionary<string, object> GetMetadata();
    }
}