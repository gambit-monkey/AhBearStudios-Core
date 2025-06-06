using System;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Builder interface for creating and configuring dependency containers.
    /// </summary>
    public interface IDependencyContainerBuilder
    {
        /// <summary>
        /// Gets the name of the container being built.
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;

        /// <summary>
        /// Registers a singleton service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create the service.</param>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory);

        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterInstance<TInterface>(TInterface instance);

        /// <summary>
        /// Registers a transient service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;

        /// <summary>
        /// Registers a transient service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create services.</param>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory);

        /// <summary>
        /// Builds the dependency container.
        /// </summary>
        /// <returns>The configured dependency container.</returns>
        IDependencyContainer Build();
    }
}