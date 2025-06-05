using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Coroutine.Interfaces
{
    /// <summary>
    /// Interface for coroutine execution services.
    /// Provides a unified API for starting, stopping, and managing coroutines across all systems.
    /// </summary>
    public interface ICoroutineRunner
    {
        /// <summary>
        /// Gets whether this coroutine runner is active and can execute coroutines.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the number of currently active coroutines.
        /// </summary>
        int ActiveCoroutineCount { get; }

        /// <summary>
        /// Gets the total number of coroutines started since initialization.
        /// </summary>
        long TotalCoroutinesStarted { get; }

        /// <summary>
        /// Starts a coroutine and returns a handle for managing it.
        /// </summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartCoroutine(IEnumerator<> routine, string tag = null);

        /// <summary>
        /// Starts a coroutine with a callback when it completes.
        /// </summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="onComplete">Callback invoked when the coroutine completes.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartCoroutine(IEnumerator routine, Action onComplete, string tag = null);

        /// <summary>
        /// Starts a coroutine with error handling.
        /// </summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="onError">Callback invoked if the coroutine throws an exception.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartCoroutine(IEnumerator routine, Action<Exception> onError, string tag = null);

        /// <summary>
        /// Starts a coroutine with both completion and error callbacks.
        /// </summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="onComplete">Callback invoked when the coroutine completes successfully.</param>
        /// <param name="onError">Callback invoked if the coroutine throws an exception.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartCoroutine(IEnumerator routine, Action onComplete, Action<Exception> onError, string tag = null);

        /// <summary>
        /// Stops a coroutine using its handle.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to stop.</param>
        /// <returns>True if the coroutine was stopped, false if it wasn't found or already completed.</returns>
        bool StopCoroutine(ICoroutineHandle handle);

        /// <summary>
        /// Stops all coroutines with the specified tag.
        /// </summary>
        /// <param name="tag">The tag of coroutines to stop.</param>
        /// <returns>The number of coroutines that were stopped.</returns>
        int StopCoroutinesByTag(string tag);

        /// <summary>
        /// Stops all currently running coroutines.
        /// </summary>
        /// <returns>The number of coroutines that were stopped.</returns>
        int StopAllCoroutines();

        /// <summary>
        /// Checks if a coroutine is still running.
        /// </summary>
        /// <param name="handle">The handle of the coroutine to check.</param>
        /// <returns>True if the coroutine is still running, false otherwise.</returns>
        bool IsCoroutineRunning(ICoroutineHandle handle);

        /// <summary>
        /// Gets the number of active coroutines with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to count coroutines for.</param>
        /// <returns>The number of active coroutines with the specified tag.</returns>
        int GetActiveCoroutineCount(string tag);

        /// <summary>
        /// Starts a delayed action as a coroutine.
        /// </summary>
        /// <param name="delay">The delay in seconds before executing the action.</param>
        /// <param name="action">The action to execute after the delay.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartDelayedAction(float delay, Action action, string tag = null);

        /// <summary>
        /// Starts a repeating action as a coroutine.
        /// </summary>
        /// <param name="interval">The interval in seconds between executions.</param>
        /// <param name="action">The action to execute repeatedly.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartRepeatingAction(float interval, Action action, string tag = null);

        /// <summary>
        /// Starts a conditional action that executes when a condition becomes true.
        /// </summary>
        /// <param name="condition">The condition to wait for.</param>
        /// <param name="action">The action to execute when the condition is met.</param>
        /// <param name="checkInterval">How often to check the condition (in seconds).</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <returns>A handle that can be used to stop the coroutine.</returns>
        ICoroutineHandle StartConditionalAction(Func<bool> condition, Action action, float checkInterval = 0f, string tag = null);
    }
}