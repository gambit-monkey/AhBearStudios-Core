namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for registry of log configurations that supports retrieval by both name and type.
    /// </summary>
    public interface ILogConfigRegistry
    {
        /// <summary>
        /// Registers a log configuration by name
        /// </summary>
        void RegisterConfig(string name, ILogTargetConfig config);
        
        /// <summary>
        /// Registers a log configuration by type
        /// </summary>
        void RegisterConfigForType<T>(ILogTargetConfig config) where T : class;
        
        /// <summary>
        /// Gets a registered configuration by name
        /// </summary>
        ILogTargetConfig GetConfig(string name);
        
        /// <summary>
        /// Gets a registered configuration by name with type checking
        /// </summary>
        TConfig GetConfig<TConfig>(string name) where TConfig : class, ILogTargetConfig;
        
        /// <summary>
        /// Gets a registered configuration by type
        /// </summary>
        ILogTargetConfig GetConfigForType<T>() where T : class;
        
        /// <summary>
        /// Tries to get a registered configuration by name
        /// </summary>
        bool TryGetConfig(string name, out ILogTargetConfig config);
        
        /// <summary>
        /// Checks if a configuration exists
        /// </summary>
        bool HasConfig(string name);
        
        /// <summary>
        /// Removes a configuration
        /// </summary>
        bool RemoveConfig(string name);
        
        /// <summary>
        /// Clears all configurations
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// Gets or creates a default configuration
        /// </summary>
        ILogTargetConfig GetOrCreateConfig(string name);
    }
}