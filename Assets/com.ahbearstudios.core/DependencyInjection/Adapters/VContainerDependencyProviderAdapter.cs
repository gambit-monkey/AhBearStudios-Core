using System;
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
    }
    
    /// <summary>
    /// Exception thrown when a service cannot be resolved from the container.
    /// </summary>
    internal sealed class ServiceResolutionException : Exception
    {
        /// <summary>
        /// Gets the type that failed to resolve.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Initializes a new instance of the ServiceResolutionException class.
        /// </summary>
        /// <param name="serviceType">The type that failed to resolve.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ServiceResolutionException(Type serviceType, string message, Exception innerException = null) 
            : base(message, innerException)
        {
            ServiceType = serviceType;
        }
    }
}