using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Adapters.VContainer;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Bridges.VContainer
{
    /// <summary>
    /// High-level bridge that wraps VContainerAdapter to implement IDependencyContainer.
    /// Provides the complete IDependencyContainer interface while delegating to VContainerAdapter.
    /// This enables full compatibility with our DI abstraction layer.
    /// </summary>
    public sealed class VContainerContainerBridge : IDependencyContainer
    {
        private readonly VContainerAdapter _adapter;
        private readonly IServiceResolver _resolver;
        private bool _disposed;

        /// <summary>
        /// Gets whether this container has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed || _adapter.IsDisposed;

        /// <summary>
        /// Gets the name or identifier of this container instance.
        /// </summary>
        public string ContainerName => _adapter.ContainerName;

        /// <summary>
        /// Gets the framework this container uses.
        /// </summary>
        public ContainerFramework Framework => _adapter.Framework;

        /// <summary>
        /// Gets the configuration used by this container.
        /// </summary>
        public IDependencyInjectionConfig Configuration => _adapter.Configuration;

        /// <summary>
        /// Gets the message bus used for publishing container events.
        /// </summary>
        public IMessageBusService MessageBusService => _adapter.MessageBusService;

        /// <summary>
        /// Gets whether the container has been built and is ready for resolution.
        /// </summary>
        public bool IsBuilt => _adapter.IsBuilt;

        /// <summary>
        /// Initializes a new VContainerContainerBridge.
        /// </summary>
        /// <param name="adapter">The VContainer adapter to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when adapter is null.</exception>
        public VContainerContainerBridge(VContainerAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        /// <summary>
        /// Creates a new VContainerContainerBridge with a fresh VContainer builder.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configuration">Optional configuration for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <param name="parentResolver">Optional parent resolver for hierarchical containers.</param>
        /// <returns>A new VContainerContainerBridge instance.</returns>
        public static VContainerContainerBridge Create(
            string containerName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null,
            IObjectResolver parentResolver = null)
        {
            var builder = parentResolver != null
                ? new ContainerBuilder(parentResolver)
                : new ContainerBuilder();

            var adapter = new VContainerAdapter(
                builder,
                containerName,
                configuration,
                messageBusService);

            return new VContainerContainerBridge(adapter);
        }

        // Core registration methods - delegate to adapter

        /// <summary>
        /// Registers a singleton instance of the specified type.
        /// </summary>
        public IDependencyContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _adapter.RegisterSingleton<TInterface, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a singleton instance with a factory method.
        /// </summary>
        public IDependencyContainer RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            // Convert IDependencyProvider factory to IServiceResolver factory
            _adapter.RegisterSingleton<TInterface>(resolver =>
            {
                // Create a dependency provider adapter from the service resolver
                var dependencyProvider = new ServiceResolverToDependencyProviderAdapter(resolver);
                return factory(dependencyProvider);
            });
            return this;
        }

        /// <summary>
        /// Registers a singleton instance directly.
        /// </summary>
        public IDependencyContainer RegisterInstance<TInterface>(TInterface instance)
        {
            _adapter.RegisterInstance<TInterface>(instance);
            return this;
        }

        /// <summary>
        /// Registers a transient type (new instance created each time).
        /// </summary>
        public IDependencyContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _adapter.RegisterTransient<TInterface, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a transient type with a factory method.
        /// </summary>
        public IDependencyContainer RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            // Convert IDependencyProvider factory to IServiceResolver factory
            _adapter.RegisterTransient<TInterface>(resolver =>
            {
                // Create a dependency provider adapter from the service resolver
                var dependencyProvider = new ServiceResolverToDependencyProviderAdapter(resolver);
                return factory(dependencyProvider);
            });
            return this;
        }

        // Enhanced registration methods - VContainer-specific implementations

        /// <summary>
        /// Registers a factory function for creating instances on demand.
        /// </summary>
        public IDependencyContainer RegisterFactory<T>()
        {
            // Register Func<T> as a factory
            _adapter.RegisterSingleton<Func<T>>(resolver => () => resolver.Resolve<T>());
            return this;
        }

        /// <summary>
        /// Registers a lazy wrapper for deferred initialization.
        /// </summary>
        public IDependencyContainer RegisterLazy<T>()
        {
            // Register Lazy<T> for deferred initialization
            _adapter.RegisterSingleton<Lazy<T>>(resolver => new Lazy<T>(() => resolver.Resolve<T>()));
            return this;
        }

        /// <summary>
        /// Registers multiple implementations as a collection.
        /// </summary>
        public IDependencyContainer RegisterCollection<TInterface>(params Type[] implementations)
        {
            if (implementations == null || implementations.Length == 0)
                return this;

            // Register each implementation individually
            foreach (var implementation in implementations)
            {
                // Use reflection to call RegisterTransient with the specific types
                var method = typeof(VContainerAdapter).GetMethod(nameof(_adapter.RegisterTransient));
                var genericMethod = method.MakeGenericMethod(typeof(TInterface), implementation);
                genericMethod.Invoke(_adapter, null);
            }

            return this;
        }

        /// <summary>
        /// Registers a decorator that wraps an existing service.
        /// </summary>
        public IDependencyContainer RegisterDecorator<TService, TDecorator>() where TDecorator : class, TService
        {
            // VContainer decorator pattern - register the decorator with dependency on the original
            _adapter.RegisterSingleton<TService, TDecorator>();
            return this;
        }

        // Core resolution and query methods - delegate to resolver

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        public T Resolve<T>()
        {
            EnsureBuilt();
            return _resolver.Resolve<T>();
        }

        /// <summary>
        /// Checks if a type is registered in this container.
        /// </summary>
        public bool IsRegistered<T>()
        {
            return _adapter.IsRegistered<T>();
        }

        /// <summary>
        /// Checks if a type is registered in this container.
        /// </summary>
        public bool IsRegistered(Type type)
        {
            return _adapter.IsRegistered(type);
        }

        /// <summary>
        /// Attempts to resolve a service, returning false if not found.
        /// </summary>
        public bool TryResolve<T>(out T service)
        {
            if (!IsBuilt)
            {
                service = default;
                return false;
            }

            return _resolver.TryResolve<T>(out service);
        }

        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            if (!IsBuilt)
                return defaultValue;

            return _resolver.ResolveOrDefault<T>(defaultValue);
        }

        // Enhanced resolution methods

        /// <summary>
        /// Resolves all registered implementations of the specified type.
        /// </summary>
        public IEnumerable<T> ResolveAll<T>()
        {
            EnsureBuilt();
            return _resolver.ResolveAll<T>();
        }

        /// <summary>
        /// Resolves a service by name identifier (if named services are enabled).
        /// </summary>
        public T ResolveNamed<T>(string name)
        {
            EnsureBuilt();
            return _resolver.ResolveNamed<T>(name);
        }

        /// <summary>
        /// Attempts to resolve a named service.
        /// </summary>
        public bool TryResolveNamed<T>(string name, out T service)
        {
            if (!IsBuilt)
            {
                service = default;
                return false;
            }

            return _resolver.TryResolveNamed<T>(name, out service);
        }

        // Container management

        /// <summary>
        /// Builds the container and makes it ready for resolution.
        /// </summary>
        public IDependencyContainer Build()
        {
            if (!IsBuilt)
            {
                var resolver = _adapter.Build();
                // Store the resolver for direct access
                _resolver = resolver;
            }

            return this;
        }

        /// <summary>
        /// Creates a child container that inherits registrations from this container.
        /// </summary>
        public IDependencyContainer CreateChildContainer(string childName = null)
        {
            var childAdapter = (VContainerAdapter)_adapter.CreateChild(childName);
            return new VContainerContainerBridge(childAdapter);
        }

        /// <summary>
        /// Creates a scoped container for temporary dependency overrides.
        /// </summary>
        public IDependencyContainer CreateScope(string scopeName = null)
        {
            EnsureBuilt();
            var scopedResolver = _resolver.CreateScope(scopeName);

            // Create a new bridge that wraps a scoped adapter
            // This is a simplified approach - in practice, you might want a more sophisticated scoping mechanism
            throw new NotSupportedException(
                "CreateScope is not directly supported through the bridge pattern. " +
                "Use CreateChildContainer for similar hierarchical dependency scenarios.");
        }

        /// <summary>
        /// Validates that all registered types can be resolved without circular dependencies.
        /// </summary>
        public IContainerValidationResult ValidateRegistrations()
        {
            return _adapter.Validate();
        }

        /// <summary>
        /// Gets performance metrics for this container (if metrics are enabled).
        /// </summary>
        public IContainerMetrics GetMetrics()
        {
            return _adapter.GetMetrics();
        }

        /// <summary>
        /// Gets the underlying container adapter.
        /// </summary>
        public IContainerAdapter GetAdapter()
        {
            return _adapter;
        }

        /// <summary>
        /// Disposes the container and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _resolver?.Dispose();
                _adapter?.Dispose();
                _disposed = true;
            }
            catch (Exception)
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Ensures the container is built before resolution operations.
        /// </summary>
        private void EnsureBuilt()
        {
            if (!IsBuilt)
            {
                Build();
            }
        }
    }
}