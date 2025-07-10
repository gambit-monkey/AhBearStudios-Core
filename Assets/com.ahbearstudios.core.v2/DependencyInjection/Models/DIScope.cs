namespace AhBearStudios.Core.DependencyInjection.Models;

/// <summary>
/// Scope enumeration for dependency injection.
/// </summary>
public enum DIScope
{
    /// <summary>
    /// Use the default scope behavior for the DI framework.
    /// </summary>
    Default,
        
    /// <summary>
    /// Resolve from the root container.
    /// </summary>
    Root,
        
    /// <summary>
    /// Resolve from the current scope.
    /// </summary>
    Current,
        
    /// <summary>
    /// Create a new scope for this dependency.
    /// </summary>
    New
}