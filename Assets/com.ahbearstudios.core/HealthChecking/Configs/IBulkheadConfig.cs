using System;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Configuration interface for bulkhead isolation in circuit breakers
    /// </summary>
    public interface IBulkheadConfig
    {
        /// <summary>
        /// Maximum number of concurrent calls allowed
        /// </summary>
        int MaxConcurrentCalls { get; }

        /// <summary>
        /// Maximum wait duration before rejecting requests
        /// </summary>
        TimeSpan MaxWaitDuration { get; }

        /// <summary>
        /// Whether to enable bulkhead isolation
        /// </summary>
        bool EnableBulkhead { get; }

        /// <summary>
        /// Maximum queue size for waiting calls when bulkhead is enabled
        /// </summary>
        int MaxQueueSize { get; }
    }
}