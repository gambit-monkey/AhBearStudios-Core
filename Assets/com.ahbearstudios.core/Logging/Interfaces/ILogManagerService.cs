using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging.Adapters;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Interfaces
{
   /// <summary>
    /// Service interface for managing the logging system lifecycle and configuration.
    /// Provides abstraction for dependency injection and testing.
    /// </summary>
    public interface ILogManagerService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the number of configured log targets.
        /// </summary>
        int TargetCount { get; }

        /// <summary>
        /// Gets the number of messages currently queued for processing.
        /// </summary>
        int QueuedMessageCount { get; }

        /// <summary>
        /// Gets or sets the global minimum logging level.
        /// </summary>
        LogLevel GlobalMinimumLevel { get; set; }

        /// <summary>
        /// Gets the underlying job logger manager.
        /// </summary>
        JobLoggerManager LoggerManager { get; }

        /// <summary>
        /// Gets the Unity logger adapter.
        /// </summary>
        UnityLoggerAdapter UnityLoggerAdapter { get; }

        /// <summary>
        /// Gets the burst-compatible logger adapter.
        /// </summary>
        IBurstLogger BurstLoggerAdapter { get; }

        /// <summary>
        /// Gets the total number of messages processed.
        /// </summary>
        int TotalMessagesProcessed { get; }

        /// <summary>
        /// Gets the number of flush operations performed.
        /// </summary>
        int FlushCount { get; }

        /// <summary>
        /// Initializes the logging service asynchronously.
        /// </summary>
        /// <param name="config">The logging configuration.</param>
        /// <returns>A task representing the initialization.</returns>
        Task InitializeAsync(LogManagerConfig config);

        /// <summary>
        /// Flushes all pending log messages.
        /// </summary>
        /// <returns>The number of messages flushed.</returns>
        int Flush();

        /// <summary>
        /// Adds a log target to the system.
        /// </summary>
        /// <param name="target">The target to add.</param>
        void AddTarget(ILogTarget target);

        /// <summary>
        /// Removes a log target from the system.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns>True if removed successfully.</returns>
        bool RemoveTarget(ILogTarget target);

        /// <summary>
        /// Creates a job logger with specified parameters.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <param name="defaultTag">The default tag.</param>
        /// <returns>A new job logger.</returns>
        JobLogger CreateJobLogger(LogLevel minimumLevel, Tagging.LogTag defaultTag);

        /// <summary>
        /// Gets performance metrics for the logging system.
        /// </summary>
        /// <returns>Performance metrics dictionary.</returns>
        Dictionary<string, object> GetPerformanceMetrics();

        /// <summary>
        /// Resets performance metrics.
        /// </summary>
        void ResetPerformanceMetrics();
    }
}