using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Comprehensive configuration for health check reporting, monitoring, and data export
    /// </summary>
    public sealed record ReportingConfig
    {
        /// <summary>
        /// Unique identifier for this reporting configuration
        /// </summary>
        public FixedString64Bytes Id { get; init; } = GenerateId();

        /// <summary>
        /// Display name for this reporting configuration
        /// </summary>
        public string Name { get; init; } = "Default Reporting Configuration";

        /// <summary>
        /// Whether reporting is enabled
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Interval for generating automatic reports
        /// </summary>
        public TimeSpan ReportInterval { get; init; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Types of reports to generate automatically
        /// </summary>
        public HashSet<ReportType> EnabledReports { get; init; } = new()
        {
            ReportType.HealthSummary,
            ReportType.StatusTrends,
            ReportType.PerformanceMetrics
        };

        /// <summary>
        /// Output formats for reports
        /// </summary>
        public HashSet<ReportFormat> OutputFormats { get; init; } = new()
        {
            ReportFormat.Json,
            ReportFormat.Html
        };

        /// <summary>
        /// Data retention configuration
        /// </summary>
        public DataRetentionConfig DataRetention { get; init; } = new();

        /// <summary>
        /// Export configuration for external systems
        /// </summary>
        public ExportConfig ExportConfig { get; init; } = new();

        /// <summary>
        /// Dashboard configuration for real-time monitoring
        /// </summary>
        public DashboardConfig DashboardConfig { get; init; } = new();

        /// <summary>
        /// Notification configuration for report events
        /// </summary>
        public NotificationConfig NotificationConfig { get; init; } = new();

        /// <summary>
        /// Aggregation configuration for report data
        /// </summary>
        public AggregationConfig AggregationConfig { get; init; } = new();

        /// <summary>
        /// Filtering configuration for report content
        /// </summary>
        public FilteringConfig FilteringConfig { get; init; } = new();

        /// <summary>
        /// Visualization configuration for charts and graphs
        /// </summary>
        public VisualizationConfig VisualizationConfig { get; init; } = new();

        /// <summary>
        /// Security configuration for report access
        /// </summary>
        public ReportSecurityConfig SecurityConfig { get; init; } = new();

        /// <summary>
        /// Performance configuration for report generation
        /// </summary>
        public ReportPerformanceConfig PerformanceConfig { get; init; } = new();

        /// <summary>
        /// Template configuration for custom reports
        /// </summary>
        public TemplateConfig TemplateConfig { get; init; } = new();

        /// <summary>
        /// Scheduling configuration for report generation
        /// </summary>
        public ReportSchedulingConfig SchedulingConfig { get; init; } = new();

        /// <summary>
        /// Custom metadata for this reporting configuration
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// Validates the reporting configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name cannot be null or empty");

            if (ReportInterval <= TimeSpan.Zero)
                errors.Add("ReportInterval must be greater than zero");

            if (EnabledReports.Count == 0)
                errors.Add("At least one report type must be enabled");

            if (OutputFormats.Count == 0)
                errors.Add("At least one output format must be specified");

            // Validate report types
            foreach (var reportType in EnabledReports)
            {
                if (!Enum.IsDefined(typeof(ReportType), reportType))
                    errors.Add($"Invalid report type: {reportType}");
            }

            // Validate output formats
            foreach (var format in OutputFormats)
            {
                if (!Enum.IsDefined(typeof(ReportFormat), format))
                    errors.Add($"Invalid output format: {format}");
            }

            // Validate nested configurations
            errors.AddRange(DataRetention.Validate());
            errors.AddRange(ExportConfig.Validate());
            errors.AddRange(DashboardConfig.Validate());
            errors.AddRange(NotificationConfig.Validate());
            errors.AddRange(AggregationConfig.Validate());
            errors.AddRange(FilteringConfig.Validate());
            errors.AddRange(VisualizationConfig.Validate());
            errors.AddRange(SecurityConfig.Validate());
            errors.AddRange(PerformanceConfig.Validate());
            errors.AddRange(TemplateConfig.Validate());
            errors.AddRange(SchedulingConfig.Validate());

            return errors;
        }

        /// <summary>
        /// Creates a reporting configuration optimized for production monitoring
        /// </summary>
        /// <returns>Production monitoring configuration</returns>
        public static ReportingConfig ForProduction()
        {
            return new ReportingConfig
            {
                Name = "Production Monitoring Reports",
                ReportInterval = TimeSpan.FromMinutes(15),
                EnabledReports = new HashSet<ReportType>
                {
                    ReportType.HealthSummary,
                    ReportType.StatusTrends,
                    ReportType.PerformanceMetrics,
                    ReportType.AvailabilityReport,
                    ReportType.AlertSummary,
                    ReportType.DegradationReport
                },
                OutputFormats = new HashSet<ReportFormat>
                {
                    ReportFormat.Json,
                    ReportFormat.Html,
                    ReportFormat.Csv
                },
                DataRetention = DataRetentionConfig.ForCompliance(),
                SecurityConfig = ReportSecurityConfig.ForCompliance(),
                SchedulingConfig = ReportSchedulingConfig.ForCompliance()
            };
        }

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }
    }
}