using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Consolidated configuration for performance monitoring, throttling, and resource management.
/// Merges ThrottlingConfig, PerformanceThrottlingConfig, LoadSheddingConfig, and ResourceLimitsConfig.
/// Designed for Unity game development with 60+ FPS performance targets.
/// </summary>
public sealed record PerformanceConfig
{
    #region Throttling Settings
    
    /// <summary>
    /// Whether throttling is enabled for health checks
    /// </summary>
    public bool EnableThrottling { get; init; } = false;
    
    /// <summary>
    /// Maximum number of concurrent health check operations
    /// </summary>
    public int MaxConcurrentOperations { get; init; } = 5;
    
    /// <summary>
    /// Maximum requests per second for health checks
    /// </summary>
    public int MaxRequestsPerSecond { get; init; } = 100;
    
    /// <summary>
    /// Burst allowance for temporary spikes
    /// </summary>
    public int BurstAllowance { get; init; } = 10;
    
    /// <summary>
    /// Throttling window duration
    /// </summary>
    public TimeSpan ThrottlingWindow { get; init; } = TimeSpan.FromSeconds(1);
    
    #endregion

    #region Load Shedding Settings
    
    /// <summary>
    /// Whether load shedding is enabled
    /// </summary>
    public bool EnableLoadShedding { get; init; } = false;
    
    /// <summary>
    /// CPU usage threshold for load shedding (0.0 to 1.0)
    /// </summary>
    public double LoadSheddingCpuThreshold { get; init; } = 0.8;
    
    /// <summary>
    /// Memory usage threshold for load shedding (0.0 to 1.0)
    /// </summary>
    public double LoadSheddingMemoryThreshold { get; init; } = 0.85;
    
    /// <summary>
    /// Percentage of health checks to shed under load (0.0 to 1.0)
    /// </summary>
    public double LoadSheddingPercentage { get; init; } = 0.5;
    
    #endregion

    #region Resource Limits
    
    /// <summary>
    /// Maximum memory usage for health checking in MB
    /// </summary>
    public int MaxMemoryUsageMB { get; init; } = 50;
    
    /// <summary>
    /// Maximum execution time for individual health checks
    /// </summary>
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Maximum total execution time for all health checks per cycle
    /// </summary>
    public TimeSpan MaxTotalExecutionTime { get; init; } = TimeSpan.FromMilliseconds(16); // Frame budget aware (16.67ms)
    
    /// <summary>
    /// Maximum CPU usage percentage for health checking (0.0 to 1.0)
    /// </summary>
    public double MaxCpuUsagePercentage { get; init; } = 0.1;
    
    #endregion

    #region Performance Monitoring
    
