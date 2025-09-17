using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Consolidated configuration for degradation impact assessment and monitoring.
/// Merges DegradationImpactConfig, DegradationLevelConfig, and DegradationMonitoringConfig.
/// Designed for Unity game development with performance-first approach.
/// </summary>
public sealed record DegradationConfig
{
    #region Impact Settings
    
    /// <summary>
    /// Impact level when this health check is degraded
    /// </summary>
    public DegradationLevel DegradedImpact { get; init; } = DegradationLevel.Minor;

    /// <summary>
    /// Impact level when this health check is unhealthy
    /// </summary>
    public DegradationLevel UnhealthyImpact { get; init; } = DegradationLevel.Moderate;

    /// <summary>
    /// Features that should be disabled when this check is unhealthy
    /// </summary>
    public HashSet<FixedString64Bytes> DisabledFeatures { get; init; } = new();

    /// <summary>
    /// Services that should be degraded when this check is unhealthy
    /// </summary>
    public HashSet<FixedString64Bytes> DegradedServices { get; init; } = new();
    
    #endregion

    #region Level Configuration
    
    /// <summary>
    /// Current degradation level for this configuration
    /// </summary>
    public DegradationLevel Level { get; init; } = DegradationLevel.None;
    
    /// <summary>
    /// Threshold percentage for minor degradation (0.0 to 1.0)
    /// </summary>
    public double MinorDegradationThreshold { get; init; } = 0.1;
    
    /// <summary>
    /// Threshold percentage for moderate degradation (0.0 to 1.0)
    /// </summary>
    public double ModerateDegradationThreshold { get; init; } = 0.25;
    
    /// <summary>
    /// Threshold percentage for major degradation (0.0 to 1.0)
    /// </summary>
    public double MajorDegradationThreshold { get; init; } = 0.5;
    
    #endregion

    #region Monitoring Settings
    
    /// <summary>
    /// Whether degradation monitoring is enabled
    /// </summary>
    public bool EnableMonitoring { get; init; } = true;

    /// <summary>
    /// Interval between degradation monitoring checks
    /// </summary>
    public TimeSpan MonitoringInterval { get; init; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Whether to automatically adjust degradation levels based on health status
    /// </summary>
    public bool EnableAutomaticAdjustment { get; init; } = true;
    
    /// <summary>
    /// Time window for degradation trend analysis
    /// </summary>
    public TimeSpan TrendWindow { get; init; } = TimeSpan.FromMinutes(5);
    
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates degradation configuration for critical game systems
    /// </summary>
    /// <returns>Critical system degradation configuration</returns>
    public static DegradationConfig ForCriticalSystem()
    {
        return new DegradationConfig
        {
            DegradedImpact = DegradationLevel.Moderate,
            UnhealthyImpact = DegradationLevel.Severe,
            MinorDegradationThreshold = 0.05,
            ModerateDegradationThreshold = 0.1,
            MajorDegradationThreshold = 0.2,
            MonitoringInterval = TimeSpan.FromSeconds(15),
            EnableAutomaticAdjustment = true,
            TrendWindow = TimeSpan.FromMinutes(2)
        };
    }

    /// <summary>
    /// Creates degradation configuration for performance monitoring
    /// </summary>
    /// <returns>Performance monitoring degradation configuration</returns>
    public static DegradationConfig ForPerformanceMonitoring()
    {
        return new DegradationConfig
        {
            DegradedImpact = DegradationLevel.Minor,
            UnhealthyImpact = DegradationLevel.Moderate,
            MinorDegradationThreshold = 0.2,
            ModerateDegradationThreshold = 0.4,
            MajorDegradationThreshold = 0.7,
            MonitoringInterval = TimeSpan.FromMinutes(1),
            EnableAutomaticAdjustment = false,
            TrendWindow = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>
    /// Creates minimal degradation configuration for development
    /// </summary>
    /// <returns>Development degradation configuration</returns>
    public static DegradationConfig ForDevelopment()
    {
        return new DegradationConfig
        {
            EnableMonitoring = false,
            EnableAutomaticAdjustment = false,
            DegradedImpact = DegradationLevel.None,
            UnhealthyImpact = DegradationLevel.Minor
        };
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates degradation configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!System.Enum.IsDefined(typeof(DegradationLevel), DegradedImpact))
            errors.Add($"Invalid degraded impact level: {DegradedImpact}");

        if (!System.Enum.IsDefined(typeof(DegradationLevel), UnhealthyImpact))
            errors.Add($"Invalid unhealthy impact level: {UnhealthyImpact}");

        if (DegradedImpact >= UnhealthyImpact)
            errors.Add("Unhealthy impact should be more severe than degraded impact");
            
        if (MinorDegradationThreshold < 0.0 || MinorDegradationThreshold > 1.0)
            errors.Add("MinorDegradationThreshold must be between 0.0 and 1.0");
            
        if (ModerateDegradationThreshold < 0.0 || ModerateDegradationThreshold > 1.0)
            errors.Add("ModerateDegradationThreshold must be between 0.0 and 1.0");
            
        if (MajorDegradationThreshold < 0.0 || MajorDegradationThreshold > 1.0)
            errors.Add("MajorDegradationThreshold must be between 0.0 and 1.0");
            
        if (MinorDegradationThreshold >= ModerateDegradationThreshold)
            errors.Add("ModerateDegradationThreshold must be greater than MinorDegradationThreshold");
            
        if (ModerateDegradationThreshold >= MajorDegradationThreshold)
            errors.Add("MajorDegradationThreshold must be greater than ModerateDegradationThreshold");

        if (MonitoringInterval <= TimeSpan.Zero)
            errors.Add("MonitoringInterval must be greater than zero");
            
        if (TrendWindow <= TimeSpan.Zero)
            errors.Add("TrendWindow must be greater than zero");

        return errors;
    }

    #endregion
}