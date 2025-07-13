namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
    /// Types of reports that can be generated
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// Summary of current health status
        /// </summary>
        HealthSummary,

        /// <summary>
        /// Trends and patterns in health status over time
        /// </summary>
        StatusTrends,

        /// <summary>
        /// Performance metrics and statistics
        /// </summary>
        PerformanceMetrics,

        /// <summary>
        /// System availability and uptime report
        /// </summary>
        AvailabilityReport,

        /// <summary>
        /// Alert summary and analysis
        /// </summary>
        AlertSummary,

        /// <summary>
        /// Degradation events and impact analysis
        /// </summary>
        DegradationReport,

        /// <summary>
        /// Circuit breaker statistics and events
        /// </summary>
        CircuitBreakerReport,

        /// <summary>
        /// Compliance and regulatory report
        /// </summary>
        ComplianceReport,

        /// <summary>
        /// Security-related health events
        /// </summary>
        SecurityReport,

        /// <summary>
        /// Detailed audit trail of health events
        /// </summary>
        AuditTrail,

        /// <summary>
        /// Historical data analysis and insights
        /// </summary>
        HistoricalAnalysis,

        /// <summary>
        /// Capacity planning and resource utilization
        /// </summary>
        CapacityReport,

        /// <summary>
        /// SLA compliance and performance against targets
        /// </summary>
        SlaReport,

        /// <summary>
        /// Detailed debug information for troubleshooting
        /// </summary>
        DebugReport,

        /// <summary>
        /// Executive summary for management
        /// </summary>
        ExecutiveSummary,

        /// <summary>
        /// Custom report based on templates
        /// </summary>
        CustomReport
    }