using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service for selecting the appropriate pooling strategy based on configuration.
    /// Integrates with existing strategy factories to create optimal strategies.
    /// </summary>
    public interface IPoolingStrategySelector
    {
        /// <summary>
        /// Selects and creates the appropriate pooling strategy based on the pool configuration.
        /// </summary>
        /// <param name="configuration">Pool configuration containing strategy type and parameters</param>
        /// <returns>Configured pooling strategy instance</returns>
        IPoolingStrategy SelectStrategy(PoolConfiguration configuration);

        /// <summary>
        /// Determines if the strategy selector can create the requested strategy type.
        /// </summary>
        /// <param name="configuration">Pool configuration containing strategy type</param>
        /// <returns>True if the strategy can be created</returns>
        bool CanCreateStrategy(PoolConfiguration configuration);

        /// <summary>
        /// Gets the default strategy when no specific strategy is configured.
        /// </summary>
        /// <returns>Default pooling strategy instance</returns>
        IPoolingStrategy GetDefaultStrategy();
    }
}