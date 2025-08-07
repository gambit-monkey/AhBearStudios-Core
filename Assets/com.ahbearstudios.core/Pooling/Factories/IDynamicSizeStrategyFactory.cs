using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating DynamicSizeStrategy instances following the Builder → Config → Factory → Service pattern.
    /// </summary>
    public interface IDynamicSizeStrategyFactory
    {
        /// <summary>
        /// Creates a new DynamicSizeStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured DynamicSizeStrategy instance.</returns>
        DynamicSizeStrategy Create(PoolingStrategyConfig configuration);

        /// <summary>
        /// Creates a new DynamicSizeStrategy instance with default configuration.
        /// </summary>
        /// <returns>A configured DynamicSizeStrategy instance with default settings.</returns>
        DynamicSizeStrategy CreateDefault();

        /// <summary>
        /// Creates a new DynamicSizeStrategy instance with custom threshold parameters.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="expandThreshold">Utilization threshold to trigger expansion (0.0-1.0).</param>
        /// <param name="contractThreshold">Utilization threshold to trigger contraction (0.0-1.0).</param>
        /// <param name="maxUtilization">Maximum allowed utilization before forcing expansion (0.0-1.0).</param>
        /// <param name="validationInterval">Interval between validation checks.</param>
        /// <param name="idleTimeThreshold">Time threshold for considering objects idle.</param>
        /// <returns>A configured DynamicSizeStrategy instance.</returns>
        DynamicSizeStrategy CreateWithThresholds(
            PoolingStrategyConfig configuration,
            double expandThreshold,
            double contractThreshold,
            double maxUtilization,
            TimeSpan validationInterval,
            TimeSpan idleTimeThreshold);
    }
}