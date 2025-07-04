
using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Enhanced core interface for dependency injection containers with comprehensive registration and resolution capabilities.
    /// Abstracts the underlying DI implementation to allow swapping between different frameworks.
    /// Uses MessageBusService for event communication and provides advanced features like collections, decorators, and named services.
    /// </summary>
    public interface IDependencyContainer : IDependencyProvider, IDisposable
    {
        /// <summary>
        /// Gets whether this container has been disposed.
        /// </summary>
        bool IsDisposed { get; }
        
        /// <summary>
        /// Gets the name or identifier of this container instance.
        /// </summary>
        string ContainerName { get; }
        
        /// <summary>
        /// Gets the message bus used for publishing container events.
        /// </summary>
        IMessageBusService MessageBusService { get; }
        
        // Core registration methods
        /// <summary>
        /// Registers a singleton instance of the specified type.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a singleton instance with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="factory">Factory method to create the instance.</param>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory);
        
        /// <summary>
        /// Registers a singleton instance directly.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterInstance<TInterface>(TInterface instance);
        
        /// <summary>
        /// Registers a transient type (new instance created each time).
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a transient type with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="factory">Factory method to create instances.</param>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory);
        
        // Enhanced registration methods
        /// <summary>
        /// Registers a factory function for creating instances on demand.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterFactory<T>();
        
        /// <summary>
        /// Registers a lazy wrapper for deferred initialization.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterLazy<T>();
        
        /// <summary>
        /// Registers multiple implementations as a collection.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="implementations">The implementation types.</param>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterCollection<TInterface>(params Type[] implementations);
        
        /// <summary>
        /// Registers a decorator that wraps an existing service.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TDecorator">The decorator type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        IDependencyContainer RegisterDecorator<TService, TDecorator>() where TDecorator : class, TService;
        
        // Core resolution and query methods
        /// <summary>
        /// Checks if a type is registered in this container.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <returns>True if the type is registered, false otherwise.</returns>
        bool IsRegistered<T>();
        
        /// <summary>
        /// Checks if a type is registered in this container.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        bool IsRegistered(Type type);
        
        /// <summary>
        /// Attempts to resolve a service, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        bool TryResolve<T>(out T service);
        
        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or default value.</returns>
        T ResolveOrDefault<T>(T defaultValue = default);
        
        // Enhanced resolution methods
        /// <summary>
        /// Resolves all registered implementations of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>All registered implementations.</returns>
        IEnumerable<T> ResolveAll<T>();
        
        /// <summary>
        /// Resolves a service by name identifier.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="name">The name identifier.</param>
        /// <returns>The named service.</returns>
        T ResolveNamed<T>(string name);
        
        /// <summary>
        /// Attempts to resolve a named service.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="name">The name identifier.</param>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful.</returns>
        bool TryResolveNamed<T>(string name, out T service);
        
        // Container management
        /// <summary>
        /// Creates a child container that inherits registrations from this container.
        /// </summary>
        /// <param name="childName">Optional name for the child container.</param>
        /// <returns>A new child container.</returns>
        IDependencyContainer CreateChildContainer(string childName = null);
        
        /// <summary>
        /// Validates that all registered types can be resolved without circular dependencies.
        /// </summary>
        /// <returns>True if all registrations are valid, false otherwise.</returns>
        bool ValidateRegistrations();
    }
}