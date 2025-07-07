namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Enumeration of supported DI container frameworks.
    /// </summary>
    public enum ContainerFramework
    {
        /// <summary>
        /// VContainer - Unity-focused DI container with Burst compatibility.
        /// </summary>
        VContainer = 0,
        
        /// <summary>
        /// Reflex - Performance-focused DI container with minimal allocation.
        /// </summary>
        Reflex = 1,
        
        /// <summary>
        /// Zenject - Feature-rich DI container (future support).
        /// </summary>
        Zenject = 2,
        
        /// <summary>
        /// Microsoft DI - .NET standard DI container (future support).
        /// </summary>
        Microsoft = 3
    }
}