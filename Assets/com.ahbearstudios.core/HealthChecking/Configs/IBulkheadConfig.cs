using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for bulkhead isolation pattern configuration
    /// </summary>
    public interface IBulkheadConfig
    {
        /// <summary>
        /// Whether bulkhead isolation is enabled
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Maximum number of concurrent calls allowed
        /// </summary>
        int MaxConcurrentCalls { get; }

        /// <summary>
        /// Maximum time to wait for a call slot to become available
        /// </summary>
        TimeSpan MaxWaitDuration { get; }

        /// <summary>
        /// Whether to use fair queuing for waiting calls
        /// </summary>
        bool UseFairQueuing { get; }

        /// <summary>
        /// Maximum queue size for waiting calls
        /// </summary>
        int MaxQueueSize { get; }

        /// <summary>
        /// Validates bulkhead configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        List<string> Validate();
    }
}