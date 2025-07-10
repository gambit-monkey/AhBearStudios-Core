using System;

namespace AhBearStudios.Core.DependencyInjection.Attributes
{
    /// <summary>
    /// Attribute that marks constructors, properties, fields, or methods for dependency injection.
    /// Provides a framework-agnostic way to mark dependencies while supporting underlying DI frameworks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
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
        public string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the order of injection when multiple injectable members exist.
        /// Lower values are injected first.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Initializes a new instance of the InjectAttribute class.
        /// </summary>
        public InjectAttribute()
        {
            Optional = false;
            Order = 0;
        }

        /// <summary>
        /// Initializes a new instance of the InjectAttribute class with an identifier.
        /// </summary>
        /// <param name="identifier">The identifier for named dependency resolution.</param>
        public InjectAttribute(string identifier)
        {
            Identifier = identifier;
            Optional = false;
            Order = 0;
        }

        /// <summary>
        /// Initializes a new instance of the InjectAttribute class with optional specification.
        /// </summary>
        /// <param name="optional">Whether the dependency is optional.</param>
        public InjectAttribute(bool optional)
        {
            Optional = optional;
            Order = 0;
        }

        /// <summary>
        /// Initializes a new instance of the InjectAttribute class with identifier and optional specification.
        /// </summary>
        /// <param name="identifier">The identifier for named dependency resolution.</param>
        /// <param name="optional">Whether the dependency is optional.</param>
        public InjectAttribute(string identifier, bool optional)
        {
            Identifier = identifier;
            Optional = optional;
            Order = 0;
        }

        /// <summary>
        /// Gets the underlying VContainer InjectAttribute for framework compatibility.
        /// </summary>
        /// <returns>A VContainer InjectAttribute instance.</returns>
        public VContainer.InjectAttribute ToVContainerAttribute()
        {
            return new VContainer.InjectAttribute();
        }

        /// <summary>
        /// Converts this attribute to the underlying framework's inject attribute.
        /// </summary>
        /// <typeparam name="T">The type of attribute to convert to.</typeparam>
        /// <returns>The converted attribute.</returns>
        /// <exception cref="NotSupportedException">Thrown when the target attribute type is not supported.</exception>
        public T ToFrameworkAttribute<T>() where T : Attribute
        {
            if (typeof(T) == typeof(VContainer.InjectAttribute))
            {
                return ToVContainerAttribute() as T;
            }

            throw new NotSupportedException($"Conversion to attribute type '{typeof(T).FullName}' is not supported");
        }

        /// <summary>
        /// Creates an inject attribute from a VContainer attribute.
        /// </summary>
        /// <param name="vcontainerAttribute">The VContainer attribute to convert from.</param>
        /// <returns>A new InjectAttribute instance.</returns>
        public static InjectAttribute FromVContainerAttribute(VContainer.InjectAttribute vcontainerAttribute)
        {
            if (vcontainerAttribute == null)
                return null;

            return new InjectAttribute();
        }

        /// <summary>
        /// Checks if the specified member has an inject attribute.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <returns>True if the member has an inject attribute, false otherwise.</returns>
        public static bool IsMarkedForInjection(System.Reflection.MemberInfo member)
        {
            if (member == null)
                return false;

            // Check for our attribute
            if (member.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                return true;

            // Check for VContainer attribute for compatibility
            if (member.GetCustomAttributes(typeof(VContainer.InjectAttribute), true).Length > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the inject attribute from a member, handling multiple attribute types.
        /// </summary>
        /// <param name="member">The member to get the attribute from.</param>
        /// <returns>The inject attribute if found, null otherwise.</returns>
        public static InjectAttribute GetInjectAttribute(System.Reflection.MemberInfo member)
        {
            if (member == null)
                return null;

            // First try to get our attribute
            var ourAttribute = member.GetCustomAttributes(typeof(InjectAttribute), true);
            if (ourAttribute.Length > 0)
                return (InjectAttribute)ourAttribute[0];

            // Fallback to VContainer attribute
            var vcontainerAttribute = member.GetCustomAttributes(typeof(VContainer.InjectAttribute), true);
            if (vcontainerAttribute.Length > 0)
                return FromVContainerAttribute((VContainer.InjectAttribute)vcontainerAttribute[0]);

            return null;
        }

        /// <summary>
        /// Returns a string representation of this attribute.
        /// </summary>
        /// <returns>A string describing this attribute.</returns>
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            
            if (!string.IsNullOrEmpty(Identifier))
                parts.Add($"Identifier='{Identifier}'");
            
            if (Optional)
                parts.Add("Optional=true");
            
            if (Order != 0)
                parts.Add($"Order={Order}");

            return parts.Count > 0 ? $"[Inject({string.Join(", ", parts)})]" : "[Inject]";
        }

        /// <summary>
        /// Determines whether the specified object is equal to this attribute.
        /// </summary>
        /// <param name="obj">The object to compare with this attribute.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is InjectAttribute other)
            {
                return Optional == other.Optional &&
                       string.Equals(Identifier, other.Identifier, StringComparison.Ordinal) &&
                       Order == other.Order;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for this attribute.
        /// </summary>
        /// <returns>A hash code value for this attribute.</returns>
        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 23 + Optional.GetHashCode();
            hash = hash * 23 + (Identifier?.GetHashCode() ?? 0);
            hash = hash * 23 + Order.GetHashCode();
            return hash;
        }
    }
}