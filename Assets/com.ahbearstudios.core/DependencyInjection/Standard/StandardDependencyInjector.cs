using System;
using System.Collections.Generic;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Attributes;

namespace AhBearStudios.Core.DependencyInjection.Standard
{
    public class StandardDependencyInjector : IDependencyInjector
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        public T Resolve<T>()
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                return (T)_services[type];
            }

            // If the type is not registered, we try to create it via reflection
            var instance = Activator.CreateInstance(type);

            // Check for methods with the [Inject] attribute and call them
            foreach (var method in type.GetMethods())
            {
                if (method.IsPublic && method.GetCustomAttribute<InjectAttribute>() != null)
                {
                    // Assuming the method is a void method and has a single parameter
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        var param = parameters[0];
                        var paramType = param.ParameterType;
                        var dependency = _services[paramType]; // Resolve the dependency from the container
                        method.Invoke(instance, new object[] { dependency });
                    }
                }
            }

            return (T)instance;
        }
    }
}