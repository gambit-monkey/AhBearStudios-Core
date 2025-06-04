namespace AhBearStudios.Core.DependencyInjection.Configuration
{
    /// <summary>
    /// Enumeration of service lifetimes.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// A new instance is created each time the service is requested.
        /// </summary>
        Transient,
        
        /// <summary>
        /// A single instance is created and reused for all requests.
        /// </summary>
        Singleton,
        
        /// <summary>
        /// A specific instance was registered directly.
        /// </summary>
        Instance
    }
}