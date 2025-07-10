using System;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// Simple adapter that wraps an IDependencyProvider to implement the standard .NET IServiceProvider interface.
    /// Provides basic .NET compatibility layer for frameworks that expect IServiceProvider.
    /// Thread-safe and optimized for minimal allocations.
    /// </summary>
    public sealed class ServiceProviderAdapter : IServiceProvider, IDisposable
    {
        private readonly IDependencyProvider _dependencyProvider;
        private bool _disposed;

        /// <summary>
        /// Gets the underlying dependency provider.
        /// </summary>
        public IDependencyProvider DependencyProvider => _dependencyProvider;

        /// <summary>
        /// Gets whether this adapter has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed || _dependencyProvider.IsDisposed;

        /// <summary>
        /// Initializes a new ServiceProviderAdapter.
        /// </summary>
        /// <param name="dependencyProvider">The dependency provider to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencyProvider is null.</exception>
        public ServiceProviderAdapter(IDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider ?? throw new ArgumentNullException(nameof(dependencyProvider));
        }

        /// <summary>
        /// Gets a service of the specified type from the container.
        /// Returns null if the service cannot be resolved (standard .NET IServiceProvider behavior).
        /// </summary>
        /// <param name="serviceType">The type of service to resolve.</param>
        /// <returns>The resolved service or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when serviceType is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the adapter has been disposed.</exception>
        public object GetService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ThrowIfDisposed();

            // Handle special case where they're asking for IServiceProvider itself
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            // Handle case where they're asking for the underlying IDependencyProvider
            if (serviceType == typeof(IDependencyProvider))
            {
                return _dependencyProvider;
            }

            try
            {
                // Use reflection to call TryResolve<T> on the dependency provider
                var method = typeof(IDependencyProvider).GetMethod(nameof(IDependencyProvider.TryResolve))
                    ?.MakeGenericMethod(serviceType);

                if (method != null)
                {
                    var parameters = new object[] { null };
                    var success = (bool)method.Invoke(_dependencyProvider, parameters);
                    return success ? parameters[0] : null;
                }

                // Fallback to direct Resolve if TryResolve reflection fails
                var resolveMethod = typeof(IDependencyProvider).GetMethod(nameof(IDependencyProvider.Resolve))
                    ?.MakeGenericMethod(serviceType);

                return resolveMethod?.Invoke(_dependencyProvider, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ServiceResolutionException)
            {
                // Service resolution exceptions should return null for GetService (standard .NET behavior)
                return null;
            }
            catch (Exception)
            {
                // Any other exception should also return null to maintain .NET IServiceProvider contract
                return null;
            }
        }

        /// <summary>
        /// Gets a service of the specified type from the container.
        /// Generic convenience method.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service or default(T) if not found.</returns>
        public T GetService<T>()
        {
            var service = GetService(typeof(T));
            return service != null ? (T)service : default(T);
        }

        /// <summary>
        /// Gets a required service of the specified type from the container.
        /// Throws if the service cannot be resolved (follows .NET GetRequiredService pattern).
        /// </summary>
        /// <param name="serviceType">The type of service to resolve.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when serviceType is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the adapter has been disposed.</exception>
        public object GetRequiredService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            var service = GetService(serviceType);
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve required service for type '{serviceType.FullName}'. " +
                    $"Framework: {_dependencyProvider.Framework}");
            }

            return service;
        }

        /// <summary>
        /// Gets a required service of the specified type from the container.
        /// Generic convenience method.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        public T GetRequiredService<T>()
        {
            return (T)GetRequiredService(typeof(T));
        }

        /// <summary>
        /// Disposes the adapter and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Only dispose the underlying provider if it implements IDisposable
                if (_dependencyProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }

                _disposed = true;
            }
            catch (Exception)
            {
                // Suppress disposal exceptions to prevent finalizer issues
                _disposed = true;
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the adapter has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceProviderAdapter));
        }
    }
}