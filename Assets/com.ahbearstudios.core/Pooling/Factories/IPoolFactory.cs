using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Services;

namespace AhBearStudios.Pooling.Factories
{
    /// <summary>
    /// Base interface for all pool factories, defining common functionality
    /// shared across different factory implementations.
    /// </summary>
    public interface IPoolFactory
    {
        #region Core Properties

        /// <summary>
        /// Gets a unique identifier for this factory
        /// </summary>
        string FactoryId { get; }

        /// <summary>
        /// Gets the version of this factory implementation
        /// </summary>
        Version ImplementationVersion { get; }

        /// <summary>
        /// Gets the factory's current state
        /// </summary>
        FactoryState State { get; }

        /// <summary>
        /// Gets the last error that occurred in this factory
        /// </summary>
        FactoryError LastError { get; }

        #endregion

        #region Core Pool Creation

        /// <summary>
        /// Creates a generic pool for the specified item type
        /// </summary>
        /// <typeparam name="T">Type of items the pool will manage</typeparam>
        /// <param name="factory">Function that creates new pool items</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset items when released</param>
        /// <param name="poolName">Name for the pool</param>
        /// <returns>A new pool instance</returns>
        IPool<T> CreatePool<T>(Func<T> factory, IPoolConfig config = null, Action<T> resetAction = null, string poolName = null) where T : class;

        /// <summary>
        /// Creates a pool with advanced options
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="options">Advanced creation options</param>
        /// <returns>The created pool</returns>
        IPool<T> CreatePoolWithOptions<T>(PoolCreationOptions<T> options) where T : class;

        /// <summary>
        /// Creates a pool based on the actual runtime type of the provided config
        /// </summary>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="poolName">Name for the pool</param>
        /// <param name="factoryArgs">Additional arguments needed for pool creation</param>
        /// <returns>The created pool</returns>
        IPool CreatePoolWithConfig(Type itemType, IPoolConfig config, string poolName, params object[] factoryArgs);

        #endregion

        #region Configuration Management

        /// <summary>
        /// Gets a pool configuration for the specified type, either from the registry or by creating a new default one
        /// </summary>
        /// <typeparam name="T">Type of items the configuration is for</typeparam>
        /// <param name="config">Optional provided configuration</param>
        /// <returns>The configuration to use</returns>
        IPoolConfig GetOrCreateConfigFor<T>(IPoolConfig config = null);

        /// <summary>
        /// Gets or sets the configuration registry for this factory
        /// </summary>
        IPoolConfigRegistry ConfigRegistry { get; set; }

        /// <summary>
        /// Updates the factory with new configuration settings
        /// </summary>
        /// <param name="config">Configuration to apply</param>
        /// <returns>True if update was successful</returns>
        bool UpdateConfiguration(IPoolConfig config);

        #endregion

        #region Pool Management & Registration

        /// <summary>
        /// Registers a custom pool creation strategy
        /// </summary>
        /// <param name="poolInterfaceType">Pool interface type (e.g., typeof(IPool&lt;&gt;))</param>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="creationStrategy">Function that creates the pool</param>
        void RegisterPoolCreationStrategy(Type poolInterfaceType, Type itemType, Func<object[], object> creationStrategy);
        
        /// <summary>
        /// Registers a custom pool creation strategy for a specific generic type
        /// </summary>
        /// <typeparam name="TPool">Pool interface type</typeparam>
        /// <typeparam name="TItem">Type of items to pool</typeparam>
        /// <param name="creationStrategy">Function that creates the pool</param>
        void RegisterPoolCreationStrategy<TPool, TItem>(Func<object[], TPool> creationStrategy) where TPool : IPool;

        /// <summary>
        /// Checks if this factory can create a pool for the specified item type
        /// </summary>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="poolInterfaceType">Specific pool interface type to create</param>
        /// <returns>True if this factory can create the requested pool type</returns>
        bool CanCreatePoolFor(Type itemType, Type poolInterfaceType = null);

        /// <summary>
        /// Tracks an existing pool with this factory
        /// </summary>
        /// <param name="pool">Pool to track</param>
        /// <returns>True if the pool was successfully tracked</returns>
        bool TrackPool(IPool pool);

        /// <summary>
        /// Removes tracking of an existing pool
        /// </summary>
        /// <param name="pool">Pool to untrack</param>
        /// <returns>True if the pool was successfully untracked</returns>
        bool UntrackPool(IPool pool);

        /// <summary>
        /// Gets all pools currently managed by this factory
        /// </summary>
        /// <returns>Collection of managed pools</returns>
        IReadOnlyCollection<IPool> GetManagedPools();

        #endregion

        #region Lifecycle Management

        /// <summary>
        /// Initializes the factory with any required dependencies or configurations
        /// </summary>
        /// <param name="initializer">Optional configuration object for initialization</param>
        /// <returns>True if initialization was successful</returns>
        bool Initialize(object initializer = null);

        /// <summary>
        /// Asynchronously initializes the factory
        /// </summary>
        /// <param name="initializer">Optional initialization data</param>
        /// <returns>Task representing the operation</returns>
        Task<bool> InitializeAsync(object initializer = null);

        /// <summary>
        /// Shuts down the factory, cleaning up any resources
        /// </summary>
        /// <param name="shutdownMode">Mode controlling how aggressive cleanup should be</param>
        /// <returns>True if shutdown was successful</returns>
        bool Shutdown(FactoryShutdownMode shutdownMode = FactoryShutdownMode.GracefulShutdown);

        /// <summary>
        /// Resets the factory to its initial state
        /// </summary>
        /// <returns>True if reset was successful</returns>
        bool Reset();

        #endregion

        #region Diagnostics and Services

        /// <summary>
        /// Generates a detailed report of this factory's current state and operation
        /// </summary>
        /// <param name="options">Options controlling what to include in the report</param>
        /// <returns>A diagnostic report</returns>
        string GenerateReport(FactoryReportOptions options = null);

        /// <summary>
        /// Sets an external service provider to resolve dependencies
        /// </summary>
        /// <param name="serviceProvider">The service provider to use</param>
        void SetServiceProvider(IServiceProvider serviceProvider);

        /// <summary>
        /// Gets or sets a service locator for accessing runtime services
        /// </summary>
        IPoolingServiceLocator ServiceLocator { get; set; }

        /// <summary>
        /// Checks if this factory supports the specified feature
        /// </summary>
        /// <param name="featureId">Feature identifier</param>
        /// <returns>True if the feature is supported</returns>
        bool SupportsFeature(string featureId);

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a pool is created by this factory
        /// </summary>
        event EventHandler<PoolCreatedEventArgs> PoolCreated;

        /// <summary>
        /// Event raised when a pool is destroyed
        /// </summary>
        event EventHandler<PoolDestroyedEventArgs> PoolDestroyed;

        /// <summary>
        /// Event raised when a factory error occurs
        /// </summary>
        event EventHandler<FactoryErrorEventArgs> FactoryError;

        #endregion

        #region Extensions

        /// <summary>
        /// Gets an extension point for adding custom functionality to the factory
        /// </summary>
        /// <typeparam name="TExtension">Type of extension</typeparam>
        /// <returns>The extension instance or null if not supported</returns>
        TExtension GetExtension<TExtension>() where TExtension : class;

        /// <summary>
        /// Adds or replaces an extension for this factory
        /// </summary>
        /// <typeparam name="TExtension">Type of extension</typeparam>
        /// <param name="extension">Extension instance</param>
        /// <returns>True if extension was registered successfully</returns>
        bool AddExtension<TExtension>(TExtension extension) where TExtension : class;

        #endregion
    }
}