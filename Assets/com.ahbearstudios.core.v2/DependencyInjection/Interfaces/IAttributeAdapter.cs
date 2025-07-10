using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Abstractions
{
    /// <summary>
    /// Interface for attribute adapters that can convert our framework-agnostic attributes
    /// to framework-specific attributes for different DI libraries.
    /// </summary>
    public interface IAttributeAdapter
    {
        /// <summary>
        /// Gets the framework this adapter supports.
        /// </summary>
        ContainerFramework SupportedFramework { get; }

        /// <summary>
        /// Gets the framework-specific attribute type this adapter produces.
        /// </summary>
        Type FrameworkAttributeType { get; }

        /// <summary>
        /// Converts our DIInjectAttribute to the framework-specific attribute.
        /// </summary>
        /// <param name="diAttribute">Our framework-agnostic attribute.</param>
        /// <returns>The framework-specific attribute.</returns>
        Attribute ConvertToFrameworkAttribute(DIInjectAttribute diAttribute);

        /// <summary>
        /// Converts a framework-specific attribute to our DIInjectAttribute.
        /// </summary>
        /// <param name="frameworkAttribute">The framework-specific attribute.</param>
        /// <returns>Our framework-agnostic attribute.</returns>
        DIInjectAttribute ConvertFromFrameworkAttribute(Attribute frameworkAttribute);

        /// <summary>
        /// Checks if the given attribute is supported by this adapter.
        /// </summary>
        /// <param name="attribute">The attribute to check.</param>
        /// <returns>True if supported, false otherwise.</returns>
        bool SupportsAttribute(Attribute attribute);
    }
}