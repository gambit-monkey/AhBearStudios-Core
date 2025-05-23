using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of ISubscriptionTracker that manages subscription lifecycle and statistics.
    /// </summary>
    internal sealed class SubscriptionTracker : ISubscriptionTracker
    {
        private readonly IBurstLogger _logger;
        private readonly string _subscriberName;
        private readonly object _syncLock = new object();
        private readonly List<IDisposable> _activeSubscriptions = new List<IDisposable>();
        
        private long _totalSubscriptions;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the SubscriptionTracker class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="subscriberName">The name of the subscriber for logging purposes.</param>
        public SubscriptionTracker(IBurstLogger logger, string subscriberName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriberName = subscriberName ?? throw new ArgumentNullException(nameof(subscriberName));
        }

        /// <inheritdoc />
        public long TotalSubscriptions
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalSubscriptions;
                }
            }
        }

        /// <inheritdoc />
        public int ActiveSubscriptionCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _activeSubscriptions.Count;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable TrackSubscription<TKey>(IDisposable subscription, TKey key)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            lock (_syncLock)
            {
                _totalSubscriptions++;
                _activeSubscriptions.Add(subscription);
            }

            return new SubscriptionHandle<TKey>(subscription, () =>
            {
                lock (_syncLock)
                {
                    _activeSubscriptions.Remove(subscription);
                }
            }, key, _logger, _subscriberName);
        }

        /// <inheritdoc />
        public IDisposable TrackSubscription(IDisposable subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            lock (_syncLock)
            {
                _totalSubscriptions++;
                _activeSubscriptions.Add(subscription);
            }

            return new SubscriptionHandle(subscription, () =>
            {
                lock (_syncLock)
                {
                    _activeSubscriptions.Remove(subscription);
                }
            }, _logger, _subscriberName);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                if (_disposed)
                    return;

                _logger.Log(LogLevel.Debug,
                    $"Disposing {_subscriberName}. Total subscriptions: {_totalSubscriptions}, " +
                    $"Active subscriptions: {_activeSubscriptions.Count}",
                    "MessagePipeKeyedSubscriber");

                foreach (var subscription in _activeSubscriptions)
                {
                    try
                    {
                        subscription?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Warning,
                            $"Error disposing subscription: {ex.Message}",
                            "MessagePipeKeyedSubscriber");
                    }
                }

                _activeSubscriptions.Clear();
                _disposed = true;
            }
        }
    }
}