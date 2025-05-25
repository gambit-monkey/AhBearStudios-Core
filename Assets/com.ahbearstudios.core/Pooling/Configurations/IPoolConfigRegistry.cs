using System;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Interface for registry of pool configurations that supports retrieval by both name and type.
    /// Implementations should provide thread safety and memory optimization.
    /// </summary>
    public interface IPoolConfigRegistry
    {
        /// <summary>
        /// Registers a pool configuration by name
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <param name="config">Configuration to register</param>
        /// <exception cref="ArgumentNullException">Thrown if name or config is null</exception>
        void RegisterConfig(string name, IPoolConfig config);
        
        /// <summary>
        /// Registers a pool configuration by type
        /// </summary>
        /// <param name="config">Configuration to register</param>
        /// <typeparam name="T">Type of the items the configuration is for</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        void RegisterConfigForType<T>(IPoolConfig config) where T : class;
        
        /// <summary>
        /// Registers a pool configuration by both name and type
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <param name="config">Configuration to register</param>
        /// <typeparam name="T">Type of the items the configuration is for</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if name or config is null</exception>
        void RegisterConfig<T>(string name, IPoolConfig config) where T : class;
        
        /// <summary>
        /// Gets a registered configuration by name
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <returns>The requested configuration or null if not found</returns>
        IPoolConfig GetConfig(string name);
        
        /// <summary>
        /// Gets a registered configuration by name with type checking
        /// </summary>
        /// <typeparam name="TConfig">Configuration type</typeparam>
        /// <param name="name">Configuration name</param>
        /// <returns>The requested configuration or null if not found or type mismatch</returns>
        TConfig GetConfig<TConfig>(string name) where TConfig : class, IPoolConfig;
        
        /// <summary>
        /// Gets a registered configuration by item type
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <returns>The requested configuration or null if not found</returns>
        IPoolConfig GetConfigForType<TItem>() where TItem : class;
        
        /// <summary>
        /// Gets a registered configuration by item type with config type checking
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <typeparam name="TConfig">Expected configuration type</typeparam>
        /// <returns>The requested configuration or null if not found or type mismatch</returns>
        TConfig GetConfigForType<TItem, TConfig>()
            where TItem : class
            where TConfig : class, IPoolConfig;
        
        /// <summary>
        /// Tries to get a registered configuration by name
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <param name="config">Output parameter for the configuration</param>
        /// <returns>True if found, false otherwise</returns>
        bool TryGetConfig(string name, out IPoolConfig config);
        
        /// <summary>
        /// Tries to get a registered configuration by name with type checking
        /// </summary>
        /// <typeparam name="TConfig">Configuration type</typeparam>
        /// <param name="name">Configuration name</param>
        /// <param name="config">Output parameter for the configuration</param>
        /// <returns>True if found and type matches, false otherwise</returns>
        bool TryGetConfig<TConfig>(string name, out TConfig config)
            where TConfig : class, IPoolConfig;
        
        /// <summary>
        /// Tries to get a registered configuration by item type
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <param name="config">Output parameter for the configuration</param>
        /// <returns>True if found, false otherwise</returns>
        bool TryGetConfigForType<TItem>(out IPoolConfig config) where TItem : class;
        
        /// <summary>
        /// Tries to get a registered configuration by item type with config type checking
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <typeparam name="TConfig">Expected configuration type</typeparam>
        /// <param name="config">Output parameter for the configuration</param>
        /// <returns>True if found and type matches, false otherwise</returns>
        bool TryGetConfigForType<TItem, TConfig>(out TConfig config)
            where TItem : class
            where TConfig : class, IPoolConfig;
        
        /// <summary>
        /// Checks if a configuration with the specified name exists
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <returns>True if exists, false otherwise</returns>
        bool HasConfig(string name);
        
        /// <summary>
        /// Checks if a configuration for the specified item type exists
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <returns>True if exists, false otherwise</returns>
        bool HasConfigForType<TItem>() where TItem : class;
        
        /// <summary>
        /// Removes a configuration by name
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <returns>True if removed, false if not found</returns>
        bool RemoveConfig(string name);
        
        /// <summary>
        /// Removes a configuration by item type
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <returns>True if removed, false if not found</returns>
        bool RemoveConfigForType<TItem>() where TItem : class;
        
        /// <summary>
        /// Clears all registered configurations
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// Creates a default configuration for a specific item type
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <returns>A new default configuration</returns>
        IPoolConfig CreateDefaultConfigForType<TItem>() where TItem : class;
        
        /// <summary>
        /// Gets a configuration by name, creating a default one if not found
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <returns>The existing configuration or a new default one</returns>
        IPoolConfig GetOrCreateConfig(string name);
        
        /// <summary>
        /// Gets a configuration for a type, creating a default one if not found
        /// </summary>
        /// <typeparam name="TItem">Type of items the configuration is for</typeparam>
        /// <returns>The existing configuration or a new default one</returns>
        IPoolConfig GetOrCreateConfigForType<TItem>() where TItem : class;
    }
}