using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating HighPerformanceStrategy instances following the Builder → Config → Factory → Service pattern.
    /// </summary>
    public interface IHighPerformanceStrategyFactory
    {
        /// <summary>
        /// Creates a new HighPerformanceStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured HighPerformanceStrategy instance.</returns>
        HighPerformanceStrategy Create(PoolingStrategyConfig configuration);

        /// <summary>
        /// Creates a new HighPerformanceStrategy instance with default configuration.
        /// </summary>
        /// <returns>A configured HighPerformanceStrategy instance with default settings.</returns>
        HighPerformanceStrategy CreateDefault();

        /// <summary>
        /// Creates a new HighPerformanceStrategy with custom performance parameters.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="preAllocationSize">Size to pre-allocate at startup.</param>
        /// <param name="aggressiveExpansionThreshold">Threshold for aggressive expansion.</param>
        /// <param name="conservativeContractionThreshold">Threshold for conservative contraction.</param>
        /// <returns>A configured HighPerformanceStrategy instance.</returns>
        HighPerformanceStrategy CreateWithParameters(
            PoolingStrategyConfig configuration,
            int preAllocationSize,
            double aggressiveExpansionThreshold,
            double conservativeContractionThreshold);

        /// <summary>
        /// Creates a new HighPerformanceStrategy optimized for 60 FPS gameplay.
        /// </summary>
        /// <param name="preAllocationSize">Size to pre-allocate.</param>
        /// <returns>60 FPS optimized strategy.</returns>
        HighPerformanceStrategy CreateFor60FPS(int preAllocationSize);

        /// <summary>
        /// Creates a new HighPerformanceStrategy optimized for competitive gaming (120+ FPS).
        /// </summary>
        /// <param name="preAllocationSize">Size to pre-allocate.</param>
        /// <returns>120+ FPS optimized strategy.</returns>
        HighPerformanceStrategy CreateForCompetitiveGaming(int preAllocationSize);

        /// <summary>
        /// Creates a new HighPerformanceStrategy optimized for VR applications.
        /// </summary>
        /// <param name="preAllocationSize">Size to pre-allocate.</param>
        /// <returns>VR optimized strategy.</returns>
        HighPerformanceStrategy CreateForVR(int preAllocationSize);
    }
}