    /// <summary>
    /// Whether performance monitoring is enabled
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; } = true;
    
    /// <summary>
    /// Threshold for considering a health check slow (in milliseconds)
    /// </summary>
    public int SlowExecutionThresholdMs { get; init; } = 100;
    
    /// <summary>
    /// Whether to automatically adjust performance settings based on system load
    /// </summary>
    public bool EnableAdaptivePerformance { get; init; } = true;
    
    /// <summary>
    /// Interval for performance metric collection
    /// </summary>
    public TimeSpan PerformanceMetricInterval { get; init; } = TimeSpan.FromSeconds(10);
    
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates performance configuration optimized for high-performance games
    /// </summary>
    /// <returns>High-performance game configuration</returns>
    public static PerformanceConfig ForHighPerformanceGames()
    {
        return new PerformanceConfig
        {
            EnableThrottling = true,
            MaxConcurrentOperations = 2,
            MaxRequestsPerSecond = 30,
            EnableLoadShedding = true,
            LoadSheddingCpuThreshold = 0.6,
            LoadSheddingMemoryThreshold = 0.7,
            MaxMemoryUsageMB = 25,
            MaxExecutionTime = TimeSpan.FromMilliseconds(5),
            MaxTotalExecutionTime = TimeSpan.FromMilliseconds(10),
            MaxCpuUsagePercentage = 0.05,
            SlowExecutionThresholdMs = 50,
            EnableAdaptivePerformance = true
        };
    }

    /// <summary>
    /// Creates performance configuration for production environments
    /// </summary>
    /// <returns>Production performance configuration</returns>
    public static PerformanceConfig ForProduction()
    {
        return new PerformanceConfig
        {
            EnableThrottling = true,
            MaxConcurrentOperations = 10,
            MaxRequestsPerSecond = 100,
            EnableLoadShedding = true,
            LoadSheddingCpuThreshold = 0.8,
            LoadSheddingMemoryThreshold = 0.85,
            MaxMemoryUsageMB = 100,
            MaxExecutionTime = TimeSpan.FromSeconds(10),
            MaxTotalExecutionTime = TimeSpan.FromMilliseconds(50),
            MaxCpuUsagePercentage = 0.2,
            SlowExecutionThresholdMs = 200,
            EnableAdaptivePerformance = true
        };
    }

    /// <summary>
    /// Creates performance configuration for development environments
    /// </summary>
    /// <returns>Development performance configuration</returns>
    public static PerformanceConfig ForDevelopment()
    {
        return new PerformanceConfig
        {
            EnableThrottling = false,
            EnableLoadShedding = false,
            MaxConcurrentOperations = 20,
            MaxRequestsPerSecond = 1000,
            MaxMemoryUsageMB = 200,
            MaxExecutionTime = TimeSpan.FromSeconds(30),
            MaxTotalExecutionTime = TimeSpan.FromSeconds(5),
            MaxCpuUsagePercentage = 0.5,
            SlowExecutionThresholdMs = 1000,
            EnableAdaptivePerformance = false
        };
    }

    /// <summary>
    /// Creates minimal performance configuration
    /// </summary>
    /// <returns>Minimal performance configuration</returns>
    public static PerformanceConfig Minimal()
    {
        return new PerformanceConfig
        {
            EnableThrottling = false,
            EnableLoadShedding = false,
            EnablePerformanceMonitoring = false,
            EnableAdaptivePerformance = false,
            MaxConcurrentOperations = 1,
            MaxRequestsPerSecond = 10
        };
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates performance configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxConcurrentOperations <= 0)
            errors.Add("MaxConcurrentOperations must be greater than zero");
            
        if (MaxRequestsPerSecond <= 0)
            errors.Add("MaxRequestsPerSecond must be greater than zero");
            
        if (BurstAllowance < 0)
            errors.Add("BurstAllowance must be non-negative");
            
        if (ThrottlingWindow <= TimeSpan.Zero)
            errors.Add("ThrottlingWindow must be greater than zero");

        if (LoadSheddingCpuThreshold < 0.0 || LoadSheddingCpuThreshold > 1.0)
            errors.Add("LoadSheddingCpuThreshold must be between 0.0 and 1.0");
            
        if (LoadSheddingMemoryThreshold < 0.0 || LoadSheddingMemoryThreshold > 1.0)
            errors.Add("LoadSheddingMemoryThreshold must be between 0.0 and 1.0");
            
        if (LoadSheddingPercentage < 0.0 || LoadSheddingPercentage > 1.0)
            errors.Add("LoadSheddingPercentage must be between 0.0 and 1.0");

        if (MaxMemoryUsageMB <= 0)
            errors.Add("MaxMemoryUsageMB must be greater than zero");
            
        if (MaxExecutionTime <= TimeSpan.Zero)
            errors.Add("MaxExecutionTime must be greater than zero");
            
        if (MaxTotalExecutionTime <= TimeSpan.Zero)
            errors.Add("MaxTotalExecutionTime must be greater than zero");
            
        if (MaxCpuUsagePercentage < 0.0 || MaxCpuUsagePercentage > 1.0)
            errors.Add("MaxCpuUsagePercentage must be between 0.0 and 1.0");

        if (SlowExecutionThresholdMs <= 0)
            errors.Add("SlowExecutionThresholdMs must be greater than zero");
            
        if (PerformanceMetricInterval <= TimeSpan.Zero)
            errors.Add("PerformanceMetricInterval must be greater than zero");

        // Unity game development specific validations
        if (MaxTotalExecutionTime > TimeSpan.FromMilliseconds(16.67))
            errors.Add("MaxTotalExecutionTime should not exceed 16.67ms for 60 FPS target");

        return errors;
    }

    #endregion
}