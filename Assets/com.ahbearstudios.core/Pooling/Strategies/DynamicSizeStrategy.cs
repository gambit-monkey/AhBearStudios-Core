using System;
using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Dynamic pool strategy that adjusts pool size based on usage patterns.
    /// Expands when utilization is high and contracts when objects are idle.
    /// </summary>
    public class DynamicSizeStrategy : IPoolStrategy
    {
        private readonly double _expandThreshold;
        private readonly double _contractThreshold;
        private readonly double _maxUtilization;
        private readonly TimeSpan _validationInterval;
        private readonly TimeSpan _idleTimeThreshold;

        /// <summary>
        /// Initializes a new instance of the DynamicSizeStrategy.
        /// </summary>
        /// <param name="expandThreshold">Utilization threshold to trigger expansion (0.0-1.0)</param>
        /// <param name="contractThreshold">Utilization threshold to trigger contraction (0.0-1.0)</param>
        /// <param name="maxUtilization">Maximum allowed utilization before forcing expansion (0.0-1.0)</param>
        /// <param name="validationInterval">Interval between validation checks</param>
        /// <param name="idleTimeThreshold">Time threshold for considering objects idle</param>
        public DynamicSizeStrategy(
            double expandThreshold = 0.8,
            double contractThreshold = 0.3,
            double maxUtilization = 0.95,
            TimeSpan? validationInterval = null,
            TimeSpan? idleTimeThreshold = null)
        {
            _expandThreshold = Math.Clamp(expandThreshold, 0.0, 1.0);
            _contractThreshold = Math.Clamp(contractThreshold, 0.0, 1.0);
            _maxUtilization = Math.Clamp(maxUtilization, 0.0, 1.0);
            _validationInterval = validationInterval ?? TimeSpan.FromMinutes(5);
            _idleTimeThreshold = idleTimeThreshold ?? TimeSpan.FromMinutes(10);

            if (_contractThreshold >= _expandThreshold)
                throw new ArgumentException("Contract threshold must be less than expand threshold");
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "DynamicSize";

        /// <summary>
        /// Calculates the target size for the pool based on current statistics.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>Target pool size</returns>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            if (statistics == null) return 0;

            var currentUtilization = statistics.Utilization / 100.0;
            var currentSize = statistics.TotalCount;

            // If utilization is very high, expand aggressively
            if (currentUtilization >= _maxUtilization)
            {
                return Math.Max(currentSize * 2, currentSize + 10);
            }

            // If utilization is above expand threshold, grow gradually
            if (currentUtilization >= _expandThreshold)
            {
                var growthFactor = 1.0 + (currentUtilization - _expandThreshold) / (1.0 - _expandThreshold) * 0.5;
                return (int)Math.Ceiling(currentSize * growthFactor);
            }

            // If utilization is below contract threshold, shrink gradually
            if (currentUtilization <= _contractThreshold && currentSize > 1)
            {
                var shrinkFactor = 0.8 + (_contractThreshold - currentUtilization) / _contractThreshold * 0.2;
                return Math.Max(1, (int)Math.Floor(currentSize * shrinkFactor));
            }

            // Maintain current size
            return currentSize;
        }

        /// <summary>
        /// Determines if the pool should expand.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should expand</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            return utilization >= _expandThreshold || statistics.AvailableCount == 0;
        }

        /// <summary>
        /// Determines if the pool should contract.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should contract</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            var utilization = statistics.Utilization / 100.0;
            var hasIdleTime = statistics.AverageIdleTimeMinutes > _idleTimeThreshold.TotalMinutes;
            
            return utilization <= _contractThreshold && hasIdleTime && statistics.TotalCount > 1;
        }

        /// <summary>
        /// Determines if a new object should be created.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if a new object should be created</returns>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            if (statistics == null) return true;

            // Create new if no objects available
            if (statistics.AvailableCount == 0)
                return true;

            // Create new if utilization is very high
            var utilization = statistics.Utilization / 100.0;
            return utilization >= _maxUtilization;
        }

        /// <summary>
        /// Determines if objects should be destroyed.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if objects should be destroyed</returns>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            if (statistics == null) return false;

            // Only destroy if we have excess capacity and low utilization
            var utilization = statistics.Utilization / 100.0;
            var hasExcess = statistics.AvailableCount > statistics.ActiveCount;
            var hasIdleTime = statistics.AverageIdleTimeMinutes > _idleTimeThreshold.TotalMinutes * 2;
            
            return utilization <= _contractThreshold && hasExcess && hasIdleTime;
        }

        /// <summary>
        /// Gets the interval between validation checks.
        /// </summary>
        /// <returns>Validation interval</returns>
        public TimeSpan GetValidationInterval()
        {
            return _validationInterval;
        }

        /// <summary>
        /// Validates the pool configuration against this strategy.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            if (config == null) return false;

            // Validate basic requirements
            if (config.InitialCapacity < 0 || config.MaxCapacity < 1)
                return false;

            if (config.InitialCapacity > config.MaxCapacity)
                return false;

            if (config.Factory == null)
                return false;

            // Validate timeouts
            if (config.MaxIdleTime <= TimeSpan.Zero)
                return false;

            if (config.ValidationInterval <= TimeSpan.Zero)
                return false;

            return true;
        }
    }
}