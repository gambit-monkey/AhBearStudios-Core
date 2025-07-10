using System.Collections.Generic;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Abstractions;
using AhBearStudios.Core.DependencyInjection.Adapters.Reflex;
using AhBearStudios.Core.DependencyInjection.Adapters.VContainer;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Services
{
    /// <summary>
    /// Service that manages attribute adapters and provides conversion between
    /// framework-agnostic and framework-specific attributes.
    /// </summary>
    public sealed class AttributeAdapterService
    {
        private readonly Dictionary<ContainerFramework, IAttributeAdapter> _adapters;
        private readonly Dictionary<Type, IAttributeAdapter> _attributeTypeMap;

        /// <summary>
        /// Initializes a new AttributeAdapterService with default adapters.
        /// </summary>
        public AttributeAdapterService()
        {
            _adapters = new Dictionary<ContainerFramework, IAttributeAdapter>();
            _attributeTypeMap = new Dictionary<Type, IAttributeAdapter>();

            // Register default adapters
            RegisterAdapter(new VContainerAttributeAdapter());
            RegisterAdapter(new ReflexAttributeAdapter());
        }

        /// <summary>
        /// Registers an attribute adapter for a specific framework.
        /// </summary>
        /// <param name="adapter">The adapter to register.</param>
        public void RegisterAdapter(IAttributeAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            _adapters[adapter.SupportedFramework] = adapter;
            _attributeTypeMap[adapter.FrameworkAttributeType] = adapter;
        }

        /// <summary>
        /// Converts our DIInjectAttribute to the appropriate framework-specific attribute.
        /// </summary>
        /// <param name="diAttribute">Our framework-agnostic attribute.</param>
        /// <param name="targetFramework">The target DI framework.</param>
        /// <returns>The framework-specific attribute.</returns>
        public Attribute ConvertToFrameworkAttribute(DIInjectAttribute diAttribute, ContainerFramework targetFramework)
        {
            if (diAttribute == null) return null;

            if (_adapters.TryGetValue(targetFramework, out var adapter))
            {
                return adapter.ConvertToFrameworkAttribute(diAttribute);
            }

            throw new NotSupportedException($"No attribute adapter registered for framework: {targetFramework}");
        }

        /// <summary>
        /// Converts a framework-specific attribute to our DIInjectAttribute.
        /// </summary>
        /// <param name="frameworkAttribute">The framework-specific attribute.</param>
        /// <returns>Our framework-agnostic attribute, or null if not supported.</returns>
        public DIInjectAttribute ConvertFromFrameworkAttribute(Attribute frameworkAttribute)
        {
            if (frameworkAttribute == null) return null;

            var attributeType = frameworkAttribute.GetType();
            if (_attributeTypeMap.TryGetValue(attributeType, out var adapter))
            {
                return adapter.ConvertFromFrameworkAttribute(frameworkAttribute);
            }

            // Try to find adapter by checking all adapters
            foreach (var adapterPair in _adapters)
            {
                if (adapterPair.Value.SupportsAttribute(frameworkAttribute))
                {
                    return adapterPair.Value.ConvertFromFrameworkAttribute(frameworkAttribute);
                }
            }

            return null; // Unsupported attribute
        }

        /// <summary>
        /// Gets all injectable members from a type using our framework-agnostic attributes.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>Collection of injectable members with their attributes.</returns>
        public IEnumerable<InjectableMember> GetInjectableMembers(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var members = new List<InjectableMember>();

            // Check constructors
            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var diAttribute = GetDIInjectAttribute(constructor);
                if (diAttribute != null)
                {
                    members.Add(new InjectableMember(constructor, diAttribute, InjectableMemberType.Constructor));
                }
            }

            // Check properties
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance))
            {
                var diAttribute = GetDIInjectAttribute(property);
                if (diAttribute != null)
                {
                    members.Add(new InjectableMember(property, diAttribute, InjectableMemberType.Property));
                }
            }

            // Check fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var diAttribute = GetDIInjectAttribute(field);
                if (diAttribute != null)
                {
                    members.Add(new InjectableMember(field, diAttribute, InjectableMemberType.Field));
                }
            }

            // Check methods
            foreach (var method in
                     type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var diAttribute = GetDIInjectAttribute(method);
                if (diAttribute != null)
                {
                    members.Add(new InjectableMember(method, diAttribute, InjectableMemberType.Method));
                }
            }

            return members.OrderBy(m => m.Attribute.Order);
        }

        /// <summary>
        /// Gets our DIInjectAttribute from a member, checking both our attribute and framework-specific ones.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <returns>Our DIInjectAttribute if found, null otherwise.</returns>
        private DIInjectAttribute GetDIInjectAttribute(MemberInfo member)
        {
            // First check for our own attribute
            var diAttribute = member.GetCustomAttribute<DIInjectAttribute>();
            if (diAttribute != null)
                return diAttribute;

            // Check for framework-specific attributes and convert them
            foreach (var attribute in member.GetCustomAttributes())
            {
                var convertedAttribute = ConvertFromFrameworkAttribute(attribute);
                if (convertedAttribute != null)
                    return convertedAttribute;
            }

            return null;
        }
    }
}