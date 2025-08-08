using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Performance metrics for alerts.
    /// </summary>
    public sealed partial record AlertPerformanceMetrics
    {
        /// <summary>
        /// Operation duration in ticks.
        /// </summary>
        public long DurationTicks { get; init; }

        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; init; }

        /// <summary>
        /// CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsagePercent { get; init; }

        /// <summary>
        /// Additional performance metrics.
        /// </summary>
        public Dictionary<string, double> AdditionalMetrics { get; init; } = new();

        /// <summary>
        /// Gets the duration as TimeSpan.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(DurationTicks);
    }
}