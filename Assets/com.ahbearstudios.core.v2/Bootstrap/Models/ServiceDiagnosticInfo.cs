namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Service diagnostic information for troubleshooting.
/// </summary>
public readonly struct ServiceDiagnosticInfo
{
    /// <summary>Gets the service type that was registered.</summary>
    public readonly Type ServiceType;
        
    /// <summary>Gets the implementation type that was registered.</summary>
    public readonly Type ImplementationType;
        
    /// <summary>Gets the service lifetime configuration.</summary>
    public readonly ServiceLifetime Lifetime;
        
    /// <summary>Gets whether the service was successfully registered.</summary>
    public readonly bool IsRegistered;
        
    /// <summary>Gets whether the service can be resolved from the container.</summary>
    public readonly bool CanBeResolved;
        
    /// <summary>Gets any error messages related to this service.</summary>
    public readonly string ErrorMessage;
        
    /// <summary>Gets the registration timestamp.</summary>
    public readonly DateTime RegistrationTime;
        
    /// <summary>
    /// Initializes service diagnostic information.
    /// </summary>
    public ServiceDiagnosticInfo(Type serviceType, Type implementationType, 
        ServiceLifetime lifetime, bool isRegistered, bool canBeResolved,
        string errorMessage, DateTime registrationTime)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        IsRegistered = isRegistered;
        CanBeResolved = canBeResolved;
        ErrorMessage = errorMessage;
        RegistrationTime = registrationTime;
    }
}