using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory for creating MessagePipe subscription wrappers.
/// Simple creation following CLAUDE.md guidelines - no lifecycle management.
/// </summary>
public sealed class MessagePipeSubscriptionWrapperFactory : IMessagePipeSubscriptionWrapperFactory
{
    /// <inheritdoc />
    public async UniTask<IMessagePipeSubscriptionWrapper> CreateWrapperAsync(
        IMessageBusAdapter adapter,
        ILoggingService logger,
        IPoolingService poolingService,
        Action onDispose,
        Type messageType)
    {
        if (adapter == null)
            throw new ArgumentNullException(nameof(adapter));
        
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));
        
        if (onDispose == null)
            throw new ArgumentNullException(nameof(onDispose));
        
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        await UniTask.SwitchToMainThread();

        // Simple creation - factory doesn't track the created instance
        return new MessagePipeSubscriptionWrapper(
            adapter,
            logger,
            poolingService,
            onDispose,
            messageType);
    }
}