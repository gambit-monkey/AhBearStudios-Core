using System;
using System.Threading;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Wrapper class for MessagePipe subscription disposal tracking.
/// Provides proper resource management and statistics tracking following CLAUDE.md guidelines.
/// Uses IPoolingService for memory efficiency and ILoggingService for comprehensive monitoring.
/// </summary>
public sealed class MessagePipeSubscriptionWrapper : IMessagePipeSubscriptionWrapper
{
    #region Private Fields

    private readonly IMessageBusAdapter _adapter;
    private readonly ILoggingService _logger;
    private readonly IPoolingService _poolingService;
    private readonly Action _onDispose;
    private readonly Type _messageType;
    private readonly DateTime _createdAt;
    private readonly Guid _subscriptionId;
    private readonly ProfilerMarker _disposeMarker = new("MessagePipeSubscriptionWrapper.Dispose");
    
    private volatile bool _disposed;
    private long _messagesReceived;
    private long _processingTimeMs;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the unique subscription identifier.
    /// </summary>
    public Guid SubscriptionId => _subscriptionId;

    /// <summary>
    /// Gets the message type for this subscription.
    /// </summary>
    public Type MessageType => _messageType;

    /// <summary>
    /// Gets when this subscription was created.
    /// </summary>
    public DateTime CreatedAt => _createdAt;

    /// <summary>
    /// Gets whether this subscription has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the number of messages received by this subscription.
    /// </summary>
    public long MessagesReceived => _messagesReceived;

    /// <summary>
    /// Gets the total processing time in milliseconds.
    /// </summary>
    public long TotalProcessingTimeMs => _processingTimeMs;

    /// <summary>
    /// Gets the average processing time per message in milliseconds.
    /// </summary>
    public double AverageProcessingTime => _messagesReceived > 0 ? (double)_processingTimeMs / _messagesReceived : 0.0;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the MessagePipeSubscriptionWrapper.
    /// </summary>
    /// <param name="adapter">The parent adapter</param>
    /// <param name="logger">The logging service</param>
    /// <param name="poolingService">The pooling service</param>
    /// <param name="onDispose">Action to call on disposal</param>
    /// <param name="messageType">The message type for this subscription</param>
    public MessagePipeSubscriptionWrapper(
        IMessageBusAdapter adapter,
        ILoggingService logger,
        IPoolingService poolingService,
        Action onDispose,
        Type messageType)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _poolingService = poolingService;
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        
        _subscriptionId = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;

        _logger.LogInfo($"Created MessagePipe subscription {_subscriptionId} for {_messageType.Name}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Records that a message was received and processed.
    /// </summary>
    /// <param name="processingTimeMs">Processing time in milliseconds</param>
    public void RecordMessageReceived(long processingTimeMs)
    {
        if (_disposed) return;

        Interlocked.Increment(ref _messagesReceived);
        Interlocked.Add(ref _processingTimeMs, processingTimeMs);
    }

    /// <summary>
    /// Gets subscription statistics.
    /// </summary>
    /// <returns>Statistics for this subscription</returns>
    public MessagePipeSubscriptionStatistics GetStatistics()
    {
        return new MessagePipeSubscriptionStatistics
        {
            SubscriptionId = _subscriptionId,
            MessageType = _messageType,
            CreatedAt = _createdAt,
            IsActive = !_disposed,
            MessagesReceived = _messagesReceived,
            TotalProcessingTime = _processingTimeMs,
            AverageProcessingTime = AverageProcessingTime
        };
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the subscription wrapper and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        using (_disposeMarker.Auto())
        {
            try
            {
                _onDispose();
                _disposed = true;

                var lifetime = DateTime.UtcNow - _createdAt;
                _logger.LogInfo($"Disposed MessagePipe subscription {_subscriptionId} for {_messageType.Name}. " +
                               $"Lifetime: {lifetime.TotalSeconds:F2}s, Messages: {_messagesReceived}, Avg Processing: {AverageProcessingTime:F2}ms");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Error disposing MessagePipe subscription wrapper {_subscriptionId}",ex);
            }
        }
    }

    #endregion
}