using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Interface for log message batching services.
    /// Provides standardized log batching capabilities with support for high-performance scenarios and Unity.Collections.
    /// </summary>
    public interface ILogBatchingService : IDisposable
    {
        /// <summary>
        /// Gets the maximum number of messages to queue before forcing a flush.
        /// </summary>
        int MaxQueueSize { get; }

        /// <summary>
        /// Gets the interval at which batched messages are flushed.
        /// </summary>
        TimeSpan FlushInterval { get; }

        /// <summary>
        /// Gets whether high-performance mode is enabled for zero-allocation logging.
        /// </summary>
        bool HighPerformanceMode { get; }

        /// <summary>
        /// Gets whether Burst compilation compatibility is enabled.
        /// </summary>
        bool BurstCompatibility { get; }

        /// <summary>
        /// Gets the registered log targets for batch processing.
        /// </summary>
        IReadOnlyList<ILogTarget> Targets { get; }

        /// <summary>
        /// Gets the current queue size.
        /// </summary>
        int QueueSize { get; }

        /// <summary>
        /// Gets whether the service is currently processing messages.
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Gets batching performance metrics.
        /// </summary>
        BatchingMetrics Metrics { get; }

        /// <summary>
        /// Event raised when a batch is processed.
        /// </summary>
        event EventHandler<BatchProcessedEventArgs> BatchProcessed;

        /// <summary>
        /// Event raised when the queue reaches capacity.
        /// </summary>
        event EventHandler<QueueCapacityEventArgs> QueueCapacityReached;

        /// <summary>
        /// Enqueues a log message for batch processing using native collections.
        /// </summary>
        /// <param name="logMessage">The log message to enqueue</param>
        /// <returns>True if the message was enqueued successfully, false if the queue is full</returns>
        bool EnqueueMessage(in LogMessage logMessage);

        /// <summary>
        /// Enqueues multiple log messages for batch processing.
        /// </summary>
        /// <param name="logMessages">The log messages to enqueue</param>
        /// <returns>The number of messages successfully enqueued</returns>
        int EnqueueMessages(IReadOnlyList<LogMessage> logMessages);

        /// <summary>
        /// Enqueues messages from a native array for Burst compatibility.
        /// </summary>
        /// <param name="logMessages">The native array of log messages</param>
        /// <returns>The number of messages successfully enqueued</returns>
        int EnqueueMessages(NativeArray<LogMessage> logMessages);

        /// <summary>
        /// Enqueues messages from a native array of NativeLogMessage for optimal Burst compatibility.
        /// </summary>
        /// <param name="nativeLogMessages">The native array of native log messages</param>
        /// <returns>The number of messages successfully enqueued</returns>
        int EnqueueNativeMessages(NativeArray<NativeLogMessage> nativeLogMessages);

        /// <summary>
        /// Enqueues a native log message directly for optimal performance.
        /// </summary>
        /// <param name="nativeLogMessage">The native log message to enqueue</param>
        /// <returns>True if the message was enqueued successfully, false if the queue is full</returns>
        bool EnqueueNativeMessage(in NativeLogMessage nativeLogMessage);

        /// <summary>
        /// Forces an immediate flush of all queued messages.
        /// </summary>
        void ForceFlush();

        /// <summary>
        /// Flushes all queued messages asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous flush operation</returns>
        Task FlushAsync();

        /// <summary>
        /// Gets the current batching performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        BatchingMetrics GetMetrics();

        /// <summary>
        /// Resets the batching performance metrics.
        /// </summary>
        void ResetMetrics();

        /// <summary>
        /// Updates the target list for batch processing.
        /// </summary>
        /// <param name="newTargets">The new list of targets</param>
        void UpdateTargets(IReadOnlyList<ILogTarget> newTargets);
    }
}