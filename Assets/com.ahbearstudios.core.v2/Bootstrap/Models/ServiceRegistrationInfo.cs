namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Service registration information for dependency analysis.
/// Provides details about services that will be registered by the installer.
/// </summary>
public readonly struct ServiceRegistrationInfo
{
    /// <summary>Gets the service interface type.</summary>
    public readonly Type ServiceType;
        
    /// <summary>Gets the implementation type.</summary>
    public readonly Type ImplementationType;
        
    /// <summary>Gets the service lifecycle (singleton, transient, etc.).</summary>
    public readonly ServiceLifetime Lifetime;
        
    /// <summary>Gets whether this is a critical service required for system operation.</summary>
    public readonly bool IsCritical;
        
    /// <summary>Gets estimated memory overhead for this service.</summary>
    public readonly long EstimatedMemoryBytes;
        
    /// <summary>
    /// Initializes service registration information.
    /// </summary>
    public ServiceRegistrationInfo(Type serviceType, Type implementationType, 
        ServiceLifetime lifetime, bool isCritical, long estimatedMemoryBytes)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        IsCritical = isCritical;
        EstimatedMemoryBytes = estimatedMemoryBytes;
    }
}