using System;
using AhBearStudios.Core.Pooling.Configurations;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various semaphore pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on SemaphorePoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a semaphore pool configuration builder with default settings
        /// </summary>
        /// <returns>A new semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder Semaphore()
        {
            return new SemaphorePoolConfigBuilder();
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder with specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder Semaphore(int initialCapacity)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder with specified initial capacity and initial count
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="initialCount">Initial count of available semaphore resources</param>
        /// <returns>A new semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder Semaphore(int initialCapacity, int initialCount)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithInitialCount(initialCount);
        }

        /// <summary>
        /// Creates a high-performance semaphore pool configuration builder
        /// Optimizes for maximum throughput in high-demand scenarios.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-performance semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreHighPerformance(int initialCapacity = 32)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a debug-friendly semaphore pool configuration builder
        /// Enables all tracking, validation, and logging features.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new debug-friendly semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreDebug(int initialCapacity = 16)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder optimized for high contention scenarios
        /// </summary>
        /// <param name="initialCount">Initial count of semaphore resources</param>
        /// <param name="maxConcurrentWaits">Maximum number of concurrent wait operations</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new contention-optimized semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreForContention(
            int initialCount = 4, 
            int maxConcurrentWaits = 512, 
            int initialCapacity = 64)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsForContention(initialCount, maxConcurrentWaits);
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder with timeout settings
        /// </summary>
        /// <param name="timeoutMs">Default timeout in milliseconds</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new timeout-configured semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreWithTimeout(int timeoutMs = 1000, int initialCapacity = 16)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsWithTimeout(timeoutMs);
        }

        /// <summary>
        /// Creates a recursive semaphore pool configuration builder
        /// Allows the same thread to acquire multiple semaphore resources.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new recursive semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreRecursive(int initialCapacity = 16)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsRecursive();
        }

        /// <summary>
        /// Creates a balanced semaphore pool configuration builder
        /// Provides good performance while maintaining safety and monitoring.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new balanced semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreBalanced(int initialCapacity = 16)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder with full tracking capabilities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new tracking-enabled semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreWithTracking(int initialCapacity = 16)
        {
            return new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsWithTracking();
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new semaphore pool configuration builder initialized with existing settings</returns>
        public static SemaphorePoolConfigBuilder FromExistingSemaphoreConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            // If the config is already a SemaphorePoolConfig, we can use its ToBuilder method
            if (existingConfig is SemaphorePoolConfig semaphoreConfig)
            {
                return semaphoreConfig.ToBuilder();
            }

            // Otherwise, create a new builder with base settings from the existing config
            return new SemaphorePoolConfigBuilder(new SemaphorePoolConfig(existingConfig));
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreWithRegistry(
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

            var builder = new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);

            // Return builder for further configuration if needed
            return new SemaphorePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 16) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new SemaphorePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a semaphore pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder SemaphoreWithNameAndType<T>(
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

            var builder = new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);

            // Return builder for further configuration if needed
            return new SemaphorePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a high-contention semaphore pool configuration builder for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCount">Initial count of semaphore resources</param>
        /// <param name="maxConcurrentWaits">Maximum number of concurrent wait operations</param>
        /// <returns>A new high-contention semaphore pool configuration builder</returns>
        public static SemaphorePoolConfigBuilder HighContentionSemaphoreForType<T>(
            IPoolConfigRegistry registry,
            int initialCount = 4,
            int maxConcurrentWaits = 512) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new SemaphorePoolConfigBuilder()
                .WithInitialCapacity(64)
                .AsForContention(initialCount, maxConcurrentWaits);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new SemaphorePoolConfigBuilder(config);
        }
    }
}