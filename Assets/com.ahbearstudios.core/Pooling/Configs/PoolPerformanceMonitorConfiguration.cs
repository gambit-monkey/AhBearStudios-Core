using System;

namespace AhBearStudios.Core.Pooling.Configs
{
    public sealed class PoolPerformanceMonitorConfiguration
    {
        public bool EnablePerformanceMonitoring { get; init; } = true;
        
        public bool LogPerformanceWarnings { get; init; } = true;
        
        public bool EnablePerformanceBudgets { get; init; } = true;
        
        public bool EnableAlerts { get; init; } = true;
        
        public bool EnableProfiling { get; init; } = true;
        
        public double ViolationRateThreshold { get; init; } = 10.0;
        
        public double AverageTimeThreshold { get; init; } = 0.8;
        
        public double CriticalViolationMultiplier { get; init; } = 2.0;
        
        public bool EnableStatisticsCollection { get; init; } = true;
        
        public int MaxStatisticsHistoryCount { get; init; } = 1000;
        
        public TimeSpan StatisticsRetentionPeriod { get; init; } = TimeSpan.FromHours(1);
        
        public bool EnableOperationTypeTracking { get; init; } = true;
        
        public bool EnablePerformanceReporting { get; init; } = true;
        
        public TimeSpan ReportingInterval { get; init; } = TimeSpan.FromMinutes(5);

        public static PoolPerformanceMonitorConfiguration Default => new();
        
        public static PoolPerformanceMonitorConfiguration HighPerformance => new()
        {
            EnablePerformanceMonitoring = true,
            LogPerformanceWarnings = true,
            EnablePerformanceBudgets = true,
            EnableAlerts = true,
            EnableProfiling = true,
            ViolationRateThreshold = 5.0,
            AverageTimeThreshold = 0.6,
            CriticalViolationMultiplier = 1.5,
            MaxStatisticsHistoryCount = 2000,
            StatisticsRetentionPeriod = TimeSpan.FromHours(2),
            ReportingInterval = TimeSpan.FromMinutes(1)
        };
        
        public static PoolPerformanceMonitorConfiguration Minimal => new()
        {
            EnablePerformanceMonitoring = true,
            LogPerformanceWarnings = false,
            EnablePerformanceBudgets = false,
            EnableAlerts = false,
            EnableProfiling = false,
            ViolationRateThreshold = 20.0,
            AverageTimeThreshold = 0.9,
            MaxStatisticsHistoryCount = 100,
            StatisticsRetentionPeriod = TimeSpan.FromMinutes(15),
            EnablePerformanceReporting = false
        };
        
        public static PoolPerformanceMonitorConfiguration Disabled => new()
        {
            EnablePerformanceMonitoring = false,
            LogPerformanceWarnings = false,
            EnablePerformanceBudgets = false,
            EnableAlerts = false,
            EnableProfiling = false,
            EnableStatisticsCollection = false,
            EnableOperationTypeTracking = false,
            EnablePerformanceReporting = false
        };
    }
}