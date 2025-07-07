using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Providers.VContainer
{
    /// <summary>
    /// VContainer implementation of IDependencyProvider that adapts VContainer's IObjectResolver.
    /// Provides a lightweight, high-performance wrapper for basic dependency resolution scenarios.
    /// Complements VContainerServiceResolver by focusing on the core IDependencyProvider contract.
    /// </summary>
    public sealed class VContainerDependencyProviderAdapter : IDependencyProvider
    {
        private readonly IObjectResolver _resolver;
        private bool _disposed;

        /// <summary>
        /// Gets the framework this provider uses.
        /// </summary>
        public ContainerFramework Framework => ContainerFramework.VContainer;

        /// <summary>
        /// Gets whether this provider is disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

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
        /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
        public T Resolve<T>()
        {
            ThrowIfDisposed();

            try
            {
                return _resolver.Resolve<T>();
            }
            catch (VContainerException ex)
            {
                throw new ServiceResolutionException(
                    typeof(T),
                    $"Failed to resolve service of type '{typeof(T).FullName}' from VContainer",
                    ex,
                    Framework);
            }
            catch (Exception ex) when (!(ex is DependencyInjectionException))
            {
                throw new ServiceResolutionException(
                    typeof(T),
                    $"Unexpected error resolving service of type '{typeof(T).FullName}' from VContainer",
                    ex,
                    Framework);
            }
        }

        /// <summary>
        /// Attempts to resolve a dependency, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
        public bool TryResolve<T>(out T service)
        {
            ThrowIfDisposed();

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
        /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            ThrowIfDisposed();

            // Use TryResolve to attempt resolution safely
            if (TryResolve<T>(out T service))
            {
                return service;
            }

            return defaultValue;
        }

        /// <summary>
        /// Throws ObjectDisposedException if the provider has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VContainerDependencyProviderAdapter));
        }

        /// <summary>
        /// Disposes the dependency provider and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // VContainer's IObjectResolver doesn't typically implement IDisposable
                // But if it does, we should dispose it
                if (_resolver is IDisposable disposableResolver)
                {
                    disposableResolver.Dispose();
                }

                _disposed = true;
            }
            catch (Exception)
            {
                // Suppress disposal errors to prevent exceptions in finalizers
                _disposed = true;
            }
        }
    }
}