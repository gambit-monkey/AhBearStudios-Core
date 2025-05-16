using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.MessageBuses
{
    /// <summary>
    /// Implementation of a hierarchical message bus
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public class HierarchicalMessageBus<TMessage> : IHierarchicalMessageBus<TMessage> where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _innerBus;
        private readonly List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();

        private readonly List<IHierarchicalMessageBus<TMessage>> _children =
            new List<IHierarchicalMessageBus<TMessage>>();

        private readonly Dictionary<Guid, bool> _processedMessages = new Dictionary<Guid, bool>();
        private readonly IBurstLogger _logger;
        private readonly object _syncRoot = new object();

        private IHierarchicalMessageBus<TMessage> _parent;

        public IHierarchicalMessageBus<TMessage> Parent => _parent;

        public IReadOnlyList<IHierarchicalMessageBus<TMessage>> Children => _children.AsReadOnly();

        public MessagePropagationMode PropagationMode { get; set; }

        public HierarchicalMessageBus(IMessageBus<TMessage> innerBus,
            MessagePropagationMode propagationMode = MessagePropagationMode.Bidirectional, IBurstLogger logger = null)
        {
            _innerBus = innerBus ?? throw new ArgumentNullException(nameof(innerBus));
            PropagationMode = propagationMode;
            _logger = logger;
        }

        public void Publish(TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Check if we've already processed this message to prevent cycles
            if (HasProcessedMessage(message.Id))
                return;

            // Mark as processed
            MarkMessageAsProcessed(message.Id);

            // Publish locally
            _innerBus.Publish(message);

            // Propagate based on mode
            PropagateMessage(message);
        }

        public Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Check if we've already processed this message to prevent cycles
            if (HasProcessedMessage(message.Id))
                return Task.CompletedTask;

            // Mark as processed
            MarkMessageAsProcessed(message.Id);

            // Publish locally
            var localTask = _innerBus.PublishAsync(message, cancellationToken);

            // Propagate based on mode
            var propagationTask = PropagateMessageAsync(message, cancellationToken);

            return Task.WhenAll(localTask, propagationTask);
        }

        public ISubscriptionToken Subscribe(Action<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return _innerBus.Subscribe(handler);
        }

        public ISubscriptionToken SubscribeAsync(Func<TMessage, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
        
            // Subscribe to the inner message bus with the async handler
            // The inner bus will be responsible for properly executing the async Tasks
            var token = _innerBus.SubscribeAsync(async message => 
            {
                // Only handle messages that should be propagated based on the propagation mode
                if (ShouldHandleMessage(message))
                {
                    await handler(message);
                }
            });
    
            // Store the subscription to manage hierarchy-specific behavior
            _subscriptions.Add(token);
    
            return token;
        }
        
        /// <summary>
        /// Determines whether a message should be handled based on the propagation mode
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <returns>True if the message should be handled, false otherwise</returns>
        private bool ShouldHandleMessage(TMessage message)
        {
            // In most cases, we want to handle all messages that reach us
            // But if we need specific filtering based on propagation mode:
    
            // Example: If we only want to handle messages from our own bus or parent
            // if (message.SourceId != null && !message.SourceId.Equals(this.Id) && 
            //     (Parent == null || !message.SourceId.Equals(Parent.Id)))
            //     return false;
    
            // For now, handle all messages that reach this bus
            return true;
        }

        public ISubscriptionToken SubscribeLocal(Action<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return _innerBus.Subscribe(handler);
        }

        public void AddChild(IHierarchicalMessageBus<TMessage> child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            lock (_syncRoot)
            {
                if (!_children.Contains(child))
                {
                    _children.Add(child);
                    child.SetParent(this);
                }
            }
        }

        public bool RemoveChild(IHierarchicalMessageBus<TMessage> child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            lock (_syncRoot)
            {
                if (_children.Remove(child))
                {
                    child.ClearParent();
                    return true;
                }

                return false;
            }
        }

        public void SetParent(IHierarchicalMessageBus<TMessage> parent)
        {
            if (parent == this)
                throw new InvalidOperationException("Cannot set self as parent");

            _parent = parent;
        }

        public void ClearParent()
        {
            _parent = null;
        }

        private void PropagateMessage(TMessage message)
        {
            // Propagate to parent if enabled
            if ((PropagationMode == MessagePropagationMode.UpstreamOnly ||
                 PropagationMode == MessagePropagationMode.Bidirectional) &&
                _parent != null)
            {
                _parent.Publish(message);
            }

            // Propagate to children if enabled
            if (PropagationMode == MessagePropagationMode.DownstreamOnly ||
                PropagationMode == MessagePropagationMode.Bidirectional)
            {
                lock (_syncRoot)
                {
                    foreach (var child in _children)
                    {
                        child.Publish(message);
                    }
                }
            }
        }

        private async Task PropagateMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            // Propagate to parent if enabled
            if ((PropagationMode == MessagePropagationMode.UpstreamOnly ||
                 PropagationMode == MessagePropagationMode.Bidirectional) &&
                _parent != null)
            {
                tasks.Add(_parent.PublishAsync(message, cancellationToken));
            }

            // Propagate to children if enabled
            if (PropagationMode == MessagePropagationMode.DownstreamOnly ||
                PropagationMode == MessagePropagationMode.Bidirectional)
            {
                lock (_syncRoot)
                {
                    foreach (var child in _children)
                    {
                        tasks.Add(child.PublishAsync(message, cancellationToken));
                    }
                }
            }

            await Task.WhenAll(tasks);
        }

        private bool HasProcessedMessage(Guid messageId)
        {
            lock (_syncRoot)
            {
                return _processedMessages.ContainsKey(messageId);
            }
        }

        private void MarkMessageAsProcessed(Guid messageId)
        {
            lock (_syncRoot)
            {
                _processedMessages[messageId] = true;

                // Clean up old processed messages periodically
                if (_processedMessages.Count > 1000)
                {
                    // Remove oldest entries (beyond the first 100)
                    var keysToRemove = _processedMessages.Keys.Skip(100).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _processedMessages.Remove(key);
                    }
                }
            }
        }
    }
}