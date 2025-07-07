using AhBearStudios.Core.DependencyInjection.Abstractions;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Adapters.Reflex;

/// <summary>
/// Reflex attribute adapter (placeholder for when Reflex support is added).
/// </summary>
public sealed class ReflexAttributeAdapter : IAttributeAdapter
{
    /// <summary>
    /// Gets the framework this adapter supports.
    /// </summary>
    public ContainerFramework SupportedFramework => ContainerFramework.Reflex;

    /// <summary>
    /// Gets the framework-specific attribute type this adapter produces.
    /// </summary>
    public Type FrameworkAttributeType => typeof(object); // Placeholder - replace with actual Reflex attribute

    /// <summary>
    /// Converts our DIInjectAttribute to Reflex's inject attribute.
    /// </summary>
    public Attribute ConvertToFrameworkAttribute(DIInjectAttribute diAttribute)
    {
        if (diAttribute == null) return null;

        // TODO: Implement when Reflex support is added
        // return new Reflex.InjectAttribute();
        throw new NotImplementedException("Reflex attribute conversion not yet implemented");
    }

    /// <summary>
    /// Converts Reflex's inject attribute to our DIInjectAttribute.
    /// </summary>
    public DIInjectAttribute ConvertFromFrameworkAttribute(Attribute frameworkAttribute)
    {
        // TODO: Implement when Reflex support is added
        throw new NotImplementedException("Reflex attribute conversion not yet implemented");
    }

    /// <summary>
    /// Checks if the given attribute is supported by Reflex.
    /// </summary>
    public bool SupportsAttribute(Attribute attribute)
    {
        // TODO: Implement when Reflex support is added
        return false;
    }
}