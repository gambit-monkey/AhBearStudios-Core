using System;
using System.Collections;
using System.Collections.Generic;
using AhBearStudios.Core.Coroutine.Handles;
using AhBearStudios.Core.Coroutine.Interfaces;
using UnityEngine;
using Unity.Collections;

namespace AhBearStudios.Core.Coroutine
{
    /// <summary>
    /// Unity MonoBehaviour-based coroutine runner with advanced management features.
    /// Provides high-performance coroutine execution with statistics tracking and error handling.
    /// </summary>
    public sealed class UnityCoroutineRunner : MonoBehaviour, ICoroutineRunner, IDisposable
    {
        #region Private Fields

        private readonly Dictionary<int, CoroutineHandle> _activeCoroutines = new Dictionary<int, CoroutineHandle>();
        private readonly Dictionary<string, List<int>> _coroutinesByTag = new Dictionary<string, List<int>>();
        private readonly CoroutineStatistics _statistics = new CoroutineStatistics();
        
        private NativeParallelHashMap<int, byte> _activeCoroutineIds;
        private int _nextCoroutineId;
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isPaused;
        private string _runnerName;

        #endregion

        #region Properties

        /// <inheritdoc />
        public bool IsActive => _isInitialized && !_isDisposed && !_isPaused;

        /// <inheritdoc />
        public int ActiveCoroutineCount => _activeCoroutines.Count;

        /// <inheritdoc />
        public long TotalCoroutinesStarted => _statistics.TotalCoroutinesStarted;

        /// <summary>
        /// Gets the name of this coroutine runner.
        /// </summary>
        public string RunnerName => _runnerName ?? gameObject.name;

        /// <summary>
        /// Gets the statistics for this runner.
        /// </summary>
        public ICoroutineStatistics Statistics => _statistics;

        #endregion

        #region Initialization

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the coroutine runner.
        /// </summary>
        /// <param name="runnerName">Optional name for this runner instance.</param>
        public void Initialize(string runnerName = null)
        {
            if (_isInitialized)
                return;

            _runnerName = runnerName;
            _activeCoroutineIds = new NativeParallelHashMap<int, byte>(64, Allocator.Persistent);
            _nextCoroutineId = 1;
            _isInitialized = true;
            _isDisposed = false;
            _isPaused = false;

            Debug.Log($"[{RunnerName}] Coroutine runner initialized");
        }

        #endregion

        #region MonoBehaviour Lifecycle

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region ICoroutineRunner Implementation

