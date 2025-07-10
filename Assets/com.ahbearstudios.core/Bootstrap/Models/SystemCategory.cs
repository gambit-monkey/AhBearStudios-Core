namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Enumeration of system categories for installer organization and filtering.
/// Used for conditional installation based on build configuration and platform requirements.
/// </summary>
public enum SystemCategory : byte
{
    /// <summary>Essential systems required for application functionality.</summary>
    Core = 0,
        
    /// <summary>Optional systems that enhance functionality but are not required.</summary>
    Optional = 1,
        
    /// <summary>Development and debugging systems for non-production builds.</summary>
    Development = 2,
        
    /// <summary>Platform-specific systems for particular deployment targets.</summary>
    Platform = 3,
        
    /// <summary>Third-party integrations and external service connections.</summary>
    Integration = 4,
        
    /// <summary>Performance optimization and monitoring systems.</summary>
    Performance = 5
}