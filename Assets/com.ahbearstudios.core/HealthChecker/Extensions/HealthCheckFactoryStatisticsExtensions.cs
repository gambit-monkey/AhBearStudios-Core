using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.HealthCheck.Extensions;

/// <summary>
/// Extension methods for HealthCheckFactoryStatistics to provide additional functionality.
/// </summary>
public static class HealthCheckFactoryStatisticsExtensions
{
    /// <summary>
    /// Gets statistics formatted for display in monitoring dashboards.
    /// </summary>
    /// <param name="statistics">The statistics instance.</param>
    /// <returns>A dictionary of formatted metric values suitable for display.</returns>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    public static IReadOnlyDictionary<string, string> GetFormattedMetrics(this HealthCheckFactoryStatistics statistics)
    {
        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));

        return new Dictionary<string, string>
        {
            ["Total Creations"] = statistics.TotalCreationsAttempted.ToString("N0"),
            ["Success Rate"] = $"{statistics.CreationSuccessRate:F1}%",
            ["Failure Rate"] = $"{statistics.CreationFailureRate:F1}%",
            ["Avg Creation Time"] = statistics.AverageCreationTimeMs?.ToString("F1") + "ms" ?? "N/A",
            ["Fastest Creation"] = statistics.FastestCreationTimeMs?.ToString("F1") + "ms" ?? "N/A",
            ["Slowest Creation"] = statistics.SlowestCreationTimeMs?.ToString("F1") + "ms" ?? "N/A",
            ["Creation Rate"] = $"{statistics.AverageCreationRate:F2}/sec",
            ["Current Rate"] = $"{statistics.CurrentCreationRate:F2}/sec",
            ["Peak Rate"] = $"{statistics.PeakCreationRate:F2}/sec",
            ["Unique Types"] = statistics.UniqueHealthCheckTypes.ToString("N0"),
            ["Services Created"] = statistics.TotalServicesCreated.ToString("N0"),
            ["Cache Clears"] = statistics.TotalCacheClears.ToString("N0"),
            ["Uptime"] = FormatTimeSpan(statistics.TotalUptime),
            ["Most Created Type"] = statistics.MostCreatedType ?? "None",
            ["Highest Failure Type"] = statistics.HighestFailureRateType ?? "None"
        };
    }

    /// <summary>
    /// Gets a health score based on the factory's performance metrics.
    /// </summary>
    /// <param name="statistics">The statistics instance.</param>
    /// <returns>A health score from 0.0 (poor) to 1.0 (excellent).</returns>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    public static double GetHealthScore(this HealthCheckFactoryStatistics statistics)
    {
        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));

        if (statistics.TotalCreationsAttempted == 0)
            return 1.0; // Perfect score if no operations attempted

        var successRateScore = statistics.CreationSuccessRate / 100.0;
        var performanceScore = CalculatePerformanceScore(statistics);
        var reliabilityScore = CalculateReliabilityScore(statistics);

        // Weighted average: 50% success rate, 25% performance, 25% reliability
        return (successRateScore * 0.5) + (performanceScore * 0.25) + (reliabilityScore * 0.25);
    }

    /// <summary>
    /// Gets performance trends over recent time periods.
    /// </summary>
    /// <param name="statistics">The statistics instance.</param>
    /// <param name="periodMinutes">The time period in minutes to analyze.</param>
    /// <returns>Performance trend information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when periodMinutes is less than 1.</exception>
    public static PerformanceTrend GetPerformanceTrend(this HealthCheckFactoryStatistics statistics, int periodMinutes = 15)
    {
        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));
        if (periodMinutes < 1)
            throw new ArgumentOutOfRangeException(nameof(periodMinutes), "Period must be at least 1 minute");

        var recentEvents = statistics.GetRecentEvents(TimeSpan.FromMinutes(periodMinutes));
        
        if (recentEvents.Count == 0)
        {
            return new PerformanceTrend(
                PeriodMinutes: periodMinutes,
                TotalEvents: 0,
                SuccessfulEvents: 0,
                FailedEvents: 0,
                AverageCreationTimeMs: null,
                TrendDirection: TrendDirection.Stable
            );
        }

        var successfulEvents = recentEvents.Where(e => e.Success).ToList();
        var failedEvents = recentEvents.Where(e => !e.Success).ToList();
        var avgCreationTime = successfulEvents.Any() ? successfulEvents.Average(e => e.CreationTimeMs) : (double?)null;

        // Calculate trend direction based on recent vs. historical success rate
        var recentSuccessRate = recentEvents.Count > 0 ? (double)successfulEvents.Count / recentEvents.Count * 100.0 : 0.0;
        var historicalSuccessRate = statistics.CreationSuccessRate;
        
        var trendDirection = Math.Abs(recentSuccessRate - historicalSuccessRate) < 5.0 
            ? TrendDirection.Stable
            : recentSuccessRate > historicalSuccessRate 
                ? TrendDirection.Improving 
                : TrendDirection.Declining;

        return new PerformanceTrend(
            PeriodMinutes: periodMinutes,
            TotalEvents: recentEvents.Count,
            SuccessfulEvents: successfulEvents.Count,
            FailedEvents: failedEvents.Count,
            AverageCreationTimeMs: avgCreationTime,
            TrendDirection: trendDirection
        );
    }

    /// <summary>
    /// Detects performance anomalies in the factory statistics.
    /// </summary>
    /// <param name="statistics">The statistics instance.</param>
    /// <returns>A list of detected performance anomalies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    public static IReadOnlyList<PerformanceAnomaly> DetectAnomalies(this HealthCheckFactoryStatistics statistics)
    {
        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));

        var anomalies = new List<PerformanceAnomaly>();

        // Check for high failure rate
        if (statistics.CreationFailureRate > 10.0 && statistics.TotalCreationsAttempted > 10)
        {
            anomalies.Add(new PerformanceAnomaly(
                Type: AnomalyType.HighFailureRate,
                Description: $"Factory failure rate is {statistics.CreationFailureRate:F1}% (threshold: 10%)",
                Severity: statistics.CreationFailureRate > 25.0 ? AnomalySeverity.High : AnomalySeverity.Medium,
                DetectedAt: DateTime.UtcNow
            ));
        }

        // Check for slow average creation time
        if (statistics.AverageCreationTimeMs > 1000.0 && statistics.TotalCreationsSucceeded > 5)
        {
            anomalies.Add(new PerformanceAnomaly(
                Type: AnomalyType.SlowPerformance,
                Description: $"Average creation time is {statistics.AverageCreationTimeMs:F1}ms (threshold: 1000ms)",
                Severity: statistics.AverageCreationTimeMs > 5000.0 ? AnomalySeverity.High : AnomalySeverity.Medium,
                DetectedAt: DateTime.UtcNow
            ));
        }

        // Check for very slow worst-case performance
        if (statistics.SlowestCreationTimeMs > 10000.0)
        {
            anomalies.Add(new PerformanceAnomaly(
                Type: AnomalyType.PerformanceSpike,
                Description: $"Slowest creation time is {statistics.SlowestCreationTimeMs:F1}ms (threshold: 10000ms)",
                Severity: AnomalySeverity.Medium,
                DetectedAt: DateTime.UtcNow
            ));
        }

        // Check for low creation rate when there should be activity
        if (statistics.CurrentCreationRate < 0.1 && statistics.TotalUptime > TimeSpan.FromMinutes(10))
        {
            anomalies.Add(new PerformanceAnomaly(
                Type: AnomalyType.LowActivity,
                Description: $"Current creation rate is {statistics.CurrentCreationRate:F2}/sec (very low activity)",
                Severity: AnomalySeverity.Low,
                DetectedAt: DateTime.UtcNow
            ));
        }

        return anomalies;
    }

    private static double CalculatePerformanceScore(HealthCheckFactoryStatistics statistics)
    {
        if (!statistics.AverageCreationTimeMs.HasValue)
            return 1.0;

        var avgTime = statistics.AverageCreationTimeMs.Value;
        
        // Score based on average creation time (excellent < 100ms, poor > 5000ms)
        if (avgTime <= 100) return 1.0;
        if (avgTime <= 500) return 0.8;
        if (avgTime <= 1000) return 0.6;
        if (avgTime <= 2500) return 0.4;
        if (avgTime <= 5000) return 0.2;
        return 0.1;
    }

    private static double CalculateReliabilityScore(HealthCheckFactoryStatistics statistics)
    {
        // Score based on consistency and error patterns
        var typeStats = statistics.GetAllTypeStatistics();
        
        if (!typeStats.Any())
            return 1.0;

        var avgFailureRate = typeStats.Values.Average(ts => ts.FailureRate);
        var maxFailureRate = typeStats.Values.Max(ts => ts.FailureRate);
        
        // Penalize high average failure rates and high variance in failure rates
        var avgScore = Math.Max(0.0, 1.0 - (avgFailureRate / 100.0));
        var varianceScore = Math.Max(0.0, 1.0 - (maxFailureRate / 100.0));
        
        return (avgScore + varianceScore) / 2.0;
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        return $"{timeSpan.TotalSeconds:F1}s";
    }
}