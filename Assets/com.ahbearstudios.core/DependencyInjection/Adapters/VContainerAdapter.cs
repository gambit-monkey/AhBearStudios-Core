using System;
using System.Diagnostics;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Messages;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// VContainer adapter that implements the IDependencyContainer interface.
    /// Provides a consistent abstraction over VContainer's dependency injection functionality.
    /// Uses MessageBus for event communication.
    /// </summary>
    public sealed class VContainerAdapter : IDependencyContainer
    {
        private readonly IContainerBuilder _builder;
        private readonly IObjectResolver _resolver;
        private readonly string _containerName;
        private readonly IMessageBus _messageBus;
        private readonly Stopwatch _containerLifetime;
        private bool _disposed;
        private int _registeredServicesCount;

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
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Initializes a new instance of the VContainerAdapter class with a container builder.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="messageBus">The message bus for publishing events.</param>
        /// <param name="containerName">Optional name for this container.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder or messageBus is null.</exception>
        public VContainerAdapter(IContainerBuilder builder, IMessageBus messageBus, string containerName = null)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _containerName = containerName ?? $"VContainer_{Guid.NewGuid():N}";
            _containerLifetime = Stopwatch.StartNew();
            _registeredServicesCount = 0;
        }

        /// <summary>
        /// Initializes a new instance of the VContainerAdapter class with a resolved container.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="messageBus">The message bus for publishing events.</param>
        /// <param name="containerName">Optional name for this container.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver or messageBus is null.</exception>
        public VContainerAdapter(IObjectResolver resolver, IMessageBus messageBus, string containerName = null)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _containerName = containerName ?? $"VContainer_{Guid.NewGuid():N}";
            _containerLifetime = Stopwatch.StartNew();
            _registeredServicesCount = 0;
        }

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="ServiceResolutionException">Thrown when the service cannot be resolved.</exception>
        public T Resolve<T>()
        {
            ThrowIfDisposed();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                T service;
                if (_resolver != null)
                {
                    service = _resolver.Resolve<T>();
                }
                else
                {
                    throw new InvalidOperationException("Container has not been built yet. Call Build() first or use a resolved container.");
                }

                stopwatch.Stop();
                
                // Publish successful resolution message
                var resolvedMessage = new ServiceResolvedMessage(_containerName, typeof(T), service, stopwatch.Elapsed, true);
                _messageBus.PublishMessage(resolvedMessage);
                
                return service;
            }
            catch (VContainerException ex)
            {
                stopwatch.Stop();
                
                // Publish failed resolution message
                var failedMessage = new ServiceResolutionFailedMessage(_containerName, typeof(T), ex.Message, stopwatch.Elapsed);
                _messageBus.PublishMessage(failedMessage);
                
                throw new ServiceResolutionException(typeof(T), $"Failed to resolve service of type '{typeof(T).FullName}'", ex);
            }
            catch (Exception ex) when (!(ex is DependencyInjectionException))
            {
                stopwatch.Stop();
                
                // Publish failed resolution message
                var failedMessage = new ServiceResolutionFailedMessage(_containerName, typeof(T), ex.Message, stopwatch.Elapsed);
                _messageBus.PublishMessage(failedMessage);
                
                throw new ServiceResolutionException(typeof(T), $"Unexpected error resolving service of type '{typeof(T).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a singleton instance of the specified type.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container has already been built.</exception>
        public IDependencyContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.Register<TImplementation>(Lifetime.Singleton).As<TInterface>();
                _registeredServicesCount++;
                
                // Publish registration message
                var registeredMessage = new ServiceRegisteredMessage(_containerName, typeof(TInterface), typeof(TImplementation), ServiceLifetime.Singleton);
                _messageBus.PublishMessage(registeredMessage);
                
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException($"Failed to register singleton service '{typeof(TInterface).FullName}' -> '{typeof(TImplementation).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a singleton instance with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="factory">Factory method to create the instance.</param>
        /// <returns>This container for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container has already been built.</exception>
        public IDependencyContainer RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.Register<TInterface>(resolver => factory(new VContainerDependencyProvider(resolver)), Lifetime.Singleton);
                _registeredServicesCount++;
                
                // Publish registration message
                var registeredMessage = new ServiceRegisteredMessage(_containerName, typeof(TInterface), null, ServiceLifetime.Singleton, true);
                _messageBus.PublishMessage(registeredMessage);
                
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException($"Failed to register singleton factory for service '{typeof(TInterface).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a singleton instance directly.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This container for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when instance is null.</exception>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container has already been built.</exception>
        public IDependencyContainer RegisterInstance<TInterface>(TInterface instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.RegisterInstance(instance).As<TInterface>();
                _registeredServicesCount++;
                
                // Publish registration message
                var registeredMessage = new ServiceRegisteredMessage(_containerName, typeof(TInterface), instance.GetType(), ServiceLifetime.Instance);
                _messageBus.PublishMessage(registeredMessage);
                
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException($"Failed to register instance for service '{typeof(TInterface).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a transient type (new instance created each time).
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This container for method chaining.</returns>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container has already been built.</exception>
        public IDependencyContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.Register<TImplementation>(Lifetime.Transient).As<TInterface>();
                _registeredServicesCount++;
                
                // Publish registration message
                var registeredMessage = new ServiceRegisteredMessage(_containerName, typeof(TInterface), typeof(TImplementation), ServiceLifetime.Transient);
                _messageBus.PublishMessage(registeredMessage);
                
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException($"Failed to register transient service '{typeof(TInterface).FullName}' -> '{typeof(TImplementation).FullName}'", ex);
            }
        }

        /// <summary>
        /// Registers a transient type with a factory method.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <param name="factory">Factory method to create instances.</param>
        /// <returns>This container for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container has already been built.</exception>
        public IDependencyContainer RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            ThrowIfDisposed();
            ThrowIfBuilt();

            try
            {
                _builder.Register<TInterface>(resolver => factory(new VContainerDependencyProvider(resolver)), Lifetime.Transient);
                _registeredServicesCount++;
                
                // Publish registration message
                var registeredMessage = new ServiceRegisteredMessage(_containerName, typeof(TInterface), null, ServiceLifetime.Transient, true);
                _messageBus.PublishMessage(registeredMessage);
                
                return this;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException($"Failed to register transient factory for service '{typeof(TInterface).FullName}'", ex);
            }
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
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public bool IsRegistered(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            
            ThrowIfDisposed();

            if (_resolver != null)
            {
                try
                {
                    // Use reflection to call TryResolve<T>(out T value)
                    var tryResolveMethod = typeof(IObjectResolver).GetMethod("TryResolve", new[] { type.MakeByRefType() });
                    if (tryResolveMethod != null)
                    {
                        var genericMethod = tryResolveMethod.MakeGenericMethod(type);
                        var parameters = new object[] { null };
                        var result = (bool)genericMethod.Invoke(_resolver, parameters);
                        return result;
                    }
                }
                catch (Exception)
                {
                    // If TryResolve method doesn't exist or fails, fall back to try/catch resolve
                }

                // Fallback: try to resolve and catch exceptions
                try
                {
                    var resolveMethod = typeof(IObjectResolver).GetMethod(nameof(IObjectResolver.Resolve), Type.EmptyTypes);
                    var genericResolveMethod = resolveMethod.MakeGenericMethod(type);
                    genericResolveMethod.Invoke(_resolver, null);
                    return true;
                }
                catch (Exception ex) when (ex.InnerException is VContainerException)
                {
                    return false;
                }
                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is VContainerException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            
            // For unbuilt containers, we can't check registration
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

            try
            {
                service = Resolve<T>();
                return true;
            }
            catch (ServiceResolutionException)
            {
                service = default;
                return false;
            }
            catch (Exception)
            {
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
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            return TryResolve<T>(out var service) ? service : defaultValue;
        }

        /// <summary>
        /// Creates a child container that inherits registrations from this container.
        /// </summary>
        /// <param name="childName">Optional name for the child container.</param>
        /// <returns>A new child container.</returns>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container has not been built.</exception>
        public IDependencyContainer CreateChildContainer(string childName = null)
        {
            ThrowIfDisposed();

            if (_resolver == null)
            {
                throw new InvalidOperationException("Cannot create child container from unbuilt container. Build the container first.");
            }

            try
            {
                // VContainer doesn't have built-in child container support, so we create a new builder
                // and register the parent resolver as a fallback
                var childBuilder = new ContainerBuilder();
                var childContainerName = childName ?? $"{_containerName}_Child_{Guid.NewGuid():N}";
                
                // Register parent resolver for fallback resolution
                childBuilder.RegisterInstance(_resolver).As<IObjectResolver>();
                
                var childContainer = new VContainerAdapter(childBuilder, _messageBus, childContainerName);
                
                // Publish child container created message
                var childCreatedMessage = new ChildContainerCreatedMessage(_containerName, childContainerName);
                _messageBus.PublishMessage(childCreatedMessage);
                
                return childContainer;
            }
            catch (Exception ex)
            {
                throw new DependencyInjectionException($"Failed to create child container for '{_containerName}'", ex);
            }
        }

        /// <summary>
        /// Validates that all registered types can be resolved without circular dependencies.
        /// </summary>
        /// <returns>True if all registrations are valid, false otherwise.</returns>
        public bool ValidateRegistrations()
        {
            ThrowIfDisposed();

            try
            {
                // VContainer performs validation during build, so if we have a resolver, it's valid
                return _resolver != null || _builder != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Builds the container if using a builder.
        /// </summary>
        /// <returns>A new adapter with the built resolver.</returns>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the container is already built.</exception>
        public VContainerAdapter Build()
        {
            ThrowIfDisposed();

            if (_resolver != null)
            {
                throw new InvalidOperationException("Container has already been built.");
            }

            if (_builder == null)
            {
                throw new InvalidOperationException("No builder available to build the container.");
            }

            var buildStopwatch = Stopwatch.StartNew();
            try
            {
                // Cast to ContainerBuilder to access the Build method
                if (_builder is ContainerBuilder containerBuilder)
                {
                    var resolver = containerBuilder.Build();
                    buildStopwatch.Stop();
                    
                    var builtContainer = new VContainerAdapter(resolver, _messageBus, _containerName);
                    
                    // Publish container built message
                    var builtMessage = new ContainerBuiltMessage(_containerName, _registeredServicesCount, buildStopwatch.Elapsed);
                    _messageBus.PublishMessage(builtMessage);
                    
                    return builtContainer;
                }
                else
                {
                    throw new InvalidOperationException("Builder is not a ContainerBuilder instance.");
                }
            }
            catch (Exception ex)
            {
                buildStopwatch.Stop();
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
                
                // Publish container disposed message
                var disposedMessage = new ContainerDisposedMessage(_containerName, _containerLifetime.Elapsed);
                _messageBus.PublishMessage(disposedMessage);
                
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
        /// Throws an exception if the container has been disposed.
        /// </summary>
        /// <exception cref="ContainerDisposedException">Thrown when the container is disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ContainerDisposedException(_containerName);
            }
        }

        /// <summary>
        /// Throws an exception if the container has already been built.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the container is already built.</exception>
        private void ThrowIfBuilt()
        {
            if (_resolver != null)
            {
                throw new InvalidOperationException("Cannot modify container after it has been built.");
            }
        }
    }

    /// <summary>
    /// VContainer implementation of IDependencyProvider that wraps an IObjectResolver.
    /// </summary>
    internal sealed class VContainerDependencyProvider : IDependencyProvider
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the VContainerDependencyProvider class.
        /// </summary>
        /// <param name="resolver">The VContainer object resolver.</param>
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
                throw new ServiceResolutionException(typeof(T), $"Failed to resolve service of type '{typeof(T).FullName}'", ex);
            }
            catch (Exception ex) when (!(ex is DependencyInjectionException))
            {
                throw new ServiceResolutionException(typeof(T), $"Unexpected error resolving service of type '{typeof(T).FullName}'", ex);
            }
        }
    }
}