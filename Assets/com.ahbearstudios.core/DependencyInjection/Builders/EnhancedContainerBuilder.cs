using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Builders
{
    /// <summary>
    /// Implementation of enhanced container builder with fluent configuration API.
    /// Optimized for minimal allocations and high performance.
    /// </summary>
    public sealed class EnhancedContainerBuilder : IEnhancedContainerBuilder
    {
        private IDependencyInjectionConfig _config;
        private string _containerName;
        private IMessageBusService _messageBusService;
        private readonly List<Action<IContainerAdapter>> _configurations;
        
        /// <summary>
        /// Initializes a new enhanced container builder.
        /// </summary>
        public EnhancedContainerBuilder(IDependencyInjectionConfig config = null)
        {
            _config = config ?? DependencyContainerFactory.DefaultConfiguration;
            _configurations = new List<Action<IContainerAdapter>>();
        }
        
        /// <summary>
        /// Sets the container name.
        /// </summary>
        public IEnhancedContainerBuilder WithName(string name)
        {
            _containerName = name;
            return this;
        }
        
        /// <summary>
        /// Sets the message bus service.
        /// </summary>
        public IEnhancedContainerBuilder WithMessageBus(IMessageBusService messageBusService)
        {
            _messageBusService = messageBusService;
            return this;
        }
        
        /// <summary>
        /// Configures the DI system settings.
        /// </summary>
        public IEnhancedContainerBuilder WithConfiguration(Action<IDependencyInjectionConfigBuilder> configBuilder)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));
            
            var builder = new DependencyInjectionConfigBuilder()
                .WithFramework(_config.PreferredFramework)
                .WithValidation(_config.EnableValidation)
                .WithDebugLogging(_config.EnableDebugLogging)
                .WithPerformanceMetrics(_config.EnablePerformanceMetrics)
                .WithThrowOnValidationFailure(_config.ThrowOnValidationFailure)
                .WithMaxBuildTimeWarning(_config.MaxBuildTimeWarningMs)
                .WithScoping(_config.EnableScoping)
                .WithNamedServices(_config.EnableNamedServices);
            
            // Apply framework-specific options
            foreach (var option in _config.FrameworkSpecificOptions)
            {
                builder.WithFrameworkOption(option.Key, option.Value);
            }
            
            configBuilder(builder);
            _config = builder.Build();
            return this;
        }
        
        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        public IEnhancedContainerBuilder RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _configurations.Add(container => container.RegisterSingleton<TInterface, TImplementation>());
            return this;
        }
        
        /// <summary>
        /// Registers a singleton service with factory.
        /// </summary>
        public IEnhancedContainerBuilder RegisterSingleton<TInterface>(Func<IServiceResolver, TInterface> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            _configurations.Add(container => container.RegisterSingleton(factory));
            return this;
        }
        
        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        public IEnhancedContainerBuilder RegisterInstance<TInterface>(TInterface instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            _configurations.Add(container => container.RegisterInstance(instance));
            return this;
        }
        
        /// <summary>
        /// Registers a transient service.
        /// </summary>
        public IEnhancedContainerBuilder RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _configurations.Add(container => container.RegisterTransient<TInterface, TImplementation>());
            return this;
        }
        
        /// <summary>
        /// Registers a transient service with factory.
        /// </summary>
        public IEnhancedContainerBuilder RegisterTransient<TInterface>(Func<IServiceResolver, TInterface> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            _configurations.Add(container => container.RegisterTransient(factory));
            return this;
        }
        
        /// <summary>
        /// Applies a configuration action to the underlying container.
        /// </summary>
        public IEnhancedContainerBuilder Configure(Action<IContainerAdapter> configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            _configurations.Add(configuration);
            return this;
        }
        
        /// <summary>
        /// Builds the container and returns it.
        /// </summary>
        public IContainerAdapter Build()
        {
            var container = DependencyContainerFactory.Create(_config, _containerName, _messageBusService);
            
            // Apply all configurations
            foreach (var configuration in _configurations)
            {
                configuration(container);
            }
            
            return container;
        }
        
        /// <summary>
        /// Builds the container and returns a service resolver.
        /// </summary>
        public IServiceResolver BuildResolver()
        {
            var container = Build();
            return container.Build();
        }
    }
}