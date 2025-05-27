using System;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// Additional extension methods for VContainer integration with the profiling system.
    /// </summary>
    public static class VContainerExtensions
    {
        /// <summary>
        /// Attempts to resolve a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <param name="resolver">The object resolver.</param>
        /// <param name="service">The resolved service, or default if not found.</param>
        /// <returns>True if the service was resolved successfully, false otherwise.</returns>
        public static bool TryResolve<T>(this IObjectResolver resolver, out T service)
        {
            if (resolver == null)
            {
                service = default;
                return false;
            }

            try
            {
                service = resolver.Resolve<T>();
                return true;
            }
            catch (VContainerException)
            {
                service = default;
                return false;
            }
        }

        /// <summary>
        /// Resolves a service of the specified type, or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <param name="resolver">The object resolver.</param>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or the default value.</returns>
        public static T ResolveOrDefault<T>(this IObjectResolver resolver, T defaultValue = default)
        {
            if (resolver == null)
                return defaultValue;

            try
            {
                return resolver.Resolve<T>();
            }
            catch (VContainerException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Registers a conditional factory method that only executes if all dependencies are available.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TDep1">First dependency type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">Factory method that creates the service.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterConditional<TService, TDep1>(
            this IContainerBuilder builder,
            Func<TDep1, TService> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            return builder.Register<TService>(container =>
            {
                if (container.TryResolve<TDep1>(out var dep1))
                {
                    return factory(dep1);
                }
                return default;
            }, lifetime);
        }

        /// <summary>
        /// Registers a conditional factory method that only executes if all dependencies are available.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TDep1">First dependency type.</typeparam>
        /// <typeparam name="TDep2">Second dependency type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">Factory method that creates the service.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterConditional<TService, TDep1, TDep2>(
            this IContainerBuilder builder,
            Func<TDep1, TDep2, TService> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            return builder.Register<TService>(container =>
            {
                if (container.TryResolve<TDep1>(out var dep1) && 
                    container.TryResolve<TDep2>(out var dep2))
                {
                    return factory(dep1, dep2);
                }
                return default;
            }, lifetime);
        }

        /// <summary>
        /// Registers a conditional factory method that only executes if all dependencies are available.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TDep1">First dependency type.</typeparam>
        /// <typeparam name="TDep2">Second dependency type.</typeparam>
        /// <typeparam name="TDep3">Third dependency type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">Factory method that creates the service.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterConditional<TService, TDep1, TDep2, TDep3>(
            this IContainerBuilder builder,
            Func<TDep1, TDep2, TDep3, TService> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            return builder.Register<TService>(container =>
            {
                if (container.TryResolve<TDep1>(out var dep1) && 
                    container.TryResolve<TDep2>(out var dep2) &&
                    container.TryResolve<TDep3>(out var dep3))
                {
                    return factory(dep1, dep2, dep3);
                }
                return default;
            }, lifetime);
        }

        /// <summary>
        /// Registers multiple implementations for the same interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="implementations">Array of implementation types.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterMultiple<TInterface>(
            this IContainerBuilder builder,
            Type[] implementations,
            Lifetime lifetime = Lifetime.Transient)
        {
            foreach (var impl in implementations)
            {
                if (typeof(TInterface).IsAssignableFrom(impl))
                {
                    builder.Register(impl, lifetime).As<TInterface>();
                }
            }
            return builder;
        }

        /// <summary>
        /// Registers a decorator for an existing service registration.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TDecorator">The decorator type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">Registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterDecorator<TService, TDecorator>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TDecorator : class, TService
        {
            return builder.Register<TService>(container =>
            {
                // This is a simplified decorator pattern - in practice, you might need
                // more sophisticated logic to handle the original registration
                return container.Resolve<TDecorator>();
            }, lifetime);
        }

        /// <summary>
        /// Registers a lazy factory for the specified service type.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterLazy<T>(this IContainerBuilder builder)
        {
            return builder.Register<Lazy<T>>(container =>
            {
                return new Lazy<T>(() => container.Resolve<T>());
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers a factory function for the specified service type.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterFactory<T>(this IContainerBuilder builder)
        {
            return builder.Register<Func<T>>(container =>
            {
                return () => container.Resolve<T>();
            }, Lifetime.Singleton);
        }
    }
}