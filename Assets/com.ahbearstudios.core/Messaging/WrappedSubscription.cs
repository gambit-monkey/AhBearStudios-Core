namespace AhBearStudios.Core.Messaging;

/// <summary>
/// Wrapped subscription that provides proper cleanup tracking.
/// </summary>
public sealed class WrappedSubscription : IDisposable
{
    private readonly IDisposable _innerSubscription;
    private readonly MessageBusService _messageBusService;
    private readonly Type _messageType;
    private volatile bool _disposed;

    public WrappedSubscription(IDisposable innerSubscription, MessageBusService messageBusService, Type messageType)
    {
        _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _innerSubscription?.Dispose();
            _disposed = true;
                    
            _messageBusService._logger.LogInfo($"[{_messageBusService._correlationId}] Disposed subscription for {_messageType.Name}");
        }
        catch (Exception ex)
        {
            _messageBusService._logger.LogException(ex, $"[{_messageBusService._correlationId}] Error disposing subscription for {_messageType.Name}");
        }
    }
}