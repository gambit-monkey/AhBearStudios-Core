using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using UnityEngine;

namespace AhBearStudios.Pooling.Factories
{
    /// <summary>
    /// Represents the current state of a pool factory
    /// </summary>
    public enum FactoryState
    {
        /// <summary>
        /// Factory is created but not initialized
        /// </summary>
        Created,

        /// <summary>
        /// Factory is initializing
        /// </summary>
        Initializing,

        /// <summary>
        /// Factory is fully initialized and ready
        /// </summary>
        Ready,

        /// <summary>
        /// Factory is in an error state
        /// </summary>
        Error,
        
        /// <summary>
        /// Failed to shutdown the factory properly
        /// </summary>
        Shutdown,

        /// <summary>
        /// Factory is shutting down
        /// </summary>
        ShuttingDown,

        /// <summary>
        /// Factory is disposed
        /// </summary>
        Disposed
    }

    /// <summary>
    /// Represents options for factory shutdown
    /// </summary>
    public enum FactoryShutdownMode
    {
        /// <summary>
        /// Perform a graceful shutdown, waiting for operations to complete
        /// </summary>
        GracefulShutdown,

        /// <summary>
        /// Force an immediate shutdown, canceling any pending operations
        /// </summary>
        ForceShutdown,

        /// <summary>
        /// Shutdown but keep pools alive
        /// </summary>
        KeepPoolsAlive
    }

    /// <summary>
    /// Options for pool creation
    /// </summary>
    /// <typeparam name="T">Type of items in the pool</typeparam>
    public class PoolCreationOptions<T>
    {
        /// <summary>
        /// Gets or sets the factory function for creating new items
        /// </summary>
        public Func<T> Factory { get; set; }

        /// <summary>
        /// Gets or sets the pool configuration
        /// </summary>
        public IPoolConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the action to reset items when released
        /// </summary>
        public Action<T> ResetAction { get; set; }

        /// <summary>
        /// Gets or sets the action to execute when an item is acquired
        /// </summary>
        public Action<T> OnAcquireAction { get; set; }

        /// <summary>
        /// Gets or sets the action to execute when an item is destroyed
        /// </summary>
        public Action<T> OnDestroyAction { get; set; }

        /// <summary>
        /// Gets or sets the function to validate an item
        /// </summary>
        public Func<T, bool> Validator { get; set; }

        /// <summary>
        /// Gets or sets the name for the pool
        /// </summary>
        public string PoolName { get; set; }

        /// <summary>
        /// Gets or sets the prefab (for Unity pools)
        /// </summary>
        public UnityEngine.Object Prefab { get; set; }

        /// <summary>
        /// Gets or sets the parent transform (for Unity pools)
        /// </summary>
        public Transform Parent { get; set; }

        /// <summary>
        /// Gets or sets whether to enable diagostics for this pool
        /// </summary>
        public bool EnableDiagnostics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically track this pool
        /// </summary>
        public bool AutoTrack { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial count of objects to prewarm
        /// </summary>
        public int PrewarmCount { get; set; }

        /// <summary>
        /// Gets or sets whether to create the pool asynchronously
        /// </summary>
        public bool CreateAsync { get; set; }

        /// <summary>
        /// Creates a new instance of PoolCreationOptions
        /// </summary>
        public PoolCreationOptions() { }

        /// <summary>
        /// Creates a new instance of PoolCreationOptions with a factory
        /// </summary>
        /// <param name="factory">Factory function</param>
        public PoolCreationOptions(Func<T> factory)
        {
            Factory = factory;
        }
    }

