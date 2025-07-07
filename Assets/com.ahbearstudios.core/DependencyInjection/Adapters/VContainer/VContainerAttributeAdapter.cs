using AhBearStudios.Core.DependencyInjection.Abstractions;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Adapters.VContainer;

/// <summary>
/// VContainer attribute adapter that converts between our DIInjectAttribute and VContainer's InjectAttribute.
/// </summary>
public sealed class VContainerAttributeAdapter : IAttributeAdapter
{
    /// <summary>
    /// Gets the framework this adapter supports.
    /// </summary>
    public ContainerFramework SupportedFramework => ContainerFramework.VContainer;

    /// <summary>
    /// Gets the framework-specific attribute type this adapter produces.
    /// </summary>
    public Type FrameworkAttributeType => typeof(VContainer.InjectAttribute);

    /// <summary>
    /// Converts our DIInjectAttribute to VContainer's InjectAttribute.
    /// </summary>
    /// <param name="diAttribute">Our framework-agnostic attribute.</param>
    /// <returns>VContainer's InjectAttribute.</returns>
    public Attribute ConvertToFrameworkAttribute(DIInjectAttribute diAttribute)
    {
        if (diAttribute == null) return null;

        // VContainer's InjectAttribute is simple and doesn't support our advanced features
        // but we can create it and document the limitations
        return new VContainer.InjectAttribute();
    }

    /// <summary>
    /// Converts VContainer's InjectAttribute to our DIInjectAttribute.
    /// </summary>
    /// <param name="frameworkAttribute">VContainer's InjectAttribute.</param>
    /// <returns>Our framework-agnostic attribute.</returns>
    public DIInjectAttribute ConvertFromFrameworkAttribute(Attribute frameworkAttribute)
    {
        if (frameworkAttribute is not VContainer.InjectAttribute)
            return null;

        // VContainer's attribute doesn't have optional/name properties,
        // so we create a basic DIInjectAttribute
        return new DIInjectAttribute();
    }

    /// <summary>
    /// Checks if the given attribute is VContainer's InjectAttribute.
    /// </summary>
    /// <param name="attribute">The attribute to check.</param>
    /// <returns>True if it's VContainer's InjectAttribute.</returns>
    public bool SupportsAttribute(Attribute attribute)
    {
        return attribute is VContainer.InjectAttribute;
    }
}