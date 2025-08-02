using System;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies.Models;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Enhanced interface for pool sizing and management strategies.
    /// Defines how pools should expand, contract, and manage their objects with production-ready features.
    /// Optimized for Unity game development with frame-budget awareness and performance monitoring.
    /// </summary>
    public interface IPoolingStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        string Name { get; }
        
        // Size management
        /// <summary>
        /// Calculates the target size for the pool based on current statistics.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>Target pool size</returns>
        int CalculateTargetSize(PoolStatistics statistics);
        
        /// <summary>
        /// Determines if the pool should expand.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should expand</returns>
        bool ShouldExpand(PoolStatistics statistics);
        
        /// <summary>
        /// Determines if the pool should contract.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should contract</returns>
        bool ShouldContract(PoolStatistics statistics);
        
        // Object lifecycle
        /// <summary>
        /// Determines if a new object should be created.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if a new object should be created</returns>
        bool ShouldCreateNew(PoolStatistics statistics);
        
        /// <summary>
        /// Determines if objects should be destroyed.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if objects should be destroyed</returns>
        bool ShouldDestroy(PoolStatistics statistics);
        
        // Validation
        /// <summary>
        /// Gets the interval between validation checks.
        /// </summary>
        /// <returns>Validation interval</returns>
        TimeSpan GetValidationInterval();
        
        /// <summary>
        /// Validates the pool configuration against this strategy.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfiguration(PoolConfiguration config);

        // Production-ready enhancements
        /// <summary>
        /// Determines if the circuit breaker should be triggered based on current statistics.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if circuit breaker should be triggered</returns>
        bool ShouldTriggerCircuitBreaker(PoolStatistics statistics);

        /// <summary>
        /// Gets the performance budget for this strategy.
        /// </summary>
        /// <returns>Performance budget configuration</returns>
        PerformanceBudget GetPerformanceBudget();

        /// <summary>
        /// Gets the current health status of this strategy.
        /// </summary>
        /// <returns>Strategy health status</returns>
        StrategyHealthStatus GetHealthStatus();

        /// <summary>
        /// Called when a pool operation starts (for performance monitoring).
        /// </summary>
        void OnPoolOperationStart();

        /// <summary>
        /// Called when a pool operation completes (for performance monitoring).
        /// </summary>
        /// <param name="duration">Duration of the operation</param>
        void OnPoolOperationComplete(TimeSpan duration);

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        void OnPoolError(Exception error);

        /// <summary>
        /// Gets network-specific metrics if this strategy supports network optimizations.
        /// </summary>
        /// <returns>Network pooling metrics, or null if not supported</returns>
        NetworkPoolingMetrics GetNetworkMetrics();

        /// <summary>
        /// Gets the strategy configuration.
        /// </summary>
        /// <returns>Strategy configuration</returns>
        PoolingStrategyConfig GetConfiguration();
    }
}