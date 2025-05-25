using System;
using AhBearStudios.Pooling.Configurations;
using Unity.Collections;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various job-compatible pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on JobCompatiblePoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a job-compatible pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatible(int initialCapacity = 32)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder with specific initial capacity and allocator
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">The allocator to use for native collections</param>
        /// <returns>A new job-compatible pool configuration builder with specified capacity and allocator</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatible(int initialCapacity, Allocator allocator)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithAllocator(allocator);
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder with specific initial and maximum capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size of the pool</param>
        /// <returns>A new job-compatible pool configuration builder with specified capacities</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatible(int initialCapacity, int maxSize)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maxSize);
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder optimized for parallel execution
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder optimized for parallel processing</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleParallelOptimized(int initialCapacity = 64)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsParallelOptimized();
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder optimized for Burst compilation
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder optimized for Burst compilation</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleBurstOptimized(int initialCapacity = 64)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBurstCompatible();
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder with debug settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder with debug settings</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleDebug(int initialCapacity = 16)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder optimized for balanced usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder with balanced settings</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleBalanced(int initialCapacity = 32)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder optimized for memory efficiency</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleMemoryEfficient(int initialCapacity = 16)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder optimized for sequential operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder optimized for sequential operations</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleSequentialOptimized(int initialCapacity = 32)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsSequentialOptimized();
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder with custom safety settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="useSafetyChecks">Whether to enable safety checks</param>
        /// <returns>A new job-compatible pool configuration builder with custom safety settings</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleWithSafety(int initialCapacity = 32, bool useSafetyChecks = true)
        {
            return new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithSafetyChecks(useSafetyChecks);
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new job-compatible pool configuration builder initialized with existing settings</returns>
        public static JobCompatiblePoolConfigBuilder FromExistingJobCompatibleConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new JobCompatiblePoolConfigBuilder()
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

            // Copy job-compatible specific properties if available
            if (existingConfig is JobCompatiblePoolConfig jobConfig)
            {
                builder
                    .WithSafetyChecks(jobConfig.UseSafetyChecks)
                    .WithBurstCompatibleCollections(jobConfig.UseBurstCompatibleCollections)
                    .WithParallelJobAccess(jobConfig.EnableParallelJobAccess)
                    .WithSchedulingMode(jobConfig.SchedulingMode);
            }

            return builder;
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleWithRegistry(
            IPoolConfigRegistry registry, 
            string configName, 
            int initialCapacity = 32)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }
            
            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }
            
            var builder = new JobCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
                
            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);
            
            // Return builder for further configuration if needed
            return new JobCompatiblePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a job-compatible pool configuration builder optimized for parallel execution for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible pool configuration builder optimized for parallel processing</returns>
        public static JobCompatiblePoolConfigBuilder JobCompatibleParallelOptimizedForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 64) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = JobCompatibleParallelOptimized(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new JobCompatiblePoolConfigBuilder(config);
        }
    }
}