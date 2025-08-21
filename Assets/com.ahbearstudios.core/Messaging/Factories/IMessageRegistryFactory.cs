using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating message registry services.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageRegistryFactory
{
    /// <summary>
    /// Creates a message registry service instance.
    /// </summary>
    /// <param name="config">Configuration for the service</param>
    /// <param name="logger">Logging service dependency</param>
    /// <param name="poolingService">Pooling service for memory efficiency</param>
    /// <returns>Configured message registry service</returns>
    UniTask<IMessageRegistry> CreateServiceAsync(
        MessageRegistryConfig config,
        ILoggingService logger,
        IPoolingService poolingService);

    /// <summary>
    /// Creates a message registry service with default configuration.
    /// </summary>
    /// <param name="logger">Logging service dependency</param>
    /// <param name="poolingService">Pooling service for memory efficiency</param>
    /// <returns>Message registry service with default configuration</returns>
    UniTask<IMessageRegistry> CreateDefaultServiceAsync(
        ILoggingService logger,
        IPoolingService poolingService);
}