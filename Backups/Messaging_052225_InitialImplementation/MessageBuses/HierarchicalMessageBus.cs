using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Extensions;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using System.Collections.Concurrent;
using AhBearStudios.Core.Messaging.Data;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of a hierarchical message bus that supports parent-child relationships.
    /// Allows for message propagation between related buses based on propagation mode.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public class HierarchicalMessageBus<TMessage> : IMessageBus<TMessage>, IHierarchicalMessageBus<TMessage> where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _innerBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly object _hierarchyLock = new object();
        private readonly HashSet<ISubscriptionToken> _parentSubscriptions = new HashSet<ISubscriptionToken>();
        //private readonly HashSet<PropagationInfo> _propagatingMessages = new HashSet<PropagationInfo>();
        private readonly ConcurrentDictionary<Guid, PropagationInfo> _propagatingMessages = 
            new ConcurrentDictionary<Guid, PropagationInfo>();
        private IReadOnlyList<IHierarchicalMessageBus<TMessage>> _childrenReadOnly;
        
        private HierarchicalMessageBus<TMessage> _parent;
        private readonly List<HierarchicalMessageBus<TMessage>> _children = new List<HierarchicalMessageBus<TMessage>>();
        
        private MessagePropagationMode _propagationMode;
        private string _name;
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets the name of this message bus.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// Gets or sets the propagation mode for this message bus.
        /// </summary>
        public MessagePropagationMode PropagationMode
        {
            get => _propagationMode;
            set => _propagationMode = value;
        }

        /// <summary>
        /// Gets the parent message bus, if any.
        /// </summary>
        public IHierarchicalMessageBus<TMessage> Parent
        {
            get => _parent;
        }

        /// <summary>
        /// Gets a read-only list of child message buses.
        /// </summary>
        public IReadOnlyList<IHierarchicalMessageBus<TMessage>> Children
        {
            get
            {
                lock (_hierarchyLock)
                {
                    return _childrenReadOnly ??= _children
                        .Cast<IHierarchicalMessageBus<TMessage>>()
                        .ToList()
                        .AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the HierarchicalMessageBus class.
        /// </summary>
        /// <param name="innerBus">The underlying message bus to use for message delivery.</param>
        /// <param name="name">Optional name for this bus instance.</param>
        /// <param name="propagationMode">The mode that controls how messages propagate.</param>
        /// <param name="logger">Optional logger for bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public HierarchicalMessageBus(
            IMessageBus<TMessage> innerBus,
            string name = null,
            MessagePropagationMode propagationMode = MessagePropagationMode.None,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            _innerBus = innerBus ?? throw new ArgumentNullException(nameof(innerBus));
            _name = name ?? $"HierarchicalBus_{Guid.NewGuid():N}";
            _propagationMode = propagationMode;
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info($"HierarchicalMessageBus '{_name}' initialized with propagation mode {_propagationMode.GetDescription()}");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.Subscribe(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeAsync(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeToAll(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeToAllAsync(handler);
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                _innerBus.Unsubscribe(token);
            }
        }

        /// <inheritdoc/>
        public void Publish(TMessage message)
        {
            using var _ = _profiler?.BeginSample("HierarchicalMessageBus.Publish");
    
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                var propagationInfo = new PropagationInfo(message.Id);
        
                if (!AddPropagationInfo(propagationInfo))
                    return;

                try
                {
                    _innerBus.Publish(message);
                    PropagateToParent(message, propagationInfo);
                    PropagateToChildren(message, propagationInfo);
            
                    _logger?.Debug($"Published message {message.Id} to bus '{_name}'");
                }
                finally
                {
                    RemovePropagationInfo(propagationInfo);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error publishing message {message.Id}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Create a propagation context to track message propagation
                PropagationInfo propagationInfo = new PropagationInfo(message.Id);
                
                // Only propagate if this is the first time we're seeing this message
                if (AddPropagationInfo(propagationInfo))
                {
                    // Publish to the inner bus
                    await _innerBus.PublishAsync(message, cancellationToken);
                    
                    // Propagate to parent if needed
                    await PropagateToParentAsync(message, propagationInfo, cancellationToken);
                    
                    // Propagate to children if needed
                    await PropagateToChildrenAsync(message, propagationInfo, cancellationToken);
                    
                    // Remove the propagation info after propagation is complete
                    RemovePropagationInfo(propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Published message {message.Id} to bus '{_name}' asynchronously");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void AddChild(IHierarchicalMessageBus<TMessage> child)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.AddChild"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                if (child == null)
                {
                    throw new ArgumentNullException(nameof(child));
                }

                if (!(child is HierarchicalMessageBus<TMessage> typedChild))
                {
                    throw new ArgumentException("Child must be of type HierarchicalMessageBus<TMessage>", nameof(child));
                }

                lock (_hierarchyLock)
                {
                    // Make sure the child doesn't already have a parent
                    if (typedChild._parent != null)
                    {
                        throw new InvalidOperationException("Child already has a parent. Remove it from its current parent first.");
                    }
                    
                    // Make sure the child isn't already in our child list
                    if (_children.Contains(typedChild))
                    {
                        return;
                    }
                    
                    // Make sure this wouldn't create a cycle
                    if (IsDescendantOf(typedChild))
                    {
                        throw new InvalidOperationException("Cannot add a bus as a child of its own descendant.");
                    }
                    
                    // Add the child
                    _children.Add(typedChild);
                    typedChild._parent = this;
                    
                    // Set up subscriptions for bidirectional propagation
                    SetupChildSubscriptions(typedChild);
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Added child bus '{typedChild._name}' to parent '{_name}'");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveChild(IHierarchicalMessageBus<TMessage> child)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.RemoveChild"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                if (child == null)
                {
                    throw new ArgumentNullException(nameof(child));
                }

                if (!(child is HierarchicalMessageBus<TMessage> typedChild))
                {
                    throw new ArgumentException("Child must be of type HierarchicalMessageBus<TMessage>", nameof(child));
                }

                lock (_hierarchyLock)
                {
                    // Make sure the child is actually our child
                    if (!_children.Contains(typedChild) || typedChild._parent != this)
                    {
                        return;
                    }
                    
                    // Remove the child
                    _children.Remove(typedChild);
                    typedChild._parent = null;
                    
                    // Clean up any subscriptions to the child
                    CleanupChildSubscriptions(typedChild);
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Removed child bus '{typedChild._name}' from parent '{_name}'");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveFromParent()
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.RemoveFromParent"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                lock (_hierarchyLock)
                {
                    var parent = _parent;
                    if (parent != null)
                    {
                        parent.RemoveChild(this);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void SetPropagationMode(MessagePropagationMode mode)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.SetPropagationMode"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                lock (_hierarchyLock)
                {
                    if (_propagationMode == mode)
                    {
                        return;
                    }
                    
                    _propagationMode = mode;
                    
                    // Update subscriptions for all children
                    foreach (var child in _children)
                    {
                        CleanupChildSubscriptions(child);
                        SetupChildSubscriptions(child);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Changed propagation mode for bus '{_name}' to {_propagationMode.GetDescription()}");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool IsDescendantOf(IHierarchicalMessageBus<TMessage> potentialAncestor)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.IsDescendantOf"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                if (potentialAncestor == null)
                {
                    return false;
                }

                lock (_hierarchyLock)
                {
                    var current = _parent;
                    while (current != null)
                    {
                        if (current == potentialAncestor)
                        {
                            return true;
                        }
                        
                        current = current._parent;
                    }
                    
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsAncestorOf(IHierarchicalMessageBus<TMessage> potentialDescendant)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.IsAncestorOf"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(HierarchicalMessageBus<TMessage>));
                }

                if (potentialDescendant == null)
                {
                    return false;
                }

                return potentialDescendant.IsDescendantOf(this);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the hierarchical message bus.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                RemoveFromParent();
        
                lock (_hierarchyLock)
                {
                    var childrenCopy = _children.ToList();
                    foreach (var child in childrenCopy)
                    {
                        RemoveChild(child);
                    }
            
                    foreach (var subscription in _parentSubscriptions)
                    {
                        subscription.Dispose();
                    }
            
                    _parentSubscriptions.Clear();
                    _propagatingMessages.Clear();
                    _childrenReadOnly = null;
                }
        
                if (_innerBus is IDisposable disposableBus)
                {
                    disposableBus.Dispose();
                }
        
                _logger?.Info($"HierarchicalMessageBus '{_name}' disposed");
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~HierarchicalMessageBus()
        {
            Dispose(false);
        }

        /// <summary>
        /// Propagates a message to the parent bus.
        /// </summary>
        /// <param name="message">The message to propagate.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        private void PropagateToParent(TMessage message, PropagationInfo propagationInfo)
        {
            if (_parent == null || !_propagationMode.IncludesUpwardPropagation())
            {
                return;
            }
            
            try
            {
                // Publish to the parent
                _parent.OnMessageFromChild(this, message, propagationInfo);
                
                if (_logger != null)
                {
                    _logger.Debug($"Propagated message {message.Id} from '{_name}' to parent '{_parent._name}'");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Error($"Error propagating message {message.Id} to parent: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Propagates a message to the parent bus asynchronously.
        /// </summary>
        /// <param name="message">The message to propagate.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        /// <param name="cancellationToken">A cancellation token to stop propagation.</param>
        private async Task PropagateToParentAsync(TMessage message, PropagationInfo propagationInfo, CancellationToken cancellationToken)
        {
            if (_parent == null || !_propagationMode.IncludesUpwardPropagation())
            {
                return;
            }
            
            try
            {
                // Publish to the parent
                await _parent.OnMessageFromChildAsync(this, message, propagationInfo, cancellationToken);
                
                if (_logger != null)
                {
                    _logger.Debug($"Propagated message {message.Id} from '{_name}' to parent '{_parent._name}' asynchronously");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Error($"Error propagating message {message.Id} to parent asynchronously: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Propagates a message to all child buses.
        /// </summary>
        /// <param name="message">The message to propagate.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        private void PropagateToChildren(TMessage message, PropagationInfo propagationInfo)
        {
            if (!_propagationMode.IncludesDownwardPropagation() || _children.Count == 0)
            {
                return;
            }
            
            List<HierarchicalMessageBus<TMessage>> childrenCopy;
            
            lock (_hierarchyLock)
            {
                childrenCopy = _children.ToList();
            }
            
            foreach (var child in childrenCopy)
            {
                try
                {
                    // Publish to the child
                    child.OnMessageFromParent(this, message, propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Propagated message {message.Id} from '{_name}' to child '{child._name}'");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error propagating message {message.Id} to child '{child._name}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Propagates a message to all child buses asynchronously.
        /// </summary>
        /// <param name="message">The message to propagate.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        /// <param name="cancellationToken">A cancellation token to stop propagation.</param>
        private async Task PropagateToChildrenAsync(TMessage message, PropagationInfo propagationInfo, 
            CancellationToken cancellationToken)
        {
            if (!_propagationMode.IncludesDownwardPropagation() || _children.Count == 0)
                return;

            List<HierarchicalMessageBus<TMessage>> childrenCopy;
            lock (_hierarchyLock)
            {
                childrenCopy = new List<HierarchicalMessageBus<TMessage>>(_children.Count);
                childrenCopy.AddRange(_children);
            }

            var propagationTasks = childrenCopy.Select(child => new ValueTask(
                child.OnMessageFromParentAsync(this, message, propagationInfo, cancellationToken)));

            await Task.WhenAll(propagationTasks.Select(vt => vt.AsTask()));
        }
        /// <summary>
        /// Handles a message received from a parent bus.
        /// </summary>
        /// <param name="parent">The parent bus that sent the message.</param>
        /// <param name="message">The message being propagated.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        internal void OnMessageFromParent(HierarchicalMessageBus<TMessage> parent, TMessage message, PropagationInfo propagationInfo)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.OnMessageFromParent"))
            {
                if (_isDisposed)
                {
                    return;
                }

                // Make sure this is actually our parent
                if (_parent != parent)
                {
                    if (_logger != null)
                    {
                        _logger.Warning($"Received message {message.Id} from bus '{parent._name}' which is not the parent of '{_name}'");
                    }
                    
                    return;
                }

                // Only propagate if we haven't seen this message before
                if (AddPropagationInfo(propagationInfo))
                {
                    // Publish to our inner bus
                    _innerBus.Publish(message);
                    
                    // Propagate to children if needed
                    PropagateToChildren(message, propagationInfo);
                    
                    // Remove the propagation info after propagation is complete
                    RemovePropagationInfo(propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Received message {message.Id} from parent '{parent._name}' in bus '{_name}'");
                    }
                }
            }
        }

        /// <summary>
        /// Handles a message received from a parent bus asynchronously.
        /// </summary>
        /// <param name="parent">The parent bus that sent the message.</param>
        /// <param name="message">The message being propagated.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        /// <param name="cancellationToken">A cancellation token to stop propagation.</param>
        internal async Task OnMessageFromParentAsync(HierarchicalMessageBus<TMessage> parent, TMessage message, PropagationInfo propagationInfo, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.OnMessageFromParentAsync"))
            {
                if (_isDisposed)
                {
                    return;
                }

                // Make sure this is actually our parent
                if (_parent != parent)
                {
                    if (_logger != null)
                    {
                        _logger.Warning($"Received message {message.Id} from bus '{parent._name}' which is not the parent of '{_name}' asynchronously");
                    }
                    
                    return;
                }

                // Only propagate if we haven't seen this message before
                if (AddPropagationInfo(propagationInfo))
                {
                    // Publish to our inner bus
                    await _innerBus.PublishAsync(message, cancellationToken);
                    
                    // Propagate to children if needed
                    await PropagateToChildrenAsync(message, propagationInfo, cancellationToken);
                    
                    // Remove the propagation info after propagation is complete
                    RemovePropagationInfo(propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Received message {message.Id} from parent '{parent._name}' in bus '{_name}' asynchronously");
                    }
                }
            }
        }

        /// <summary>
        /// Handles a message received from a child bus.
        /// </summary>
        /// <param name="child">The child bus that sent the message.</param>
        /// <param name="message">The message being propagated.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        internal void OnMessageFromChild(HierarchicalMessageBus<TMessage> child, TMessage message, PropagationInfo propagationInfo)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.OnMessageFromChild"))
            {
                if (_isDisposed)
                {
                    return;
                }
                // Make sure this is actually one of our children
                if (!_children.Contains(child))
                {
                    if (_logger != null)
                    {
                        _logger.Warning($"Received message {message.Id} from bus '{child._name}' which is not a child of '{_name}'");
                    }
                    
                    return;
                }

                // Only propagate if we haven't seen this message before
                if (AddPropagationInfo(propagationInfo))
                {
                    // Publish to our inner bus
                    _innerBus.Publish(message);
                    
                    // Propagate to parent if needed
                    PropagateToParent(message, propagationInfo);
                    
                    // Propagate to other children if sibling propagation is enabled
                    if (_propagationMode.IncludesSiblingPropagation())
                    {
                        PropagateToSiblings(child, message, propagationInfo);
                    }
                    
                    // Remove the propagation info after propagation is complete
                    RemovePropagationInfo(propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Received message {message.Id} from child '{child._name}' in bus '{_name}'");
                    }
                }
            }
        }

        /// <summary>
        /// Handles a message received from a child bus asynchronously.
        /// </summary>
        /// <param name="child">The child bus that sent the message.</param>
        /// <param name="message">The message being propagated.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        /// <param name="cancellationToken">A cancellation token to stop propagation.</param>
        internal async Task OnMessageFromChildAsync(HierarchicalMessageBus<TMessage> child, TMessage message, PropagationInfo propagationInfo, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("HierarchicalMessageBus.OnMessageFromChildAsync"))
            {
                if (_isDisposed)
                {
                    return;
                }

                // Make sure this is actually one of our children
                if (!_children.Contains(child))
                {
                    if (_logger != null)
                    {
                        _logger.Warning($"Received message {message.Id} from bus '{child._name}' which is not a child of '{_name}' asynchronously");
                    }
                    
                    return;
                }

                // Only propagate if we haven't seen this message before
                if (AddPropagationInfo(propagationInfo))
                {
                    // Publish to our inner bus
                    await _innerBus.PublishAsync(message, cancellationToken);
                    
                    // Propagate to parent if needed
                    await PropagateToParentAsync(message, propagationInfo, cancellationToken);
                    
                    // Propagate to other children if sibling propagation is enabled
                    if (_propagationMode.IncludesSiblingPropagation())
                    {
                        await PropagateToSiblingsAsync(child, message, propagationInfo, cancellationToken);
                    }
                    
                    // Remove the propagation info after propagation is complete
                    RemovePropagationInfo(propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Received message {message.Id} from child '{child._name}' in bus '{_name}' asynchronously");
                    }
                }
            }
        }

        /// <summary>
        /// Propagates a message to all children except the sender.
        /// </summary>
        /// <param name="sender">The child bus that sent the message.</param>
        /// <param name="message">The message to propagate.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        private void PropagateToSiblings(HierarchicalMessageBus<TMessage> sender, TMessage message, PropagationInfo propagationInfo)
        {
            List<HierarchicalMessageBus<TMessage>> siblings;
            
            lock (_hierarchyLock)
            {
                siblings = _children.Where(c => c != sender).ToList();
            }
            
            foreach (var sibling in siblings)
            {
                try
                {
                    // Publish to the sibling
                    sibling.OnMessageFromParent(this, message, propagationInfo);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Propagated message {message.Id} from child '{sender._name}' to sibling '{sibling._name}'");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error propagating message {message.Id} to sibling '{sibling._name}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Propagates a message to all children except the sender asynchronously.
        /// </summary>
        /// <param name="sender">The child bus that sent the message.</param>
        /// <param name="message">The message to propagate.</param>
        /// <param name="propagationInfo">Propagation tracking information.</param>
        /// <param name="cancellationToken">A cancellation token to stop propagation.</param>
        private async Task PropagateToSiblingsAsync(HierarchicalMessageBus<TMessage> sender, TMessage message, PropagationInfo propagationInfo, CancellationToken cancellationToken)
        {
            List<HierarchicalMessageBus<TMessage>> siblings;
            
            lock (_hierarchyLock)
            {
                siblings = _children.Where(c => c != sender).ToList();
            }
            
            // Propagate to all siblings in parallel
            var propagationTasks = siblings.Select(async sibling =>
            {
                try
                {
                    // Publish to the sibling
                    await sibling.OnMessageFromParentAsync(this, message, propagationInfo, cancellationToken);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Propagated message {message.Id} from child '{sender._name}' to sibling '{sibling._name}' asynchronously");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error propagating message {message.Id} to sibling '{sibling._name}' asynchronously: {ex.Message}");
                    }
                }
            });
            
            await Task.WhenAll(propagationTasks);
        }

        /// <summary>
        /// Sets up subscriptions for a child bus.
        /// </summary>
        /// <param name="child">The child bus to set up subscriptions for.</param>
        private void SetupChildSubscriptions(HierarchicalMessageBus<TMessage> child)
        {
            // No subscriptions needed for mode None
            if (_propagationMode == MessagePropagationMode.None)
            {
                return;
            }
            
            // No subscriptions needed for UpwardOnly, as the child will push to parent
            // No subscriptions needed for DownwardOnly, as the parent will push to child
            
            // No additional subscriptions needed at the moment
        }

        /// <summary>
        /// Cleans up subscriptions for a child bus.
        /// </summary>
        /// <param name="child">The child bus to clean up subscriptions for.</param>
        private void CleanupChildSubscriptions(HierarchicalMessageBus<TMessage> child)
        {
            // No subscriptions needed currently, so nothing to clean up
        }

        /// <summary>
        /// Adds propagation info to track message propagation.
        /// </summary>
        /// <param name="propagationInfo">The propagation info to add.</param>
        /// <returns>True if the propagation info was added, false if it already existed.</returns>
        private bool AddPropagationInfo(PropagationInfo propagationInfo)
        {
            return _propagatingMessages.TryAdd(propagationInfo.MessageId, propagationInfo);
        }

        /// <summary>
        /// Removes propagation info after message propagation is complete.
        /// </summary>
        /// <param name="propagationInfo">The propagation info to remove.</param>
        private void RemovePropagationInfo(PropagationInfo propagationInfo)
        {
            _propagatingMessages.TryRemove(propagationInfo.MessageId, out _);
        }
    }
}