using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various pool configuration builders.
    /// Provides unified access to all specialized pool builders.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a standard pool configuration builder with default settings
        /// </summary>
        /// <returns>A new standard pool configuration builder</returns>
        public static PoolConfigBuilder Standard()
        {
            return new PoolConfigBuilder();
        }

        /// <summary>
        /// Creates a pool configuration builder initialized with values from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new pool configuration builder initialized with existing settings</returns>
        public static PoolConfigBuilder FromExistingConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            return new PoolConfigBuilder()
                .WithInitialCapacity(existingConfig.InitialCapacity)
                .WithMaxSize(existingConfig.MaximumCapacity)
                .WithPrewarming(existingConfig.PrewarmOnInit)
                .WithExponentialGrowth(existingConfig.UseExponentialGrowth)
                .WithGrowthFactor(existingConfig.GrowthFactor)
                .WithGrowthIncrement(existingConfig.GrowthIncrement)
                .WithAutoShrink(existingConfig.EnableAutoShrink)
                .WithShrinkThreshold(existingConfig.ShrinkThreshold)
                .WithShrinkInterval(existingConfig.ShrinkInterval)
                .WithThreadingMode(existingConfig.ThreadingMode)
                .WithNativeAllocator(existingConfig.NativeAllocator)
                .WithWarningLogging(existingConfig.LogWarnings)
                .WithMetricsCollection(existingConfig.CollectMetrics)
                .WithDetailedLogging(existingConfig.DetailedLogging)
                .WithResetOnRelease(existingConfig.ResetOnRelease)
                .WithExceptionOnExceedingMaxCount(existingConfig.ThrowIfExceedingMaxCount);
        }
    }
}