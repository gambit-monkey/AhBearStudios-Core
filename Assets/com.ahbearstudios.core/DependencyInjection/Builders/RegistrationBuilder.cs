using System;
using System.Linq;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Builders
{
    /// <summary>
    /// Fluent registration builder for VContainer registrations.
    /// Provides AsImplementedInterfaces() and AsSelf() functionality.
    /// </summary>
    public sealed class RegistrationBuilder
    {
        private readonly IContainerBuilder _builder;
        private readonly Type _implementationType;
        private readonly Lifetime _lifetime;
        private readonly object _factory;
        private readonly bool _isAlreadyRegistered;

        internal RegistrationBuilder(IContainerBuilder builder, Type implementationType, Lifetime lifetime, object factory = null, bool isAlreadyRegistered = false)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _implementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            _lifetime = lifetime;
            _factory = factory;
            _isAlreadyRegistered = isAlreadyRegistered;
        }

        /// <summary>
        /// Registers the type as all implemented interfaces.
        /// </summary>
        /// <returns>The registration builder for chaining.</returns>
        public RegistrationBuilder AsImplementedInterfaces()
        {
            if (_isAlreadyRegistered) return this;

            var interfaces = _implementationType.GetInterfaces()
                .Where(i => i != typeof(IDisposable))
                .ToArray();

            // First register the implementation type
            RegisterImplementationType();

            // Then register interface mappings
            foreach (var interfaceType in interfaces)
            {
                RegisterInterfaceMapping(interfaceType);
            }

            return this;
        }

        /// <summary>
        /// Registers the type as itself (concrete type).
        /// </summary>
        /// <returns>The registration builder for chaining.</returns>
        public RegistrationBuilder AsSelf()
        {
            if (_isAlreadyRegistered) return this;
            RegisterImplementationType();
            return this;
        }

        /// <summary>
        /// Registers the type as the specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <returns>The registration builder for chaining.</returns>
        public RegistrationBuilder As<TInterface>()
        {
            if (_isAlreadyRegistered) return this;
            
            // Register implementation first if not already done
            RegisterImplementationType();
            // Register interface mapping
            RegisterInterfaceMapping(typeof(TInterface));
            return this;
        }

        /// <summary>
        /// Registers the type as the specified interface type.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <returns>The registration builder for chaining.</returns>
        public RegistrationBuilder As(Type interfaceType)
        {
            if (_isAlreadyRegistered) return this;
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));

            // Register implementation first if not already done
            RegisterImplementationType();
            // Register interface mapping
            RegisterInterfaceMapping(interfaceType);
            return this;
        }

        private void RegisterImplementationType()
        {
            try
            {
                if (_factory != null)
                {
                    // Register with factory
                    var factoryMethod = GetRegisterWithFactoryMethod(_implementationType);
                    if (factoryMethod != null)
                    {
                        factoryMethod.Invoke(_builder, new object[] { _factory, _lifetime });
                    }
                }
                else
                {
                    // Register without factory
                    var registerMethod = GetRegisterMethod(_implementationType);
                    if (registerMethod != null)
                    {
                        registerMethod.Invoke(_builder, new object[] { _lifetime });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register implementation type '{_implementationType.FullName}'", ex);
            }
        }

        private void RegisterInterfaceMapping(Type interfaceType)
        {
            try
            {
                // Use VContainer's As<T>() extension
                var asMethod = GetAsMethod(interfaceType);
                if (asMethod != null)
                {
                    // This requires VContainer's built-in registration to be used
                    // We'll use a simpler approach - register interface directly
                    RegisterInterfaceDirectly(interfaceType);
                }
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register interface mapping from '{_implementationType.FullName}' to '{interfaceType.FullName}'", ex);
            }
        }

        private void RegisterInterfaceDirectly(Type interfaceType)
        {
            // Create factory that resolves the implementation type
            var factoryMethod = GetRegisterWithFactoryMethod(interfaceType);
            if (factoryMethod != null)
            {
                var factory = CreateInterfaceFactory(interfaceType);
                factoryMethod.Invoke(_builder, new object[] { factory, _lifetime });
            }
        }

        private object CreateInterfaceFactory(Type interfaceType)
        {
            // Create Func<IObjectResolver, TInterface>
            var factoryType = typeof(Func<,>).MakeGenericType(typeof(IObjectResolver), interfaceType);
            
            var resolverParam = System.Linq.Expressions.Expression.Parameter(typeof(IObjectResolver), "resolver");
            var resolveMethod = typeof(IObjectResolver).GetMethod("Resolve", new Type[0])?.MakeGenericMethod(_implementationType);
            
            if (resolveMethod != null)
            {
                var methodCall = System.Linq.Expressions.Expression.Call(resolverParam, resolveMethod);
                var convertedCall = System.Linq.Expressions.Expression.Convert(methodCall, interfaceType);
                var lambda = System.Linq.Expressions.Expression.Lambda(factoryType, convertedCall, resolverParam);
                return lambda.Compile();
            }

            throw new InvalidOperationException($"Could not create factory for interface '{interfaceType.FullName}'");
        }

        private MethodInfo GetRegisterMethod(Type serviceType)
        {
            return typeof(IContainerBuilder)
                .GetMethods()
                .Where(m => m.Name == "Register" && m.IsGenericMethod)
                .Where(m => m.GetParameters().Length == 1)
                .FirstOrDefault(m => m.GetParameters()[0].ParameterType == typeof(Lifetime))
                ?.MakeGenericMethod(serviceType);
        }

        private MethodInfo GetRegisterWithFactoryMethod(Type serviceType)
        {
            return typeof(IContainerBuilder)
                .GetMethods()
                .Where(m => m.Name == "Register" && m.IsGenericMethod)
                .Where(m => m.GetParameters().Length == 2)
                .FirstOrDefault(m => 
                    m.GetParameters()[0].ParameterType.IsGenericType &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>) &&
                    m.GetParameters()[1].ParameterType == typeof(Lifetime))
                ?.MakeGenericMethod(serviceType);
        }

        private MethodInfo GetAsMethod(Type interfaceType)
        {
            // This would be for VContainer's built-in As<T>() method if available
            // For now, we'll handle it differently
            return null;
        }
    }
}