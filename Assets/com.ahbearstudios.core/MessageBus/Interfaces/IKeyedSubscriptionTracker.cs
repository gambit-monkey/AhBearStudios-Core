using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Tracks active subscriptions and provides statistics.
    /// </summary>
    public interface IKeyedSubscriptionTracker : ISubscriptionTracker
    {
        /// <summary>
        /// Tracks a keyed subscription and returns a handle for disposal.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="subscription">The subscription to track.</param>
        /// <param name="key">The key associated with the subscription.</param>
        /// <returns>A disposable handle for the tracked subscription.</returns>
        IDisposable TrackSubscription<TKey>(IDisposable subscription, TKey key);
    }
}