using System;

namespace AhBearStudios.Core.DependencyInjection.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
        // This can be empty or extended later if you need specific behavior
    }
}