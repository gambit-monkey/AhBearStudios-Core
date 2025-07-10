using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Enhanced container builder with fluent API for creating and configuring containers.
    /// </summary>
    public interface IEnhancedContainerBuilder
    {
        /// <summary>
        /// Sets the container name.
        /// </summary>
        IEnhancedContainerBuilder WithName(string name);
        
        /// <summary>
        /// Sets the message bus service.
        /// </summary>
        IEnhancedContainerBuilder WithMessageBus(IMessageBusService messageBusService);
        
        /// <summary>
        /// Configures the DI system settings.
        /// </summary>
        IEnhancedContainerBuilder WithConfiguration(Action<IDependencyInjectionConfigBuilder> configBuilder);
        
        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        IEnhancedContainerBuilder RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a singleton service with factory.
        /// </summary>
        IEnhancedContainerBuilder RegisterSingleton<TInterface>(Func<IServiceResolver, TInterface> factory);
        
        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        IEnhancedContainerBuilder RegisterInstance<TInterface>(TInterface instance);
        
        /// <summary>
        /// Registers a transient service.
        /// </summary>
        IEnhancedContainerBuilder RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Registers a transient service with factory.
        /// </summary>
        IEnhancedContainerBuilder RegisterTransient<TInterface>(Func<IServiceResolver, TInterface> factory);
        
        /// <summary>
        /// Applies a configuration action to the underlying container.
        /// </summary>
        IEnhancedContainerBuilder Configure(Action<IContainerAdapter> configuration);
        
        /// <summary>
        /// Builds the container and returns it.
        /// </summary>
        IContainerAdapter Build();
        
        /// <summary>
        /// Builds the container and returns a service resolver.
        /// </summary>
        IServiceResolver BuildResolver();
    }
}