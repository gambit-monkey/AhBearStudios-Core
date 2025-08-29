using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Configs;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating PoolingService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// Handles complex initialization and dependency wiring for production-ready pooling services.
    /// </summary>
    public interface IPoolingServiceFactory
    {
        #region Synchronous Creation

        /// <summary>
        /// Creates a PoolingService instance using the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration for the pooling service</param>
        /// <returns>Configured PoolingService instance</returns>
        IPoolingService CreatePoolingService(PoolingServiceConfiguration configuration);

        #endregion

        #region Asynchronous Creation

        /// <summary>
        /// Creates a PoolingService instance asynchronously using the provided configuration.
        /// Useful when specialized services require async initialization.
        /// </summary>
        /// <param name="configuration">Configuration for the pooling service</param>
        /// <returns>Task containing the configured PoolingService instance</returns>
        UniTask<IPoolingService> CreatePoolingServiceAsync(PoolingServiceConfiguration configuration);

        #endregion
    }
}