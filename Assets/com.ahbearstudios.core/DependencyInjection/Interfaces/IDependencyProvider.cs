using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Enhanced interface for abstracting dependency injection containers.
    /// Provides framework-agnostic service resolution with performance optimization.
    /// </summary>
    public interface IDependencyProvider
    {
        /// <summary>
        /// Gets the framework this provider uses.
        /// </summary>
        ContainerFramework Framework { get; }
        
        /// <summary>
        /// Gets whether this provider is disposed.
        /// </summary>
        bool IsDisposed { get; }
        
        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the service cannot be resolved.</exception>
        T Resolve<T>();

        /// <summary>
        /// Attempts to resolve a dependency, returning false if not found.
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
    }
}