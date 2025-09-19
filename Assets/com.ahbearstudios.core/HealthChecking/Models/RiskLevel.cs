namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Risk level for recovery actions.
    /// Categorizes the potential impact and safety of recovery operations.
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>
        /// Low risk, safe to execute automatically.
        /// Operations with minimal impact that can be automated safely.
        /// </summary>
        Low,

        /// <summary>
        /// Medium risk, requires monitoring.
        /// Operations that need oversight but can proceed with caution.
        /// </summary>
        Medium,

        /// <summary>
        /// High risk, requires approval or manual intervention.
        /// Operations that could impact system stability and need human approval.
        /// </summary>
        High,

        /// <summary>
        /// Critical risk, should only be used as last resort.
        /// Operations with potential for significant system impact or downtime.
        /// </summary>
        Critical
    }
}