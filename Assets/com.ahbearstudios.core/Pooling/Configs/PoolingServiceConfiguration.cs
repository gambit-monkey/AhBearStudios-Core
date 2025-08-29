using System;
using AhBearStudios.Core.Pooling.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Configs
{
    /// <summary>
    /// Configuration for the PoolingService with production-ready settings.
    /// Contains settings for specialized services, performance budgets, and system integration.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public class PoolingServiceConfiguration
    {
        #region Core Settings

        /// <summary>
        /// Gets or sets the name of the pooling service instance for identification.
        /// </summary>
        public FixedString64Bytes ServiceName { get; set; } = "DefaultPoolingService";

        /// <summary>
        /// Gets or sets whether to enable comprehensive health monitoring.
        /// </summary>
        public bool EnableHealthMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable performance budget enforcement.
        /// </summary>
        public bool EnablePerformanceBudgets { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable automatic pool scaling.
        /// </summary>
        public bool EnableAutoScaling { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable error recovery mechanisms.
        /// </summary>
        public bool EnableErrorRecovery { get; set; } = true;

        #endregion

        #region Performance Settings

        /// <summary>
        /// Gets or sets the default performance budget for pool operations.
        /// Used when specific budgets are not provided.
        /// </summary>
        public PerformanceBudget DefaultPerformanceBudget { get; set; } = new PerformanceBudget
        {
            MaxOperationTime = TimeSpan.FromMilliseconds(1.0), // 1ms for 60 FPS compatibility
            MaxValidationTime = TimeSpan.FromMilliseconds(2.0), // 2ms for validation operations
            MaxExpansionTime = TimeSpan.FromMilliseconds(5.0), // 5ms for pool expansion
            MaxContractionTime = TimeSpan.FromMilliseconds(2.0), // 2ms for pool contraction
            TargetFrameRate = 60,
            FrameTimePercentage = 0.05, // 5% of frame time
            EnablePerformanceMonitoring = true,
            LogPerformanceWarnings = true
        };

        /// <summary>
        /// Gets or sets the maximum number of pools that can be registered.
        /// </summary>
        public int MaxRegisteredPools { get; set; } = 100;

        /// <summary>
        /// Gets or sets the timeout for async pool operations.
        /// </summary>
        public TimeSpan AsyncOperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        #endregion

        #region Auto-Scaling Settings

        /// <summary>
        /// Gets or sets the interval between auto-scaling checks.
        /// </summary>
        public TimeSpan AutoScalingCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the minimum utilization threshold for pool expansion.
        /// </summary>
        public double ScalingExpansionThreshold { get; set; } = 0.8; // 80%

        /// <summary>
        /// Gets or sets the maximum utilization threshold for pool contraction.
        /// </summary>
        public double ScalingContractionThreshold { get; set; } = 0.3; // 30%

        #endregion

        #region Error Recovery Settings

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed operations.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets whether to enable emergency recovery mechanisms.
        /// </summary>
        public bool EnableEmergencyRecovery { get; set; } = true;

        #endregion

        #region Validation Settings

        /// <summary>
        /// Gets or sets whether to enable object validation during return operations.
        /// </summary>
        public bool EnableObjectValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable pool validation checks.
        /// </summary>
        public bool EnablePoolValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for automatic pool validation.
        /// </summary>
        public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMinutes(5);

        #endregion

        #region Health Check Settings

        /// <summary>
        /// Gets or sets the interval for health check operations.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the threshold for triggering health alerts (failure rate).
        /// </summary>
        public double HealthAlertThreshold { get; set; } = 0.1; // 10%

        /// <summary>
        /// Gets or sets whether to enable circuit breaker functionality.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        #endregion

        #region Statistics Settings

        /// <summary>
        /// Gets or sets whether to collect detailed statistics.
        /// </summary>
        public bool EnableDetailedStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for statistics updates.
        /// </summary>
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets how long to keep historical statistics.
        /// </summary>
        public TimeSpan StatisticsRetentionPeriod { get; set; } = TimeSpan.FromHours(24);

        #endregion

        #region Factory Method

        /// <summary>
        /// Creates a default configuration optimized for Unity game development.
        /// </summary>
        /// <param name="serviceName">Name of the pooling service instance</param>
        /// <returns>Default configuration with game-optimized settings</returns>
        public static PoolingServiceConfiguration CreateDefault(string serviceName = "DefaultPoolingService")
        {
            return new PoolingServiceConfiguration
            {
                ServiceName = new FixedString64Bytes(serviceName),
                EnableHealthMonitoring = true,
                EnablePerformanceBudgets = true,
                EnableAutoScaling = false, // Disabled by default for predictable performance
                EnableErrorRecovery = true,
                DefaultPerformanceBudget = new PerformanceBudget
                {
                    MaxOperationTime = TimeSpan.FromMilliseconds(1.0), // 1ms target for 60 FPS
                    MaxValidationTime = TimeSpan.FromMilliseconds(2.0), // 2ms for validation
                    MaxExpansionTime = TimeSpan.FromMilliseconds(5.0), // 5ms for expansion
                    MaxContractionTime = TimeSpan.FromMilliseconds(2.0), // 2ms for contraction
                    TargetFrameRate = 60,
                    FrameTimePercentage = 0.05, // 5% of frame time
                    EnablePerformanceMonitoring = true,
                    LogPerformanceWarnings = true
                },
                MaxRegisteredPools = 100,
                AsyncOperationTimeout = TimeSpan.FromSeconds(5),
                AutoScalingCheckInterval = TimeSpan.FromSeconds(30),
                ScalingExpansionThreshold = 0.8,
                ScalingContractionThreshold = 0.3,
                MaxRetryAttempts = 3,
                RetryDelay = TimeSpan.FromMilliseconds(100),
                EnableEmergencyRecovery = true,
                EnableObjectValidation = true,
                EnablePoolValidation = true,
                ValidationInterval = TimeSpan.FromMinutes(5),
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                HealthAlertThreshold = 0.1,
                EnableCircuitBreaker = true,
                EnableDetailedStatistics = true,
                StatisticsUpdateInterval = TimeSpan.FromSeconds(10),
                StatisticsRetentionPeriod = TimeSpan.FromHours(24)
            };
        }

        /// <summary>
        /// Creates a high-performance configuration with minimal overhead.
        /// Suitable for production environments with strict performance requirements.
        /// </summary>
        /// <param name="serviceName">Name of the pooling service instance</param>
        /// <returns>High-performance configuration</returns>
        public static PoolingServiceConfiguration CreateHighPerformance(string serviceName = "HighPerfPoolingService")
        {
            return new PoolingServiceConfiguration
            {
                ServiceName = new FixedString64Bytes(serviceName),
                EnableHealthMonitoring = false, // Reduced monitoring overhead
                EnablePerformanceBudgets = true,
                EnableAutoScaling = false,
                EnableErrorRecovery = false, // Reduced error handling overhead
                DefaultPerformanceBudget = new PerformanceBudget
                {
                    MaxOperationTime = TimeSpan.FromMilliseconds(0.5), // Tighter constraints
                    MaxValidationTime = TimeSpan.FromMilliseconds(1.0), // 1ms for validation
                    MaxExpansionTime = TimeSpan.FromMilliseconds(2.0), // 2ms for expansion
                    MaxContractionTime = TimeSpan.FromMilliseconds(1.0), // 1ms for contraction
                    TargetFrameRate = 60,
                    FrameTimePercentage = 0.03, // 3% of frame time for high performance
                    EnablePerformanceMonitoring = false, // Reduced monitoring overhead
                    LogPerformanceWarnings = false
                },
                MaxRegisteredPools = 50,
                AsyncOperationTimeout = TimeSpan.FromSeconds(2),
                EnableObjectValidation = false, // Skip validation for speed
                EnablePoolValidation = false,
                EnableCircuitBreaker = false,
                EnableDetailedStatistics = false, // Minimal statistics
                StatisticsUpdateInterval = TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>
        /// Creates a development configuration with comprehensive monitoring and debugging features.
        /// </summary>
        /// <param name="serviceName">Name of the pooling service instance</param>
        /// <returns>Development configuration with full monitoring</returns>
        public static PoolingServiceConfiguration CreateDevelopment(string serviceName = "DevPoolingService")
        {
            return new PoolingServiceConfiguration
            {
                ServiceName = new FixedString64Bytes(serviceName),
                EnableHealthMonitoring = true,
                EnablePerformanceBudgets = true,
                EnableAutoScaling = true, // Enable for testing
                EnableErrorRecovery = true,
                DefaultPerformanceBudget = new PerformanceBudget
                {
                    MaxOperationTime = TimeSpan.FromMilliseconds(5.0), // More lenient for debugging
                    MaxValidationTime = TimeSpan.FromMilliseconds(10.0), // 10ms for validation
                    MaxExpansionTime = TimeSpan.FromMilliseconds(20.0), // 20ms for expansion
                    MaxContractionTime = TimeSpan.FromMilliseconds(10.0), // 10ms for contraction
                    TargetFrameRate = 60,
                    FrameTimePercentage = 0.2, // 20% of frame time for development
                    EnablePerformanceMonitoring = true,
                    LogPerformanceWarnings = true
                },
                MaxRegisteredPools = 200,
                AsyncOperationTimeout = TimeSpan.FromSeconds(30),
                AutoScalingCheckInterval = TimeSpan.FromSeconds(10), // More frequent checks
                EnableObjectValidation = true,
                EnablePoolValidation = true,
                ValidationInterval = TimeSpan.FromMinutes(1), // More frequent validation
                HealthCheckInterval = TimeSpan.FromSeconds(30),
                EnableCircuitBreaker = true,
                EnableDetailedStatistics = true,
                StatisticsUpdateInterval = TimeSpan.FromSeconds(5), // Frequent updates
                StatisticsRetentionPeriod = TimeSpan.FromDays(7) // Longer retention
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration settings.
        /// </summary>
        /// <returns>True if the configuration is valid</returns>
        public bool IsValid()
        {
            if (MaxRegisteredPools <= 0) return false;
            if (AsyncOperationTimeout <= TimeSpan.Zero) return false;
            if (ScalingExpansionThreshold <= 0 || ScalingExpansionThreshold > 1) return false;
            if (ScalingContractionThreshold <= 0 || ScalingContractionThreshold > 1) return false;
            if (MaxRetryAttempts < 0) return false;
            if (RetryDelay < TimeSpan.Zero) return false;
            if (HealthAlertThreshold < 0 || HealthAlertThreshold > 1) return false;
            
            return true;
        }

        #endregion
    }
}