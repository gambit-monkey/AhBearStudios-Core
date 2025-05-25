using System;
using AhBearStudios.Pooling.Configurations;
using Unity.Collections;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various Burst-compatible pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on BurstCompatiblePoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a Burst-compatible pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder</returns>
        public static BurstCompatiblePoolConfigBuilder Burst(int initialCapacity = 32)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder with default settings and specific allocator
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Native memory allocator to use</param>
        /// <returns>A new Burst-compatible pool configuration builder</returns>
        public static BurstCompatiblePoolConfigBuilder Burst(int initialCapacity, Allocator allocator)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithAllocator(allocator);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder optimized for jobs
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder optimized for jobs</returns>
        public static BurstCompatiblePoolConfigBuilder BurstJobOptimized(int initialCapacity = 64)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsJobOptimized()
                .WithMemoryAlignment(16)
                .WithJobBatchSize(64);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder with debug settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder with debug settings</returns>
        public static BurstCompatiblePoolConfigBuilder BurstDebug(int initialCapacity = 16)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug()
                .WithSafetyChecks(true)
                .WithAllocator(Allocator.Persistent);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder for high performance usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder optimized for performance</returns>
        public static BurstCompatiblePoolConfigBuilder BurstHighPerformance(int initialCapacity = 64)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance()
                .WithMaxSize(initialCapacity * 4)
                .WithAllocator(Allocator.Persistent);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder optimized for SIMD operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder optimized for SIMD operations</returns>
        public static BurstCompatiblePoolConfigBuilder BurstSIMDOptimized(int initialCapacity = 64)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsSIMDOptimized()
                .WithMemoryAlignment(32) // Aligned for AVX operations
                .WithMaxSize(initialCapacity * 2);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder optimized for balanced usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder with balanced settings</returns>
        public static BurstCompatiblePoolConfigBuilder BurstBalanced(int initialCapacity = 32)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder optimized for temporary operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder optimized for temporary operations</returns>
        public static BurstCompatiblePoolConfigBuilder BurstTemporary(int initialCapacity = 16)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithAllocator(Allocator.TempJob)
                .WithSafetyChecks(true)
                .WithAutoShrink(true)
                .WithDisposeOnFinalize(true)
                .WithMaxSize(initialCapacity * 2);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder optimized for parallel compute operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="batchSize">The batch size for parallel jobs</param>
        /// <returns>A new Burst-compatible pool configuration builder optimized for parallel compute</returns>
        public static BurstCompatiblePoolConfigBuilder BurstParallelCompute(int initialCapacity = 128, int batchSize = 64)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsJobOptimized()
                .WithParallelJobs(true)
                .WithJobBatchSize(batchSize)
                .WithMemoryAlignment(64)
                .WithCacheLinePadding(true)
                .WithAllocator(Allocator.Persistent)
                .WithMaxSize(initialCapacity * 4);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible pool configuration builder optimized for memory efficiency</returns>
        public static BurstCompatiblePoolConfigBuilder BurstMemoryEfficient(int initialCapacity = 16)
        {
            return new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 2)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.5f)
                .WithShrinkInterval(5.0f)
                .WithExponentialGrowth(false)
                .WithGrowthIncrement(4)
                .WithMetrics(false)
                .WithMemoryAlignment(4)
                .WithCacheLinePadding(false)
                .WithAllocator(Allocator.Persistent);
        }

        /// <summary>
        /// Creates a Burst-compatible pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new Burst-compatible pool configuration builder initialized with existing settings</returns>
        public static BurstCompatiblePoolConfigBuilder FromExistingBurstConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new BurstCompatiblePoolConfigBuilder()
                .WithInitialCapacity(existingConfig.InitialCapacity)
                .WithMaxSize(existingConfig.MaximumCapacity)
                .WithAutoShrink(existingConfig.EnableAutoShrink)
                .WithThreadingMode(existingConfig.ThreadingMode)
                .WithAllocator(existingConfig.NativeAllocator)
                .WithMetrics(existingConfig.CollectMetrics);

            if (existingConfig is BurstCompatiblePoolConfig burstConfig)
            {
                // Copy Burst-specific properties if the existing config is a BurstCompatiblePoolConfig
                builder
                    .WithSafetyChecks(burstConfig.UseSafetyChecks)
                    .WithJobOptimization(burstConfig.IsJobOptimized)
                    .WithMemoryAlignment(burstConfig.MemoryAlignment)
                    .WithDisposeOnFinalize(burstConfig.DisposeOnFinalize)
                    .WithCacheLinePadding(burstConfig.UseCacheLinePadding)
                    .WithDebugName(burstConfig.DebugName)
                    .WithParallelJobs(burstConfig.UseParallelJobs)
                    .WithJobBatchSize(burstConfig.JobBatchSize);
            }

            return builder;
        }
    }
}