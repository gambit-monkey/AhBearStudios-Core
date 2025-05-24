using System;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    public interface IDependencyInjectionAttribute
    {
        Type UnderlyingAttributeType { get; }
        Attribute GetUnderlyingAttribute();
    }
}