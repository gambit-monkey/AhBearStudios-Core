using System;
using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Unity;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Factories
{
    /// <summary>
    /// Factory for creating dependency injection containers.
    /// Provides abstraction over different DI container implementations.
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
        /// If not set, a new MessageBusProvider instance will be created.
        /// </summary>
        public static IMessageBus DefaultMessageBus
        {
            get => _defaultMessageBus ??= MessageBusProvider.Instance.MessageBus;
            set => _defaultMessageBus = value;
        }

        /// <summary>
        /// Creates a new dependency container using the current implementation.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        /// <returns>A new dependency container instance.</returns>
        public static IDependencyContainer Create(string containerName = null, IMessageBus messageBus = null)
        {
            return CurrentImplementation switch
            {
                ContainerImplementation.VContainer => CreateVContainer(containerName, messageBus),
                _ => throw new NotSupportedException($"Container implementation '{CurrentImplementation}' is not supported")
            };
        }

        /// <summary>
        /// Creates a new VContainer-based dependency container.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        /// <returns>A new VContainer-based dependency container.</returns>
        public static IDependencyContainer CreateVContainer(string containerName = null, IMessageBus messageBus = null)
        {
            var builder = new ContainerBuilder();
            return new VContainerAdapter(builder, messageBus ?? DefaultMessageBus, containerName);
        }

        /// <summary>
        /// Creates a dependency container from an existing VContainer builder.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        /// <returns>A new dependency container wrapping the builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IDependencyContainer FromVContainerBuilder(IContainerBuilder builder, string containerName = null, IMessageBus messageBus = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            
            return new VContainerAdapter(builder, messageBus ?? DefaultMessageBus, containerName);
        }

        /// <summary>
        /// Creates a dependency container from an existing VContainer resolver.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        /// <returns>A new dependency container wrapping the resolver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public static IDependencyContainer FromVContainerResolver(IObjectResolver resolver, string containerName = null, IMessageBus messageBus = null)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            
            return new VContainerAdapter(resolver, messageBus ?? DefaultMessageBus, containerName);
        }

        /// <summary>
        /// Creates a pre-configured container with common registrations.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configureContainer">Optional action to configure the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        /// <returns>A configured dependency container.</returns>
        public static IDependencyContainer CreateConfigured(
            string containerName = null, 
            Action<IDependencyContainer> configureContainer = null,
            IMessageBus messageBus = null)
        {
            var container = Create(containerName, messageBus);
            
            // Register common framework dependencies
            RegisterCommonDependencies(container);
            
            // Apply custom configuration
            configureContainer?.Invoke(container);
            
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
            DefaultMessageBus = messageBus;
        }

        /// <summary>
        /// Registers common framework dependencies in the container.
        /// </summary>
        /// <param name="container">The container to register dependencies in.</param>
        private static void RegisterCommonDependencies(IDependencyContainer container)
        {
            // Register the message bus instance if not already registered
            if (!container.IsRegistered<IMessageBus>())
            {
                container.RegisterInstance<IMessageBus>(container.MessageBus);
            }

            // Register the container as IDependencyProvider (self-registration)
            // Note: This will be available after the container is built
        }

        /// <summary>
        /// Creates a container builder that can be configured before building.
        /// </summary>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        /// <returns>A container builder that can be configured.</returns>
        public static IDependencyContainerBuilder CreateBuilder(string containerName = null, IMessageBus messageBus = null)
        {
            return new DependencyContainerBuilder(containerName, messageBus);
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
        /// Gets the message bus being used by the container.
        /// </summary>
        IMessageBus MessageBus { get; }

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
        /// Gets the message bus being used by the container.
        /// </summary>
        public IMessageBus MessageBus { get; }

        /// <summary>
        /// Initializes a new instance of the DependencyContainerBuilder class.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="messageBus">Optional message bus instance. If null, uses DefaultMessageBus.</param>
        public DependencyContainerBuilder(string containerName = null, IMessageBus messageBus = null)
        {
            ContainerName = containerName ?? $"Container_{Guid.NewGuid():N}";
            MessageBus = messageBus ?? DependencyContainerFactory.DefaultMessageBus;
            _container = DependencyContainerFactory.Create(ContainerName, MessageBus);
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
}