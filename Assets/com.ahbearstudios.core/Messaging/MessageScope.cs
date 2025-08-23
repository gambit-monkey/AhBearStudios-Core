using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of message scope for automatic subscription cleanup.
    /// Provides scoped subscription management with deterministic disposal.
    /// </summary>
    internal sealed class MessageScope : IMessageScope
    {
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _logger;
        private readonly ConcurrentBag<IDisposable> _subscriptions;
        private volatile bool _disposed;

        /// <summary>
        /// Gets the unique identifier for this scope.
        /// </summary>
        public Guid Id { get; }

        /// <inheritdoc />
        public int ActiveSubscriptions => _subscriptions.Count;

        /// <inheritdoc />
        public bool IsActive => !_disposed;

        /// <summary>
        /// Initializes a new instance of the MessageScope class.
        /// </summary>
        /// <param name="messageBusService">The message bus service</param>
        /// <param name="logger">The logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageScope(IMessageBusService messageBusService, ILoggingService logger)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptions = new ConcurrentBag<IDisposable>();
            Id = Guid.NewGuid();

            _logger.LogInfo($"MessageScope {Id} created");
        }

        /// <inheritdoc />
        public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscription = _messageBusService.SubscribeToMessage(handler);
                var scopedSubscription = new ScopedSubscription(subscription, this, typeof(TMessage).Name);
                
                _subscriptions.Add(scopedSubscription);
                
                _logger.LogInfo($"Created scoped subscription for {typeof(TMessage).Name} in scope {Id}");
                
                return scopedSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to create scoped subscription for {typeof(TMessage).Name} in scope {Id}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscription = _messageBusService.SubscribeToMessageAsync(handler);
                var scopedSubscription = new ScopedSubscription(subscription, this, typeof(TMessage).Name);
                
                _subscriptions.Add(scopedSubscription);
                
                _logger.LogInfo($"Created async scoped subscription for {typeof(TMessage).Name} in scope {Id}");
                
                return scopedSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to create async scoped subscription for {typeof(TMessage).Name} in scope {Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a subscription from tracking (called when individual subscriptions are disposed).
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        internal void RemoveSubscription(ScopedSubscription subscription)
        {
            // Note: ConcurrentBag doesn't support removal, but that's okay since we dispose all on scope disposal
            _logger.LogInfo($"Subscription removed from scope {Id}");
        }

        /// <summary>
        /// Throws an exception if the scope has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the scope is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"MessageScope {Id}");
        }

        /// <summary>
        /// Disposes the scope and all associated subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"Disposing MessageScope {Id} with {_subscriptions.Count} active subscriptions");

            try
            {
                // Dispose all subscriptions
                foreach (var subscription in _subscriptions)
                {
                    try
                    {
                        subscription?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"Error disposing subscription in scope {Id}", ex);
                    }
                }

                // Notify the message bus service
                if (_messageBusService is MessageBusService messageBusService)
                {
                    messageBusService.OnScopeDisposed(Id);
                }

                _disposed = true;
                _logger.LogInfo($"MessageScope {Id} disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Error during MessageScope {Id} disposal", ex);
            }
        }

        /// <summary>
        /// Scoped subscription wrapper that notifies the scope when disposed.
        /// </summary>
        internal sealed class ScopedSubscription : IDisposable
        {
            private readonly IDisposable _innerSubscription;
            private readonly MessageScope _scope;
            private readonly string _messageTypeName;
            private volatile bool _disposed;

            /// <summary>
            /// Initializes a new instance of the ScopedSubscription class.
            /// </summary>
            /// <param name="innerSubscription">The inner subscription</param>
            /// <param name="scope">The owning scope</param>
            /// <param name="messageTypeName">The message type name for logging</param>
            public ScopedSubscription(IDisposable innerSubscription, MessageScope scope, string messageTypeName)
            {
                _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
                _scope = scope ?? throw new ArgumentNullException(nameof(scope));
                _messageTypeName = messageTypeName ?? throw new ArgumentNullException(nameof(messageTypeName));
            }

            /// <summary>
            /// Disposes the scoped subscription.
            /// </summary>
            public void Dispose()
            {
                if (_disposed) return;

                try
                {
                    _innerSubscription?.Dispose();
                    _scope.RemoveSubscription(this);
                    _disposed = true;
                }
                catch (Exception ex)
                {
                    _scope._logger.LogException($"Error disposing scoped subscription for {_messageTypeName}", ex);
                }
            }
        }
    }
}