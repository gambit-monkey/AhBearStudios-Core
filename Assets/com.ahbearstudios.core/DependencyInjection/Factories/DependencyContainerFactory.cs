using System;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Extensions;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Unity;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Factories
{
    /// <summary>
    /// Factory for creating dependency injection containers.
    /// Provides abstraction over different DI container implementations with proper lifecycle management.
    /// </summary>
    public static class DependencyContainerFactory
    {
        /// <summary>
        /// The current container implementation being used.
        /// </summary>
        public static ContainerImplementation CurrentImplementation { get; private set; } = ContainerImplementation.VContainer;

        /// <summary>
        /// The default message bus instance to use for containers.
        /// </summary>
        private static IMessageBus _defaultMessageBus;

        /// <summary>
        /// Gets or sets the default message bus used by containers.
        /// If not set, containers will resolve MessageBus from themselves or create a fallback.
        /// </summary>
        public static IMessageBus DefaultMessageBus
        {
            get => _defaultMessageBus;
            set => _defaultMessageBus = value;
        }

        /// <summary>
        /// Creates a new dependency container using the current implementation.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new dependency container instance.</returns>
        public static IDependencyContainer Create(string containerName = null)
        {
            return CurrentImplementation switch
            {
                ContainerImplementation.VContainer => CreateVContainer(containerName),
                _ => throw new NotSupportedException($"Container implementation '{CurrentImplementation}' is not supported")
            };
        }

        /// <summary>
        /// Creates a new VContainer-based dependency container.
        /// MessageBus will be resolved from the container or use default if available.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new VContainer-based dependency container.</returns>
        public static IDependencyContainer CreateVContainer(string containerName = null)
        {
            var builder = new ContainerBuilder();
            
            // Register default MessageBus if available
            if (_defaultMessageBus != null)
            {
                builder.RegisterInstance(_defaultMessageBus).As<IMessageBus>();
            }
            else
            {
                // Register a MessageBus that will be resolved later
                RegisterDefaultMessageBus(builder);
            }
            
            return new VContainerAdapter(builder, containerName);
        }

        /// <summary>
        /// Creates a dependency container from an existing VContainer builder.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A new dependency container wrapping the builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IDependencyContainer FromVContainerBuilder(IContainerBuilder builder, string containerName = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            
            // Ensure MessageBus is registered
            EnsureMessageBusRegistered(builder);
            
            return new VContainerAdapter(builder, containerName);
        }

        /// <summary>
        /// Creates a dependency container from an existing VContainer resolver.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBus">Optional message bus instance.</param>
        /// <returns>A new dependency container wrapping the resolver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public static IDependencyContainer FromVContainerResolver(IObjectResolver resolver, string containerName = null, IMessageBus messageBus = null)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            
            // Use provided MessageBus or try to resolve from container
            var effectiveMessageBus = messageBus ?? resolver.ResolveOrDefault<IMessageBus>(_defaultMessageBus);
            
            return new VContainerAdapter(resolver, effectiveMessageBus, containerName);
        }

        
        /// <summary>
        /// Creates a pre-configured container with common registrations.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <returns>A configured dependency container.</returns>
        public static IDependencyContainer CreateConfigured(
            string containerName = null, 
            Action<IDependencyContainer> configureContainer = null)
        {
            var container = Create(containerName);
    
            // Apply custom configuration first
            configureContainer?.Invoke(container);
    
            // Register common framework dependencies
            RegisterCommonDependencies(container);
    
            return container;
        }

        /// <summary>
        /// Creates a pre-configured container with common registrations and a specific message bus.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <param name="messageBus">The message bus to use for the container.</param>
        /// <returns>A configured dependency container.</returns>
        public static IDependencyContainer CreateConfigured(
            string containerName, 
            Action<IDependencyContainer> configureContainer,
            IMessageBus messageBus)
        {
            // Temporarily set the message bus for this container creation
            var previousMessageBus = _defaultMessageBus;
            _defaultMessageBus = messageBus;
    
            try
            {
                var container = Create(containerName);
        
                // Apply custom configuration first
                configureContainer?.Invoke(container);
        
                // Register common framework dependencies
                RegisterCommonDependencies(container);
        
                return container;
            }
            finally
            {
                // Restore the previous default message bus
                _defaultMessageBus = previousMessageBus;
            }
        }

        /// <summary>
        /// Creates a fully built and ready-to-use container.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <returns>A built and ready dependency container.</returns>
        public static IDependencyContainer CreateAndBuild(
            string containerName = null, 
            Action<IDependencyContainer> configureContainer = null)
        {
            var container = CreateConfigured(containerName, configureContainer);
            
            // Build the container if it's a VContainerAdapter
            if (container is VContainerAdapter adapter)
            {
                return adapter.Build();
            }
            
            return container;
        }

        /// <summary>
        /// Sets the default container implementation to use.
        /// </summary>
        /// <param name="implementation">The container implementation to use.</param>
        public static void SetDefaultImplementation(ContainerImplementation implementation)
        {
            CurrentImplementation = implementation;
        }

        /// <summary>
        /// Sets the default message bus instance for all containers.
        /// </summary>
        /// <param name="messageBus">The message bus instance to use as default.</param>
        public static void SetDefaultMessageBus(IMessageBus messageBus)
        {
            _defaultMessageBus = messageBus;
        }

        /// <summary>
        /// Registers common framework dependencies in the container.
        /// </summary>
        /// <param name="container">The container to register dependencies in.</param>
        private static void RegisterCommonDependencies(IDependencyContainer container)
        {
            if (container == null) return;

            try
            {
                // Register the container as IDependencyProvider (self-registration)
                if (!container.IsRegistered<IDependencyProvider>())
                {
                    container.RegisterInstance<IDependencyProvider>(container);
                }

                // Register IServiceProvider adapter for .NET compatibility
                if (!container.IsRegistered<IServiceProvider>())
                {
                    container.RegisterSingleton<IServiceProvider>(provider => 
                        new ServiceProviderAdapter(provider));
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DependencyContainerFactory] Failed to register common dependencies: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures MessageBus is registered in the builder if not already present.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private static void EnsureMessageBusRegistered(IContainerBuilder builder)
        {
            if (builder == null) return;

            try
            {
                // Check if MessageBus is already registered
                if (!builder.IsRegistered<IMessageBus>())
                {
                    RegisterDefaultMessageBus(builder);
                }
            }
            catch (Exception)
            {
                // If we can't check registration, try to register anyway
                RegisterDefaultMessageBus(builder);
            }
        }

        /// <summary>
        /// Registers a default MessageBus implementation in the builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private static void RegisterDefaultMessageBus(IContainerBuilder builder)
        {
            if (builder == null) return;

            try
            {
                if (_defaultMessageBus != null)
                {
                    builder.RegisterInstance(_defaultMessageBus).As<IMessageBus>();
                }
                else
                {
                    // Register a factory that creates MessageBus from Unity provider
                    builder.Register<IMessageBus>(resolver =>
                    {
                        try
                        {
                            // Try to get from MessageBusProvider
                            var provider = MessageBusProvider.Instance;
                            if (provider != null && provider.IsInitialized)
                            {
                                return provider.MessageBus;
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"[DependencyContainerFactory] Failed to get MessageBus from provider: {ex.Message}");
                        }

                        // Fallback: create a null message bus to avoid breaking the container
                        return new NullMessageBus();
                    }, Lifetime.Singleton);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DependencyContainerFactory] Failed to register MessageBus: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a container builder that can be configured before building.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <returns>A container builder that can be configured.</returns>
        public static IDependencyContainerBuilder CreateBuilder(string containerName = null)
        {
            return new DependencyContainerBuilder(containerName);
        }
    }

    /// <summary>
    /// Enumeration of supported container implementations.
    /// </summary>
    public enum ContainerImplementation
    {
        /// <summary>
        /// VContainer implementation (Unity-focused DI container).
        /// </summary>
        VContainer,

        /// <summary>
        /// Future support for other container implementations.
        /// </summary>
        // Zenject,
        // Microsoft,
        // Custom
    }

    /// <summary>
    /// Builder interface for creating and configuring dependency containers.
    /// </summary>
    public interface IDependencyContainerBuilder
    {
        /// <summary>
        /// Gets the name of the container being built.
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;

        /// <summary>
        /// Registers a singleton service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create the service.</param>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory);

        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterInstance<TInterface>(TInterface instance);

        /// <summary>
        /// Registers a transient service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;

        /// <summary>
        /// Registers a transient service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create services.</param>
        /// <returns>This builder for method chaining.</returns>
        IDependencyContainerBuilder RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory);

        /// <summary>
        /// Builds the dependency container.
        /// </summary>
        /// <returns>The configured dependency container.</returns>
        IDependencyContainer Build();
    }

    /// <summary>
    /// Default implementation of IDependencyContainerBuilder.
    /// </summary>
    internal sealed class DependencyContainerBuilder : IDependencyContainerBuilder
    {
        private readonly IDependencyContainer _container;

        /// <summary>
        /// Gets the name of the container being built.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Initializes a new instance of the DependencyContainerBuilder class.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        public DependencyContainerBuilder(string containerName = null)
        {
            ContainerName = containerName ?? $"Container_{Guid.NewGuid():N}";
            _container = DependencyContainerFactory.Create(ContainerName);
        }

        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _container.RegisterSingleton<TInterface, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a singleton service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create the service.</param>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterSingleton<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            _container.RegisterSingleton(factory);
            return this;
        }

        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterInstance<TInterface>(TInterface instance)
        {
            _container.RegisterInstance(instance);
            return this;
        }

        /// <summary>
        /// Registers a transient service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _container.RegisterTransient<TInterface, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a transient service with a factory.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="factory">Factory method to create services.</param>
        /// <returns>This builder for method chaining.</returns>
        public IDependencyContainerBuilder RegisterTransient<TInterface>(Func<IDependencyProvider, TInterface> factory)
        {
            _container.RegisterTransient(factory);
            return this;
        }

        /// <summary>
        /// Builds the dependency container.
        /// </summary>
        /// <returns>The configured dependency container.</returns>
        public IDependencyContainer Build()
        {
            // If using VContainer adapter, build it
            if (_container is VContainerAdapter vcontainerAdapter)
            {
                return vcontainerAdapter.Build();
            }

            return _container;
        }
    }

    /// <summary>
    /// Adapter class to bridge IDependencyProvider to IServiceProvider.
    /// Provides compatibility with .NET's standard IServiceProvider interface.
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
                return null;
            }
        }
    }

    /// <summary>
    /// Null implementation of IMessageBus for fallback scenarios.
    /// </summary>
    internal sealed class NullMessageBus : IMessageBus
    {
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() => new NullPublisher<TMessage>();
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() => new NullSubscriber<TMessage>();
        public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>() => new NullKeyedPublisher<TKey, TMessage>();
        public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>() => new NullKeyedSubscriber<TKey, TMessage>();
        public void ClearCaches() { }
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage { }
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage => new NullDisposable();
        public IDisposable SubscribeToAllMessages(Action<IMessage> handler) => new NullDisposable();
        public IMessageRegistry GetMessageRegistry() => new NullMessageRegistry();

        private class NullPublisher<T> : IMessagePublisher<T>
        {
            public void Publish(T message) { }
            public IDisposable PublishAsync(T message) => new NullDisposable();
        }

        private class NullSubscriber<T> : IMessageSubscriber<T>
        {
            public IDisposable Subscribe(Action<T> handler) => new NullDisposable();
            public IDisposable Subscribe(Action<T> handler, Func<T, bool> filter) => new NullDisposable();
        }

        private class NullKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>
        {
            public void Publish(TKey key, TMessage message) { }
            public IDisposable PublishAsync(TKey key, TMessage message) => new NullDisposable();
        }

        private class NullKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>
        {
            public IDisposable Subscribe(TKey key, Action<TMessage> handler) => new NullDisposable();
            public IDisposable Subscribe(Action<TKey, TMessage> handler) => new NullDisposable();
            public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter) => new NullDisposable();
        }

        private class NullMessageRegistry : IMessageRegistry
        {
            public void DiscoverMessages() { }
            public void RegisterMessageType(Type messageType) { }
            public void RegisterMessageType(Type messageType, ushort typeCode) { }
            public System.Collections.Generic.IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes() => 
                new System.Collections.Generic.Dictionary<Type, IMessageInfo>();
            public System.Collections.Generic.IReadOnlyList<string> GetCategories() => 
                new System.Collections.Generic.List<string>();
            public System.Collections.Generic.IReadOnlyList<Type> GetMessageTypesByCategory(string category) => 
                new System.Collections.Generic.List<Type>();
            public IMessageInfo GetMessageInfo(Type messageType) => null;
            public IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage => null;
            public bool IsRegistered(Type messageType) => false;
            public bool IsRegistered<TMessage>() where TMessage : IMessage => false;
            public ushort GetTypeCode(Type messageType) => 0;
            public ushort GetTypeCode<TMessage>() where TMessage : IMessage => 0;
            public Type GetMessageType(ushort typeCode) => null;
            public System.Collections.Generic.IReadOnlyDictionary<ushort, Type> GetAllTypeCodes() => 
                new System.Collections.Generic.Dictionary<ushort, Type>();
            public void Clear() { }
        }

        private class NullDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}