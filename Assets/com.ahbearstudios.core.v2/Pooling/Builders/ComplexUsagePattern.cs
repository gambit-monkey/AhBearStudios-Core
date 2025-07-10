namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Predefined usage patterns for complex object pools
    /// </summary>
    public enum ComplexPoolUsagePattern
    {
        /// <summary>
        /// Default balanced configuration
        /// </summary>
        Default,
        
        /// <summary>
        /// Configuration optimized for pooling Unity GameObjects
        /// </summary>
        GameObjects,
        
        /// <summary>
        /// Configuration optimized for pooling Unity Components
        /// </summary>
        Components,
        
        /// <summary>
        /// Configuration optimized for high performance with minimal overhead
        /// </summary>
        HighPerformance,
        
        /// <summary>
        /// Configuration optimized for debugging with extensive logging and validation
        /// </summary>
        Debugging,
        
        /// <summary>
        /// Configuration optimized for memory-constrained environments
        /// </summary>
        MemoryConstrained,
        
        /// <summary>
        /// Configuration optimized for objects with complex initialization requirements
        /// </summary>
        ComplexInitialization,
        
        /// <summary>
        /// Configuration optimized for disposable objects
        /// </summary>
        DisposableObjects,
        
        /// <summary>
        /// Configuration optimized for UI elements
        /// </summary>
        UIElements,
        
        /// <summary>
        /// Configuration optimized for ScriptableObjects
        /// </summary>
        ScriptableObjects,
        
        /// <summary>
        /// Configuration optimized for high-frequency runtime usage
        /// </summary>
        Runtime
    }
}