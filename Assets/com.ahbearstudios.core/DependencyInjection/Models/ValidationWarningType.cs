namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Types of validation warnings that can occur.
    /// </summary>
    public enum ValidationWarningType
    {
        /// <summary>
        /// Singleton service implements IDisposable.
        /// </summary>
        SingletonDisposable,
        
        /// <summary>
        /// Service registered multiple times with different lifetimes.
        /// </summary>
        MultipleRegistrations,
        
        /// <summary>
        /// Service has multiple constructors - using longest.
        /// </summary>
        MultipleConstructors,
        
        /// <summary>
        /// Service registered but never resolved (potential unused registration).
        /// </summary>
        UnusedRegistration,
        
        /// <summary>
        /// Service resolution takes longer than expected.
        /// </summary>
        SlowResolution,
        
        /// <summary>
        /// Service has complex dependency chain.
        /// </summary>
        ComplexDependencyChain,
        
        /// <summary>
        /// Framework-specific performance warning.
        /// </summary>
        PerformanceWarning
    }
}