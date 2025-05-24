using System;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    public class VContainerInjectAttributeAdapter : IDependencyInjectionAttribute
    {
        private readonly VContainer.InjectAttribute _vcontainerAttribute;

        public VContainerInjectAttributeAdapter()
        {
            _vcontainerAttribute = new VContainer.InjectAttribute();
        }

        public Type UnderlyingAttributeType => typeof(VContainer.InjectAttribute);

        public Attribute GetUnderlyingAttribute()
        {
            return _vcontainerAttribute;
        }
    }
}