    /// <summary>
    /// Represents options for generating a factory report
    /// </summary>
    public class FactoryReportOptions
    {
        /// <summary>
        /// Gets or sets whether to include pool statistics
        /// </summary>
        public bool IncludePoolStats { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include detailed pool information
        /// </summary>
        public bool IncludePoolDetails { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include factory configuration
        /// </summary>
        public bool IncludeFactoryConfig { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include error history
        /// </summary>
        public bool IncludeErrorHistory { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include extension information
        /// </summary>
        public bool IncludeExtensions { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of errors to include
        /// </summary>
        public int MaxErrors { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to include memory usage information
        /// </summary>
        public bool IncludeMemoryUsage { get; set; } = true;
    }

    /// <summary>
    /// Provides statistics about factory operations
    /// </summary>
    public interface IFactoryStatistics
    {
        /// <summary>
        /// Gets the total number of pools created by this factory
        /// </summary>
        int TotalPoolsCreated { get; }

        /// <summary>
        /// Gets the number of currently managed pools
        /// </summary>
        int ManagedPoolCount { get; }

        /// <summary>
        /// Gets the total number of creation errors
        /// </summary>
        int TotalCreationErrors { get; }

        /// <summary>
        /// Gets the creation success rate (0-1)
        /// </summary>
        float CreationSuccessRate { get; }

        /// <summary>
        /// Gets the total memory usage of all managed pools in bytes
        /// </summary>
        long TotalMemoryUsageBytes { get; }

        /// <summary>
        /// Gets the timestamp when the factory was created
        /// </summary>
        DateTime CreationTime { get; }

        /// <summary>
        /// Gets the timestamp when the factory was last initialized
        /// </summary>
        DateTime LastInitTime { get; }

        /// <summary>
        /// Gets a dictionary with pool type counts
        /// </summary>
        IReadOnlyDictionary<Type, int> PoolTypeCounts { get; }

        /// <summary>
        /// Gets a copy of the recent errors
        /// </summary>
        IReadOnlyList<FactoryError> RecentErrors { get; }

        /// <summary>
        /// Resets the statistics
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Provides compatibility information about the factory
    /// </summary>
    public interface IFactoryCompatibilityInfo
    {
        /// <summary>
        /// Gets the minimum Unity version supported
        /// </summary>
        string MinUnityVersion { get; }

        /// <summary>
        /// Gets the minimum .NET version supported
        /// </summary>
        string MinDotNetVersion { get; }

        /// <summary>
        /// Gets the supported pool types
        /// </summary>
        IReadOnlyList<Type> SupportedPoolTypes { get; }

        /// <summary>
        /// Gets the supported item types
        /// </summary>
        IReadOnlyList<Type> SupportedItemTypes { get; }

        /// <summary>
        /// Gets the supported features
        /// </summary>
        IReadOnlyList<string> SupportedFeatures { get; }

        /// <summary>
        /// Checks if a specific type is supported
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if supported</returns>
        bool IsTypeSupported(Type type);

        /// <summary>
        /// Checks if a feature is supported
        /// </summary>
        /// <param name="featureId">Feature identifier</param>
        /// <returns>True if supported</returns>
        bool IsFeatureSupported(string featureId);
    }

    /// <summary>
    /// Event arguments for when a pool is created by a factory
    /// </summary>
    public class PoolCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the created pool
        /// </summary>
        public IPool Pool { get; }

        /// <summary>
        /// Gets the pool name
        /// </summary>
        public string PoolName { get; }

        /// <summary>
        /// Gets the item type
        /// </summary>
        public Type ItemType { get; }

        /// <summary>
        /// Gets the creation timestamp
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Creates a new instance of PoolCreatedEventArgs
        /// </summary>
        /// <param name="pool">Created pool</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="itemType">Item type</param>
        public PoolCreatedEventArgs(IPool pool, string poolName, Type itemType)
        {
            Pool = pool;
            PoolName = poolName;
            ItemType = itemType;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for when a pool is destroyed
    /// </summary>
    public class PoolDestroyedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the pool name
        /// </summary>
        public string PoolName { get; }

        /// <summary>
        /// Gets the item type
        /// </summary>
        public Type ItemType { get; }

        /// <summary>
        /// Gets the destruction timestamp
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Creates a new instance of PoolDestroyedEventArgs
        /// </summary>
        /// <param name="poolName">Pool name</param>
        /// <param name="itemType">Item type</param>
        public PoolDestroyedEventArgs(string poolName, Type itemType)
        {
            PoolName = poolName;
            ItemType = itemType;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for factory errors
    /// </summary>
    public class FactoryErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error
        /// </summary>
        public FactoryError Error { get; }

        /// <summary>
        /// Creates a new instance of FactoryErrorEventArgs
        /// </summary>
        /// <param name="error">Factory error</param>
        public FactoryErrorEventArgs(FactoryError error)
        {
            Error = error;
        }
    }

    /// <summary>
    /// Factory configuration
    /// </summary>
    public interface FactorySupports
    {
        /// <summary>
        /// Gets or sets the maximum number of pools this factory can manage
        /// </summary>
        int MaxManagedPools { get; set; }

        /// <summary>
        /// Gets or sets whether to enable automatic metrics collection
        /// </summary>
        bool EnableMetrics { get; set; }

        /// <summary>
        /// Gets or sets whether to enable detailed logging
        /// </summary>
        bool EnableDetailedLogging { get; set; }

        /// <summary>
        /// Gets or sets whether to validate pools on creation
        /// </summary>
        bool ValidateOnCreation { get; set; }

        /// <summary>
        /// Gets or sets the default pool config provider
        /// </summary>
        Type DefaultConfigProviderType { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically shrink pools
        /// </summary>
        bool AutoShrinkPools { get; set; }

        /// <summary>
        /// Gets or sets the auto-shrink interval in seconds
        /// </summary>
        float AutoShrinkInterval { get; set; }

        /// <summary>
        /// Gets or sets whether to use thread-safe implementations where possible
        /// </summary>
        bool UseThreadSafety { get; set; }

        /// <summary>
        /// Gets or sets whether to throw exceptions on errors or just log them
        /// </summary>
        bool ThrowOnError { get; set; }

        /// <summary>
        /// Gets or sets whether diagnostics are enabled
        /// </summary>
        bool EnableDiagnostics { get; set; }

        /// <summary>
        /// Gets or sets additional configuration options
        /// </summary>
        Dictionary<string, object> AdditionalOptions { get; set; }
    }
}