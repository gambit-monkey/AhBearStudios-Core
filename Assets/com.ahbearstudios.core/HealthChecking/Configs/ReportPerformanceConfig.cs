using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

public sealed record ReportPerformanceConfig
{
    public int MaxConcurrentReports { get; init; } = 5;
    public TimeSpan ReportTimeout { get; init; } = TimeSpan.FromMinutes(10);
    public bool EnableCaching { get; init; } = true;
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(15);

    public static ReportPerformanceConfig ForProduction() => new()
    {
        MaxConcurrentReports = 10,
        ReportTimeout = TimeSpan.FromMinutes(5),
        EnableCaching = true,
        CacheDuration = TimeSpan.FromMinutes(30)
    };

    public static ReportPerformanceConfig ForDevelopment() => new()
    {
        MaxConcurrentReports = 2,
        ReportTimeout = TimeSpan.FromMinutes(2),
        EnableCaching = false
    };

    public List<string> Validate() => new();
}