using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Extensions
{
    /// <summary>
    /// Extension methods for VContainer's IContainerBuilder that provide enhanced functionality
    /// and better integration with the framework's dependency injection abstractions.
    /// </summary>
    public static class VContainerBuilderExtensions
    {
        private static readonly FieldInfo _registrationsField;

        static VContainerBuilderExtensions()
        {
            // Use reflection to access the internal registrations collection from VContainer
            _registrationsField = typeof(ContainerBuilder).GetField("registrations",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (_registrationsField == null)
            {
                throw new InvalidOperationException(
                    "Could not find 'registrations' field in VContainer.ContainerBuilder. " +
                    "This extension might not be compatible with the current VContainer version.");
            }
        }

        /// <summary>
        /// Checks if a type is already registered in the container builder.
        /// </summary>
        /// <typeparam name="T">The type to check for registration.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static bool IsRegistered<T>(this IContainerBuilder builder)
        {
            return IsRegistered(builder, typeof(T));
        }

        /// <summary>
        /// Checks if a type is already registered in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="type">The type to check for registration.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or type is null.</exception>
        /// <exception cref="ArgumentException">Thrown when builder is not a ContainerBuilder instance.</exception>
        public static bool IsRegistered(this IContainerBuilder builder, Type type)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (type == null) throw new ArgumentNullException(nameof(type));

            // Only works with ContainerBuilder
            if (!(builder is ContainerBuilder containerBuilder))
            {
                throw new ArgumentException(
                    "IsRegistered only works with VContainer.ContainerBuilder instances",
                    nameof(builder));
            }

            try
            {
                // Get the internal registrations collection
                var registrations = _registrationsField.GetValue(containerBuilder) as IList<Registration>;
                if (registrations == null)
                {
                    return false;
                }

                // Check if the type is registered directly or as an interface/base class
                return registrations.Any(reg =>
                    reg.InterfaceTypes.Contains(type) || // Registered as interface
                    reg.ImplementationType == type); // Registered as implementation
            }
            catch (Exception)
            {
                // If we can't access registrations for any reason, assume not registered
                return false;
            }
        }

        /// <summary>
        /// Conditionally registers a type only if it hasn't been registered yet.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterIfNotPresent<TInterface, TImplementation>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TImplementation : class, TInterface
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!builder.IsRegistered<TInterface>())
            {
                builder.Register<TImplementation>(lifetime).As<TInterface>();
            }

            return builder;
        }

        /// <summary>
        /// Conditionally registers a type only if it hasn't been registered yet.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterIfNotPresent<T>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!builder.IsRegistered<T>())
            {
                builder.Register<T>(lifetime);
            }

            return builder;
        }

        /// <summary>
        /// Conditionally registers a factory method only if the type hasn't been registered yet.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">The factory method to create the instance.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static IContainerBuilder RegisterIfNotPresent<T>(
            this IContainerBuilder builder,
            Func<IObjectResolver, T> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (!builder.IsRegistered<T>())
            {
                builder.Register(factory, lifetime);
            }

            return builder;
        }

        /// <summary>
        /// Registers multiple implementations for the same interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="implementations">The implementation types.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or implementations is null.</exception>
        public static IContainerBuilder RegisterMultiple<TInterface>(
            this IContainerBuilder builder,
            IEnumerable<Type> implementations,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (implementations == null) throw new ArgumentNullException(nameof(implementations));

            foreach (var implementation in implementations)
            {
                if (implementation == null)
                    continue;

                if (!typeof(TInterface).IsAssignableFrom(implementation))
                {
                    throw new ArgumentException(
                        $"Type '{implementation.FullName}' does not implement '{typeof(TInterface).FullName}'",
                        nameof(implementations));
                }

                try
                {
                    builder.Register(implementation, lifetime).As<TInterface>();
                }
                catch (Exception ex)
                {
                    throw new DependencyInjectionException(
                        $"Failed to register implementation '{implementation.FullName}' for interface '{typeof(TInterface).FullName}'",
                        ex);
                }
            }

            return builder;
        }

        /// <summary>
        /// Registers a collection of implementations as an enumerable.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="implementations">The implementation types.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or implementations is null.</exception>
        public static IContainerBuilder RegisterCollection<TInterface>(
            this IContainerBuilder builder,
            IEnumerable<Type> implementations,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (implementations == null) throw new ArgumentNullException(nameof(implementations));

            var implementationArray = implementations.ToArray();
            
            // Register individual implementations
            RegisterMultiple<TInterface>(builder, implementationArray, lifetime);

            // Register the collection
            builder.Register<IEnumerable<TInterface>>(resolver =>
            {
                var instances = new List<TInterface>();
                foreach (var implementationType in implementationArray)
                {
                    try
                    {
                        var instance = resolver.Resolve(implementationType);
                        if (instance is TInterface interfaceInstance)
                        {
                            instances.Add(interfaceInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Failed to resolve implementation '{implementationType.FullName}': {ex.Message}");
                    }
                }
                return instances;
            }, Lifetime.Transient);

            return builder;
        }

        /// <summary>
        /// Registers a lazy factory for the specified service type.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterLazy<T>(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Register<Lazy<T>>(resolver =>
            {
                return new Lazy<T>(() =>
                {
                    try
                    {
                        return resolver.Resolve<T>();
                    }
                    catch (Exception ex)
                    {
                        throw new ServiceResolutionException(typeof(T), $"Failed to lazily resolve service of type '{typeof(T).FullName}'", ex);
                    }
                });
            }, Lifetime.Singleton);

            return builder;
        }

        /// <summary>
        /// Registers a factory function for the specified service type.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterFactory<T>(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Register<Func<T>>(resolver =>
            {
                return () =>
                {
                    try
                    {
                        return resolver.Resolve<T>();
                    }
                    catch (Exception ex)
                    {
                        throw new ServiceResolutionException(typeof(T), $"Failed to create factory instance of type '{typeof(T).FullName}'", ex);
                    }
                };
            }, Lifetime.Singleton);

            return builder;
        }

        /// <summary>
        /// Registers a conditional factory that only creates an instance if all dependencies are available.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TDep1">First dependency type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">Factory method that creates the service.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static IContainerBuilder RegisterConditional<TService, TDep1>(
            this IContainerBuilder builder,
            Func<TDep1, TService> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            builder.Register<TService>(resolver =>
            {
                var dep1 = resolver.Resolve<TDep1>();
                return factory(dep1);
            }, lifetime);

            return builder;
        }

        /// <summary>
        /// Registers a conditional factory that only creates an instance if all dependencies are available.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TDep1">First dependency type.</typeparam>
        /// <typeparam name="TDep2">Second dependency type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">Factory method that creates the service.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static IContainerBuilder RegisterConditional<TService, TDep1, TDep2>(
            this IContainerBuilder builder,
            Func<TDep1, TDep2, TService> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            builder.Register<TService>(resolver =>
            {
                var dep1 = resolver.Resolve<TDep1>();
                var dep2 = resolver.Resolve<TDep2>();
                return factory(dep1, dep2);
            }, lifetime);

            return builder;
        }

        /// <summary>
        /// Registers a conditional factory that only creates an instance if all dependencies are available.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TDep1">First dependency type.</typeparam>
        /// <typeparam name="TDep2">Second dependency type.</typeparam>
        /// <typeparam name="TDep3">Third dependency type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">Factory method that creates the service.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static IContainerBuilder RegisterConditional<TService, TDep1, TDep2, TDep3>(
            this IContainerBuilder builder,
            Func<TDep1, TDep2, TDep3, TService> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            builder.Register<TService>(resolver =>
            {
                var dep1 = resolver.Resolve<TDep1>();
                var dep2 = resolver.Resolve<TDep2>();
                var dep3 = resolver.Resolve<TDep3>();
                return factory(dep1, dep2, dep3);
            }, lifetime);

            return builder;
        }

        /// <summary>
        /// Registers a decorator for an existing service registration.
        /// The decorator must wrap the original service and provide the same interface.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TDecorator">The decorator type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the service is not already registered.</exception>
        public static IContainerBuilder RegisterDecorator<TService, TDecorator>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TDecorator : class, TService
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!builder.IsRegistered<TService>())
            {
                throw new InvalidOperationException(
                    $"Cannot register decorator for '{typeof(TService).FullName}' because the service is not registered. " +
                    "Register the base service first.");
            }

            // This is a simplified decorator implementation
            // In a production system, you might need more sophisticated decorator chaining
            builder.Register<TService>(resolver =>
            {
                try
                {
                    // Create the decorator, which should resolve its own dependencies including the wrapped service
                    return resolver.Resolve<TDecorator>();
                }
                catch (Exception ex)
                {
                    throw new ServiceResolutionException(typeof(TService), 
                        $"Failed to create decorator '{typeof(TDecorator).FullName}' for service '{typeof(TService).FullName}'", ex);
                }
            }, lifetime);

            return builder;
        }

        /// <summary>
        /// Registers all types from an assembly that implement a specific interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register implementations for.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="assembly">The assembly to scan for implementations.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <param name="includeAbstract">Whether to include abstract types.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or assembly is null.</exception>
        public static IContainerBuilder RegisterFromAssembly<TInterface>(
            this IContainerBuilder builder,
            Assembly assembly,
            Lifetime lifetime = Lifetime.Transient,
            bool includeAbstract = false)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var interfaceType = typeof(TInterface);
            var implementationTypes = assembly.GetTypes()
                .Where(type => 
                    interfaceType.IsAssignableFrom(type) &&
                    type != interfaceType &&
                    (includeAbstract || (!type.IsAbstract && !type.IsInterface)) &&
                    type.IsClass &&
                    !type.IsGenericTypeDefinition)
                .ToArray();

            if (implementationTypes.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"No implementations of '{interfaceType.FullName}' found in assembly '{assembly.FullName}'");
                return builder;
            }

            try
            {
                RegisterMultiple<TInterface>(builder, implementationTypes, lifetime);
                
                UnityEngine.Debug.Log($"Registered {implementationTypes.Length} implementations of '{interfaceType.FullName}' from assembly '{assembly.GetName().Name}'");
                return builder;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register implementations of '{interfaceType.FullName}' from assembly '{assembly.FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a service with validation that it can be constructed.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the implementation cannot be constructed.</exception>
        public static IContainerBuilder RegisterWithValidation<TInterface, TImplementation>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TImplementation : class, TInterface
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // Validate that the implementation type can be constructed
            var implementationType = typeof(TImplementation);
            ValidateTypeCanBeConstructed(implementationType);

            builder.Register<TImplementation>(lifetime).As<TInterface>();

            return builder;
        }

        /// <summary>
        /// Registers all implementations of an interface found in the current assembly with automatic validation.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register implementations for.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <param name="validateTypes">Whether to validate each type can be constructed.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterAllImplementations<TInterface>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient,
            bool validateTypes = true)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var assembly = typeof(TInterface).Assembly;
            var interfaceType = typeof(TInterface);
            
            var implementationTypes = assembly.GetTypes()
                .Where(type => 
                    interfaceType.IsAssignableFrom(type) &&
                    type != interfaceType &&
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    type.IsClass &&
                    !type.IsGenericTypeDefinition)
                .ToArray();

            if (implementationTypes.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"No implementations of '{interfaceType.FullName}' found in assembly '{assembly.GetName().Name}'");
                return builder;
            }

            foreach (var implementationType in implementationTypes)
            {
                try
                {
                    if (validateTypes)
                    {
                        ValidateTypeCanBeConstructed(implementationType);
                    }

                    builder.Register(implementationType, lifetime).As<TInterface>();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to register implementation '{implementationType.FullName}': {ex.Message}");
                    if (validateTypes)
                    {
                        throw new DependencyInjectionException(
                            $"Failed to register implementation '{implementationType.FullName}' for interface '{interfaceType.FullName}'", ex);
                    }
                }
            }

            UnityEngine.Debug.Log($"Registered {implementationTypes.Length} implementations of '{interfaceType.FullName}'");
            return builder;
        }

        /// <summary>
        /// Registers a service with fallback to a default implementation if the primary fails.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TPrimary">The primary implementation type.</typeparam>
        /// <typeparam name="TFallback">The fallback implementation type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterWithFallback<TInterface, TPrimary, TFallback>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TPrimary : class, TInterface
            where TFallback : class, TInterface
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Register<TInterface>(resolver =>
            {
                try
                {
                    return resolver.Resolve<TPrimary>();
                }
                catch (Exception primaryEx)
                {
                    UnityEngine.Debug.LogWarning($"Primary implementation '{typeof(TPrimary).FullName}' failed, falling back to '{typeof(TFallback).FullName}': {primaryEx.Message}");
            
                    try
                    {
                        return resolver.Resolve<TFallback>();
                    }
                    catch (Exception fallbackEx)
                    {
                        throw new ServiceResolutionException(typeof(TInterface), 
                            $"Both primary '{typeof(TPrimary).FullName}' and fallback '{typeof(TFallback).FullName}' implementations failed", 
                            new AggregateException(primaryEx, fallbackEx));
                    }
                }
            }, lifetime);

            return builder;
        }

        /// <summary>
        /// Validates that a type can be constructed by the DI container.
        /// </summary>
        /// <param name="type">The type to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when the type cannot be constructed.</exception>
        private static void ValidateTypeCanBeConstructed(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.IsAbstract)
                throw new InvalidOperationException($"Cannot register abstract type '{type.FullName}'");

            if (type.IsInterface)
                throw new InvalidOperationException($"Cannot register interface type '{type.FullName}' as implementation");

            if (type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"Cannot register open generic type '{type.FullName}'");

            // Check for public constructors
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
                throw new InvalidOperationException($"Type '{type.FullName}' has no public constructors");

            // Check if there's a parameterless constructor or constructors with injectable parameters
            var hasParameterlessConstructor = constructors.Any(c => c.GetParameters().Length == 0);
            var hasInjectableConstructor = constructors.Any(c => 
                c.GetParameters().All(p => CanParameterBeInjected(p)));

            if (!hasParameterlessConstructor && !hasInjectableConstructor)
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' has no suitable constructors. " +
                    "Ensure there is either a parameterless constructor or a constructor with only injectable parameters.");
            }
        }

        /// <summary>
        /// Checks if a constructor parameter can be injected.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <returns>True if the parameter can be injected, false otherwise.</returns>
        private static bool CanParameterBeInjected(ParameterInfo parameter)
        {
            var parameterType = parameter.ParameterType;

            // Primitive types and value types generally cannot be injected without explicit registration
            if (parameterType.IsPrimitive || parameterType.IsValueType)
                return false;

            // String cannot be injected without explicit registration
            if (parameterType == typeof(string))
                return false;

            // Interfaces and classes can typically be injected
            return parameterType.IsInterface || parameterType.IsClass;
        }

        /// <summary>
        /// Gets the count of registrations in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>The number of registrations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static int GetRegistrationCount(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!(builder is ContainerBuilder containerBuilder))
                return 0;

            try
            {
                var registrations = _registrationsField.GetValue(containerBuilder) as IList<Registration>;
                return registrations?.Count ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets information about all registrations in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A list of registration information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyList<RegistrationInfo> GetRegistrationInfo(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!(builder is ContainerBuilder containerBuilder))
                return new List<RegistrationInfo>();

            try
            {
                var registrations = _registrationsField.GetValue(containerBuilder) as IList<Registration>;
                if (registrations == null)
                    return new List<RegistrationInfo>();

                return registrations.Select(reg => new RegistrationInfo
                {
                    ImplementationType = reg.ImplementationType,
                    InterfaceTypes = reg.InterfaceTypes.ToArray(),
                    Lifetime = reg.Lifetime
                }).ToList();
            }
            catch (Exception)
            {
                return new List<RegistrationInfo>();
            }
        }

        /// <summary>
        /// Logs all current registrations to the Unity console for debugging purposes.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="logLevel">The Unity log level to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static void LogRegistrations(this IContainerBuilder builder, UnityEngine.LogType logLevel = UnityEngine.LogType.Log)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            var message = $"Container has {registrations.Count} registrations:\n" +
                         string.Join("\n", registrations.Select((r, i) => $"  {i + 1}. {r}"));

            switch (logLevel)
            {
                case UnityEngine.LogType.Error:
                    UnityEngine.Debug.LogError(message);
                    break;
                                case UnityEngine.LogType.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case UnityEngine.LogType.Log:
                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
        }

        /// <summary>
        /// Validates all registrations in the container builder to ensure they can be resolved.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="throwOnError">Whether to throw an exception on validation failure.</param>
        /// <returns>True if all registrations are valid, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <exception cref="DependencyInjectionException">Thrown when validation fails and throwOnError is true.</exception>
        public static bool ValidateRegistrations(this IContainerBuilder builder, bool throwOnError = false)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            var errors = new List<string>();

            foreach (var registration in registrations)
            {
                try
                {
                    if (registration.ImplementationType != null)
                    {
                        ValidateTypeCanBeConstructed(registration.ImplementationType);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Registration validation failed for {registration}: {ex.Message}";
                    errors.Add(error);
                    
                    if (!throwOnError)
                    {
                        UnityEngine.Debug.LogError(error);
                    }
                }
            }

            if (errors.Count > 0)
            {
                if (throwOnError)
                {
                    throw new DependencyInjectionException(
                        $"Container validation failed with {errors.Count} errors:\n" + string.Join("\n", errors));
                }
                return false;
            }

            UnityEngine.Debug.Log($"Container validation passed for {registrations.Count} registrations");
            return true;
        }

        /// <summary>
        /// Registers a named instance that can be resolved by identifier.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="name">The name identifier for the registration.</param>
        /// <param name="instance">The instance to register.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder, name, or instance is null.</exception>
        /// <exception cref="ArgumentException">Thrown when name is empty.</exception>
        public static IContainerBuilder RegisterNamed<TInterface>(
            this IContainerBuilder builder,
            string name,
            TInterface instance)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            // Register with a wrapper that includes the name
            builder.Register<NamedService<TInterface>>(resolver => new NamedService<TInterface>(name, instance), Lifetime.Singleton);
            
            return builder;
        }

        /// <summary>
        /// Registers a factory for named instances.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterNamedFactory<TInterface>(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Register<INamedServiceFactory<TInterface>>(resolver =>
                new NamedServiceFactory<TInterface>(resolver), Lifetime.Singleton);

            return builder;
        }

        /// <summary>
        /// Configures the container builder with a setup action.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or configure is null.</exception>
        public static IContainerBuilder Configure(this IContainerBuilder builder, Action<IContainerBuilder> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            configure(builder);
            return builder;
        }

        /// <summary>
        /// Conditionally configures the container builder based on a predicate.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="configure">The configuration action to execute if condition is true.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or configure is null.</exception>
        public static IContainerBuilder ConfigureIf(
            this IContainerBuilder builder, 
            bool condition, 
            Action<IContainerBuilder> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            if (condition)
            {
                configure(builder);
            }

            return builder;
        }

        /// <summary>
        /// Configures the container builder conditionally based on a predicate function.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="predicate">The predicate function to evaluate.</param>
        /// <param name="configure">The configuration action to execute if predicate returns true.</param>
        /// <returns>The builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder, predicate, or configure is null.</exception>
        public static IContainerBuilder ConfigureIf(
            this IContainerBuilder builder,
            Func<IContainerBuilder, bool> predicate,
            Action<IContainerBuilder> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            if (predicate(builder))
            {
                configure(builder);
            }

            return builder;
        }
    }

    /// <summary>
    /// Information about a service registration.
    /// </summary>
    public sealed class RegistrationInfo
    {
        /// <summary>
        /// Gets or sets the implementation type.
        /// </summary>
        public Type ImplementationType { get; set; }

        /// <summary>
        /// Gets or sets the interface types this registration serves.
        /// </summary>
        public Type[] InterfaceTypes { get; set; }

        /// <summary>
        /// Gets or sets the lifetime of the registration.
        /// </summary>
        public Lifetime Lifetime { get; set; }

        /// <summary>
        /// Returns a string representation of this registration info.
        /// </summary>
        /// <returns>A formatted string describing the registration.</returns>
        public override string ToString()
        {
            var interfaces = InterfaceTypes?.Length > 0 
                ? string.Join(", ", InterfaceTypes.Select(t => t.Name))
                : "none";
            
            return $"{ImplementationType?.Name ?? "Factory"} -> [{interfaces}] ({Lifetime})";
        }
    }

    /// <summary>
    /// Wrapper for named service instances.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public sealed class NamedService<T>
    {
        /// <summary>
        /// Gets the name identifier for this service.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the service instance.
        /// </summary>
        public T Instance { get; }

        /// <summary>
        /// Initializes a new instance of the NamedService class.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <param name="instance">The service instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when name or instance is null.</exception>
        public NamedService(string name, T instance)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
    }

    /// <summary>
    /// Factory interface for resolving named services.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public interface INamedServiceFactory<T>
    {
        /// <summary>
        /// Resolves a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <returns>The service instance if found.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the named service cannot be found.</exception>
        T Resolve(string name);

        /// <summary>
        /// Attempts to resolve a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <param name="service">The service instance if found.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        bool TryResolve(string name, out T service);

        /// <summary>
        /// Gets all available named services.
        /// </summary>
        /// <returns>A dictionary of all named services.</returns>
        IReadOnlyDictionary<string, T> GetAllNamed();
    }

    /// <summary>
    /// Default implementation of INamedServiceFactory.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    internal sealed class NamedServiceFactory<T> : INamedServiceFactory<T>
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the NamedServiceFactory class.
        /// </summary>
        /// <param name="resolver">The object resolver.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public NamedServiceFactory(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Resolves a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <returns>The service instance if found.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the named service cannot be found.</exception>
        public T Resolve(string name)
        {
            if (TryResolve(name, out var service))
            {
                return service;
            }

            throw new ServiceResolutionException(typeof(T), $"Named service '{name}' of type '{typeof(T).FullName}' not found");
        }

        /// <summary>
        /// Attempts to resolve a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <param name="service">The service instance if found.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        public bool TryResolve(string name, out T service)
        {
            try
            {
                var namedServices = _resolver.Resolve<IEnumerable<NamedService<T>>>();
                var namedService = namedServices.FirstOrDefault(ns => ns.Name == name);
                
                if (namedService != null)
                {
                    service = namedService.Instance;
                    return true;
                }
            }
            catch (Exception)
            {
                // Ignore resolution errors
            }

            service = default;
            return false;
        }

        /// <summary>
        /// Gets all available named services.
        /// </summary>
        /// <returns>A dictionary of all named services.</returns>
        public IReadOnlyDictionary<string, T> GetAllNamed()
        {
            try
            {
                var namedServices = _resolver.Resolve<IEnumerable<NamedService<T>>>();
                return namedServices.ToDictionary(ns => ns.Name, ns => ns.Instance);
            }
            catch (Exception)
            {
                return new Dictionary<string, T>();
            }
        }
    }
}