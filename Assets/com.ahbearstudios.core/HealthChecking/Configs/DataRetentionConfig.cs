using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Data retention configuration for health check data
/// </summary>
public sealed record DataRetentionConfig
{
    /// <summary>
    /// How long to retain raw health check results
    /// </summary>
    public TimeSpan RawDataRetention { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How long to retain aggregated data
    /// </summary>
    public TimeSpan AggregatedDataRetention { get; init; } = TimeSpan.FromDays(365);

    /// <summary>
    /// How long to retain reports
    /// </summary>
    public TimeSpan ReportRetention { get; init; } = TimeSpan.FromDays(90);

    /// <summary>
    /// How long to retain alert data
    /// </summary>
    public TimeSpan AlertDataRetention { get; init; } = TimeSpan.FromDays(180);

    /// <summary>
    /// Whether to enable automatic data cleanup
    /// </summary>
    public bool EnableAutomaticCleanup { get; init; } = true;

    /// <summary>
    /// Cleanup frequency
    /// </summary>
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Data compression settings
    /// </summary>
    public CompressionConfig CompressionConfig { get; init; } = new();

    /// <summary>
    /// Archival settings for long-term storage
    /// </summary>
    public ArchivalConfig ArchivalConfig { get; init; } = new();

    /// <summary>
    /// Creates data retention configuration for production environments
    /// </summary>
    /// <returns>Production data retention configuration</returns>
    public static DataRetentionConfig ForProduction()
    {
        return new DataRetentionConfig
        {
            RawDataRetention = TimeSpan.FromDays(90),
            AggregatedDataRetention = TimeSpan.FromDays(1095), // 3 years
            ReportRetention = TimeSpan.FromDays(365),
            AlertDataRetention = TimeSpan.FromDays(730), // 2 years
            EnableAutomaticCleanup = true,
            CleanupInterval = TimeSpan.FromHours(6),
            CompressionConfig = CompressionConfig.ForProduction(),
            ArchivalConfig = ArchivalConfig.ForProduction()
        };
    }

    /// <summary>
    /// Creates data retention configuration for development environments
    /// </summary>
    /// <returns>Development data retention configuration</returns>
    public static DataRetentionConfig ForDevelopment()
    {
        return new DataRetentionConfig
        {
            RawDataRetention = TimeSpan.FromDays(7),
            AggregatedDataRetention = TimeSpan.FromDays(30),
            ReportRetention = TimeSpan.FromDays(14),
            AlertDataRetention = TimeSpan.FromDays(30),
            EnableAutomaticCleanup = true,
            CleanupInterval = TimeSpan.FromHours(12)
        };
    }

    /// <summary>
    /// Creates data retention configuration for compliance requirements
    /// </summary>
    /// <returns>Compliance data retention configuration</returns>
    public static DataRetentionConfig ForCompliance()
    {
        return new DataRetentionConfig
        {
            RawDataRetention = TimeSpan.FromDays(365),
            AggregatedDataRetention = TimeSpan.FromDays(2555), // 7 years
            ReportRetention = TimeSpan.FromDays(2555), // 7 years
            AlertDataRetention = TimeSpan.FromDays(2555), // 7 years
            EnableAutomaticCleanup = false, // Manual cleanup for compliance
            ArchivalConfig = ArchivalConfig.ForCompliance()
        };
    }

    /// <summary>
    /// Validates data retention configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (RawDataRetention <= TimeSpan.Zero)
            errors.Add("RawDataRetention must be greater than zero");

        if (AggregatedDataRetention <= TimeSpan.Zero)
            errors.Add("AggregatedDataRetention must be greater than zero");

        if (ReportRetention <= TimeSpan.Zero)
            errors.Add("ReportRetention must be greater than zero");

        if (AlertDataRetention <= TimeSpan.Zero)
            errors.Add("AlertDataRetention must be greater than zero");

        if (CleanupInterval <= TimeSpan.Zero)
            errors.Add("CleanupInterval must be greater than zero");

        errors.AddRange(CompressionConfig.Validate());
        errors.AddRange(ArchivalConfig.Validate());

        return errors;
    }
}