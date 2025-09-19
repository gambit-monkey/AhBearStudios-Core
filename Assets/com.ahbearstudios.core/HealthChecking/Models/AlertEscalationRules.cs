using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Alert escalation rules for health check alerting.
    /// Defines how alerts should be escalated based on health status and conditions.
    /// </summary>
    public sealed record AlertEscalationRules
    {
        /// <summary>
        /// Gets the escalation delays by health status.
        /// </summary>
        public IReadOnlyDictionary<HealthStatus, TimeSpan> EscalationDelays { get; init; } =
            new Dictionary<HealthStatus, TimeSpan>();

        /// <summary>
        /// Gets the repeat thresholds by health status.
        /// </summary>
        public IReadOnlyDictionary<HealthStatus, int> RepeatThresholds { get; init; } =
            new Dictionary<HealthStatus, int>();

        /// <summary>
        /// Gets whether to escalate on degradation.
        /// </summary>
        public bool EscalateOnDegradation { get; init; }

        /// <summary>
        /// Gets the escalation tags for categorizing alerts.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> EscalationTags { get; init; } =
            new List<FixedString64Bytes>();

        /// <summary>
        /// Creates a new AlertEscalationRules with the specified parameters.
        /// </summary>
        /// <param name="escalationDelays">Escalation delays by health status</param>
        /// <param name="repeatThresholds">Repeat thresholds by health status</param>
        /// <param name="escalateOnDegradation">Whether to escalate on degradation</param>
        /// <param name="escalationTags">Escalation tags</param>
        /// <returns>New AlertEscalationRules instance</returns>
        public static AlertEscalationRules Create(
            Dictionary<HealthStatus, TimeSpan> escalationDelays = null,
            Dictionary<HealthStatus, int> repeatThresholds = null,
            bool escalateOnDegradation = false,
            List<FixedString64Bytes> escalationTags = null)
        {
            return new AlertEscalationRules
            {
                EscalationDelays = escalationDelays != null ? new Dictionary<HealthStatus, TimeSpan>(escalationDelays) : new Dictionary<HealthStatus, TimeSpan>(),
                RepeatThresholds = repeatThresholds != null ? new Dictionary<HealthStatus, int>(repeatThresholds) : new Dictionary<HealthStatus, int>(),
                EscalateOnDegradation = escalateOnDegradation,
                EscalationTags = escalationTags != null ? escalationTags.AsReadOnly() : new List<FixedString64Bytes>()
            };
        }

        /// <summary>
        /// Returns a string representation of these alert escalation rules.
        /// </summary>
        /// <returns>Alert escalation rules summary</returns>
        public override string ToString()
        {
            return $"AlertEscalationRules: {EscalationDelays.Count} delays, {RepeatThresholds.Count} thresholds, Escalate on degradation: {EscalateOnDegradation}";
        }
    }
}