        /// <inheritdoc />
        public ICoroutineHandle StartCoroutine(IEnumerator routine, string tag = null)
        {
            return StartCoroutineInternal(routine, null, null, tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartCoroutine(IEnumerator routine, Action onComplete, string tag = null)
        {
            return StartCoroutineInternal(routine, onComplete, null, tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartCoroutine(IEnumerator routine, Action<Exception> onError, string tag = null)
        {
            return StartCoroutineInternal(routine, null, onError, tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartCoroutine(IEnumerator routine, Action onComplete, Action<Exception> onError, string tag = null)
        {
            return StartCoroutineInternal(routine, onComplete, onError, tag);
        }

        /// <inheritdoc />
        public bool StopCoroutine(ICoroutineHandle handle)
        {
            if (handle == null || !IsActive)
                return false;

            if (handle is CoroutineHandle coroutineHandle && _activeCoroutines.TryGetValue(handle.Id, out var existingHandle))
            {
                if (existingHandle == coroutineHandle)
                {
                    StopCoroutineInternal(coroutineHandle, true);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public int StopCoroutinesByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || !IsActive)
                return 0;

            if (!_coroutinesByTag.TryGetValue(tag, out var coroutineIds))
                return 0;

            int stoppedCount = 0;
            var idsToStop = new List<int>(coroutineIds);

            foreach (int id in idsToStop)
            {
                if (_activeCoroutines.TryGetValue(id, out var handle))
                {
                    StopCoroutineInternal(handle, true);
                    stoppedCount++;
                }
            }

            Debug.Log($"[{RunnerName}] Stopped {stoppedCount} coroutines with tag '{tag}'");
            return stoppedCount;
        }

        /// <inheritdoc />
        public int StopAllCoroutines()
        {
            if (!IsActive)
                return 0;

            int stoppedCount = _activeCoroutines.Count;
            var handlesToStop = new List<CoroutineHandle>(_activeCoroutines.Values);

            foreach (var handle in handlesToStop)
            {
                StopCoroutineInternal(handle, true);
            }

            Debug.Log($"[{RunnerName}] Stopped all {stoppedCount} active coroutines");
            return stoppedCount;
        }

        /// <inheritdoc />
        public bool IsCoroutineRunning(ICoroutineHandle handle)
        {
            return handle != null && handle.IsRunning && _activeCoroutines.ContainsKey(handle.Id);
        }

        /// <inheritdoc />
        public int GetActiveCoroutineCount(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return 0;

            return _coroutinesByTag.TryGetValue(tag, out var coroutineIds) ? coroutineIds.Count : 0;
        }

        /// <inheritdoc />
        public ICoroutineHandle StartDelayedAction(float delay, Action action, string tag = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (delay < 0f)
                throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative");

            return StartCoroutine(DelayedActionCoroutine(delay, action), tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartRepeatingAction(float interval, Action action, string tag = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (interval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive");

            return StartCoroutine(RepeatingActionCoroutine(interval, action), tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartConditionalAction(Func<bool> condition, Action action, float checkInterval = 0f, string tag = null)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (checkInterval < 0f)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), "Check interval cannot be negative");

            return StartCoroutine(ConditionalActionCoroutine(condition, action, checkInterval), tag);
        }

        #endregion

        #region Internal Coroutine Management

        private ICoroutineHandle StartCoroutineInternal(IEnumerator routine, Action onComplete, Action<Exception> onError, string tag)
        {
            if (routine == null)
                throw new ArgumentNullException(nameof(routine));

            if (!IsActive)
                throw new InvalidOperationException($"CoroutineRunner '{RunnerName}' is not active");

            int id = _nextCoroutineId++;
            var handle = new CoroutineHandle(id, tag, null, OnCoroutineHandleDisposed);

            try
            {
                // Wrap the routine with error handling and completion tracking
                var wrappedRoutine = WrapCoroutineWithHandling(handle, routine, onComplete, onError);
                var unityCoroutine = base.StartCoroutine(wrappedRoutine);
                handle.SetCoroutine(unityCoroutine);

                // Track the coroutine
                _activeCoroutines[id] = handle;
                if (_activeCoroutineIds.IsCreated)
                {
                    _activeCoroutineIds.TryAdd(id, 1);
                }

                // Track by tag if provided
                if (!string.IsNullOrEmpty(tag))
                {
                    if (!_coroutinesByTag.TryGetValue(tag, out var tagList))
                    {
                        tagList = new List<int>();
                        _coroutinesByTag[tag] = tagList;
                    }
                    tagList.Add(id);
                }

                // Record statistics
                _statistics.RecordStart(tag);

                return handle;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{RunnerName}] Failed to start coroutine: {ex.Message}");
                handle?.Dispose();
                throw;
            }
        }

        private void StopCoroutineInternal(CoroutineHandle handle, bool cancelled)
        {
            if (handle == null)
                return;

            try
            {
                var unityCoroutine = handle.GetCoroutine();
                if (unityCoroutine != null)
                {
                    base.StopCoroutine(unityCoroutine);
                }

                RemoveCoroutineTracking(handle);

                if (cancelled)
                {
                    handle.MarkCancelled();
                    _statistics.RecordCancellation(handle.Tag);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{RunnerName}] Error stopping coroutine {handle.Id}: {ex.Message}");
            }
        }

        private void RemoveCoroutineTracking(CoroutineHandle handle)
        {
            if (handle == null)
                return;

            // Remove from active coroutines
            _activeCoroutines.Remove(handle.Id);
            
            if (_activeCoroutineIds.IsCreated)
            {
                _activeCoroutineIds.Remove(handle.Id);
            }

            // Remove from tag tracking
            if (!string.IsNullOrEmpty(handle.Tag) && _coroutinesByTag.TryGetValue(handle.Tag, out var tagList))
            {
                tagList.Remove(handle.Id);
                if (tagList.Count == 0)
                {
                    _coroutinesByTag.Remove(handle.Tag);
                }
            }
        }

        private void OnCoroutineHandleDisposed(CoroutineHandle handle)
        {
            if (_isDisposed)
                return;

            StopCoroutineInternal(handle, true);
        }

        #endregion

        #region Coroutine Wrappers

        private IEnumerator WrapCoroutineWithHandling(CoroutineHandle handle, IEnumerator routine, Action onComplete, Action<Exception> onError)
        {
            Exception caughtException = null;
            bool routineCompleted = false;

            try
            {
                while (true)
                {
                    // Check if we're paused
                    while (_isPaused && !_isDisposed)
                    {
                        yield return null;
                    }

                    // Check if we've been disposed
                    if (_isDisposed)
                    {
                        yield break;
                    }

                    // Execute the next step of the routine
                    bool hasNext;
                    try
                    {
                        hasNext = routine.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                        break;
                    }

                    if (!hasNext)
                    {
                        routineCompleted = true;
                        break;
                    }

                    yield return routine.Current;
                }
            }
            finally
            {
                // Handle completion or error
                if (caughtException != null)
                {
                    try
                    {
                        onError?.Invoke(caughtException);
                    }
                    catch (Exception callbackEx)
                    {
                        Debug.LogError($"[{RunnerName}] Exception in coroutine error callback for {handle.Id}: {callbackEx}");
                    }

                    Debug.LogError($"[{RunnerName}] Coroutine {handle.Id} failed with exception: {caughtException}");
                }
                else if (routineCompleted)
                {
                    try
                    {
                        onComplete?.Invoke();
                    }
                    catch (Exception callbackEx)
                    {
                        Debug.LogError($"[{RunnerName}] Exception in coroutine completion callback for {handle.Id}: {callbackEx}");
                    }

                    handle.MarkCompleted();
                    _statistics.RecordCompletion(handle.Duration, handle.Tag);
                }

                // Clean up tracking (only if not disposed)
                if (!_isDisposed)
                {
                    RemoveCoroutineTracking(handle);
                }
            }
        }

        private IEnumerator DelayedActionCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{RunnerName}] Exception in delayed action: {ex}");
                throw;
            }
        }

        private IEnumerator RepeatingActionCoroutine(float interval, Action action)
        {
            var wait = new WaitForSeconds(interval);
            
            while (true)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{RunnerName}] Exception in repeating action: {ex}");
                    // Continue the loop even if action fails
                }
                
                yield return wait;
            }
        }

        private IEnumerator ConditionalActionCoroutine(Func<bool> condition, Action action, float checkInterval)
        {
            WaitForSeconds wait = checkInterval > 0f ? new WaitForSeconds(checkInterval) : null;

            while (true)
            {
                bool conditionMet = false;
                try
                {
                    conditionMet = condition();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{RunnerName}] Exception checking condition: {ex}");
                    yield break;
                }

                if (conditionMet)
                    break;

                if (wait != null)
                    yield return wait;
                else
                    yield return null;
            }

            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{RunnerName}] Exception in conditional action: {ex}");
                throw;
            }
        }

