using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Core adapter interface for wrapping different DI container implementations.
    /// Provides framework-agnostic operations for registration, resolution, and lifecycle management.
    /// </summary>
    public interface IContainerAdapter : IDisposable
    {
        /// <summary>
        /// Gets the framework this adapter represents.
        /// </summary>
        ContainerFramework Framework { get; }
        
        /// <summary>
        /// Gets the container name or identifier.
        /// </summary>
        string ContainerName { get; }
        
        /// <summary>
        /// Gets whether this adapter has been disposed.
        /// </summary>
        bool IsDisposed { get; }
        
        /// <summary>
        /// Gets whether the container has been built and is ready for resolution.
        /// </summary>
        bool IsBuilt { get; }
        
        /// <summary>
        /// Gets the message bus used by this adapter.
        /// </summary>
        IMessageBusService MessageBusService { get; }
        
        /// <summary>
        /// Gets the configuration used by this adapter.
        /// </summary>
        IDependencyInjectionConfig Configuration { get; }
        
        /// <summary>
        /// Registers a singleton service with the container.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This adapter for method chaining.</returns>
        IContainerAdapter RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a singleton service with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <param name="factory">Factory method to create the service.</param>
        /// <returns>This adapter for method chaining.</returns>
        IContainerAdapter RegisterSingleton<TInterface>(Func<IServiceResolver, TInterface> factory);
        
        /// <summary>
        /// Registers a singleton instance directly.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This adapter for method chaining.</returns>
        IContainerAdapter RegisterInstance<TInterface>(TInterface instance);
        
        /// <summary>
        /// Registers a transient service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This adapter for method chaining.</returns>
        IContainerAdapter RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a transient service with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <param name="factory">Factory method to create services.</param>
        /// <returns>This adapter for method chaining.</returns>
        IContainerAdapter RegisterTransient<TInterface>(Func<IServiceResolver, TInterface> factory);
        
        /// <summary>
        /// Builds the container and makes it ready for resolution.
        /// </summary>
        /// <returns>A service resolver for this container.</returns>
        IServiceResolver Build();
        
        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        /// <typeparam name="T">The service type to check.</typeparam>
        /// <returns>True if registered, false otherwise.</returns>
        bool IsRegistered<T>();
        
        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        /// <param name="serviceType">The service type to check.</param>
        /// <returns>True if registered, false otherwise.</returns>
        bool IsRegistered(Type serviceType);
        
        /// <summary>
        /// Validates the container registrations.
        /// </summary>
        /// <returns>Validation result with details.</returns>
        IContainerValidationResult Validate();
        
        /// <summary>
        /// Creates a child container that inherits registrations.
        /// </summary>
        /// <param name="childName">Optional name for the child container.</param>
        /// <returns>A new child container adapter.</returns>
        IContainerAdapter CreateChild(string childName = null);
        
        /// <summary>
        /// Gets performance metrics for this container.
        /// </summary>
        /// <returns>Performance metrics if enabled, null otherwise.</returns>
        IContainerMetrics GetMetrics();
    }
}