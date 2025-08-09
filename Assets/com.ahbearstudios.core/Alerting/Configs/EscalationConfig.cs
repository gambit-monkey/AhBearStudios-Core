using System;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration for escalation behavior when suppressed alerts meet critical criteria.
    /// Defines conditions under which suppressed alerts should still be escalated for immediate attention.
    /// </summary>
    public sealed record EscalationConfig
    {
        /// <summary>
        /// Gets whether escalation is enabled for this suppression rule.
        /// When disabled, no escalation occurs regardless of other settings.
        /// </summary>
        public bool IsEnabled { get; init; } = false;

        /// <summary>
        /// Gets the severity threshold for automatic escalation.
        /// Suppressed alerts at or above this severity are automatically escalated.
        /// </summary>
        public AlertSeverity EscalationSeverity { get; init; } = AlertSeverity.Emergency;

        /// <summary>
        /// Gets the maximum number of alerts that can be suppressed before triggering escalation.
        /// When this threshold is reached, an escalation alert is generated.
        /// </summary>
        public int SuppressionThreshold { get; init; } = 100;

        /// <summary>
        /// Gets the time window for evaluating suppression thresholds.
        /// Suppression counts are evaluated over this rolling window.
        /// </summary>
        public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets the escalation channel to use for escalated alerts.
        /// Should be a high-priority channel that bypasses normal suppression rules.
        /// </summary>
        public FixedString64Bytes EscalationChannel { get; init; } = "Emergency";

        /// <summary>
        /// Gets the message template for escalation alerts.
        /// Supports placeholders: {SuppressedCount}, {TimeWindow}, {RuleName}, {Severity}.
        /// </summary>
        public string EscalationMessageTemplate { get; init; } = "Escalation: {SuppressedCount} alerts suppressed by {RuleName} in {TimeWindow}";

        /// <summary>
        /// Gets whether escalation alerts should include details of suppressed alerts.
        /// When enabled, a summary of suppressed alerts is included in the escalation.
        /// </summary>
        public bool IncludeSuppressedDetails { get; init; } = true;

        /// <summary>
        /// Gets the minimum delay between escalation alerts for the same rule.
        /// Prevents rapid-fire escalations that could overwhelm the escalation channel.
        /// </summary>
        public TimeSpan EscalationCooldown { get; init; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Validates the escalation configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (IsEnabled && EscalationChannel.IsEmpty)
                throw new InvalidOperationException("Escalation channel must be specified when escalation is enabled.");

            if (SuppressionThreshold <= 0)
                throw new InvalidOperationException("Suppression threshold must be greater than zero.");

            if (EvaluationWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Evaluation window must be greater than zero.");

            if (EscalationCooldown <= TimeSpan.Zero)
                throw new InvalidOperationException("Escalation cooldown must be greater than zero.");

            if (IsEnabled && string.IsNullOrWhiteSpace(EscalationMessageTemplate))
                throw new InvalidOperationException("Escalation message template cannot be empty when escalation is enabled.");
        }

        /// <summary>
        /// Gets the default escalation configuration with escalation disabled.
        /// </summary>
        public static EscalationConfig Default => new()
        {
            IsEnabled = false,
            EscalationSeverity = AlertSeverity.Emergency,
            SuppressionThreshold = 100,
            EvaluationWindow = TimeSpan.FromMinutes(15),
            EscalationChannel = "Emergency",
            EscalationMessageTemplate = "Escalation: {SuppressedCount} alerts suppressed by {RuleName} in {TimeWindow}",
            IncludeSuppressedDetails = true,
            EscalationCooldown = TimeSpan.FromMinutes(10)
        };

        /// <summary>
        /// Creates an escalation configuration for critical alert monitoring.
        /// Escalates when more than 50 critical alerts are suppressed in 10 minutes.
        /// </summary>
        /// <returns>A configured escalation rule for critical alerts.</returns>
        public static EscalationConfig CreateCriticalAlertEscalation()
        {
            return new EscalationConfig
            {
                IsEnabled = true,
                EscalationSeverity = AlertSeverity.Critical,
                SuppressionThreshold = 50,
                EvaluationWindow = TimeSpan.FromMinutes(10),
                EscalationChannel = "Emergency",
                EscalationMessageTemplate = "CRITICAL: {SuppressedCount} critical alerts suppressed by {RuleName} in {TimeWindow} - System may be experiencing severe issues",
                IncludeSuppressedDetails = true,
                EscalationCooldown = TimeSpan.FromMinutes(5)
            };
        }
    }
}