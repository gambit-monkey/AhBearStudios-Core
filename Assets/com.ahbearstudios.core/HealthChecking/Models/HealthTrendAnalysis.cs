namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Trend analysis for health checks
/// </summary>
public sealed record HealthTrendAnalysis
{
    public string CheckName { get; init; }
    public TrendDirection TrendDirection { get; init; }
    public double Confidence { get; init; }
    public double HealthyTrend { get; init; }
    public double UnhealthyTrend { get; init; }
    public int DataPointCount { get; init; }
    public TimeSpan AnalysisWindow { get; init; }
    public DateTime LastUpdated { get; init; }
}