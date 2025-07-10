using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various managed object pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on ManagedObjectPoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a managed object pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder</returns>
        public static ManagedObjectPoolConfigBuilder Managed(int initialCapacity = 16)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder with specific initial and maximum capacities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size of the pool</param>
        /// <returns>A new managed object pool configuration builder with specified capacities</returns>
        public static ManagedObjectPoolConfigBuilder Managed(int initialCapacity, int maxSize)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maxSize);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder optimized for frequent use scenarios
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder optimized for frequent use</returns>
        public static ManagedObjectPoolConfigBuilder ManagedFrequentUse(int initialCapacity = 32)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 4)
                .WithExponentialGrowth(true)
                .WithGrowthFactor(1.5f)
                .WithPrewarming(true)
                .WithResetOnRelease(false)
                .WithFifoOrder(false);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder optimized for memory efficiency</returns>
        public static ManagedObjectPoolConfigBuilder ManagedMemoryEfficient(int initialCapacity = 16)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a managed object pool configuration builder with monitoring capabilities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder with monitoring capabilities</returns>
        public static ManagedObjectPoolConfigBuilder ManagedMonitored(int initialCapacity = 16)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMonitored();
        }

        /// <summary>
        /// Creates a managed object pool configuration builder optimized for thread-safety
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder optimized for thread-safety</returns>
        public static ManagedObjectPoolConfigBuilder ManagedThreadSafe(int initialCapacity = 32)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsThreadSafe();
        }

        /// <summary>
        /// Creates a managed object pool configuration builder configured for long-lived objects
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder configured for long-lived objects</returns>
        public static ManagedObjectPoolConfigBuilder ManagedLongLived(int initialCapacity = 16)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsLongLived();
        }

        /// <summary>
        /// Creates a managed object pool configuration builder configured for short-lived objects
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder configured for short-lived objects</returns>
        public static ManagedObjectPoolConfigBuilder ManagedShortLived(int initialCapacity = 32)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsShortLived();
        }

        /// <summary>
        /// Creates a managed object pool configuration builder with optimized balanced settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder with balanced settings</returns>
        public static ManagedObjectPoolConfigBuilder ManagedBalanced(int initialCapacity = 32)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a managed object pool configuration builder with lifecycle methods disabled
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder with lifecycle methods disabled</returns>
        public static ManagedObjectPoolConfigBuilder ManagedNoLifecycleMethods(int initialCapacity = 32)
        {
            return new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithLifecycleMethods(false);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new managed object pool configuration builder initialized with existing settings</returns>
        public static ManagedObjectPoolConfigBuilder FromExistingManagedConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            // If the config is already a ManagedObjectPoolConfig, use specialized constructor
            if (existingConfig is ManagedObjectPoolConfig managedConfig)
            {
                return new ManagedObjectPoolConfigBuilder(managedConfig);
            }

            // Otherwise, create a new managed config from the base config
            return ManagedObjectPoolConfigBuilder.ManagedFrom(existingConfig);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder</returns>
        public static ManagedObjectPoolConfigBuilder ManagedWithRegistry(
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

            var builder = new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);

            // Return builder for further configuration if needed
            return new ManagedObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder</returns>
        public static ManagedObjectPoolConfigBuilder ManagedForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 16) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ManagedObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a managed object pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new managed object pool configuration builder</returns>
        public static ManagedObjectPoolConfigBuilder ManagedWithNameAndType<T>(
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

            var builder = new ManagedObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);

            // Return builder for further configuration if needed
            return new ManagedObjectPoolConfigBuilder(config);
        }
    }
}