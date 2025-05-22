using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Factories;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Handles dispatching actions to the main Unity thread from background threads.
    /// Ensures UI updates and Unity API calls happen on the correct thread.
    /// </summary>
    public class UnityThreadDispatcher : MonoBehaviour
    {
        private static UnityThreadDispatcher _instance;
        private static readonly object _instanceLock = new object();
        private readonly Queue<Action> _actionQueue = new Queue<Action>();
        private readonly object _queueLock = new object();
        private IBurstLogger _logger;
        private IProfiler _profiler;
        private int _mainThreadId;
        private bool _initialized;

        /// <summary>
        /// Gets the singleton instance of the UnityThreadDispatcher.
        /// </summary>
        public static UnityThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            // Try to find an existing instance
                            _instance = FindObjectOfType<UnityThreadDispatcher>();
                            
                            // If no instance exists, create one
                            if (_instance == null)
                            {
                                GameObject go = new GameObject("UnityThreadDispatcher");
                                _instance = go.AddComponent<UnityThreadDispatcher>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Checks if the current thread is the Unity main thread.
        /// </summary>
        public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// Enqueues an action to be executed on the main Unity thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // If we're already on the main thread, just execute the action
            if (IsMainThread)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error executing action on main thread: {ex.Message}");
                    }
                }
                
                return;
            }

            // Otherwise, queue it for execution in Update
            lock (_queueLock)
            {
                _actionQueue.Enqueue(action);
                
                if (_logger != null && _actionQueue.Count % 100 == 0)
                {
                    _logger.Debug($"Action queue size: {_actionQueue.Count}");
                }
            }
        }

        /// <summary>
        /// Asynchronously executes an action on the main Unity thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes when the action is executed.</returns>
        public Task EnqueueAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // If we're already on the main thread, just execute the action
            if (IsMainThread)
            {
                try
                {
                    action();
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error executing async action on main thread: {ex.Message}");
                    }
                    
                    return Task.FromException(ex);
                }
            }

            // Otherwise, create a TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();
            
            // Register cancellation if a token was provided
            if (cancellationToken != default)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled());
                
                // If already cancelled, return a cancelled task
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return tcs.Task;
                }
            }

            // Queue an action that executes the original action and completes the task
            Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error executing async action on main thread: {ex.Message}");
                    }
                    
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Asynchronously executes a function that returns a value on the main Unity thread.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes with the function's return value.</returns>
        public Task<T> EnqueueAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            // If we're already on the main thread, just execute the function
            if (IsMainThread)
            {
                try
                {
                    T result = func();
                    return Task.FromResult(result);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error executing async function on main thread: {ex.Message}");
                    }
                    
                    return Task.FromException<T>(ex);
                }
            }

            // Otherwise, create a TaskCompletionSource
            var tcs = new TaskCompletionSource<T>();
            
            // Register cancellation if a token was provided
            if (cancellationToken != default)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled());
                
                // If already cancelled, return a cancelled task
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return tcs.Task;
                }
            }

            // Queue an action that executes the original function and completes the task
            Enqueue(() =>
            {
                try
                {
                    T result = func();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error executing async function on main thread: {ex.Message}");
                    }
                    
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Executes all queued actions on the main thread.
        /// </summary>
        public void ExecuteAll()
        {
            using (_profiler?.BeginSample("UnityThreadDispatcher.ExecuteAll"))
            {
                // Ensure we're on the main thread
                if (!IsMainThread)
                {
                    if (_logger != null)
                    {
                        _logger.Warning("ExecuteAll called from a non-main thread");
                    }
                    
                    return;
                }

                // Process all queued actions
                List<Action> actionsToExecute = null;
                
                lock (_queueLock)
                {
                    if (_actionQueue.Count > 0)
                    {
                        actionsToExecute = new List<Action>(_actionQueue);
                        _actionQueue.Clear();
                    }
                }

                if (actionsToExecute != null)
                {
                    foreach (var action in actionsToExecute)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error executing queued action: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Get the logger and profiler
            _logger = BurstLoggerFactory.GetLoggerForContext("UnityThreadDispatcher");
            _profiler = ProfilerFactory.GetProfilerForContext("UnityThreadDispatcher");
            
            // Store the main thread ID
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            
            _initialized = true;
            
            if (_logger != null)
            {
                _logger.Info("UnityThreadDispatcher initialized");
            }
        }

        private void Update()
        {
            // Execute all queued actions
            ExecuteAll();
        }

        private void OnDestroy()
        {
            if (_logger != null)
            {
                _logger.Info("UnityThreadDispatcher being destroyed");
            }
            
            // Clear the instance reference if this is the singleton
            if (_instance == this)
            {
                _instance = null;
            }
            
            _logger = null;
            _profiler = null;
        }
    }

    /// <summary>
    /// Extension methods for the UnityThreadDispatcher.
    /// </summary>
    public static class UnityThreadDispatcherExtensions
    {
        /// <summary>
        /// Ensures an action is executed on the Unity main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void EnsureMainThread(this Action action)
        {
            UnityThreadDispatcher.Instance.Enqueue(action);
        }

        /// <summary>
        /// Asynchronously ensures an action is executed on the Unity main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes when the action is executed.</returns>
        public static Task EnsureMainThreadAsync(this Action action, CancellationToken cancellationToken = default)
        {
            return UnityThreadDispatcher.Instance.EnqueueAsync(action, cancellationToken);
        }

        /// <summary>
        /// Asynchronously ensures a function is executed on the Unity main thread.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes with the function's return value.</returns>
        public static Task<T> EnsureMainThreadAsync<T>(this Func<T> func, CancellationToken cancellationToken = default)
        {
            return UnityThreadDispatcher.Instance.EnqueueAsync(func, cancellationToken);
        }
    }
}