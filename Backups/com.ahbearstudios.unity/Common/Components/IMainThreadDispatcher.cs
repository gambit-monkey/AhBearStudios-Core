using System;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Unity.Common.Components;

/// <summary>
/// Interface for dispatching actions to Unity's main thread.
/// Essential for thread-safe Unity API calls from background threads.
/// Provides frame-budget compliant execution with production monitoring.
/// </summary>
public interface IMainThreadDispatcher
{
    /// <summary>
    /// Gets whether the dispatcher is initialized and ready for use.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Gets the current number of pending actions in the queue.
    /// Used for monitoring and health checking.
    /// </summary>
    int PendingActionCount { get; }
    
    /// <summary>
    /// Gets whether the current thread is Unity's main thread.
    /// </summary>
    bool IsMainThread { get; }
    
    /// <summary>
    /// Gets the maximum queue capacity before backpressure kicks in.
    /// </summary>
    int MaxQueueCapacity { get; }
    
    /// <summary>
    /// Gets the current frame processing time budget in milliseconds.
    /// </summary>
    float FrameBudgetMs { get; }
    
    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// Thread-safe method that can be called from any thread.
    /// Returns false if queue is full and action was dropped.
    /// </summary>
    /// <param name="action">The action to execute on the main thread</param>
    /// <returns>True if enqueued successfully, false if dropped due to capacity</returns>
    bool TryEnqueue(Action action);
    
    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// Thread-safe method that throws exception if queue is full.
    /// </summary>
    /// <param name="action">The action to execute on the main thread</param>
    /// <exception cref="InvalidOperationException">Thrown when queue is at capacity</exception>
    /// <exception cref="ArgumentNullException">Thrown when action is null</exception>
    void Enqueue(Action action);
    
    /// <summary>
    /// Enqueues an action with a result callback.
    /// Useful for getting results back from main thread operations.
    /// </summary>
    /// <typeparam name="T">The type of result</typeparam>
    /// <param name="func">The function to execute on the main thread</param>
    /// <param name="callback">Callback to receive the result</param>
    /// <returns>True if enqueued successfully, false if dropped due to capacity</returns>
    bool TryEnqueue<T>(Func<T> func, Action<T> callback);
    
    /// <summary>
    /// Enqueues an action and returns a UniTask that completes when the action is executed.
    /// Allows async/await pattern for main thread dispatch.
    /// </summary>
    /// <param name="action">The action to execute on the main thread</param>
    /// <returns>UniTask that completes when action is executed</returns>
    UniTask EnqueueAsync(Action action);
    
    /// <summary>
    /// Enqueues a function and returns a UniTask with the result.
    /// Allows async/await pattern for main thread dispatch with return values.
    /// </summary>
    /// <typeparam name="T">The type of result</typeparam>
    /// <param name="func">The function to execute on the main thread</param>
    /// <returns>UniTask with the function result</returns>
    UniTask<T> EnqueueAsync<T>(Func<T> func);
    
    /// <summary>
    /// Forces immediate processing of all queued actions.
    /// Should only be used in special circumstances as it may exceed frame budget.
    /// </summary>
    /// <returns>Number of actions processed</returns>
    int ProcessAllImmediately();
    
    /// <summary>
    /// Clears all pending actions in the queue.
    /// Used for emergency cleanup or system shutdown.
    /// </summary>
    /// <returns>Number of actions that were cleared</returns>
    int ClearQueue();
}