using System;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Builders
{
    /// <summary>
    /// Default implementation of IDependencyContainerBuilder.
    /// </summary>
    internal sealed class DependencyContainerBuilder : IDependencyContainerBuilder
    {
        private readonly IDependencyContainer _container;

        /// <summary>
        /// Gets the name of the container being built.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Initializes a new instance of the DependencyContainerBuilder class.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        public DependencyContainerBuilder(string containerName = null)
        {
            ContainerName = containerName ?? $"Container_{Guid.NewGuid():N}";
            _container = DependencyContainerFactory.Create(ContainerName);
        }

        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _container.RegisterSingleton<TInterface, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a singleton service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create the service.</param>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            _container.RegisterSingleton(factory);
            return this;
        }

        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterInstance<TInterface>(TInterface instance)
        {
            _container.RegisterInstance(instance);
            return this;
        }

        /// <summary>
        /// Registers a transient service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _container.RegisterTransient<TInterface, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a transient service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create services.</param>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            _container.RegisterTransient(factory);
            return this;
        }

        /// <summary>
        /// Builds the dependency container.
        /// </summary>
        /// <returns>The configured dependency container.</returns>
        public IDependencyContainer Build()
        {
            // If using VContainer adapter, build it
            if (_container is VContainerAdapter vcontainerAdapter)
            {
                return vcontainerAdapter.Build();
            }

            return _container;
        }
    }
}