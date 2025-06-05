using System;

namespace AhBearStudios.Core.Coroutine.Interfaces
{
    /// <summary>
    /// Handle for managing individual coroutines.
    /// Provides information about the coroutine's state and allows for cancellation.
    /// </summary>
    public interface ICoroutineHandle : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this coroutine.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the tag associated with this coroutine, if any.
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// Gets whether this coroutine is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets whether this coroutine has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets whether this coroutine was cancelled.
        /// </summary>
        bool IsCancelled { get; }

        /// <summary>
        /// Gets the time when this coroutine was started.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Gets the duration this coroutine has been running.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Stops the coroutine if it's still running.
        /// </summary>
        /// <returns>True if the coroutine was stopped, false if it wasn't running.</returns>
        bool Stop();

        /// <summary>
        /// Event fired when the coroutine completes or is cancelled.
        /// </summary>
        event Action<ICoroutineHandle> OnCompleted;
    }
}