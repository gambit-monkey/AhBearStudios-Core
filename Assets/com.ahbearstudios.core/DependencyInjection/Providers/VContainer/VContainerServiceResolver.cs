
using System;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Providers.VContainer
{
    /// <summary>
    /// VContainer implementation of IDependencyProvider that wraps an IObjectResolver.
    /// Provides the primary adapter between our DI abstractions and VContainer's resolution system.
    /// </summary>
    internal sealed class VContainerServiceResolver : IServiceResolver
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the VContainerServiceResolver class.
        /// </summary>
        /// <param name="resolver">The VContainer object resolver to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public VContainerServiceResolver(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the service cannot be resolved.</exception>
        public T Resolve<T>()
        {
            try
            {
                return _resolver.Resolve<T>();
            }
            catch (VContainerException ex)
            {
                throw new ServiceResolutionException(typeof(T), 
                    $"Failed to resolve service of type '{typeof(T).FullName}' from VContainer", ex);
            }
            catch (Exception ex) when (!(ex is DependencyInjectionException))
            {
                throw new ServiceResolutionException(typeof(T), 
                    $"Unexpected error resolving service of type '{typeof(T).FullName}' from VContainer", ex);
            }
        }

        /// <summary>
        /// Attempts to resolve a dependency, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        public bool TryResolve<T>(out T service)
        {
            try
            {
                // VContainer's TryResolve method returns true if successful
                return _resolver.TryResolve<T>(out service);
            }
            catch (Exception)
            {
                // If any exception occurs during resolution, consider it a failure
                service = default;
                return false;
            }
        }

        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or default value.</returns>
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            // Use TryResolve to attempt resolution safely
            if (TryResolve<T>(out T service))
            {
                return service;
            }
            
            // Return the default value if resolution failed
            return defaultValue;
        }
    }
}