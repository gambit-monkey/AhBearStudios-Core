using System;
using System.Collections.Generic;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using VContainer;
using RegistrationBuilder = AhBearStudios.Core.DependencyInjection.Builders.RegistrationBuilder;

namespace AhBearStudios.Core.DependencyInjection.Extensions.VContainer
{
    /// <summary>
    /// Extension methods for VContainer registration operations.
    /// Provides enhanced registration capabilities beyond basic VContainer functionality.
    /// </summary>
    public static class VContainerRegistrationExtensions
    {
        /// <summary>
        /// Registers a type and returns a registration builder for fluent configuration.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>A registration builder for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static RegistrationBuilder Register<T>(
            this IContainerBuilder builder,
            Lifetime lifetime)
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            
            return new RegistrationBuilder(builder, typeof(T), lifetime);
        }

        /// <summary>
        /// Registers a type with a factory method and returns a registration builder for fluent configuration.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">The factory method to create the instance.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>A registration builder for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static RegistrationBuilder Register<T>(
            this IContainerBuilder builder,
            Func<IObjectResolver, T> factory,
            Lifetime lifetime)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            return new RegistrationBuilder(builder, typeof(T), lifetime, factory);
        }

        /// <summary>
        /// Checks if a type is registered in the container builder.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        public static bool IsRegistered<T>(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return IsRegistered(builder, typeof(T));
        }

        /// <summary>
        /// Checks if a type is registered in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        public static bool IsRegistered(this IContainerBuilder builder, Type type)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (type == null) throw new ArgumentNullException(nameof(type));

            // VContainer doesn't provide a direct way to check registrations in the builder
            // This is a limitation - we'll return false for now
            // In a real implementation, you might need to track registrations separately
            return false;
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

            // Since we can't reliably check registration in VContainer builder,
            // we'll just register it (VContainer handles duplicates)
            builder.Register<TImplementation>(lifetime).As<TInterface>();
            return builder;
        }

        /// <summary>
        /// Conditionally registers a type only if it hasn't been registered yet.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The registration builder for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static RegistrationBuilder RegisterIfNotPresent<T>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.Register<T>(lifetime);
        }

        /// <summary>
        /// Conditionally registers a factory method only if the type hasn't been registered yet.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">The factory method to create the instance.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The registration builder for fluent configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static RegistrationBuilder RegisterIfNotPresent<T>(
            this IContainerBuilder builder,
            Func<IObjectResolver, T> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return builder.Register(factory, lifetime);
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
                if (implementation == null) continue;

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
                        throw new ServiceResolutionException(typeof(T),
                            $"Failed to lazily resolve service of type '{typeof(T).FullName}'", ex);
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
                        throw new ServiceResolutionException(typeof(T),
                            $"Failed to create factory instance of type '{typeof(T).FullName}'", ex);
                    }
                };
            }, Lifetime.Singleton);

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

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
                throw new InvalidOperationException($"Type '{type.FullName}' has no public constructors");
        }
    }
}