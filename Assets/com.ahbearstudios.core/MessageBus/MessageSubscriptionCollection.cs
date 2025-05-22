using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// A collection of message subscriptions that can be disposed as a group.
    /// </summary>
    public sealed class MessageSubscriptionCollection : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _isDisposed;
        
        /// <summary>
        /// Adds a subscription to the collection.
        /// </summary>
        /// <param name="subscription">The subscription to add.</param>
        public void Add(IDisposable subscription)
        {
            if (_isDisposed)
            {
                subscription.Dispose();
                throw new ObjectDisposedException(nameof(MessageSubscriptionCollection));
            }
            
            if (subscription == null) throw new ArgumentNullException(nameof(subscription));
            _subscriptions.Add(subscription);
        }
        
        /// <summary>
        /// Removes and disposes a subscription from the collection.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <returns>True if the subscription was removed; otherwise, false.</returns>
        public bool Remove(IDisposable subscription)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(MessageSubscriptionCollection));
            if (subscription == null) throw new ArgumentNullException(nameof(subscription));
            
            if (_subscriptions.Remove(subscription))
            {
                subscription.Dispose();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the number of subscriptions in the collection.
        /// </summary>
        public int Count => _subscriptions.Count;
        
        /// <summary>
        /// Clears and disposes all subscriptions in the collection.
        /// </summary>
        public void Clear()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(MessageSubscriptionCollection));
            
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            
            _subscriptions.Clear();
        }
        
        /// <summary>
        /// Disposes all subscriptions in the collection.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            
            _subscriptions.Clear();
            _isDisposed = true;
        }
    }
}