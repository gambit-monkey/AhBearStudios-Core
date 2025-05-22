using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Manages a collection of subscriptions
    /// </summary>
    public interface ISubscriptionManager : IDisposable
    {
        /// <summary>
        /// Adds a subscription to the manager
        /// </summary>
        void AddSubscription(ISubscriptionToken subscription);
    
        /// <summary>
        /// Removes a subscription from the manager
        /// </summary>
        bool RemoveSubscription(ISubscriptionToken subscription);
    
        /// <summary>
        /// Removes all subscriptions
        /// </summary>
        void ClearSubscriptions();
    
        /// <summary>
        /// Gets the number of active subscriptions
        /// </summary>
        int SubscriptionCount { get; }
    }
}