using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Advanced;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating and managing advanced object pools.
    /// Provides specialized creation methods for advanced pooling scenarios
    /// with enhanced configuration and metrics support.
    /// </summary>
    public interface IAdvancedPoolFactory : IPoolFactory
    {
        /// <summary>
        /// Creates an advanced object pool with specified configuration and metrics tracking.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Advanced pool configuration</param>
        /// <param name="metrics">Optional metrics collector for pool performance monitoring</param>
        /// <returns>New instance of IAdvancedObjectPool</returns>
        /// <exception cref="ArgumentNullException">Thrown when factory or config is null</exception>
        IAdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfig config,
            IPoolMetrics metrics = null) where T : class;

        /// <summary>
        /// Creates an advanced object pool using a configuration builder.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="configBuilder">Builder for configuring the pool</param>
        /// <returns>New instance of IAdvancedObjectPool</returns>
        /// <exception cref="ArgumentNullException">Thrown when factory or configBuilder is null</exception>
        IAdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfigBuilder configBuilder) where T : class;

        /// <summary>
        /// Creates an advanced object pool with specified configuration and registry integration.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Advanced pool configuration</param>
        /// <param name="registry">Optional pool registry for tracking</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>New instance of IAdvancedObjectPool</returns>
        /// <exception cref="ArgumentNullException">Thrown when factory or config is null</exception>
        IAdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfig config,
            IPoolRegistry registry,
            string poolName = null) where T : class;

        /// <summary>
        /// Retrieves an existing advanced pool from the registry or creates a new one.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="poolName">Name of the pool to retrieve or create</param>
        /// <param name="factory">Factory function for creating new instances if pool needs to be created</param>
        /// <param name="config">Configuration to use if pool needs to be created</param>
        /// <returns>Existing or new instance of IAdvancedObjectPool</returns>
        /// <exception cref="ArgumentException">Thrown when poolName is null or empty</exception>
        IAdvancedObjectPool<T> GetOrCreateAdvancedPool<T>(
            string poolName,
            Func<T> factory,
            AdvancedPoolConfig config) where T : class;

        /// <summary>
        /// Validates an advanced pool configuration.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>True if configuration is valid, false otherwise</returns>
        bool ValidateAdvancedConfig(AdvancedPoolConfig config);

        /// <summary>
        /// Creates a new advanced pool configuration builder.
        /// </summary>
        /// <typeparam name="T">Type of objects to be pooled</typeparam>
        /// <returns>New instance of AdvancedPoolConfigBuilder</returns>
        AdvancedPoolConfigBuilder CreateConfigBuilder<T>() where T : class;
    }
}