using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Messaging;

/// <summary>
/// Wrapped subscription that provides proper cleanup tracking.
/// </summary>
public sealed class WrappedSubscription : IDisposable
{
    private readonly IDisposable _innerSubscription;
    private readonly ILoggingService _logger;
    private readonly Type _messageType;
    private readonly Guid _correlationId;
    private volatile bool _disposed;

    public WrappedSubscription(IDisposable innerSubscription, ILoggingService logger, Type messageType, Guid correlationId)
    {
        _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        _correlationId = correlationId;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _innerSubscription?.Dispose();
            _disposed = true;
                    
            _logger.LogInfo($"[{_correlationId}] Disposed subscription for {_messageType.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogException($"[{_correlationId}] Error disposing subscription for {_messageType.Name}", ex);
        }
    }
}