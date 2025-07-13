using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Trend analysis configuration for health evaluation
/// </summary>
public sealed record TrendAnalysisConfig
{
    /// <summary>
    /// Whether trend analysis is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Number of historical data points to consider
    /// </summary>
    public int HistorySize { get; init; } = 10;

    /// <summary>
    /// Weight factor for trend in overall calculation (0.0 to 1.0)
    /// </summary>
    public double TrendWeight { get; init; } = 0.3;

    /// <summary>
    /// Threshold for considering a trend significant
    /// </summary>
    public double SignificantTrendThreshold { get; init; } = 0.1;

    /// <summary>
    /// Time window for trend analysis
    /// </summary>
    public TimeSpan TrendWindow { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Creates trend analysis configuration for critical systems
    /// </summary>
    /// <returns>Critical system trend analysis configuration</returns>
    public static TrendAnalysisConfig ForCriticalSystem()
    {
        return new TrendAnalysisConfig
        {
            Enabled = true,
            HistorySize = 20,
            TrendWeight = 0.4,
            SignificantTrendThreshold = 0.05,
            TrendWindow = TimeSpan.FromMinutes(15)
        };
    }

    /// <summary>
    /// Creates trend analysis configuration for high availability systems
    /// </summary>
    /// <returns>High availability trend analysis configuration</returns>
    public static TrendAnalysisConfig ForHighAvailability()
    {
        return new TrendAnalysisConfig
        {
            Enabled = true,
            HistorySize = 15,
            TrendWeight = 0.3,
            SignificantTrendThreshold = 0.08,
            TrendWindow = TimeSpan.FromMinutes(20)
        };
    }

    /// <summary>
    /// Validates trend analysis configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (HistorySize <= 0)
            errors.Add("HistorySize must be greater than zero");

        if (TrendWeight < 0.0 || TrendWeight > 1.0)
            errors.Add("TrendWeight must be between 0.0 and 1.0");

        if (SignificantTrendThreshold < 0.0 || SignificantTrendThreshold > 1.0)
            errors.Add("SignificantTrendThreshold must be between 0.0 and 1.0");

        if (TrendWindow <= TimeSpan.Zero)
            errors.Add("TrendWindow must be greater than zero");

        return errors;
    }
}