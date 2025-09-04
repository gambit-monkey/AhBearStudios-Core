using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Performance prediction based on trend analysis.
    /// </summary>
    public sealed record PerformancePrediction
    {
        /// <summary>
        /// Gets the predicted success rate.
        /// </summary>
        public double PredictedSuccessRate { get; init; }

        /// <summary>
        /// Gets the predicted average execution time.
        /// </summary>
        public TimeSpan PredictedExecutionTime { get; init; }

        /// <summary>
        /// Gets the confidence level of the prediction.
        /// </summary>
        public double ConfidenceLevel { get; init; }

        /// <summary>
        /// Gets the time horizon for the prediction.
        /// </summary>
        public TimeSpan PredictionHorizon { get; init; }

        /// <summary>
        /// Gets any risk factors identified.
        /// </summary>
        public IReadOnlyList<string> RiskFactors { get; init; }
    }
}