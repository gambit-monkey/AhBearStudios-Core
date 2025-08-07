using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating AdaptiveNetworkStrategy instances following the Builder → Config → Factory → Service pattern.
    /// </summary>
    public interface IAdaptiveNetworkStrategyFactory
    {
        /// <summary>
        /// Creates a new AdaptiveNetworkStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured AdaptiveNetworkStrategy instance.</returns>
        AdaptiveNetworkStrategy Create(PoolingStrategyConfig configuration);

        /// <summary>
        /// Creates a new AdaptiveNetworkStrategy instance with default configuration.
        /// </summary>
        /// <returns>A configured AdaptiveNetworkStrategy instance with network-optimized defaults.</returns>
        AdaptiveNetworkStrategy CreateDefault();

        /// <summary>
        /// Creates a new AdaptiveNetworkStrategy instance with custom network parameters.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="spikeDetectionThreshold">Threshold for detecting network spikes (0.1 to 1.0).</param>
        /// <param name="preemptiveAllocationRatio">Ratio of preemptive allocations (0.0 to 0.5).</param>
        /// <param name="burstWindow">Time window for burst detection.</param>
        /// <param name="maxBurstAllocations">Maximum allocations during burst.</param>
        /// <returns>A configured AdaptiveNetworkStrategy instance.</returns>
        AdaptiveNetworkStrategy CreateWithNetworkParameters(
            PoolingStrategyConfig configuration,
            double spikeDetectionThreshold,
            double preemptiveAllocationRatio,
            TimeSpan burstWindow,
            int maxBurstAllocations);
    }
}