using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Manager for handling multiple message subscriptions.
    /// Provides thread-safety and proper resource cleanup.
    /// </summary>
    public class SubscriptionManager : IDisposable
    {
        private readonly List<ISubscriptionToken> _subscriptions;
        private readonly object _subscriptionsLock = new object();
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the SubscriptionManager class.
        /// </summary>
        /// <param name="logger">Optional logger for subscription operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public SubscriptionManager(IBurstLogger logger = null, IProfiler profiler = null)
        {
            _subscriptions = new List<ISubscriptionToken>();
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Debug("SubscriptionManager initialized");
            }
        }

        /// <summary>
        /// Gets the number of active subscriptions.
        /// </summary>
        public int SubscriptionCount 
        {
            get 
            {
                lock (_subscriptionsLock)
                {
                    return _subscriptions.Count;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether any subscriptions are managed.
        /// </summary>
        public bool HasSubscriptions
        {
            get
            {
                lock (_subscriptionsLock)
                {
                    return _subscriptions.Count > 0;
                }
            }
        }

        /// <summary>
        /// Adds a subscription token to be managed.
        /// </summary>
        /// <param name="token">The subscription token to manage.</param>
        /// <returns>The same subscription token for chaining.</returns>
        public ISubscriptionToken AddSubscription(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("SubscriptionManager.AddSubscription"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

                lock (_subscriptionsLock)
                {
                    _subscriptions.Add(token);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Added subscription, total: {_subscriptions.Count}");
                    }
                }

                return token;
            }
        }

        /// <summary>
        /// Adds multiple subscription tokens to be managed.
        /// </summary>
        /// <param name="tokens">The subscription tokens to manage.</param>
        public void AddSubscriptions(IEnumerable<ISubscriptionToken> tokens)
        {
            using (_profiler?.BeginSample("SubscriptionManager.AddSubscriptions"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                if (tokens == null)
                {
                    throw new ArgumentNullException(nameof(tokens));
                }

                lock (_subscriptionsLock)
                {
                    foreach (var token in tokens)
                    {
                        if (token != null)
                        {
                            _subscriptions.Add(token);
                        }
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Added multiple subscriptions, total: {_subscriptions.Count}");
                    }
                }
            }
        }

        /// <summary>
        /// Removes a specific subscription token from management.
        /// </summary>
        /// <param name="token">The subscription token to remove.</param>
        /// <returns>True if the token was found and removed, false otherwise.</returns>
        public bool RemoveSubscription(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("SubscriptionManager.RemoveSubscription"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

                bool removed;
                lock (_subscriptionsLock)
                {
                    removed = _subscriptions.Remove(token);
                    
                    if (removed && _logger != null)
                    {
                        _logger.Debug($"Removed subscription, remaining: {_subscriptions.Count}");
                    }
                }

                return removed;
            }
        }

        /// <summary>
        /// Removes and disposes a specific subscription token.
        /// </summary>
        /// <param name="token">The subscription token to remove and dispose.</param>
        /// <returns>True if the token was found, removed, and disposed, false otherwise.</returns>
        public bool RemoveAndDisposeSubscription(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("SubscriptionManager.RemoveAndDisposeSubscription"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

                bool removed;
                
                lock (_subscriptionsLock)
                {
                    removed = _subscriptions.Remove(token);
                }
                
                if (removed)
                {
                    try
                    {
                        token.Dispose();
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Removed and disposed subscription, remaining: {_subscriptions.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error disposing token: {ex.Message}");
                        }
                        
                        // Still return true since we did remove it
                    }
                }

                return removed;
            }
        }

        /// <summary>
        /// Removes all inactive subscription tokens.
        /// </summary>
        /// <returns>The number of tokens removed.</returns>
        public int RemoveInactiveSubscriptions()
        {
            using (_profiler?.BeginSample("SubscriptionManager.RemoveInactiveSubscriptions"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                List<ISubscriptionToken> inactiveTokens;
                
                lock (_subscriptionsLock)
                {
                    inactiveTokens = _subscriptions.Where(token => !token.IsActive).ToList();
                    
                    foreach (var token in inactiveTokens)
                    {
                        _subscriptions.Remove(token);
                    }
                }
                
                if (_logger != null && inactiveTokens.Count > 0)
                {
                    _logger.Debug($"Removed {inactiveTokens.Count} inactive subscriptions, remaining: {_subscriptions.Count}");
                }

                return inactiveTokens.Count;
            }
        }

        /// <summary>
        /// Clears all subscriptions, disposing each one.
        /// </summary>
        public void ClearSubscriptions()
        {
            using (_profiler?.BeginSample("SubscriptionManager.ClearSubscriptions"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                List<ISubscriptionToken> tokensToClear;
                
                lock (_subscriptionsLock)
                {
                    // Take a copy of the subscriptions to dispose outside the lock
                    tokensToClear = new List<ISubscriptionToken>(_subscriptions);
                    _subscriptions.Clear();
                }

                // Dispose all the tokens
                int disposedCount = 0;
                foreach (var token in tokensToClear)
                {
                    try
                    {
                        token.Dispose();
                        disposedCount++;
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error disposing token: {ex.Message}");
                        }
                    }
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Cleared all subscriptions, disposed {disposedCount} tokens");
                }
            }
        }

        /// <summary>
        /// Create a snapshot of all current subscriptions.
        /// </summary>
        /// <returns>A list containing all current subscription tokens.</returns>
        public List<ISubscriptionToken> GetAllSubscriptions()
        {
            using (_profiler?.BeginSample("SubscriptionManager.GetAllSubscriptions"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                lock (_subscriptionsLock)
                {
                    return new List<ISubscriptionToken>(_subscriptions);
                }
            }
        }

        /// <summary>
        /// Waits for all active subscriptions to become inactive or a timeout to occur.
        /// </summary>
        /// <param name="timeout">The maximum time to wait, or null to wait indefinitely.</param>
        /// <returns>True if all subscriptions became inactive, false if timed out.</returns>
        public bool WaitForInactiveSubscriptions(TimeSpan? timeout = null)
        {
            using (_profiler?.BeginSample("SubscriptionManager.WaitForInactiveSubscriptions"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                var startTime = DateTime.UtcNow;
                var endTime = timeout.HasValue ? startTime + timeout.Value : DateTime.MaxValue;

                while (DateTime.UtcNow < endTime)
                {
                    bool anyActive;
                    
                    lock (_subscriptionsLock)
                    {
                        anyActive = _subscriptions.Any(token => token.IsActive);
                    }

                    if (!anyActive)
                    {
                        if (_logger != null)
                        {
                            _logger.Debug("All subscriptions inactive");
                        }
                        
                        return true;
                    }

                    // Small sleep to avoid tight loop
                    Thread.Sleep(10);
                }

                if (_logger != null)
                {
                    _logger.Warning("Timed out waiting for inactive subscriptions");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Asynchronously waits for all active subscriptions to become inactive or a timeout to occur.
        /// </summary>
        /// <param name="timeout">The maximum time to wait, or null to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the wait.</param>
        /// <returns>True if all subscriptions became inactive, false if timed out or cancelled.</returns>
        public async Task<bool> WaitForInactiveSubscriptionsAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("SubscriptionManager.WaitForInactiveSubscriptionsAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SubscriptionManager));
                }

                var startTime = DateTime.UtcNow;
                var endTime = timeout.HasValue ? startTime + timeout.Value : DateTime.MaxValue;

                while (DateTime.UtcNow < endTime)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    bool anyActive;
                    
                    lock (_subscriptionsLock)
                    {
                        anyActive = _subscriptions.Any(token => token.IsActive);
                    }

                    if (!anyActive)
                    {
                        if (_logger != null)
                        {
                            _logger.Debug("All subscriptions inactive");
                        }
                        
                        return true;
                    }

                    // Small delay to avoid tight loop
                    await Task.Delay(10, cancellationToken);
                }

                if (_logger != null)
                {
                    _logger.Warning("Timed out waiting for inactive subscriptions");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Disposes the subscription manager and all managed subscriptions.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("SubscriptionManager.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the subscription manager.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                ClearSubscriptions();
            }

            _isDisposed = true;
            
            if (_logger != null)
            {
                _logger.Debug("SubscriptionManager disposed");
            }
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~SubscriptionManager()
        {
            Dispose(false);
        }
    }
}