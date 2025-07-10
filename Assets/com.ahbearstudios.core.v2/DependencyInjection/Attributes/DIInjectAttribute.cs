using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Attributes
{
    /// <summary>
    /// Framework-agnostic dependency injection attribute.
    /// This attribute works with any DI framework through our abstraction system.
    /// Replaces framework-specific attributes like [Inject] from VContainer, Reflex, etc.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method |
        AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class DIInjectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether this dependency is optional.
        /// Optional dependencies will not cause injection failures if they cannot be resolved.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Gets or sets an identifier for named dependencies.
        /// This allows multiple registrations of the same type with different identifiers.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the order of injection when multiple injectable members exist.
        /// Lower values are injected first.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the scope for this dependency.
        /// Determines how the dependency should be resolved in scoped scenarios.
        /// </summary>
        public DIScope Scope { get; set; }

        /// <summary>
        /// Initializes a new instance of the DIInjectAttribute class.
        /// </summary>
        public DIInjectAttribute()
        {
            Optional = false;
            Order = 0;
            Scope = DIScope.Default;
        }

        /// <summary>
        /// Initializes a new instance with an identifier.
        /// </summary>
        /// <param name="name">The identifier for named dependency resolution.</param>
        public DIInjectAttribute(string name) : this()
        {
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance with optional specification.
        /// </summary>
        /// <param name="optional">Whether the dependency is optional.</param>
        public DIInjectAttribute(bool optional) : this()
        {
            Optional = optional;
        }

        /// <summary>
        /// Initializes a new instance with name and optional specification.
        /// </summary>
        /// <param name="name">The identifier for named dependency resolution.</param>
        /// <param name="optional">Whether the dependency is optional.</param>
        public DIInjectAttribute(string name, bool optional) : this()
        {
            Name = name;
            Optional = optional;
        }
    }
}