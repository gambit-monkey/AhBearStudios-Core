using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Coroutine.Interfaces
{
    /// <summary>
    /// Interface for managing multiple coroutine runners and providing system-wide coroutine services.
    /// </summary>
    public interface ICoroutineManager : IDisposable
    {
        /// <summary>
        /// Gets the default coroutine runner for general use.
        /// </summary>
        ICoroutineRunner DefaultRunner { get; }

        /// <summary>
        /// Gets whether the coroutine manager is initialized and active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the total number of active coroutines across all runners.
        /// </summary>
        int TotalActiveCoroutines { get; }

        /// <summary>
        /// Gets statistics about coroutine usage.
        /// </summary>
        ICoroutineStatistics Statistics { get; }

        /// <summary>
        /// Creates a new coroutine runner with the specified name.
        /// </summary>
        /// <param name="name">The name for the new runner.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <returns>The created coroutine runner.</returns>
        ICoroutineRunner CreateRunner(string name, bool persistent = false);

        /// <summary>
        /// Gets a coroutine runner by name.
        /// </summary>
        /// <param name="name">The name of the runner to get.</param>
        /// <returns>The coroutine runner with the specified name, or null if not found.</returns>
        ICoroutineRunner GetRunner(string name);

        /// <summary>
        /// Removes and disposes a coroutine runner.
        /// </summary>
        /// <param name="name">The name of the runner to remove.</param>
        /// <returns>True if the runner was removed, false if it wasn't found.</returns>
        bool RemoveRunner(string name);

        /// <summary>
        /// Gets all coroutine runner names.
        /// </summary>
        /// <returns>A collection of all runner names.</returns>
        IReadOnlyCollection<string> GetRunnerNames();

        /// <summary>
        /// Stops all coroutines across all runners.
        /// </summary>
        /// <returns>The total number of coroutines that were stopped.</returns>
        int StopAllCoroutines();

        /// <summary>
        /// Pauses all coroutines across all runners.
        /// </summary>
        void PauseAllCoroutines();

        /// <summary>
        /// Resumes all paused coroutines across all runners.
        /// </summary>
        void ResumeAllCoroutines();

        /// <summary>
        /// Event fired when a new coroutine runner is created.
        /// </summary>
        event Action<string, ICoroutineRunner> OnRunnerCreated;

        /// <summary>
        /// Event fired when a coroutine runner is removed.
        /// </summary>
        event Action<string> OnRunnerRemoved;
    }
}