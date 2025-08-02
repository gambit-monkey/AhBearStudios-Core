using System;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Defines performance budgets for pooling operations to maintain frame rate targets.
    /// Essential for Unity game development where operations must complete within frame time limits.
    /// </summary>
    public sealed class PerformanceBudget
    {
        /// <summary>
        /// Maximum time allowed for a single pool operation (Get/Return).
        /// </summary>
        public TimeSpan MaxOperationTime { get; init; }

        /// <summary>
        /// Maximum time allowed for pool validation operations.
        /// </summary>
        public TimeSpan MaxValidationTime { get; init; }

        /// <summary>
        /// Maximum time allowed for pool expansion operations.
        /// </summary>
        public TimeSpan MaxExpansionTime { get; init; }

        /// <summary>
        /// Maximum time allowed for pool contraction operations.
        /// </summary>
        public TimeSpan MaxContractionTime { get; init; }

        /// <summary>
        /// Target frame rate for performance budgeting (60 FPS, 30 FPS, etc.).
        /// </summary>
        public int TargetFrameRate { get; init; }

        /// <summary>
        /// Percentage of frame time that pooling operations can consume.
        /// </summary>
        public double FrameTimePercentage { get; init; }

        /// <summary>
        /// Calculated frame time budget based on target frame rate.
        /// </summary>
        public TimeSpan FrameTimeBudget => TimeSpan.FromMilliseconds(1000.0 / TargetFrameRate * FrameTimePercentage);

        /// <summary>
        /// Whether performance monitoring is enabled.
        /// </summary>
        public bool EnablePerformanceMonitoring { get; init; }

        /// <summary>
        /// Whether to log warnings when operations exceed budget.
        /// </summary>
        public bool LogPerformanceWarnings { get; init; }

        /// <summary>
        /// Creates a performance budget optimized for 60 FPS gameplay.
        /// </summary>
        /// <returns>60 FPS optimized performance budget</returns>
        public static PerformanceBudget For60FPS()
        {
            return new PerformanceBudget
            {
                MaxOperationTime = TimeSpan.FromMilliseconds(100), // 0.1ms
                MaxValidationTime = TimeSpan.FromMilliseconds(1), // 1ms
                MaxExpansionTime = TimeSpan.FromMilliseconds(2), // 2ms
                MaxContractionTime = TimeSpan.FromMilliseconds(1), // 1ms
                TargetFrameRate = 60,
                FrameTimePercentage = 0.05, // 5% of frame time
                EnablePerformanceMonitoring = true,
                LogPerformanceWarnings = true
            };
        }

        /// <summary>
        /// Creates a performance budget optimized for 30 FPS gameplay.
        /// </summary>
        /// <returns>30 FPS optimized performance budget</returns>
        public static PerformanceBudget For30FPS()
        {
            return new PerformanceBudget
            {
                MaxOperationTime = TimeSpan.FromMilliseconds(200), // 0.2ms
                MaxValidationTime = TimeSpan.FromMilliseconds(2), // 2ms
                MaxExpansionTime = TimeSpan.FromMilliseconds(5), // 5ms
                MaxContractionTime = TimeSpan.FromMilliseconds(2), // 2ms
                TargetFrameRate = 30,
                FrameTimePercentage = 0.1, // 10% of frame time
                EnablePerformanceMonitoring = true,
                LogPerformanceWarnings = true
            };
        }

        /// <summary>
        /// Creates a relaxed performance budget for development/testing.
        /// </summary>
        /// <returns>Development optimized performance budget</returns>
        public static PerformanceBudget ForDevelopment()
        {
            return new PerformanceBudget
            {
                MaxOperationTime = TimeSpan.FromMilliseconds(1), // 1ms
                MaxValidationTime = TimeSpan.FromMilliseconds(10), // 10ms
                MaxExpansionTime = TimeSpan.FromMilliseconds(20), // 20ms
                MaxContractionTime = TimeSpan.FromMilliseconds(10), // 10ms
                TargetFrameRate = 60,
                FrameTimePercentage = 0.2, // 20% of frame time
                EnablePerformanceMonitoring = false,
                LogPerformanceWarnings = false
            };
        }

        /// <summary>
        /// Creates an unlimited performance budget (no restrictions).
        /// </summary>
        /// <returns>Unlimited performance budget</returns>
        public static PerformanceBudget Unlimited()
        {
            return new PerformanceBudget
            {
                MaxOperationTime = TimeSpan.MaxValue,
                MaxValidationTime = TimeSpan.MaxValue,
                MaxExpansionTime = TimeSpan.MaxValue,
                MaxContractionTime = TimeSpan.MaxValue,
                TargetFrameRate = 60,
                FrameTimePercentage = 1.0,
                EnablePerformanceMonitoring = false,
                LogPerformanceWarnings = false
            };
        }

        /// <summary>
        /// Validates that the performance budget configuration is reasonable.
        /// </summary>
        /// <returns>True if the budget is valid</returns>
        public bool IsValid()
        {
            return TargetFrameRate > 0 &&
                   FrameTimePercentage > 0 && FrameTimePercentage <= 1.0 &&
                   MaxOperationTime > TimeSpan.Zero &&
                   MaxValidationTime > TimeSpan.Zero &&
                   MaxExpansionTime > TimeSpan.Zero &&
                   MaxContractionTime > TimeSpan.Zero;
        }
    }
}