using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Framework-agnostic service resolution interface.
    /// Provides high-performance service resolution with minimal allocations.
    /// </summary>
    public interface IServiceResolver : IDisposable
    {
        /// <summary>
        /// Gets the container framework this resolver uses.
        /// </summary>
        ContainerFramework Framework { get; }
        
        /// <summary>
        /// Gets whether this resolver has been disposed.
        /// </summary>
        bool IsDisposed { get; }
        
        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when service cannot be resolved.</exception>
        T Resolve<T>();
        
        /// <summary>
        /// Attempts to resolve a service, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        bool TryResolve<T>(out T service);
        
        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or default value.</returns>
        T ResolveOrDefault<T>(T defaultValue = default);
        
        /// <summary>
        /// Resolves all registered implementations of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>All registered implementations.</returns>
        IEnumerable<T> ResolveAll<T>();
        
        /// <summary>
        /// Resolves a named service if named services are enabled.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="name">The service name identifier.</param>
        /// <returns>The named service instance.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when named service cannot be resolved.</exception>
        /// <exception cref="NotSupportedException">Thrown when named services are not enabled.</exception>
        T ResolveNamed<T>(string name);
        
        /// <summary>
        /// Attempts to resolve a named service.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="name">The service name identifier.</param>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        bool TryResolveNamed<T>(string name, out T service);
        
        /// <summary>
        /// Creates a scoped resolver if scoping is enabled.
        /// </summary>
        /// <param name="scopeName">Optional name for the scope.</param>
        /// <returns>A new scoped resolver.</returns>
        /// <exception cref="NotSupportedException">Thrown when scoping is not enabled.</exception>
        IServiceResolver CreateScope(string scopeName = null);
    }
}