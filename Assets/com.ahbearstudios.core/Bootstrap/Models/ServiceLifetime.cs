namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Service lifetime enumeration for dependency injection.
/// </summary>
public enum ServiceLifetime : byte
{
    /// <summary>Single instance shared across the application.</summary>
    Singleton = 0,
        
    /// <summary>New instance created for each resolution.</summary>
    Transient = 1,
        
    /// <summary>Single instance per scope or lifetime.</summary>
    Scoped = 2
}