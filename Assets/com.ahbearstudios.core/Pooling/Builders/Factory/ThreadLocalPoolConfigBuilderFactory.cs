using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various thread-local pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on ThreadLocalPoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a thread-local pool configuration builder with default settings
        /// </summary>
        /// <returns>A new thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocal()
        {
            return new ThreadLocalPoolConfigBuilder();
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder with specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocal(int initialCapacity)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a high-performance thread-local pool configuration builder
        /// Optimizes for maximum throughput in multi-threaded scenarios.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <param name="maxThreadCount">Maximum number of threads that can have their own pools</param>
        /// <returns>A new high-performance thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalHighPerformance(int initialCapacity = 32, int maxThreadCount = 16)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxThreadCount(maxThreadCount)
                .WithMetricsCollection(false)
                .WithDetailedLogging(false)
                .WithWarningLogging(false)
                .WithAutoCleanupUnusedThreadPools(false);
        }

        /// <summary>
        /// Creates a debug-friendly thread-local pool configuration builder
        /// Enables extensive tracking, validation, and logging features.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new debug-friendly thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalDebug(int initialCapacity = 16)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMetricsCollection(true)
                .WithDetailedLogging(true)
                .WithWarningLogging(true)
                .WithObjectThreadAffinity(true)
                .WithMaxThreadCount(32);
        }

        /// <summary>
        /// Creates a memory-efficient thread-local pool configuration builder
        /// Optimizes for reduced memory usage across multiple threads.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new memory-efficient thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalMemoryEfficient(int initialCapacity = 8)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 2)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.3f)
                .WithShrinkInterval(30)
                .WithAutoCleanupUnusedThreadPools(true)
                .WithThreadPoolCleanupThreshold(60)
                .WithMaxThreadCount(8);
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder optimized for job systems
        /// Ensures compatibility with Unity's job system.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new job-compatible thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalJobCompatible(int initialCapacity = 16)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxThreadCount(JobsUtility.MaxJobThreadCount)
                .WithAutoCleanupUnusedThreadPools(false)
                .WithGlobalFallbackPool(true)
                .WithReturnToCreatorThreadPool(false);
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder with thread affinity tracking
        /// Ensures objects return to their creator thread's pool when released.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new thread-local pool configuration builder with affinity tracking</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalWithAffinity(int initialCapacity = 16)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithObjectThreadAffinity(true)
                .WithReturnToCreatorThreadPool(true);
        }

        /// <summary>
        /// Creates a balanced thread-local pool configuration builder
        /// Provides good performance while maintaining safety and monitoring.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new balanced thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalBalanced(int initialCapacity = 16)
        {
            return new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMetricsCollection(true)
                .WithAutoShrink(true)
                .WithMaxThreadCount(Math.Max(JobsUtility.JobWorkerCount, 8))
                .WithAutoCleanupUnusedThreadPools(true)
                .WithThreadPoolCleanupThreshold(120);
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new thread-local pool configuration builder initialized with existing settings</returns>
        public static ThreadLocalPoolConfigBuilder FromExistingThreadLocalConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            // If the config is already a ThreadLocalPoolConfig, we can use its ToBuilder method
            if (existingConfig is ThreadLocalPoolConfig threadLocalConfig)
            {
                return threadLocalConfig.ToBuilder();
            }

            // Otherwise, create a new builder with basic settings from the existing config
            return new ThreadLocalPoolConfigBuilder(new ThreadLocalPoolConfig(existingConfig));
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalWithRegistry(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 16)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);

            // Return builder for further configuration if needed
            return new ThreadLocalPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 16) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ThreadLocalPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a thread-local pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalWithNameAndType<T>(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 16) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new ThreadLocalPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);

            // Return builder for further configuration if needed
            return new ThreadLocalPoolConfigBuilder(config);
        }
        
        /// <summary>
        /// Creates a job-optimized thread-local pool configuration builder for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the thread-local pool</param>
        /// <returns>A new job-optimized thread-local pool configuration builder</returns>
        public static ThreadLocalPoolConfigBuilder ThreadLocalJobOptimizedForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = ThreadLocalJobCompatible(initialCapacity)
                .WithExponentialGrowth(true)
                .WithGrowthFactor(1.5f);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ThreadLocalPoolConfigBuilder(config);
        }
    }
}