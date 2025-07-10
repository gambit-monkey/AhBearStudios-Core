namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Dependency information for installer dependencies.
/// </summary>
public readonly struct DependencyInfo
{
    /// <summary>Gets the dependency installer type.</summary>
    public readonly Type DependencyType;
        
    /// <summary>Gets the dependency installer name.</summary>
    public readonly string DependencyName;
        
    /// <summary>Gets whether this dependency is satisfied.</summary>
    public readonly bool IsSatisfied;
        
    /// <summary>Gets whether this dependency is optional or required.</summary>
    public readonly bool IsOptional;
        
    /// <summary>Gets the reason if the dependency is not satisfied.</summary>
    public readonly string UnsatisfiedReason;
        
    /// <summary>
    /// Initializes dependency information.
    /// </summary>
    public DependencyInfo(Type dependencyType, string dependencyName, bool isSatisfied,
        bool isOptional, string unsatisfiedReason)
    {
        DependencyType = dependencyType;
        DependencyName = dependencyName;
        IsSatisfied = isSatisfied;
        IsOptional = isOptional;
        UnsatisfiedReason = unsatisfiedReason;
    }
}