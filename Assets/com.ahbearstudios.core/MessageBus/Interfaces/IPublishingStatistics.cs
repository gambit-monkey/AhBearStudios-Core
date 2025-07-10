using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Tracks publishing statistics and provides metrics.
    /// </summary>
    public interface IPublishingStatistics : IDisposable
    {
        /// <summary>
        /// Gets the total number of messages published.
        /// </summary>
        long TotalMessagesPublished { get; }

        /// <summary>
        /// Gets the total number of async publishes initiated.
        /// </summary>
        long TotalAsyncPublishes { get; }

        /// <summary>
        /// Records a successful synchronous publish.
        /// </summary>
        void RecordSyncPublish();

        /// <summary>
        /// Records a successful asynchronous publish.
        /// </summary>
        void RecordAsyncPublish();
    }
}