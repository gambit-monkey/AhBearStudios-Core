using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Performance settings for filters.
    /// </summary>
    public sealed class FilterPerformanceSettings
    {
        /// <summary>
        /// Maximum processing time before alert is raised.
        /// </summary>
        public TimeSpan MaxProcessingTime { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Whether to enable performance monitoring.
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;

        /// <summary>
        /// Sample rate for performance tracking (0.0 to 1.0).
        /// </summary>
        public double SampleRate { get; set; } = 0.1;

        /// <summary>
        /// Validates the performance settings.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when settings are invalid.</exception>
        public void Validate()
        {
            if (MaxProcessingTime <= TimeSpan.Zero)
                throw new InvalidOperationException("Max processing time must be greater than zero.");

            if (SampleRate < 0.0 || SampleRate > 1.0)
                throw new InvalidOperationException("Sample rate must be between 0.0 and 1.0.");
        }
    }
}