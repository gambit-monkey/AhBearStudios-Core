using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Rule action to apply when rule matches.
    /// </summary>
    public sealed partial class AlertRuleAction
    {
        /// <summary>
        /// Gets or sets the action type.
        /// </summary>
        public AlertRuleActionType ActionType { get; set; }

        /// <summary>
        /// Gets or sets action parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Applies this action to an alert.
        /// </summary>
        /// <param name="alert">Alert to modify</param>
        /// <param name="context">Action context</param>
        /// <returns>Modified alert or null if suppressed</returns>
        public Alert Apply(Alert alert, Dictionary<string, object> context)
        {
            return ActionType switch
            {
                AlertRuleActionType.Suppress => null,
                AlertRuleActionType.ModifySeverity => alert.Severity != (AlertSeverity)Parameters.GetValueOrDefault("Severity", alert.Severity) 
                    ? alert with { Severity = (AlertSeverity)Parameters["Severity"] } 
                    : alert,
                AlertRuleActionType.AddTag => alert.Tag.IsEmpty 
                    ? alert with { Tag = Parameters.GetValueOrDefault("Tag", "").ToString() } 
                    : alert,
                _ => alert
            };
        }
    }
}