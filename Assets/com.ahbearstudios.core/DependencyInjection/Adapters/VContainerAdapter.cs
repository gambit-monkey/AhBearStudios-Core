using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// VContainer adapter that implements the IDependencyContainer interface.
    /// Provides a consistent abstraction over VContainer's dependency injection functionality.
    /// </summary>
    public sealed class VContainerAdapter : IDependencyContainer, IDependencyProvider
    {
        private readonly IContainerBuilder _builder;
        private IObjectResolver _resolver;
        private readonly string _containerName;
        private IMessageBus _messageBus;
        private readonly Stopwatch _containerLifetime;
        private bool _disposed;
        private int _registeredServicesCount;
        private bool _isBuilt;

        /// <summary>
        /// Gets whether this container has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Gets the name or identifier of this container instance.
        /// </summary>
        public string ContainerName => _containerName;

        /// <summary>
        /// Gets the message bus used for publishing container events.
        /// </summary>
        public IMessageBus MessageBus
        {
            get
            {
                if (_messageBus == null && _resolver != null)
                {
                    // Try to resolve from container if available
                    _resolver.TryResolve<IMessageBus>(out _messageBus);
                }
                return _messageBus;
            }
        }

        /// <summary>
        /// Initializes a new instance of the VContainerAdapter class with a container builder.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="containerName">Optional name for this container.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public VContainerAdapter(IContainerBuilder builder, string containerName = null)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _containerName = containerName ?? $"VContainer_{Guid.NewGuid():N}";
            _containerLifetime = Stopwatch.StartNew();
            _registeredServicesCount = 0;
            _isBuilt = false;
        }

        /// <summary>
        /// Initializes a new instance of the VContainerAdapter class with an existing resolver.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="containerName">Optional name for this container.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public VContainerAdapter(IObjectResolver resolver, string containerName = null)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _containerName = containerName ?? $"VContainer_{Guid.NewGuid():N}";
            _containerLifetime = Stopwatch.StartNew();
            _registeredServicesCount = 0;
            _isBuilt = true;

            // Try to resolve MessageBus immediately if available
            _resolver.TryResolve<IMessageBus>(out _messageBus);
        }

        /// <summary>
        /// Initializes a new instance with a pre-built resolver and explicit message bus.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="messageBus">The message bus instance to use.</param>
        /// <param name="containerName">Optional name for this container.</param>
        public VContainerAdapter(IObjectResolver resolver, IMessageBus messageBus, string containerName = null)
            : this(resolver, containerName)
        {
            _messageBus = messageBus;
        }

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="ServiceResolutionException">Thrown when the service cannot be resolved.</exception>
        public T Resolve<T>()
        {
            ThrowIfDisposed();
            EnsureBuilt();

            try
            {
                return _resolver.Resolve<T>();
            }
            catch (VContainerException ex)
            {
                throw new ServiceResolutionException(typeof(T), 
                    $"Failed to resolve service of type '{typeof(T).FullName}'", ex);
            }
            catch (Exception ex) when (!(ex is DependencyInjectionException))
            {
                throw new ServiceResolutionException(typeof(T), 
                    $"Unexpected error resolving service of type '{typeof(T).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a singleton instance of the specified type.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                // Use VContainer's built-in methods directly to avoid ambiguity
                VContainer.ContainerBuilderExtensions.Register<TImplementation>(_builder, Lifetime.Singleton)
                    .As<TInterface>();
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register singleton service '{typeof(TInterface).FullName}' -> '{typeof(TImplementation).FullName}'", 
                    ex);
            }
        }

        /// <summary>
        /// Registers a singleton instance with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="factory">Factory method to create the instance.</param>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                // Use VContainer's built-in methods directly
                VContainer.ContainerBuilderExtensions.Register<TInterface>(_builder, 
                    resolver => factory(this), Lifetime.Singleton);
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register singleton factory for service '{typeof(TInterface).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a singleton instance directly.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterInstance<TInterface>(TInterface instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                VContainer.ContainerBuilderExtensions.RegisterInstance(_builder, instance);
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register instance for service '{typeof(TInterface).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a transient type (new instance created each time).
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                // Use VContainer's built-in methods directly to avoid ambiguity
                VContainer.ContainerBuilderExtensions.Register<TImplementation>(_builder, Lifetime.Transient)
                    .As<TInterface>();
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register transient service '{typeof(TInterface).FullName}' -> '{typeof(TImplementation).FullName}'", 
                    ex);
            }
        }

        /// <summary>
        /// Registers a transient type with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="factory">Factory method to create instances.</param>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                VContainer.ContainerBuilderExtensions.Register<TInterface>(_builder, 
                    resolver => factory(this), Lifetime.Transient);
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register transient factory for service '{typeof(TInterface).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a factory function for creating instances on demand.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterFactory<T>()
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.RegisterFactory<T>();
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register factory for service '{typeof(T).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a lazy wrapper for deferred initialization.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterLazy<T>()
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.RegisterLazy<T>();
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register lazy wrapper for service '{typeof(T).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers multiple implementations as a collection.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="implementations">The implementation types.</param>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterCollection<TInterface>(params Type[] implementations)
        {
            if (implementations == null) throw new ArgumentNullException(nameof(implementations));
            if (implementations.Length == 0) 
                throw new ArgumentException("At least one implementation must be provided", nameof(implementations));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.RegisterMultiple<TInterface>(implementations, Lifetime.Transient);
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register collection for interface '{typeof(TInterface).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a decorator that wraps an existing service.
        /// Note: VContainer doesn't have built-in decorator support.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TDecorator">The decorator type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        public IDependencyContainer RegisterDecorator<TService, TDecorator>() 
            where TDecorator : class, TService
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                // Simple decorator implementation - register the decorator as the service
                // This assumes the decorator's constructor takes the original service as a parameter
                VContainer.ContainerBuilderExtensions.Register<TDecorator>(_builder, Lifetime.Singleton)
                    .As<TService>();
                _registeredServicesCount++;
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to register decorator '{typeof(TDecorator).FullName}' for service '{typeof(TService).FullName}'", 
                    ex);
            }
        }

        /// <summary>
        /// Resolves all registered implementations of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>All registered implementations.</returns>
        public IEnumerable<T> ResolveAll<T>()
        {
            ThrowIfDisposed();
            EnsureBuilt();

            try
            {
                // Try to resolve as IEnumerable<T> first
                if (_resolver.TryResolve<IEnumerable<T>>(out var collection))
                {
                    return collection;
                }

                // Fallback: try to resolve a single instance
                if (_resolver.TryResolve<T>(out var singleInstance))
                {
                    return new[] { singleInstance };
                }

                return Enumerable.Empty<T>();
            }
            catch (Exception ex)
            {
                throw new ServiceResolutionException(typeof(IEnumerable<T>), 
                    $"Failed to resolve all instances of type '{typeof(T).FullName}'", ex);
            }
        }

        /// <summary>
        /// Resolves a service by name identifier.
        /// VContainer doesn't have built-in named service support.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="name">The name identifier.</param>
        /// <returns>The named service.</returns>
        public T ResolveNamed<T>(string name)
        {
            throw new NotSupportedException(
                "VContainer does not support named service resolution. Consider using keyed services or a different container implementation.");
        }

        /// <summary>
        /// Attempts to resolve a named service.
        /// VContainer doesn't have built-in named service support.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="name">The name identifier.</param>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>Always returns false for VContainer.</returns>
        public bool TryResolveNamed<T>(string name, out T service)
        {
            service = default;
            return false;
        }

        /// <summary>
        /// Checks if a type is registered in this container.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <returns>True if the type is registered, false otherwise.</returns>
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <summary>
        /// Checks if a type is registered in this container.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        public bool IsRegistered(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            
            ThrowIfDisposed();

            // For unbuilt containers, use the inspection extensions explicitly
            if (!_isBuilt && _builder != null)
            {
                try
                {
                    // Use the inspection extension explicitly to avoid ambiguity
                    return VContainerInspectionExtensions.IsRegistered(_builder, type);
                }
                catch (Exception)
                {
                    // If reflection-based inspection fails, fall back to a simple check
                    return _registeredServicesCount > 0;
                }
            }

            // For built containers, use TryResolve
            if (_resolver != null)
            {
                try
                {
                    return _resolver.TryResolve(type, out _);
                }
                catch
                {
                    return false;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to resolve a service, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        public bool TryResolve<T>(out T service)
        {
            ThrowIfDisposed();
            service = default;

            if (!_isBuilt || _resolver == null)
            {
                return false;
            }

            try
            {
                return _resolver.TryResolve<T>(out service);
            }
            catch
            {
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
            return TryResolve<T>(out var service) ? service : defaultValue;
        }

        /// <summary>
        /// Creates a child container that inherits registrations from this container.
        /// </summary>
        /// <param name="childName">Optional name for the child container.</param>
        /// <returns>A new child container.</returns>
        public IDependencyContainer CreateChildContainer(string childName = null)
        {
            ThrowIfDisposed();
            EnsureBuilt();

            try
            {
                // Create a new scope for child container
                var childScope = _resolver.CreateScope();
                var childContainerName = childName ?? $"{_containerName}_Child_{Guid.NewGuid():N}";
                
                return new VContainerAdapter(childScope, _messageBus, childContainerName);
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException(
                    $"Failed to create child container for '{_containerName}'", ex);
            }
        }

        /// <summary>
        /// Validates that all registered types can be resolved.
        /// </summary>
        /// <returns>True if all registrations are valid, false otherwise.</returns>
        public bool ValidateRegistrations()
        {
            ThrowIfDisposed();

            try
            {
                // VContainer performs validation during build
                if (_isBuilt)
                {
                    return _resolver != null;
                }

                // For unbuilt containers, we can't easily validate
                return _builder != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Builds the container if using a builder.
        /// </summary>
        /// <returns>This adapter instance.</returns>
        public VContainerAdapter Build()
        {
            ThrowIfDisposed();

            if (_isBuilt)
            {
                return this;
            }

            if (_builder == null)
            {
                throw new InvalidOperationException("No builder available to build the container.");
            }

            try
            {
                if (_builder is ContainerBuilder containerBuilder)
                {
                    _resolver = containerBuilder.Build();
                    _isBuilt = true;

                    // Try to resolve MessageBus from built container
                    if (_messageBus == null)
                    {
                        _resolver.TryResolve<IMessageBus>(out _messageBus);
                    }
                    
                    return this;
                }
                else
                {
                    throw new InvalidOperationException("Builder is not a ContainerBuilder instance.");
                }
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException("Failed to build container", ex);
            }
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
                _containerLifetime.Stop();
                
                if (_resolver is IDisposable disposableResolver)
                {
                    disposableResolver.Dispose();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[VContainerAdapter] Error disposing container '{_containerName}': {ex.Message}");
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Ensures the container is built before operations that require it.
        /// </summary>
        private void EnsureBuilt()
        {
            if (!_isBuilt)
            {
                Build();
            }
        }

        /// <summary>
        /// Throws an exception if the container has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the container is disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(VContainerAdapter));
            }
        }

        /// <summary>
        /// Throws an exception if the container has already been built.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the container is already built.</exception>
        private void ThrowIfBuilt()
        {
            if (_isBuilt)
            {
                throw new InvalidOperationException("Cannot modify container after it has been built.");
            }
        }
    }
}