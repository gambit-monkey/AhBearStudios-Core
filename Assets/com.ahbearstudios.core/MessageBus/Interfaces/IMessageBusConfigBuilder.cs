using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for message bus configuration builders that implement the fluent pattern.
    /// </summary>
    /// <typeparam name="TConfig">The message bus configuration type being built</typeparam>
    /// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
    public interface IMessageBusConfigBuilder<TConfig, TBuilder> 
        where TConfig : IMessageBusConfig
        where TBuilder : IMessageBusConfigBuilder<TConfig, TBuilder>
    {
        /// <summary>
        /// Builds the message bus configuration.
        /// </summary>
        /// <returns>The configured message bus configuration</returns>
        TConfig Build();
    }
}