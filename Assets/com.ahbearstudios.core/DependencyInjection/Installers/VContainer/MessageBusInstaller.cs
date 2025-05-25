using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe;
using AhBearStudios.Core.MessageBus.Registration;
using AhBearStudios.Core.MessageBus.Serialization;
using AhBearStudios.Core.MessageBus.Services;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Metrics.Serialization;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// VContainer installer for the message bus system.
    /// Registers all required interfaces and their implementations for proper dependency injection.
    /// </summary>
    public sealed class MessageBusInstaller : IInstaller
    {
        private readonly MessageBusConfig _configuration;

        /// <summary>
        /// Initializes a new instance of the MessageBusInstaller class with default configuration.
        /// </summary>
        public MessageBusInstaller() // : this(MessageBusConfig.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MessageBusInstaller class with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use for the message bus system.</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
        public MessageBusInstaller(MessageBusConfig configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Register configuration
            RegisterConfiguration(builder);

            // Register core logging and profiling dependencies
            RegisterCoreDependencies(builder);

            // Register message registry
            RegisterMessageRegistry(builder);

            // Register serializers
            RegisterSerializers(builder);

            // Register message bus services
            RegisterMessageBusServices(builder);

            // Register message handlers and processors
            RegisterMessageHandlers(builder);
        }

        /// <summary>
        /// Registers the message bus configuration.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterConfiguration(IContainerBuilder builder)
        {
            builder.RegisterInstance(_configuration).AsImplementedInterfaces();
        }

        /// <summary>
        /// Registers core dependencies for logging and profiling.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterCoreDependencies(IContainerBuilder builder)
        {
            // Register logging if not already registered
            if (!builder.IsRegistered<IBurstLogger>())
            {
                builder.Register<IBurstLogger, BurstLogger>(Lifetime.Singleton);
                builder.Register<IBurstLogger, NullLogger>(Lifetime.Singleton);
            }

            // Register metrics implementations
            builder.Register<ISerializerMetrics, CompositeSerializerMetrics>(Lifetime.Singleton);
        }

        /// <summary>
        /// Registers the message registry and type management components.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterMessageRegistry(IContainerBuilder builder)
        {
            // Register message type registry
            builder.Register<IMessageRegistry, CachedMessageRegistry>(Lifetime.Singleton);
            builder.Register<IMessageRegistry, DefaultMessageRegistry>(Lifetime.Singleton);

            // // Register message info provider
            // builder.Register<IMessageInfoProvider, DefaultMessageInfoProvider>(Lifetime.Singleton);

            // // Configure registry initialization
            // builder.RegisterBuildCallback(container =>
            // {
            //     var registry = container.Resolve<IMessageRegistry>();
            //     registry.AutoRegisterMessageTypes(_configuration.AutoDiscoveryAssemblies);
            // });
        }

        /// <summary>
        /// Registers message serializers based on configuration.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterSerializers(IContainerBuilder builder)
        {
            // Register individual serializers
            builder.Register<IMessageSerializer, BurstMessageSerializer>(Lifetime.Singleton);
            builder.Register<IMessageSerializer,MemoryPackMessageSerializer>(Lifetime.Singleton);

            // // Register composite serializer as the primary serializer
            // builder.Register<IMessageSerializer>(container =>
            // {
            //     var logger = container.Resolve<IBurstLogger>();
            //     var burstSerializer = container.Resolve<BurstMessageSerializer>();
            //     var memoryPackSerializer = container.Resolve<MemoryPackMessageSerializer>();
            //
            //     // Configure serializer priority based on configuration
            //     var serializers = _configuration.PreferBurstSerialization
            //         ? new IMessageSerializer[] { burstSerializer, memoryPackSerializer }
            //         : new IMessageSerializer[] { memoryPackSerializer, burstSerializer };
            //
            //     return new CompositeMessageSerializer(logger, serializers);
            // }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers message bus services and core components.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterMessageBusServices(IContainerBuilder builder)
        {
            // Register delivery services based on configuration
            builder.Register<IMessageDeliveryService, BatchOptimizedDeliveryService>(Lifetime.Singleton);
            builder.Register<IMessageDeliveryService, FireAndForgetDeliveryService>(Lifetime.Singleton);
            builder.Register<IMessageDeliveryService, ReliableMessageDeliveryService>(Lifetime.Singleton);

            // // Register subscription management
            // builder.Register<ISubscriptionManager, DefaultSubscriptionManager>(Lifetime.Singleton);
            //
            // // Register message validation
            // if (_configuration.EnableMessageValidation)
            // {
            //     builder.Register<IMessageValidator, DefaultMessageValidator>(Lifetime.Singleton);
            // }
            // else
            // {
            //     builder.Register<IMessageValidator, NullMessageValidator>(Lifetime.Singleton);
            // }
            //
            // // Register metrics collector
            // if (_configuration.EnableMetricsCollection)
            // {
            //     builder.Register<IMessageBusMetrics, MessageBusMetricsCollector>(Lifetime.Singleton);
            // }
            // else
            // {
            //     builder.Register<IMessageBusMetrics, NullMessageBusMetrics>(Lifetime.Singleton);
            // }

            // Register the main message bus
            builder.Register<IMessageBus, MessagePipeBus>(Lifetime.Singleton);
        }

        /// <summary>
        /// Registers message handlers and processing components.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterMessageHandlers(IContainerBuilder builder)
        {
            // // Register handler factory
            // builder.Register<IMessageHandlerFactory>(container =>
            // {
            //     return new DefaultMessageHandlerFactory(container.AsServiceProvider());
            // }, Lifetime.Singleton);
            //
            // // Register message processor
            // builder.Register<IMessageProcessor, DefaultMessageProcessor>(Lifetime.Singleton);
            //
            // // Register retry policy if reliable delivery is enabled
            // if (_configuration.EnableReliableDelivery)
            // {
            //     builder.Register<IRetryPolicy, ExponentialBackoffRetryPolicy>(Lifetime.Singleton);
            //     builder.Register<IReliableDeliveryManager, DefaultReliableDeliveryManager>(Lifetime.Singleton);
            // }
            //
            // // Register diagnostic handlers if enabled
            // if (_configuration.EnableDiagnosticLogging)
            // {
            //     builder.Register<IDiagnosticHandler, DefaultDiagnosticHandler>(Lifetime.Singleton);
            // }
            //
            // // Register performance profiling if enabled
            // if (_configuration.EnablePerformanceProfiling)
            // {
            //     builder.Register<IPerformanceProfiler, DefaultPerformanceProfiler>(Lifetime.Singleton);
            // }
        }
    }
}