using System;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Represents a monitoring threshold configuration.
    /// </summary>
    public sealed class MonitoringThreshold
    {
        /// <summary>
        /// Gets or sets the metric name.
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Gets or sets the threshold value.
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Gets or sets the comparison type.
        /// </summary>
        public ThresholdComparisonType ComparisonType { get; set; }

        /// <summary>
        /// Gets or sets whether this threshold is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets when this threshold was last triggered.
        /// </summary>
        public DateTime? LastTriggered { get; set; }

        /// <summary>
        /// Returns a string representation of the threshold.
        /// </summary>
        /// <returns>Threshold summary string</returns>
        public override string ToString()
        {
            return $"MonitoringThreshold[{Metric}]: {ComparisonType} {Threshold} (Enabled={Enabled})";
        }
    }
}