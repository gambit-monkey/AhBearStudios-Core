using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Handles wrapping message handlers with profiling, logging, and error handling.
    /// </summary>
    internal interface IMessageHandlerWrapper
    {
        /// <summary>
        /// Gets the total number of messages received by wrapped handlers.
        /// </summary>
        long TotalMessagesReceived { get; }
        
        /// <summary>
        /// Wraps a keyed subscription with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="key">The key for the subscription.</param>
        /// <param name="handler">The message handler to wrap.</param>
        /// <param name="subscribe">Function to create the underlying subscription.</param>
        /// <param name="tracker">The subscription tracker to use.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable WrapKeyedSubscription<TKey, TMessage>(
            TKey key,
            Action<TMessage> handler,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker);
            
        /// <summary>
        /// Wraps a global subscription with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handler">The message handler to wrap.</param>
        /// <param name="subscribe">Function to create the underlying subscription.</param>
        /// <param name="tracker">The subscription tracker to use.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable WrapGlobalSubscription<TKey, TMessage>(
            Action<TKey, TMessage> handler,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker);
            
        /// <summary>
        /// Wraps a filtered subscription with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="key">The key for the subscription.</param>
        /// <param name="handler">The message handler to wrap.</param>
        /// <param name="filter">The message filter to apply.</param>
        /// <param name="subscribe">Function to create the underlying subscription.</param>
        /// <param name="tracker">The subscription tracker to use.</param>
        /// <returns>A disposable subscription handle.</returns>
        IDisposable WrapFilteredSubscription<TKey, TMessage>(
            TKey key,
            Action<TMessage> handler,
            Func<TMessage, bool> filter,
            Func<Action<TMessage>, IDisposable> subscribe,
            ISubscriptionTracker tracker);
    }
}