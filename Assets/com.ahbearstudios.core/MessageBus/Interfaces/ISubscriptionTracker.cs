using System;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Tracks active subscriptions and provides statistics.
    /// </summary>
    internal interface ISubscriptionTracker : IDisposable
    {
        /// <summary>
        /// Gets the total number of subscriptions created.
        /// </summary>
        long TotalSubscriptions { get; }
        
        /// <summary>
        /// Gets the number of currently active subscriptions.
        /// </summary>
        int ActiveSubscriptionCount { get; }
        
        /// <summary>
        /// Tracks a keyed subscription and returns a handle for disposal.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="subscription">The subscription to track.</param>
        /// <param name="key">The key associated with the subscription.</param>
        /// <returns>A disposable handle for the tracked subscription.</returns>
        IDisposable TrackSubscription<TKey>(IDisposable subscription, TKey key);
        
        /// <summary>
        /// Tracks a global subscription and returns a handle for disposal.
        /// </summary>
        /// <param name="subscription">The subscription to track.</param>
        /// <returns>A disposable handle for the tracked subscription.</returns>
        IDisposable TrackSubscription(IDisposable subscription);
    }
}