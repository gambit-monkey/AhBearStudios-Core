using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthCheck.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Health thresholds configuration for determining overall system health with advanced calculation methods
/// </summary>
public sealed record HealthThresholds
{
    /// <summary>
    /// Unique identifier for this health thresholds configuration
    /// </summary>
    public FixedString64Bytes Id { get; init; } = GenerateId();

    /// <summary>
    /// Display name for this health thresholds configuration
    /// </summary>
    public string Name { get; init; } = "Default Health Thresholds";

    /// <summary>
    /// Percentage of healthy checks required for overall healthy status (0.0 to 1.0)
    /// </summary>
    public double HealthyThreshold { get; init; } = 0.9;

    /// <summary>
    /// Percentage of unhealthy checks that triggers degraded status (0.0 to 1.0)
    /// </summary>
    public double DegradedThreshold { get; init; } = 0.2;

    /// <summary>
    /// Percentage of unhealthy checks that triggers unhealthy status (0.0 to 1.0)
    /// </summary>
    public double UnhealthyThreshold { get; init; } = 0.5;

    /// <summary>
    /// Whether to use weighted calculation based on health check categories
    /// </summary>
    public bool UseWeightedCalculation { get; init; } = true;

    /// <summary>
    /// Whether to consider degraded checks as partially unhealthy in calculations
    /// </summary>
    public bool IncludeDegradedInCalculation { get; init; } = true;

    /// <summary>
    /// Weight factor for degraded checks when included in calculation (0.0 to 1.0)
    /// </summary>
    public double DegradedWeight { get; init; } = 0.5;

    /// <summary>
    /// Weights for different health check categories in overall health calculation
    /// </summary>
    public Dictionary<HealthCheckCategory, double> CategoryWeights { get; init; } = new()
    {
        { HealthCheckCategory.System, 1.0 },        // Critical system checks
        { HealthCheckCategory.Database, 0.9 },      // Database connectivity
        { HealthCheckCategory.Network, 0.7 },       // Network services
        { HealthCheckCategory.Performance, 0.5 },   // Performance metrics
        { HealthCheckCategory.Security, 0.8 },      // Security checks
        { HealthCheckCategory.CircuitBreaker, 0.6 }, // Circuit breaker status
        { HealthCheckCategory.Custom, 0.4 }         // Custom checks
    };

    /// <summary>
    /// Critical health checks that immediately affect overall status regardless of thresholds
    /// </summary>
    public HashSet<FixedString64Bytes> CriticalHealthChecks { get; init; } = new();

    /// <summary>
    /// Health checks that should be ignored in overall status calculation
    /// </summary>
    public HashSet<FixedString64Bytes> IgnoredHealthChecks { get; init; } = new();

    /// <summary>
    /// Minimum number of health checks required for valid threshold calculation
    /// </summary>
    public int MinimumHealthChecks { get; init; } = 1;

    /// <summary>
    /// Time window for evaluating health check results for threshold calculation
    /// </summary>
    public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to use sliding window for threshold evaluation
    /// </summary>
    public bool UseSlidingWindow { get; init; } = true;

    /// <summary>
    /// Calculation method for determining overall health status
    /// </summary>
    public HealthCalculationMethod CalculationMethod { get; init; } = HealthCalculationMethod.WeightedAverage;

    /// <summary>
    /// Hysteresis configuration to prevent status flapping
    /// </summary>
    public HysteresisConfig HysteresisConfig { get; init; } = new();

    /// <summary>
    /// Configuration for trend-based health evaluation
    /// </summary>
    public TrendAnalysisConfig TrendAnalysis { get; init; } = new();

    /// <summary>
    /// Configuration for time-based health evaluation
    /// </summary>
    public TimeBasedEvaluationConfig TimeBasedEvaluation { get; init; } = new();

    /// <summary>
    /// Advanced threshold rules for complex scenarios
    /// </summary>
    public List<AdvancedThresholdRule> AdvancedRules { get; init; } = new();

    /// <summary>
    /// Custom metadata for this health thresholds configuration
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Validates the health thresholds configuration
    /// </summary>
    /// <returns>List of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name cannot be null or empty");

        if (HealthyThreshold < 0.0 || HealthyThreshold > 1.0)
            errors.Add("HealthyThreshold must be between 0.0 and 1.0");

        if (DegradedThreshold < 0.0 || DegradedThreshold > 1.0)
            errors.Add("DegradedThreshold must be between 0.0 and 1.0");

        if (UnhealthyThreshold < 0.0 || UnhealthyThreshold > 1.0)
            errors.Add("UnhealthyThreshold must be between 0.0 and 1.0");

        if (DegradedWeight < 0.0 || DegradedWeight > 1.0)
            errors.Add("DegradedWeight must be between 0.0 and 1.0");

        // Validate threshold ordering
        if (DegradedThreshold >= UnhealthyThreshold)
            errors.Add("DegradedThreshold must be less than UnhealthyThreshold");

        if (MinimumHealthChecks <= 0)
            errors.Add("MinimumHealthChecks must be greater than zero");

