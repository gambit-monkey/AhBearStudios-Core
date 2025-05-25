using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Internal;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// Extension methods for container builder registration checks.
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        private static readonly FieldInfo _registrationsField;

        static ContainerBuilderExtensions()
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

        /// <summary>
        /// Conditionally registers a type only if it hasn't been registered yet.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="lifetime">The registration lifetime.</param>
        /// <returns>The builder for chaining.</returns>
        public static IContainerBuilder RegisterIfNotPresent<TInterface, TImplementation>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TImplementation : TInterface
        {
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
        public static IContainerBuilder RegisterIfNotPresent<T>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
        {
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
        public static IContainerBuilder RegisterIfNotPresent<T>(
            this IContainerBuilder builder,
            Func<IObjectResolver, T> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (!builder.IsRegistered<T>())
            {
                builder.Register(factory, lifetime);
            }

            return builder;
        }
    }
}