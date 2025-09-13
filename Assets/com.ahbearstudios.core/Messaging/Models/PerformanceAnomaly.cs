using System;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Represents a performance anomaly detection.
    /// </summary>
    public sealed class PerformanceAnomaly
    {
        /// <summary>
        /// Gets or sets the metric that showed anomalous behavior.
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double CurrentValue { get; set; }

        /// <summary>
        /// Gets or sets the expected baseline value.
        /// </summary>
        public double BaselineValue { get; set; }

        /// <summary>
        /// Gets or sets the deviation percentage.
        /// </summary>
        public double DeviationPercentage { get; set; }

        /// <summary>
        /// Gets or sets the anomaly severity.
        /// </summary>
        public AnomalySeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets when the anomaly was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; }
    }
}