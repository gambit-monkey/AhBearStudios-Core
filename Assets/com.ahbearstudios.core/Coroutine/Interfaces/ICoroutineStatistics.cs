using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Coroutine.Interfaces
{
    /// <summary>
    /// Interface for coroutine usage statistics and performance metrics.
    /// </summary>
    public interface ICoroutineStatistics
    {
        /// <summary>
        /// Gets the total number of coroutines started since initialization.
        /// </summary>
        long TotalCoroutinesStarted { get; }

        /// <summary>
        /// Gets the total number of coroutines completed.
        /// </summary>
        long TotalCoroutinesCompleted { get; }

        /// <summary>
        /// Gets the total number of coroutines cancelled.
        /// </summary>
        long TotalCoroutinesCancelled { get; }

        /// <summary>
        /// Gets the current number of active coroutines.
        /// </summary>
        int CurrentActiveCoroutines { get; }

        /// <summary>
        /// Gets the peak number of simultaneously active coroutines.
        /// </summary>
        int PeakActiveCoroutines { get; }

        /// <summary>
        /// Gets the average duration of completed coroutines.
        /// </summary>
        TimeSpan AverageCoroutineDuration { get; }

        /// <summary>
        /// Gets coroutine counts by tag.
        /// </summary>
        /// <returns>A dictionary mapping tags to their coroutine counts.</returns>
        IReadOnlyDictionary<string, int> GetCoroutineCountsByTag();

        /// <summary>
        /// Resets all statistics.
        /// </summary>
        void Reset();
    }
}