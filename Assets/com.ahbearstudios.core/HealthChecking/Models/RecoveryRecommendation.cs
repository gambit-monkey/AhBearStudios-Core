using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Recovery recommendation for current system state.
    /// Provides actionable guidance for system recovery based on current health status.
    /// </summary>
    public sealed record RecoveryRecommendation
    {
        /// <summary>
        /// Gets the priority of the recommendation (1 = highest).
        /// Lower numbers indicate higher priority for execution.
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        /// Gets the recovery action to take.
        /// Describes the specific action or procedure to execute.
        /// </summary>
        public string Action { get; init; } = string.Empty;

        /// <summary>
        /// Gets the reason for the recommendation.
        /// Explains why this recommendation is being suggested.
        /// </summary>
        public string Reason { get; init; } = string.Empty;

        /// <summary>
        /// Gets the expected impact of the recovery action.
        /// Describes what effects the action will have on the system.
        /// </summary>
        public string ExpectedImpact { get; init; } = string.Empty;

        /// <summary>
        /// Gets the estimated time to recovery.
        /// Provides time estimate for how long the recovery should take.
        /// </summary>
        public TimeSpan EstimatedRecoveryTime { get; init; }

        /// <summary>
        /// Gets the risk level of the recovery action.
        /// Indicates the safety and potential impact of the recommended action.
        /// </summary>
        public RiskLevel Risk { get; init; }

        /// <summary>
        /// Gets the services or features affected by the recovery.
        /// Lists components that will be impacted during recovery execution.
        /// </summary>
        public IReadOnlyList<string> AffectedComponents { get; init; } = new List<string>();

        /// <summary>
        /// Creates a new RecoveryRecommendation with the specified parameters.
        /// </summary>
        /// <param name="priority">Priority level (1 = highest)</param>
        /// <param name="action">Recovery action description</param>
        /// <param name="reason">Reason for the recommendation</param>
        /// <param name="expectedImpact">Expected impact description</param>
        /// <param name="estimatedRecoveryTime">Estimated time to complete</param>
        /// <param name="risk">Risk level of the action</param>
        /// <param name="affectedComponents">Components that will be affected</param>
        /// <returns>New RecoveryRecommendation instance</returns>
        public static RecoveryRecommendation Create(
            int priority,
            string action,
            string reason,
            string expectedImpact = "",
            TimeSpan estimatedRecoveryTime = default,
            RiskLevel risk = RiskLevel.Medium,
            List<string> affectedComponents = null)
        {
            if (priority < 1)
                throw new ArgumentException("Priority must be 1 or higher", nameof(priority));

            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action cannot be null or empty", nameof(action));

            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

            return new RecoveryRecommendation
            {
                Priority = priority,
                Action = action,
                Reason = reason,
                ExpectedImpact = expectedImpact ?? string.Empty,
                EstimatedRecoveryTime = estimatedRecoveryTime,
                Risk = risk,
                AffectedComponents = affectedComponents?.AsReadOnly() ?? new List<string>().AsReadOnly()
            };
        }

        /// <summary>
        /// Returns a string representation of this recovery recommendation.
        /// </summary>
        /// <returns>Recovery recommendation summary</returns>
        public override string ToString()
        {
            return $"RecoveryRecommendation: Priority {Priority} - {Action} (Risk: {Risk}, ETA: {EstimatedRecoveryTime.TotalMinutes:F1}min)";
        }
    }
}