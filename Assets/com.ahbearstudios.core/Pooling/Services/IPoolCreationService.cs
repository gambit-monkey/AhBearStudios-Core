using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for creating pool instances with appropriate strategies and configurations.
    /// Encapsulates the complex logic of pool instantiation and strategy selection.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface IPoolCreationService
    {
        #region Pool Creation

        /// <summary>
        /// Creates a pool instance for the specified type using the provided configuration.
        /// Automatically selects appropriate pool type and strategy based on configuration.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="configuration">Pool configuration</param>
        /// <returns>Configured pool instance</returns>
        IObjectPool<T> CreatePool<T>(PoolConfiguration configuration) where T : class, IPooledObject, new();

        /// <summary>
        /// Creates a pool instance for the specified type with default configuration.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>Pool instance with default configuration</returns>
        IObjectPool<T> CreatePool<T>(string poolName = null) where T : class, IPooledObject, new();

        #endregion

        #region Pool Type Selection

        /// <summary>
        /// Determines the appropriate pool type for the given configuration and object type.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="configuration">Pool configuration</param>
        /// <returns>Selected pool type</returns>
        PoolType SelectPoolType<T>(PoolConfiguration configuration) where T : class, IPooledObject, new();

        #endregion

        #region Strategy Creation

        /// <summary>
        /// Creates an appropriate pooling strategy for the given configuration.
        /// </summary>
        /// <param name="configuration">Pool configuration</param>
        /// <returns>Configured pooling strategy</returns>
        IPoolingStrategy CreateStrategy(PoolConfiguration configuration);

        /// <summary>
        /// Creates a default pooling strategy suitable for the specified pool type.
        /// </summary>
        /// <param name="poolType">Type of pool the strategy will be used with</param>
        /// <returns>Default pooling strategy</returns>
        IPoolingStrategy CreateDefaultStrategy(PoolType poolType);

        #endregion

        #region Validation

        /// <summary>
        /// Validates that a pool configuration is suitable for the specified type.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="configuration">Pool configuration to validate</param>
        /// <returns>True if configuration is valid for the type</returns>
        bool ValidateConfiguration<T>(PoolConfiguration configuration) where T : class, IPooledObject, new();

        /// <summary>
        /// Gets validation errors for a pool configuration and type combination.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="configuration">Pool configuration to validate</param>
        /// <returns>Array of validation error messages, empty if valid</returns>
        string[] GetValidationErrors<T>(PoolConfiguration configuration) where T : class, IPooledObject, new();

        #endregion
    }
}