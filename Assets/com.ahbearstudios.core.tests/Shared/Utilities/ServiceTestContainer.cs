using System;
using System.Collections.Generic;
using ZLinq;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Simple service container for dependency injection in tests.
    /// Provides service registration and resolution for mock services.
    /// </summary>
    public sealed class ServiceTestContainer : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private bool _disposed = false;

        /// <summary>
        /// Gets the number of registered services.
        /// </summary>
        public int RegisteredServiceCount => _services.Count + _factories.Count;

        /// <summary>
        /// Registers a service instance in the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="instance">The service instance</param>
        /// <exception cref="ArgumentNullException">Thrown when instance is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is already registered</exception>
        public void RegisterInstance<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TService);

            if (_services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered");

            _services[serviceType] = instance;
        }

        /// <summary>
        /// Registers a service factory in the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="factory">The factory function</param>
        /// <exception cref="ArgumentNullException">Thrown when factory is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is already registered</exception>
        public void RegisterFactory<TService>(Func<TService> factory) where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var serviceType = typeof(TService);

            if (_services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered");

            _factories[serviceType] = () => factory();
        }

        /// <summary>
        /// Registers a service with automatic interface resolution.
        /// </summary>
        /// <typeparam name="TInterface">The interface type</typeparam>
        /// <typeparam name="TImplementation">The implementation type</typeparam>
        /// <param name="instance">The service instance</param>
        public void RegisterAs<TInterface, TImplementation>(TImplementation instance)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var interfaceType = typeof(TInterface);

            if (_services.ContainsKey(interfaceType) || _factories.ContainsKey(interfaceType))
                throw new InvalidOperationException($"Service of type {interfaceType.Name} is already registered");

            _services[interfaceType] = instance;
        }

        /// <summary>
        /// Resolves a service from the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>The service instance, or null if not found</returns>
        public TService Resolve<TService>() where TService : class
        {
            var serviceType = typeof(TService);

            // Try to resolve from instances first
            if (_services.TryGetValue(serviceType, out var instance))
            {
                return (TService)instance;
            }

            // Try to resolve from factories
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                var createdInstance = factory();
                // Store created instance for future resolution (singleton behavior)
                _services[serviceType] = createdInstance;
                return (TService)createdInstance;
            }

            return null;
        }

        /// <summary>
        /// Resolves a service from the container, throwing an exception if not found.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
        public TService ResolveRequired<TService>() where TService : class
        {
            var service = Resolve<TService>();
            if (service == null)
                throw new InvalidOperationException($"Service of type {typeof(TService).Name} is not registered");

            return service;
        }

        /// <summary>
        /// Checks if a service is registered in the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>True if the service is registered</returns>
        public bool IsRegistered<TService>()
        {
            var serviceType = typeof(TService);
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }

        /// <summary>
        /// Unregisters a service from the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>True if the service was unregistered</returns>
        public bool Unregister<TService>()
        {
            var serviceType = typeof(TService);
            var removedFromServices = _services.Remove(serviceType);
            var removedFromFactories = _factories.Remove(serviceType);

            return removedFromServices || removedFromFactories;
        }

        /// <summary>
        /// Gets all registered service types.
        /// </summary>
        /// <returns>Collection of registered service types</returns>
        public IEnumerable<Type> GetRegisteredTypes()
        {
            var allTypes = _services.Keys.AsValueEnumerable().Concat(_factories.Keys.AsValueEnumerable());
            return allTypes.Distinct().ToList();
        }

        /// <summary>
        /// Creates a scope that automatically disposes services when the scope is disposed.
        /// </summary>
        /// <returns>A disposable scope</returns>
        public ServiceScope CreateScope()
        {
            return new ServiceScope(this);
        }

        /// <summary>
        /// Clears all registered services and factories.
        /// </summary>
        public void Clear()
        {
            // Dispose any disposable services
            foreach (var service in _services.Values.AsValueEnumerable())
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal exceptions during cleanup
                    }
                }
            }

            _services.Clear();
            _factories.Clear();
        }

        /// <summary>
        /// Disposes the container and all registered services.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
        }

        /// <summary>
        /// Represents a service scope that can automatically clean up services.
        /// </summary>
        public sealed class ServiceScope : IDisposable
        {
            private readonly ServiceTestContainer _container;
            private readonly List<Type> _scopedTypes = new List<Type>();
            private bool _disposed = false;

            internal ServiceScope(ServiceTestContainer container)
            {
                _container = container ?? throw new ArgumentNullException(nameof(container));
            }

            /// <summary>
            /// Registers a scoped service that will be disposed when the scope is disposed.
            /// </summary>
            /// <typeparam name="TService">The service type</typeparam>
            /// <param name="instance">The service instance</param>
            public void RegisterScoped<TService>(TService instance) where TService : class
            {
                _container.RegisterInstance(instance);
                _scopedTypes.Add(typeof(TService));
            }

            /// <summary>
            /// Resolves a service from the parent container.
            /// </summary>
            /// <typeparam name="TService">The service type</typeparam>
            /// <returns>The service instance</returns>
            public TService Resolve<TService>() where TService : class
            {
                return _container.Resolve<TService>();
            }

            /// <summary>
            /// Disposes the scope and unregisters scoped services.
            /// </summary>
            public void Dispose()
            {
                if (_disposed)
                    return;

                // Unregister scoped services
                foreach (var scopedType in _scopedTypes)
                {
                    if (_container._services.TryGetValue(scopedType, out var service))
                    {
                        if (service is IDisposable disposable)
                        {
                            try
                            {
                                disposable.Dispose();
                            }
                            catch
                            {
                                // Ignore disposal exceptions
                            }
                        }

                        _container._services.Remove(scopedType);
                    }
                }

                _scopedTypes.Clear();
                _disposed = true;
            }
        }
    }
}