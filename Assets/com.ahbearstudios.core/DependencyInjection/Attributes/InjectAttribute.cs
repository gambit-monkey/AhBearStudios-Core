using System;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
        private readonly IDependencyInjectionAttribute _implementationAttribute;

        public InjectAttribute()
        {
            // Default to VContainer implementation
            _implementationAttribute = new VContainerInjectAttributeAdapter();
        }

        public Type UnderlyingAttributeType => _implementationAttribute.UnderlyingAttributeType;

        public Attribute GetUnderlyingAttribute() => _implementationAttribute.GetUnderlyingAttribute();

        // Helper method to get the actual DI container's attribute
        public static Attribute GetContainerAttribute(InjectAttribute attribute)
        {
            return attribute._implementationAttribute.GetUnderlyingAttribute();
        }
    }
}