        if (EvaluationWindow <= TimeSpan.Zero)
            errors.Add("EvaluationWindow must be greater than zero");

        if (!Enum.IsDefined(typeof(HealthCalculationMethod), CalculationMethod))
            errors.Add($"Invalid calculation method: {CalculationMethod}");

        // Validate category weights
        foreach (var kvp in CategoryWeights)
        {
            if (kvp.Value < 0.0 || kvp.Value > 2.0)
                errors.Add($"Category weight for {kvp.Key} must be between 0.0 and 2.0, found: {kvp.Value}");
        }

        // Validate that critical and ignored health checks don't overlap
        var overlapping = CriticalHealthChecks.Intersect(IgnoredHealthChecks);
        if (overlapping.Any())
        {
            errors.Add($"Health checks cannot be both critical and ignored: {string.Join(", ", overlapping)}");
        }

        // Validate nested configurations
        errors.AddRange(HysteresisConfig.Validate());
        errors.AddRange(TrendAnalysis.Validate());
        errors.AddRange(TimeBasedEvaluation.Validate());

        foreach (var rule in AdvancedRules)
        {
            errors.AddRange(rule.Validate());
        }

        return errors;
    }

    /// <summary>
    /// Creates health thresholds optimized for critical systems
    /// </summary>
    /// <returns>Critical system health thresholds</returns>
    public static HealthThresholds ForCriticalSystem()
    {
        return new HealthThresholds
        {
            Name = "Critical System Health Thresholds",
            HealthyThreshold = 0.95,        // 95% healthy required
            DegradedThreshold = 0.1,        // 10% unhealthy triggers degraded
            UnhealthyThreshold = 0.25,      // 25% unhealthy triggers unhealthy
            UseWeightedCalculation = true,
            IncludeDegradedInCalculation = true,
            DegradedWeight = 0.7,          // Degraded checks count as 70% unhealthy
            MinimumHealthChecks = 3,
            EvaluationWindow = TimeSpan.FromMinutes(2),
            CalculationMethod = HealthCalculationMethod.WeightedAverage,
            CategoryWeights = new Dictionary<HealthCheckCategory, double>
            {
                { HealthCheckCategory.System, 1.5 },      // Extra weight for system checks
                { HealthCheckCategory.Database, 1.2 },
                { HealthCheckCategory.Network, 0.8 },
                { HealthCheckCategory.Performance, 0.4 },
                { HealthCheckCategory.Security, 1.0 },
                { HealthCheckCategory.CircuitBreaker, 0.7 },
                { HealthCheckCategory.Custom, 0.3 }
            },
            HysteresisConfig = HysteresisConfig.ForCriticalSystem(),
            TrendAnalysis = TrendAnalysisConfig.ForCriticalSystem()
        };
    }

    /// <summary>
    /// Creates health thresholds optimized for high-availability systems
    /// </summary>
    /// <returns>High-availability health thresholds</returns>
    public static HealthThresholds ForHighAvailability()
    {
        return new HealthThresholds
        {
            Name = "High Availability Health Thresholds",
            HealthyThreshold = 0.85,        // 85% healthy required
            DegradedThreshold = 0.2,        // 20% unhealthy triggers degraded
            UnhealthyThreshold = 0.4,       // 40% unhealthy triggers unhealthy
            UseWeightedCalculation = true,
            IncludeDegradedInCalculation = true,
            DegradedWeight = 0.5,
            MinimumHealthChecks = 5,
            EvaluationWindow = TimeSpan.FromMinutes(5),
            CalculationMethod = HealthCalculationMethod.WeightedAverage,
            HysteresisConfig = HysteresisConfig.ForHighAvailability(),
            TrendAnalysis = TrendAnalysisConfig.ForHighAvailability(),
            TimeBasedEvaluation = TimeBasedEvaluationConfig.ForHighAvailability()
        };
    }

    /// <summary>
    /// Creates health thresholds optimized for development environments
    /// </summary>
    /// <returns>Development health thresholds</returns>
    public static HealthThresholds ForDevelopment()
    {
        return new HealthThresholds
        {
            Name = "Development Health Thresholds",
            HealthyThreshold = 0.7,         // 70% healthy required
            DegradedThreshold = 0.3,        // 30% unhealthy triggers degraded
            UnhealthyThreshold = 0.6,       // 60% unhealthy triggers unhealthy
            UseWeightedCalculation = false,  // Simple calculation for development
            IncludeDegradedInCalculation = false,
            MinimumHealthChecks = 1,
            EvaluationWindow = TimeSpan.FromMinutes(1),
            CalculationMethod = HealthCalculationMethod.Simple,
            HysteresisConfig = new HysteresisConfig { Enabled = false }
        };
    }

    /// <summary>
    /// Creates health thresholds with balanced settings for general use
    /// </summary>
    /// <returns>Balanced health thresholds</returns>
    public static HealthThresholds Balanced()
    {
        return new HealthThresholds(); // Uses default values which are already balanced
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