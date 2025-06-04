using System;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// VContainer implementation of IDependencyProvider that wraps an IObjectResolver.
    /// Provides the primary adapter between our DI abstractions and VContainer's resolution system.
    /// </summary>
    internal sealed class VContainerDependencyProvider : IDependencyProvider
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the VContainerDependencyProvider class.
        /// </summary>
        /// <param name="resolver">The VContainer object resolver to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public VContainerDependencyProvider(IObjectResolver resolver)
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
    }
}