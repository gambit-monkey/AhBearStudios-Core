using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Profiling.HealthChecks
{
    /// <summary>
    /// Represents the result of an individual health assessment within the profiler health check system.
    /// Used internally by ProfilerHealthCheck to encapsulate assessment outcomes with detailed metrics.
    /// </summary>
    internal sealed class HealthAssessmentResult
    {
        #region Public Properties

        /// <summary>
        /// Gets the overall health status determined by this assessment.
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Gets the numeric health score (0.0 to 1.0, where 1.0 is perfect health).
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the human-readable description of the assessment result.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets additional detailed information about the assessment.
        /// </summary>
        public Dictionary<string, object> Details { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the HealthAssessmentResult class.
        /// </summary>
        /// <param name="status">The overall health status</param>
        /// <param name="score">The numeric health score (0.0 to 1.0)</param>
        /// <param name="description">Human-readable description of the result</param>
        /// <param name="details">Additional detailed information (optional)</param>
        public HealthAssessmentResult(HealthStatus status, double score, string description, Dictionary<string, object> details = null)
        {
            Status = status;
            Score = score;
            Description = description ?? string.Empty;
            Details = details ?? new Dictionary<string, object>();
        }

        #endregion
    }
}