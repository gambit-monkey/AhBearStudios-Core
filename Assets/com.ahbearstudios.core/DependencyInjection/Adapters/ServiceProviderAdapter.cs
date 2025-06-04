using System;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// Adapter class to bridge IDependencyProvider to .NET's IServiceProvider interface.
    /// Provides compatibility with standard .NET dependency injection patterns.
    /// </summary>
    internal sealed class ServiceProviderAdapter : IServiceProvider
    {
        private readonly IDependencyProvider _provider;

        /// <summary>
        /// Initializes a new instance of the ServiceProviderAdapter class.
        /// </summary>
        /// <param name="provider">The dependency provider to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
        public ServiceProviderAdapter(IDependencyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>A service object of the specified type, or null if there is no service object of the specified type.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == null) return null;

            try
            {
                // Use reflection to call the generic Resolve method
                var resolveMethod = typeof(IDependencyProvider).GetMethod(nameof(IDependencyProvider.Resolve));
                var genericResolveMethod = resolveMethod?.MakeGenericMethod(serviceType);
                return genericResolveMethod?.Invoke(_provider, null);
            }
            catch (Exception)
            {
                // IServiceProvider contract requires returning null for unresolvable services
                return null;
            }
        }
    }
}