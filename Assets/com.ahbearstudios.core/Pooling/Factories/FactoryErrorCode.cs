namespace AhBearStudios.Pooling.Factories
{
    /// <summary>
    /// Error codes that can occur during pool factory operations
    /// </summary>
    public enum FactoryErrorCode
    {
        /// <summary>
        /// No error occurred
        /// </summary>
        None = 0,
        
        /// <summary>
        /// An invalid argument was provided
        /// </summary>
        InvalidArgument,
        
        /// <summary>
        /// The requested type is not supported by this factory
        /// </summary>
        UnsupportedType,
        
        /// <summary>
        /// Failed to create the requested pool
        /// </summary>
        PoolCreationFailed,
        
        /// <summary>
        /// Failed to update factory configuration
        /// </summary>
        ConfigurationUpdateFailed,
        
        /// <summary>
        /// The factory is not in a valid state for this operation
        /// </summary>
        InvalidState,
        
        /// <summary>
        /// A required dependency is missing
        /// </summary>
        MissingDependency,
        
        /// <summary>
        /// Failed to shutdown the factory properly
        /// </summary>
        ShutdownFailed,
        
        /// <summary>
        /// Failed to reset the factory or pool
        /// </summary>
        ResetFailed,
        
        /// <summary>
        /// Failed to initialize the factory
        /// </summary>
        InitializationFailed
        
    }
}
