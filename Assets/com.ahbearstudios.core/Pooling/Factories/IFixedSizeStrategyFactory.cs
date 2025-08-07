using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating FixedSizeStrategy instances following the Builder → Config → Factory → Service pattern.
    /// </summary>
    public interface IFixedSizeStrategyFactory
    {
        /// <summary>
        /// Creates a new FixedSizeStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured FixedSizeStrategy instance.</returns>
        FixedSizeStrategy Create(int fixedSize, PoolingStrategyConfig configuration);

        /// <summary>
        /// Creates a new FixedSizeStrategy instance with default configuration.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A configured FixedSizeStrategy instance with default settings.</returns>
        FixedSizeStrategy CreateDefault(int fixedSize);

        /// <summary>
        /// Creates a new FixedSizeStrategy optimized for mobile devices.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A mobile-optimized FixedSizeStrategy instance.</returns>
        FixedSizeStrategy CreateForMobile(int fixedSize);

        /// <summary>
        /// Creates a new FixedSizeStrategy optimized for high-performance scenarios.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A high-performance FixedSizeStrategy instance.</returns>
        FixedSizeStrategy CreateForHighPerformance(int fixedSize);

        /// <summary>
        /// Creates a new FixedSizeStrategy optimized for development and testing.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A development-optimized FixedSizeStrategy instance.</returns>
        FixedSizeStrategy CreateForDevelopment(int fixedSize);
    }
}