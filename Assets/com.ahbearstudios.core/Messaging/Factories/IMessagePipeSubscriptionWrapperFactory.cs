using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating MessagePipe subscription wrappers.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessagePipeSubscriptionWrapperFactory
{
    /// <summary>
    /// Creates a MessagePipe subscription wrapper instance.
    /// </summary>
    /// <param name="adapter">The parent adapter</param>
    /// <param name="logger">Logging service dependency</param>
    /// <param name="poolingService">Pooling service dependency</param>
    /// <param name="onDispose">Action to call on disposal</param>
    /// <param name="messageType">The message type for this subscription</param>
    /// <returns>Configured MessagePipe subscription wrapper</returns>
    UniTask<IMessagePipeSubscriptionWrapper> CreateWrapperAsync(
        IMessageBusAdapter adapter,
        ILoggingService logger,
        IPoolingService poolingService,
        Action onDispose,
        Type messageType);
}