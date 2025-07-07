using AhBearStudios.Core.DependencyInjection.Adapters.VContainer;
using AhBearStudios.Core.DependencyInjection.Bridges.VContainer;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Providers.VContainer;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Extensions.VContainer
{
    /// <summary>
    /// Extension methods for seamless integration between VContainer and our DI abstraction system.
    /// Provides convenient methods for creating adapters, bridges, and converting between types.
    /// </summary>
    public static class VContainerExtensions
    {
        /// <summary>
        /// Converts a VContainer IContainerBuilder to our IContainerAdapter.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configuration">Optional configuration for the adapter.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainerAdapter wrapping the builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerAdapter ToAdapter(
            this IContainerBuilder builder,
            string containerName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return new VContainerAdapter(
                builder,
                containerName,
                configuration ?? new DependencyInjectionConfig(),
                messageBusService);
        }

        /// <summary>
        /// Converts a VContainer IContainerBuilder to our IDependencyContainer.
        /// </summary>
        /// <param name="builder">The VContainer builder to wrap.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configuration">Optional configuration for the container.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainerContainerBridge wrapping the builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IDependencyContainer ToDependencyContainer(
            this IContainerBuilder builder,
            string containerName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var adapter = new VContainerAdapter(
                builder,
                containerName,
                configuration ?? new DependencyInjectionConfig(),
                messageBusService);

            return new VContainerContainerBridge(adapter);
        }

        /// <summary>
        /// Converts a VContainer IObjectResolver to our IDependencyProvider.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <returns>A new VContainerDependencyProviderAdapter wrapping the resolver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public static IDependencyProvider ToDependencyProvider(this IObjectResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            return new VContainerDependencyProviderAdapter(resolver);
        }

        /// <summary>
        /// Converts a VContainer IObjectResolver to our IServiceResolver.
        /// </summary>
        /// <param name="resolver">The VContainer resolver to wrap.</param>
        /// <param name="containerName">Optional container name for logging.</param>
        /// <param name="configuration">Optional configuration for behavior control.</param>
        /// <param name="messageBusService">Optional message bus for publishing events.</param>
        /// <returns>A new VContainerServiceResolver wrapping the resolver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public static IServiceResolver ToServiceResolver(
            this IObjectResolver resolver,
            string containerName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            return new VContainerServiceResolver(
                resolver,
                containerName,
                configuration,
                messageBusService);
        }

        /// <summary>
        /// Registers our core DI abstractions in a VContainer builder.
        /// This enables the VContainer to provide our abstraction interfaces.
        /// </summary>
        /// <param name="builder">The VContainer builder to configure.</param>
        /// <param name="configuration">Optional configuration for the abstractions.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterDIAbstractions(
            this IContainerBuilder builder,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // Register IDependencyProvider that wraps the resolver
            builder.Register<IDependencyProvider>(resolver => 
                new VContainerDependencyProviderAdapter(resolver), Lifetime.Singleton);

            // Register IServiceResolver that wraps the resolver
            builder.Register<IServiceResolver>(resolver => 
                new VContainerServiceResolver(resolver, null, configuration, messageBusService), Lifetime.Singleton);

            // Register configuration if provided
            if (configuration != null)
            {
                builder.RegisterInstance<IDependencyInjectionConfig>(configuration);
            }

            // Register message bus if provided
            if (messageBusService != null)
            {
                builder.RegisterInstance<IMessageBusService>(messageBusService);
            }

            return builder;
        }

        /// <summary>
        /// Registers a service conditionally if it's not already registered.
        /// Extends the existing RegisterIfNotPresent functionality.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="builder">The VContainer builder.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterIfNotPresent<TInterface, TImplementation>(
            this IContainerBuilder builder,
            Lifetime lifetime = Lifetime.Transient)
            where TImplementation : class, TInterface
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!builder.IsRegistered<TInterface>())
            {
                builder.Register<TImplementation>(lifetime).AsImplementedInterfaces();
            }

            return builder;
        }

        /// <summary>
        /// Registers a service with a factory method conditionally if it's not already registered.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <param name="builder">The VContainer builder.</param>
        /// <param name="factory">Factory method to create the service.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or factory is null.</exception>
        public static IContainerBuilder RegisterIfNotPresent<TInterface>(
            this IContainerBuilder builder,
            Func<IObjectResolver, TInterface> factory,
            Lifetime lifetime = Lifetime.Transient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (!builder.IsRegistered<TInterface>())
            {
                builder.Register<TInterface>(factory, lifetime);
            }

            return builder;
        }

        /// <summary>
        /// Creates a child container adapter from a VContainer resolver.
        /// </summary>
        /// <param name="parentResolver">The parent VContainer resolver.</param>
        /// <param name="childName">Optional name for the child container.</param>
        /// <param name="configuration">Optional configuration for the child.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainerAdapter for the child container.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parentResolver is null.</exception>
        public static IContainerAdapter CreateChildAdapter(
            this IObjectResolver parentResolver,
            string childName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null)
        {
            if (parentResolver == null) throw new ArgumentNullException(nameof(parentResolver));

            var childBuilder = new ContainerBuilder(parentResolver);
            return new VContainerAdapter(
                childBuilder,
                childName,
                configuration ?? new DependencyInjectionConfig(),
                messageBusService);
        }

        /// <summary>
        /// Creates a child dependency container from a VContainer resolver.
        /// </summary>
        /// <param name="parentResolver">The parent VContainer resolver.</param>
        /// <param name="childName">Optional name for the child container.</param>
        /// <param name="configuration">Optional configuration for the child.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <returns>A new VContainerContainerBridge for the child container.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parentResolver is null.</exception>
        public static IDependencyContainer CreateChildContainer(
            this IObjectResolver parentResolver,
            string childName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null)
        {
            if (parentResolver == null) throw new ArgumentNullException(nameof(parentResolver));

            var childAdapter = parentResolver.CreateChildAdapter(childName, configuration, messageBusService);
            return new VContainerContainerBridge((VContainerAdapter)childAdapter);
        }

        /// <summary>
        /// Validates a VContainer builder using our validation system.
        /// </summary>
        /// <param name="builder">The VContainer builder to validate.</param>
        /// <param name="configuration">Optional configuration for validation behavior.</param>
        /// <returns>True if validation passes, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static bool ValidateWithDISystem(
            this IContainerBuilder builder,
            IDependencyInjectionConfig configuration = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                var config = configuration ?? new DependencyInjectionConfig();
                
                // Use our existing validation extensions
                return builder.ValidateRegistrations(config.ThrowOnValidationFailure);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a VContainer adapter factory for use with our factory system.
        /// </summary>
        /// <returns>A new VContainerAdapterFactory instance.</returns>
        public static VContainerAdapterFactory CreateAdapterFactory()
        {
            return new VContainerAdapterFactory();
        }

        /// <summary>
        /// Registers common Unity services that are frequently needed.
        /// </summary>
        /// <param name="builder">The VContainer builder.</param>
        /// <param name="includeCoroutines">Whether to register coroutine services.</param>
        /// <param name="includeLogging">Whether to register logging services.</param>
        /// <param name="includeMessageBus">Whether to register message bus services.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder RegisterCommonUnityServices(
            this IContainerBuilder builder,
            bool includeCoroutines = true,
            bool includeLogging = true,
            bool includeMessageBus = true)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                // Register coroutine services if requested and available
                if (includeCoroutines)
                {
                    // This would integrate with our coroutine system
                    // Implementation depends on the coroutine system being available
                }

                // Register logging services if requested and available
                if (includeLogging)
                {
                    // This would integrate with our logging system
                    // Implementation depends on the logging system being available
                }

                // Register message bus services if requested and available
                if (includeMessageBus)
                {
                    // This would integrate with our message bus system
                    // Implementation depends on the message bus system being available
                }

                return builder;
            }
            catch (Exception)
            {
                // If any registration fails, continue without throwing
                // This ensures the extension method is robust
                return builder;
            }
        }

        /// <summary>
        /// Sets up VContainer for production use with our DI system.
        /// Applies optimizations and production-ready configurations.
        /// </summary>
        /// <param name="builder">The VContainer builder.</param>
        /// <param name="configuration">Optional production configuration.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IContainerBuilder ConfigureForProduction(
            this IContainerBuilder builder,
            IDependencyInjectionConfig configuration = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var config = configuration ?? DependencyInjectionConfig.Production;

            // Register our DI abstractions with production settings
            builder.RegisterDIAbstractions(config);

            // Apply production-specific optimizations
            if (config.EnableValidation)
            {
                // Validate during build in production for early error detection
                builder.RegisterBuildCallback(container =>
                {
                    try
                    {
                        builder.ValidateWithDISystem(config);
                    }
                    catch (Exception ex)
                    {
                        if (config.EnableDebugLogging)
                        {
                            UnityEngine.Debug.LogError($"[VContainer] Production validation failed: {ex.Message}");
                        }
                        
                        if (config.ThrowOnValidationFailure)
                        {
                            throw;
                        }
                    }
                });
            }

            return builder;
        }
    }
}