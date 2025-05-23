using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Handles wrapping simple (non-keyed) subscription operations with profiling, logging, and error handling.
    /// </summary>
    public interface ISimpleSubscriptionWrapper
    {
        /// <summary>
        /// Gets the total number of messages received by wrapped subscriptions.
        /// </summary>
        long TotalMessagesReceived { get; }
        
        /// <summary>
        /// Gets the total number of messages filtered out.
        /// </summary>
        long TotalMessagesFiltered { get; }
        
        /// <summary>
        /// Wraps a simple subscription with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The message handler to wrap.</param>
        /// <param name="subscribe">Function to create the underlying subscription.</param>
        /// <param name="tracker">The subscription tracker to use.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable WrapSubscription<TMessage>(
            Action<TMessage> handler,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker);
            
        /// <summary>
        /// Wraps a filtered subscription with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The message handler to wrap.</param>
        /// <param name="filter">The message filter to apply.</param>
        /// <param name="subscribe">Function to create the underlying subscription.</param>
        /// <param name="tracker">The subscription tracker to use.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable WrapFilteredSubscription<TMessage>(
            Action<TMessage> handler,
            Func<TMessage, bool> filter,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker);
    }
}