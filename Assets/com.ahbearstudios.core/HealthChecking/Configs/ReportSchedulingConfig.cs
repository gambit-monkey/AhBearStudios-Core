using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

public sealed record ReportSchedulingConfig
{
    public bool Enabled { get; init; } = true;
    public List<ReportSchedule> Schedules { get; init; } = new();

    public static ReportSchedulingConfig ForCompliance() => new()
    {
        Enabled = true,
        Schedules = new()
        {
            new() { Name = "Daily Summary", CronExpression = "0 9 * * *", ReportType = ReportType.HealthSummary },
            new() { Name = "Weekly Report", CronExpression = "0 9 * * 1", ReportType = ReportType.ComplianceReport },
            new() { Name = "Monthly Audit", CronExpression = "0 9 1 * *", ReportType = ReportType.AuditTrail }
        }
    };

    public List<string> Validate() => new();
}