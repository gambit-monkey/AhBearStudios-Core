using System;
using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Interface for pool sizing and management strategies.
    /// Defines how pools should expand, contract, and manage their objects.
    /// </summary>
    public interface IPoolStrategy
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
    }
}