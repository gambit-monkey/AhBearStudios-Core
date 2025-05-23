using System;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Handles wrapping simple (non-keyed) publisher operations with profiling, logging, and error handling.
    /// </summary>
    public interface ISimplePublisherWrapper
    {
        /// <summary>
        /// Wraps a synchronous publish operation with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="publish">Function to perform the actual publish operation.</param>
        /// <param name="statistics">The statistics tracker to use.</param>
        void WrapSyncPublish<TMessage>(
            TMessage message,
            Action<TMessage> publish,
            IPublishingStatistics statistics);

        /// <summary>
        /// Wraps an asynchronous publish operation with profiling, logging, and error handling.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="publishAsync">Function to perform the actual async publish operation.</param>
        /// <param name="statistics">The statistics tracker to use.</param>
        /// <returns>A disposable handle for the async operation.</returns>
        IDisposable WrapAsyncPublish<TMessage>(
            TMessage message,
            Func<TMessage, UniTask> publishAsync,
            IPublishingStatistics statistics);
    }
}