        #endregion

        #region Pause/Resume

        /// <summary>
        /// Pauses all coroutines managed by this runner.
        /// </summary>
        public void Pause()
        {
            if (_isDisposed)
                return;

            _isPaused = true;
            Debug.Log($"[{RunnerName}] Paused {_activeCoroutines.Count} coroutines");
        }

        /// <summary>
        /// Resumes all paused coroutines managed by this runner.
        /// </summary>
        public void Resume()
        {
            if (_isDisposed)
                return;

            _isPaused = false;
            Debug.Log($"[{RunnerName}] Resumed {_activeCoroutines.Count} coroutines");
        }

        /// <summary>
        /// Gets whether this runner is currently paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        #endregion

        #region Debug and Diagnostics

        /// <summary>
        /// Gets detailed information about all active coroutines.
        /// </summary>
        /// <returns>A dictionary mapping coroutine IDs to their handles.</returns>
        public IReadOnlyDictionary<int, ICoroutineHandle> GetActiveCoroutines()
        {
            var result = new Dictionary<int, ICoroutineHandle>();
            foreach (var kvp in _activeCoroutines)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        /// <summary>
        /// Gets all active tags and their coroutine counts.
        /// </summary>
        /// <returns>A dictionary mapping tags to coroutine counts.</returns>
        public IReadOnlyDictionary<string, int> GetTagStatistics()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _coroutinesByTag)
            {
                result[kvp.Key] = kvp.Value.Count;
            }
            return result;
        }

        /// <summary>
        /// Logs detailed statistics about this coroutine runner.
        /// </summary>
        public void LogStatistics()
        {
            if (_isDisposed)
                return;

            var stats = _statistics;
            Debug.Log($"[{RunnerName}] Statistics:\n" +
                     $"  Active: {ActiveCoroutineCount}\n" +
                     $"  Total Started: {stats.TotalCoroutinesStarted}\n" +
                     $"  Total Completed: {stats.TotalCoroutinesCompleted}\n" +
                     $"  Total Cancelled: {stats.TotalCoroutinesCancelled}\n" +
                     $"  Peak Active: {stats.PeakActiveCoroutines}\n" +
                     $"  Average Duration: {stats.AverageCoroutineDuration.TotalMilliseconds:F2}ms");

            var tagStats = GetTagStatistics();
            if (tagStats.Count > 0)
            {
                Debug.Log($"[{RunnerName}] Active by tag: {string.Join(", ", tagStats)}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Debug.Log($"[{RunnerName}] Disposing coroutine runner with {_activeCoroutines.Count} active coroutines");

            // Stop all active coroutines
            StopAllCoroutines();

            // Clear collections
            _activeCoroutines.Clear();
            _coroutinesByTag.Clear();

            // Dispose native collections
            if (_activeCoroutineIds.IsCreated)
            {
                _activeCoroutineIds.Dispose();
            }

            // Dispose statistics
            _statistics?.Dispose();

            _isDisposed = true;
            _isInitialized = false;

            Debug.Log($"[{RunnerName}] Coroutine runner disposed");
        }

        #endregion
    }
}