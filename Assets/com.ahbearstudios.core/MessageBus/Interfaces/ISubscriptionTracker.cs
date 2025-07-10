using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Tracks active subscriptions and provides statistics.
    /// </summary>
    public interface ISubscriptionTracker : IDisposable
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
        /// Tracks a global subscription and returns a handle for disposal.
        /// </summary>
        /// <param name="subscription">The subscription to track.</param>
        /// <param name="isFiltered">Whether this is a filtered subscription.</param>
        /// <returns>A disposable handle for the tracked subscription.</returns>
        IDisposable TrackSubscription(IDisposable subscription, bool isFiltered = false);
    }
}