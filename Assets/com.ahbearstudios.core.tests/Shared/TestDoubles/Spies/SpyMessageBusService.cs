using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Spies
{
    /// <summary>
    /// Spy implementation of IMessageBusService for TDD testing.
    /// Records all interactions without implementing actual message bus logic.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class SpyMessageBusService : IMessageBusService
    {
        private readonly List<IMessage> _publishedMessages = new();
        private readonly List<PublishCall> _publishCalls = new();
        private readonly List<SubscriptionCall> _subscriptionCalls = new();
        private readonly List<UnsubscriptionCall> _unsubscriptionCalls = new();
        private readonly object _lockObject = new();
        private bool _isDisposed;

        #region Test Verification Properties

        /// <summary>
        /// Gets all published messages for test verification.
        /// </summary>
        public IReadOnlyList<IMessage> PublishedMessages
        {
            get
            {
                lock (_lockObject)
                {
                    return _publishedMessages.ToList();
                }
            }
        }

        /// <summary>
        /// Gets all publish calls with metadata for test verification.
        /// </summary>
        public IReadOnlyList<PublishCall> PublishCalls
        {
            get
            {
                lock (_lockObject)
                {
                    return _publishCalls.ToList();
                }
            }
        }

        /// <summary>
        /// Gets all subscription calls for test verification.
        /// </summary>
        public IReadOnlyList<SubscriptionCall> SubscriptionCalls
        {
            get
            {
                lock (_lockObject)
                {
                    return _subscriptionCalls.ToList();
                }
            }
        }

        /// <summary>
        /// Gets all unsubscription calls for test verification.
        /// </summary>
        public IReadOnlyList<UnsubscriptionCall> UnsubscriptionCalls
        {
            get
            {
                lock (_lockObject)
                {
                    return _unsubscriptionCalls.ToList();
                }
            }
        }

        /// <summary>
        /// Gets count of published messages by type.
        /// </summary>
        public int GetPublishCount<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _publishedMessages.OfType<T>().Count();
            }
        }

        /// <summary>
        /// Gets count of subscriptions by type.
        /// </summary>
        public int GetSubscriptionCount<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _subscriptionCalls.Count(c => c.MessageType == typeof(T));
            }
        }

        /// <summary>
        /// Checks if a specific message was published.
        /// </summary>
        public bool WasMessagePublished<T>(Func<T, bool> predicate = null) where T : IMessage
        {
            lock (_lockObject)
            {
                var messages = _publishedMessages.OfType<T>();
                return predicate == null ? messages.Any() : messages.Any(predicate);
            }
        }

        /// <summary>
        /// Gets the last published message of a specific type.
        /// </summary>
        public T GetLastMessage<T>() where T : IMessage
        {
            lock (_lockObject)
            {
                return _publishedMessages.OfType<T>().LastOrDefault();
            }
        }

        /// <summary>
        /// Clears all recorded interactions.
        /// </summary>
        public void ClearRecordedInteractions()
        {
            lock (_lockObject)
            {
                _publishedMessages.Clear();
                _publishCalls.Clear();
                _subscriptionCalls.Clear();
                _unsubscriptionCalls.Clear();
            }
        }

        #endregion

        #region IMessageBusService Implementation - Spy Behavior

        // Properties
        public bool IsEnabled { get; set; } = true;

        // Core publishing operations - record calls only
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            lock (_lockObject)
            {
                _publishedMessages.Add(message);
                _publishCalls.Add(new PublishCall
                {
                    MessageType = typeof(TMessage),
                    Message = message,
                    IsAsync = false,
                    IsBatch = false,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async UniTask PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Unity Test Runner compatible async
            await UniTask.CompletedTask;

            lock (_lockObject)
            {
                _publishedMessages.Add(message);
                _publishCalls.Add(new PublishCall
                {
                    MessageType = typeof(TMessage),
                    Message = message,
                    IsAsync = true,
                    IsBatch = false,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            lock (_lockObject)
            {
                foreach (var message in messages)
                {
                    _publishedMessages.Add(message);
                }

                _publishCalls.Add(new PublishCall
                {
                    MessageType = typeof(TMessage),
                    Message = messages.FirstOrDefault(),
                    BatchSize = messages.Length,
                    IsAsync = false,
                    IsBatch = true,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async UniTask PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            // Unity Test Runner compatible async
            await UniTask.CompletedTask;

            lock (_lockObject)
            {
                foreach (var message in messages)
                {
                    _publishedMessages.Add(message);
                }

                _publishCalls.Add(new PublishCall
                {
                    MessageType = typeof(TMessage),
                    Message = messages.FirstOrDefault(),
                    BatchSize = messages.Length,
                    IsAsync = true,
                    IsBatch = true,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Core subscription operations - record calls only, return dummy subscriptions
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                _subscriptionCalls.Add(new SubscriptionCall
                {
                    MessageType = typeof(TMessage),
                    IsAsync = false,
                    Timestamp = DateTime.UtcNow
                });
            }

            return new SpySubscription(() => RecordUnsubscription(typeof(TMessage), false));
        }

        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                _subscriptionCalls.Add(new SubscriptionCall
                {
                    MessageType = typeof(TMessage),
                    IsAsync = true,
                    Timestamp = DateTime.UtcNow
                });
            }

            return new SpySubscription(() => RecordUnsubscription(typeof(TMessage), true));
        }

        // Advanced operations - return spy implementations
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            return new SpyMessagePublisher<TMessage>();
        }

        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            return new SpyMessageSubscriber<TMessage>();
        }

        // Filtering and routing operations
        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                _subscriptionCalls.Add(new SubscriptionCall
                {
                    MessageType = typeof(TMessage),
                    IsAsync = false,
                    HasFilter = true,
                    Timestamp = DateTime.UtcNow
                });
            }

            return new SpySubscription(() => RecordUnsubscription(typeof(TMessage), false));
        }

        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                _subscriptionCalls.Add(new SubscriptionCall
                {
                    MessageType = typeof(TMessage),
                    IsAsync = true,
                    HasFilter = true,
                    Timestamp = DateTime.UtcNow
                });
            }

            return new SpySubscription(() => RecordUnsubscription(typeof(TMessage), true));
        }

        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SpyMessageBusService));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                _subscriptionCalls.Add(new SubscriptionCall
                {
                    MessageType = typeof(TMessage),
                    IsAsync = false,
                    HasPriority = true,
                    MinPriority = minPriority,
                    Timestamp = DateTime.UtcNow
                });
            }

            return new SpySubscription(() => RecordUnsubscription(typeof(TMessage), false));
        }

        // Scoped subscriptions
        public IMessageScope CreateScope()
        {
            return new SpyMessageScope("SpyScope");
        }


        // Diagnostics and management operations
        public MessageBusStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                return new MessageBusStatistics
                {
                    InstanceName = nameof(SpyMessageBusService),
                    TotalMessagesPublished = _publishCalls.Count,
                    TotalMessagesProcessed = _publishCalls.Count,
                    TotalMessagesFailed = 0,
                    ActiveSubscribers = Math.Max(0, _subscriptionCalls.Count - _unsubscriptionCalls.Count),
                    DeadLetterQueueSize = 0,
                    MessagesInRetry = 0,
                    CurrentQueueDepth = 0,
                    MemoryUsage = 0,
                    CurrentHealthStatus = IsEnabled ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    MessageTypeStatistics = new Dictionary<Type, MessageTypeStatistics>(),
                    CircuitBreakerStates = new Dictionary<Type, CircuitBreakerState>(),
                    ActiveScopes = 0,
                    LastStatsReset = DateTime.UtcNow,
                    ErrorRate = 0.0,
                    AverageProcessingTimeMs = 0.0,
                    MessagesPerSecond = 0.0,
                    SuccessRate = 1.0,
                    FailureRate = 0.0
                };
            }
        }

        public void ClearMessageHistory()
        {
            ClearRecordedInteractions();
        }

        // Health and monitoring operations
        public HealthStatus GetHealthStatus()
        {
            return IsEnabled ? HealthStatus.Healthy : HealthStatus.Unhealthy;
        }

        public async UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.CompletedTask;
            return GetHealthStatus();
        }

        // Circuit breaker operations
        public CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage
        {
            return CircuitBreakerState.Closed; // Always closed for spy
        }

        public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            // No-op: spy doesn't manage circuit breakers
        }


        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _isDisposed = true;
            ClearRecordedInteractions();
        }

        #endregion

        #region Private Helper Methods

        private void RecordUnsubscription(Type messageType, bool isAsync)
        {
            lock (_lockObject)
            {
                _unsubscriptionCalls.Add(new UnsubscriptionCall
                {
                    MessageType = messageType,
                    IsAsync = isAsync,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #endregion
    }

    #region Helper Classes for Test Verification

    /// <summary>
    /// Spy implementation of IDisposable subscription.
    /// </summary>
    internal sealed class SpySubscription : IDisposable
    {
        private readonly Action _onDispose;
        private bool _isDisposed;

        public SpySubscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _onDispose?.Invoke();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Spy implementation of IMessageScope.
    /// </summary>
    internal sealed class SpyMessageScope : IMessageScope
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int ActiveSubscriptions => 0;
        public bool IsActive => true;

        public SpyMessageScope(string scopeName)
        {
            // Scope name not part of interface, but kept for backwards compatibility
        }

        public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            return new SpySubscription(() => { });
        }

        public IDisposable SubscribeAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            return new SpySubscription(() => { });
        }

        public void Dispose()
        {
            // No-op for spy scope
        }
    }

    /// <summary>
    /// Spy implementation of IMessagePublisher<T>.
    /// </summary>
    internal sealed class SpyMessagePublisher<TMessage> : IMessagePublisher<TMessage> where TMessage : IMessage
    {
        public void Publish(TMessage message)
        {
            // No-op for spy publisher
        }

        public async UniTask PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            await UniTask.CompletedTask;
        }

        public void PublishBatch(IEnumerable<TMessage> messages)
        {
            // No-op for spy publisher
        }

        public async UniTask PublishBatchAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default)
        {
            await UniTask.CompletedTask;
        }

        public bool PublishIf(TMessage message, Func<bool> condition)
        {
            return true; // Always return true for spy
        }

        public async UniTask<bool> PublishIfAsync(TMessage message, Func<UniTask<bool>> condition, CancellationToken cancellationToken = default)
        {
            await UniTask.CompletedTask;
            return true; // Always return true for spy
        }

        public async UniTask PublishDelayedAsync(TMessage message, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            await UniTask.CompletedTask;
        }

        public PublisherStatistics GetStatistics()
        {
            return new PublisherStatistics(0, 0, 0, 0.0, 0.0, 0);
        }

        public bool IsOperational => true;
        public Type MessageType => typeof(TMessage);

        public event EventHandler<MessagePublishedEventArgs> MessagePublished;
        public event EventHandler<MessagePublishFailedEventArgs> MessagePublishFailed;

        public void Dispose()
        {
            // No-op for spy publisher
        }
    }

    /// <summary>
    /// Spy implementation of IMessageSubscriber<T>.
    /// </summary>
    internal sealed class SpyMessageSubscriber<TMessage> : IMessageSubscriber<TMessage> where TMessage : IMessage
    {
        public IDisposable Subscribe(Action<TMessage> handler)
        {
            return new SpySubscription(() => { });
        }

        public IDisposable SubscribeAsync(Func<TMessage, UniTask> handler)
        {
            return new SpySubscription(() => { });
        }

        public IDisposable SubscribeWithFilter(Action<TMessage> handler, Func<TMessage, bool> filter = null, MessagePriority minPriority = MessagePriority.Debug)
        {
            return new SpySubscription(() => { });
        }

        public IDisposable SubscribeAsyncWithFilter(Func<TMessage, UniTask> handler, Func<TMessage, UniTask<bool>> filter = null, MessagePriority minPriority = MessagePriority.Debug)
        {
            return new SpySubscription(() => { });
        }

        public void UnsubscribeAll()
        {
            // No-op for spy subscriber
        }

        public int ActiveSubscriptions => 0;
        public bool IsOperational => true;
        public Type MessageType => typeof(TMessage);

        public SubscriberStatistics GetStatistics()
        {
            return new SubscriberStatistics(0, 0, 0, 0, 0.0, 0.0, 0, 0);
        }

        public void Dispose()
        {
            // No-op for spy subscriber
        }
    }

    #endregion
}