using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Factories
{
    /// <summary>
    /// Default implementation of INamedServiceFactory.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    internal sealed class NamedServiceFactory<T> : INamedServiceFactory<T>
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the NamedServiceFactory class.
        /// </summary>
        /// <param name="resolver">The object resolver.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public NamedServiceFactory(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Resolves a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <returns>The service instance if found.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the named service cannot be found.</exception>
        public T Resolve(string name)
        {
            if (TryResolve(name, out var service))
            {
                return service;
            }

            throw new ServiceResolutionException(typeof(T),
                $"Named service '{name}' of type '{typeof(T).FullName}' not found");
        }

        /// <summary>
        /// Attempts to resolve a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <param name="service">The service instance if found.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        public bool TryResolve(string name, out T service)
        {
            try
            {
                var namedServices = _resolver.Resolve<IEnumerable<NamedService<T>>>();
                var namedService = namedServices.FirstOrDefault(ns => ns.Name == name);

                if (namedService != null)
                {
                    service = namedService.Instance;
                    return true;
                }
            }
            catch (Exception)
            {
                // Ignore resolution errors
            }

            service = default;
            return false;
        }

        /// <summary>
        /// Gets all available named services.
        /// </summary>
        /// <returns>A dictionary of all named services.</returns>
        public IReadOnlyDictionary<string, T> GetAllNamed()
        {
            try
            {
                var namedServices = _resolver.Resolve<IEnumerable<NamedService<T>>>();
                return namedServices.ToDictionary(ns => ns.Name, ns => ns.Instance);
            }
            catch (Exception)
            {
                return new Dictionary<string, T>();
            }
        }
    }
}