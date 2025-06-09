using System;
using Cysharp.Threading.Tasks;


namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Handles wrapping publisher operations with profiling, logging, and error handling.
    /// </summary>
    internal interface IKeyedPublisherWrapper
    {
        /// <summary>
        /// Wraps a synchronous publish operation with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="key">The key for the message.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="publish">Function to perform the actual publish operation.</param>
        /// <param name="statistics">The statistics tracker to use.</param>
        void WrapSyncPublish<TKey, TMessage>(
            TKey key,
            TMessage message,
            Action<TKey, TMessage> publish,
            IPublishingStatistics statistics);

        /// <summary>
        /// Wraps an asynchronous publish operation with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="key">The key for the message.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="publishAsync">Function to perform the actual async publish operation.</param>
        /// <param name="statistics">The statistics tracker to use.</param>
        /// <returns>A disposable handle for the async operation.</returns>
        IDisposable WrapAsyncPublish<TKey, TMessage>(
            TKey key,
            TMessage message,
            Func<TKey, TMessage, UniTask> publishAsync,
            IPublishingStatistics statistics);
    }
}