using System;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configuration;
using AhBearStudios.Core.Messaging.DI;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.MessageBuses.MessagePipe;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AhBearStudios.Core.Messaging.Installation
{
    /// <summary>
    /// Installer for the message bus system using VContainer.
    /// </summary>
    public sealed class MessageBusInstaller : IInstaller
    {
        private readonly MessageBusConfig _config;
        
        /// <summary>
        /// Initializes a new instance of the MessageBusInstaller class with default configuration.
        /// </summary>
        public MessageBusInstaller() : this(new MessageBusConfig())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the MessageBusInstaller class with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to use for the message bus.</param>
        public MessageBusInstaller(MessageBusConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        
        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            // Register MessagePipe
            var options = builder.RegisterMessagePipe(config =>
            {
                config.EnableCaptureStackTrace = _config.EnableCaptureStackTrace;
                
                if (_config.MaxSubscribersPerMessage > 0)
                {
                    config.InstanceLifetime = InstanceLifetime.Scoped;
                    config.DefaultInstanceLifetime = InstanceLifetime.Scoped;
                    config.MaxSubscriberCount = _config.MaxSubscribersPerMessage;
                }
                
                config.RequestHandling = _config.ValidateOnRegistration 
                    ? MessagePipe.RequestHandling.Throw 
                    : MessagePipe.RequestHandling.Ignore;
            });
            
            // Register built-in MessagePipe handlers/processors
            builder.RegisterBuildCallback(container => GlobalMessagePipe.SetProvider(container.AsServiceProvider()));
            
            // Register our IDependencyProvider implementation for VContainer
            builder.Register<IDependencyProvider>(c => 
                new VContainerDependencyProvider(c.Container), Lifetime.Singleton);
            
            // Register the configuration
            builder.RegisterInstance(_config);
            
            // Register the MessageBus as a singleton
            builder.Register<IMessageBus, MessagePipeBus>(Lifetime.Singleton);
            
            // Register diagnostic handlers if enabled
            if (_config.EnableDiagnosticLogging)
            {
                builder.RegisterMessageHandlerFilter<DiagnosticHandler>(options);
            }
            
            // Register performance monitoring handlers if enabled
            if (_config.EnablePerformanceProfiling)
            {
                builder.RegisterMessageHandlerFilter<PerformanceHandler>(options);
            }
        }
    }
}