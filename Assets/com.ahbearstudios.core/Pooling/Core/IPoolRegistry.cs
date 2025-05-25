using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Interface for a thread-safe registry that manages object pools.
    /// Provides centralized pool management with name and type-based lookups,
    /// along with configurable conflict resolution strategies.
    /// </summary>
    public interface IPoolRegistry : IDisposable
    {
        /// <summary>
        /// Gets the number of registered pools in this registry
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets whether this registry has been disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets the name of this registry
        /// </summary>
        string RegistryName { get; }

        /// <summary>
        /// Sets the default conflict resolution strategy for this registry.
        /// This strategy is used when no specific strategy is provided during pool registration.
        /// </summary>
        /// <param name="strategy">The conflict resolution strategy to use</param>
        void SetConflictResolutionStrategy(PoolNameConflictResolution strategy);

        /// <summary>
        /// Registers a pool with the registry using configurable conflict resolution.
        /// Only the first pool of a given type is registered in the type mapping.
        /// </summary>
        /// <param name="pool">Pool to register</param>
        /// <param name="name">Optional name for the pool. If null, a unique name is generated</param>
        /// <param name="conflictResolution">Optional strategy for resolving naming conflicts. If null, uses the default strategy</param>
        /// <returns>The name used to register the pool</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool is null</exception>
        /// <exception cref="ArgumentException">Thrown if a naming conflict occurs and ThrowException strategy is used</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        string RegisterPool(IPool pool, string name = null, PoolNameConflictResolution? conflictResolution = null);

        /// <summary>
        /// Registers a strongly-typed pool with the registry.
        /// Uses the default conflict resolution strategy.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">Pool to register</param>
        /// <param name="name">Optional name for the pool</param>
        /// <returns>This registry for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        IPoolRegistry RegisterPool<T>(IPool<T> pool, string name = null);

        /// <summary>
        /// Unregisters a pool by its name, removing both name and type mappings if applicable.
        /// </summary>
        /// <param name="poolName">Name of the pool to unregister</param>
        /// <returns>True if the pool was unregistered, false if it wasn't found</returns>
        /// <exception cref="ArgumentNullException">Thrown if poolName is null or empty</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        bool UnregisterPool(string poolName);

        /// <summary>
        /// Unregisters a specific pool instance, removing both name and type mappings if applicable.
        /// </summary>
        /// <param name="pool">Pool to unregister</param>
        /// <returns>True if the pool was unregistered, false if it wasn't found</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        bool UnregisterPool(IPool pool);

        /// <summary>
        /// Unregisters the pool registered for a specific type.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <returns>True if the pool was unregistered, false if it wasn't found</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        bool UnregisterPoolByType<T>();

        /// <summary>
        /// Gets a pool by its registered name.
        /// </summary>
        /// <param name="poolName">Name of the pool to get</param>
        /// <returns>The requested pool or null if not found</returns>
        /// <exception cref="ArgumentNullException">Thrown if poolName is null or empty</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        IPool GetPool(string poolName);

        /// <summary>
        /// Gets a strongly-typed pool by its registered name.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="poolName">Name of the pool to get</param>
        /// <returns>The requested pool or null if not found or if type doesn't match</returns>
        /// <exception cref="ArgumentNullException">Thrown if poolName is null or empty</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        IPool<T> GetPool<T>(string poolName);

        /// <summary>
        /// Gets the first registered pool for a specific type.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <returns>The requested pool or null if not found</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        IPool<T> GetPoolByType<T>();

        /// <summary>
        /// Checks if a pool exists with the specified name.
        /// </summary>
        /// <param name="poolName">Name of the pool to check</param>
        /// <returns>True if the pool exists, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if poolName is null or empty</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        bool HasPool(string poolName);

        /// <summary>
        /// Checks if a pool exists for a specific type.
        /// </summary>
        /// <typeparam name="T">Type to check for</typeparam>
        /// <returns>True if a pool exists for the type, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        bool HasPoolForType<T>();

        /// <summary>
        /// Gets an immutable collection of all registered pool names.
        /// </summary>
        /// <returns>Read-only collection of pool names</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        IReadOnlyCollection<string> GetAllPoolNames();

        /// <summary>
        /// Gets an immutable collection of all registered pools.
        /// </summary>
        /// <returns>Read-only collection of pools</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        IReadOnlyCollection<IPool> GetAllPools();

        /// <summary>
        /// Clears all pools from the registry with optional disposal.
        /// </summary>
        /// <param name="dispose">Whether to also dispose the pools</param>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        void ClearAllPools(bool dispose = false);

        /// <summary>
        /// Gets metrics for all registered pools that implement IPoolMetrics.
        /// </summary>
        /// <returns>Dictionary mapping pool names to their metrics</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        Dictionary<string, Dictionary<string, object>> GetAllPoolMetrics();

        /// <summary>
        /// Resets metrics for all registered pools that implement IPoolMetrics.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the registry is disposed</exception>
        void ResetAllPoolMetrics();
    }
}