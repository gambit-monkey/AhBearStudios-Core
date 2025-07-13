using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Interface for the message bus factory.
/// </summary>
public interface IMessageBusFactory
{
    /// <summary>
    /// Creates a new message bus service instance with the specified configuration.
    /// </summary>
    /// <param name="config">The message bus configuration</param>
    /// <returns>A configured message bus service instance</returns>
    IMessageBusService CreateMessageBus(MessageBusConfig config);

    /// <summary>
    /// Creates a message bus service with default configuration.
    /// </summary>
    /// <returns>A message bus service instance with default settings</returns>
    IMessageBusService CreateDefaultMessageBus();

    /// <summary>
    /// Creates a high-performance message bus service instance.
    /// </summary>
    /// <returns>A message bus service optimized for high throughput</returns>
    IMessageBusService CreateHighPerformanceMessageBus();

    /// <summary>
    /// Creates a reliable message bus service instance.
    /// </summary>
    /// <returns>A message bus service optimized for reliability</returns>
    IMessageBusService CreateReliableMessageBus();

    /// <summary>
    /// Validates that all required dependencies are available.
    /// </summary>
    void ValidateDependencies();
}