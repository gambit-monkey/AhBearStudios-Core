using System;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Providers.Unity;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// Adapter that implements IDependencyProvider using UnityDependencyProvider.
    /// </summary>
    internal sealed class UnityDependencyProviderAdapter : IDependencyProvider
    {
        private readonly UnityDependencyProvider _provider;

        /// <summary>
        /// Initializes a new instance of the UnityDependencyProviderAdapter class.
        /// </summary>
        /// <param name="provider">The Unity dependency provider to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
        public UnityDependencyProviderAdapter(UnityDependencyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        public T Resolve<T>()
        {
            return _provider.Resolve<T>();
        }

        /// <summary>
        /// Attempts to resolve a dependency, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        public bool TryResolve<T>(out T service)
        {
            return _provider.TryResolve(out service);
        }

        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or default value.</returns>
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            return _provider.ResolveOrDefault(defaultValue);
        }
    }
}