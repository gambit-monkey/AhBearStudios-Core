using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various particle system pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on ParticleSystemPoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a particle system pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystem(int initialCapacity = 8)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder with specific initial and maximum capacities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size of the pool</param>
        /// <returns>A new particle system pool configuration builder with specified capacities</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystem(int initialCapacity, int maxSize)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maxSize);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder optimized for visual effects
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder optimized for visual effects</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemVisualEffects(int initialCapacity = 16)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 2)
                .WithPrewarming(true)
                .WithExponentialGrowth(true)
                .WithGrowthFactor(1.5f)
                .WithResetOnRelease(true)
                .WithAutoCleanup(true);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder optimized for memory efficiency</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemMemoryEfficient(int initialCapacity = 8)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity)
                .WithPrewarming(true)
                .WithExponentialGrowth(false)
                .WithResetOnRelease(true)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.5f)
                .WithShrinkInterval(10f)
                .WithAutoCleanup(true)
                .WithCacheMaterials(true);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder with monitoring capabilities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder with monitoring capabilities</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemMonitored(int initialCapacity = 8)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMetricsCollection(true)
                .WithDetailedLogging(true)
                .WithWarningLogging(true);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder for high-density visual effects
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder for high-density effects</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemHighDensity(int initialCapacity = 32)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(100)
                .WithPrewarming(true)
                .WithExponentialGrowth(true)
                .WithGrowthFactor(2.0f)
                .WithResetOnRelease(true)
                .WithAutoCleanup(true)
                .WithHighDensityOptimization(true);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder with optimized settings for mobile devices
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder optimized for mobile</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemMobileOptimized(int initialCapacity = 8)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(16)
                .WithPrewarming(true)
                .WithResetOnRelease(true)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.3f)
                .WithShrinkInterval(5f)
                .WithLowOverheadMode(true)
                .WithCacheMaterials(true);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder for persistent effects
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder for persistent effects</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemPersistent(int initialCapacity = 16)
        {
            return new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 2)
                .WithPrewarming(true)
                .WithResetOnRelease(false)
                .WithAutoCleanup(false)
                .WithPersistentMode(true);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new particle system pool configuration builder initialized with existing settings</returns>
        public static ParticleSystemPoolConfigBuilder FromExistingParticleSystemConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            // If the config is already a ParticleSystemPoolConfig, use specialized constructor
            if (existingConfig is ParticleSystemPoolConfig particleConfig)
            {
                return new ParticleSystemPoolConfigBuilder(particleConfig);
            }

            // Otherwise, create a new particle system config from the base config
            return new ParticleSystemPoolConfigBuilder(existingConfig);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemWithRegistry(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 8)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);

            // Return builder for further configuration if needed
            return new ParticleSystemPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 8) where T : UnityEngine.Object
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ParticleSystemPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a particle system pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new particle system pool configuration builder</returns>
        public static ParticleSystemPoolConfigBuilder ParticleSystemWithNameAndType<T>(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 8) where T : UnityEngine.Object
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new ParticleSystemPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);

            // Return builder for further configuration if needed
            return new ParticleSystemPoolConfigBuilder(config);
        }
    }
}