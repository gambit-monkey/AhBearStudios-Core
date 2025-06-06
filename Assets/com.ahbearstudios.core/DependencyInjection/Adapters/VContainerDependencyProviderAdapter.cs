using System;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// Adapter that wraps VContainer's IObjectResolver to implement our IDependencyProvider interface.
    /// This allows the rest of the system to use our DI abstractions while VContainer provides the actual resolution.
    /// </summary>
    internal sealed class VContainerDependencyProviderAdapter : IDependencyProvider
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the VContainerDependencyProviderAdapter class.
        /// </summary>
        /// <param name="resolver">The VContainer object resolver to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public VContainerDependencyProviderAdapter(IObjectResolver resolver)
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
            catch (Exception ex) when (!(ex is ServiceResolutionException))
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
            service = default;
    
            try
            {
                // Attempt to resolve the service
                service = Resolve<T>();
                return true;
            }
            catch (ServiceResolutionException)
            {
                // Service not found or resolution failed
                return false;
            }
            catch (Exception)
            {
                // Any other exception means resolution failed
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
            // Use TryResolve to attempt resolution
            if (TryResolve<T>(out T service))
            {
                return service;
            }
    
            // Return the default value if resolution failed
            return defaultValue;
        }
    }
}