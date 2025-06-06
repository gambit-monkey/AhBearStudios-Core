using System;
using System.Collections;
using System.Collections.Generic;
using AhBearStudios.Core.Coroutine.Handles;
using AhBearStudios.Core.Coroutine.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Profilers;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Sessions;
using UnityEngine;
using Unity.Collections;

namespace AhBearStudios.Core.Coroutine
{
    /// <summary>
    /// Unity MonoBehaviour-based coroutine runner with advanced management features and profiling integration.
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
        private Guid _runnerId;
        
        // Dependency injection fields
        private CoroutineProfiler _profiler;
        private ICoroutineMetrics _coroutineMetrics;
        private IMessageBus _messageBus;
        private bool _profilingEnabled;

        // Subscription tokens for cleanup
        private IDisposable _sessionCompletedSubscription;
        private IDisposable _metricAlertSubscription;

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
        
        /// <inheritdoc />
        public Guid Id => _runnerId;
        
        /// <inheritdoc />
        public string Name => RunnerName;

        #endregion

        #region Initialization

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the coroutine runner with dependency injection.
        /// </summary>
        /// <param name="runnerName">Optional name for this runner instance.</param>
        /// <param name="profiler">Profiler for performance tracking.</param>
        /// <param name="coroutineMetrics">Metrics system for coroutine tracking.</param>
        /// <param name="messageBus">Message bus for event publishing.</param>
        public void Initialize(
            string runnerName = null, 
            CoroutineProfiler profiler = null,
            ICoroutineMetrics coroutineMetrics = null,
            IMessageBus messageBus = null)
        {
            if (_isInitialized)
                return;

            _runnerName = runnerName ?? gameObject.name;
            _runnerId = Guid.NewGuid();
            _activeCoroutineIds = new NativeParallelHashMap<int, byte>(64, Allocator.Persistent);
            _nextCoroutineId = 1;
            _isInitialized = true;
            _isDisposed = false;
            _isPaused = false;
            
            // Set up dependencies
            _profiler = profiler;
            _coroutineMetrics = coroutineMetrics;
            _messageBus = messageBus;
            _profilingEnabled = _profiler?.IsEnabled == true;
            
            // Update runner configuration in metrics
            _coroutineMetrics?.UpdateRunnerConfiguration(_runnerId, _runnerName, GetType().Name);
            
            // Subscribe to profiling messages if message bus is available
            SetupMessageSubscriptions();

            Debug.Log($"[{RunnerName}] Coroutine runner initialized with ID: {_runnerId}");
        }

        /// <summary>
        /// Sets up message bus subscriptions for profiling events.
        /// </summary>
        private void SetupMessageSubscriptions()
        {
            if (_messageBus == null) return;

            try
            {
                _sessionCompletedSubscription = _messageBus.SubscribeToMessage<CoroutineProfilerSessionCompletedMessage>(
                    OnCoroutineSessionCompleted);
                
                _metricAlertSubscription = _messageBus.SubscribeToMessage<CoroutineMetricAlertMessage>(
                    OnCoroutineMetricAlert);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{RunnerName}] Failed to setup message subscriptions: {ex.Message}");
            }
        }

        #endregion

        #region Coroutine Management

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

        /// <summary>
        /// Internal coroutine startup with profiling integration.
        /// </summary>
        private ICoroutineHandle StartCoroutineInternal(
            IEnumerator routine, 
            Action onComplete, 
            Action<Exception> onError, 
            string tag)
        {
            if (!IsActive)
                throw new InvalidOperationException($"[{RunnerName}] Cannot start coroutine: runner is not active");

            if (routine == null)
                throw new ArgumentNullException(nameof(routine));

            var coroutineId = _nextCoroutineId++;
            var handle = new CoroutineHandle(coroutineId, this, tag);
            
            // Record metrics start
            var startTime = Time.realtimeSinceStartup;
            _coroutineMetrics?.RecordStart(_runnerId, 0f, !string.IsNullOrEmpty(tag));
            
            // Start profiling session if enabled
            CoroutineProfilerSession profilingSession = null;
            if (_profilingEnabled)
            {
                var operationType = routine.GetType().Name;
                profilingSession = _profiler.BeginCoroutineScope(this, operationType, coroutineId, tag);
            }

            // Wrap the routine with error handling and profiling
            var wrappedRoutine = WrapCoroutineWithProfiling(routine, handle, onComplete, onError, profilingSession, startTime);
            var unityCoroutine = base.StartCoroutine(wrappedRoutine);
            
            // Store the handle
            handle.SetUnityCoroutine(unityCoroutine);
            _activeCoroutines[coroutineId] = handle;
            _activeCoroutineIds.TryAdd(coroutineId, 0);
            
            // Track by tag
            if (!string.IsNullOrEmpty(tag))
            {
                if (!_coroutinesByTag.TryGetValue(tag, out var taggedCoroutines))
                {
                    taggedCoroutines = new List<int>();
                    _coroutinesByTag[tag] = taggedCoroutines;
                }
                taggedCoroutines.Add(coroutineId);
            }
            
            _statistics.RecordStart(tag);
            
            return handle;
        }

        /// <summary>
/// Wraps a coroutine with profiling and error handling.
/// </summary>
private IEnumerator WrapCoroutineWithProfiling(
    IEnumerator originalRoutine,
    CoroutineHandle handle,
    Action onComplete,
    Action<Exception> onError,
    CoroutineProfilerSession profilingSession,
    float startTime)
{
    var executionStartTime = Time.realtimeSinceStartup;
    Exception caughtException = null;
    bool completed = false;
    
    // Execute the coroutine with manual exception handling
    while (!completed)
    {
        bool moveNextSucceeded = false;
        object current = null;
        
        try
        {
            if (handle.IsCancelled)
            {
                _coroutineMetrics?.RecordCancellation(_runnerId, !string.IsNullOrEmpty(handle.Tag));
                break;
            }
            
            moveNextSucceeded = originalRoutine.MoveNext();
            if (moveNextSucceeded)
            {
                current = originalRoutine.Current;
            }
            else
            {
                completed = true;
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
            completed = true;
        }
        
        if (moveNextSucceeded && caughtException == null)
        {
            yield return current;
        }
    }
    
    // Handle completion or error outside the iteration loop
    if (caughtException != null)
    {
        // Handle exception
        _coroutineMetrics?.RecordFailure(_runnerId, !string.IsNullOrEmpty(handle.Tag), false);
        handle.Complete(); // Mark as completed even with error
        
        if (onError != null)
        {
            try
            {
                onError(caughtException);
            }
            catch (Exception handlerEx)
            {
                Debug.LogError($"[{RunnerName}] Exception in error handler: {handlerEx}");
            }
        }
        else
        {
            Debug.LogError($"[{RunnerName}] Unhandled coroutine exception: {caughtException}");
        }
    }
    else if (!handle.IsCancelled)
    {
        // Successful completion
        var executionTime = (Time.realtimeSinceStartup - executionStartTime) * 1000f;
        _coroutineMetrics?.RecordCompletion(_runnerId, executionTime, 0f, !string.IsNullOrEmpty(handle.Tag));
        
        handle.Complete();
        onComplete?.Invoke();
    }
    
    // Clean up profiling session
    if (profilingSession != null)
    {
        if (caughtException != null)
            profilingSession.MarkAsFailed();
        
        profilingSession.Dispose();
    }
    
    // Clean up tracking
    CleanupCoroutine(handle.Id);
    
    var totalTime = (Time.realtimeSinceStartup - startTime) * 1000f;
    _statistics.RecordCompletion(TimeSpan.FromMilliseconds(totalTime), handle.Tag);
}

        /// <inheritdoc />
        public bool StopCoroutine(ICoroutineHandle handle)
        {
            if (handle == null || !(handle is CoroutineHandle coroutineHandle))
                return false;

            if (!_activeCoroutines.ContainsKey(coroutineHandle.Id))
                return false;

            coroutineHandle.Cancel();
            
            if (coroutineHandle.UnityCoroutine != null)
            {
                base.StopCoroutine(coroutineHandle.UnityCoroutine);
            }
            
            CleanupCoroutine(coroutineHandle.Id);
            _statistics.RecordCancellation(coroutineHandle.Tag);
            _coroutineMetrics?.RecordCancellation(_runnerId, !string.IsNullOrEmpty(coroutineHandle.Tag));
            
            return true;
        }

        /// <inheritdoc />
        public int StopCoroutinesByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || !_coroutinesByTag.TryGetValue(tag, out var coroutineIds))
                return 0;

            var stoppedCount = 0;
            var coroutinesToStop = new List<int>(coroutineIds);
            
            foreach (var coroutineId in coroutinesToStop)
            {
                if (_activeCoroutines.TryGetValue(coroutineId, out var handle))
                {
                    if (StopCoroutine(handle))
                        stoppedCount++;
                }
            }
            
            return stoppedCount;
        }

        /// <inheritdoc />
        public int StopAllCoroutines()
        {
            var coroutinesToStop = new List<CoroutineHandle>(_activeCoroutines.Values);
            var stoppedCount = 0;
            
            foreach (var handle in coroutinesToStop)
            {
                if (StopCoroutine(handle))
                    stoppedCount++;
            }
            
            return stoppedCount;
        }

        /// <inheritdoc />
        public bool IsCoroutineRunning(ICoroutineHandle handle)
        {
            return handle is CoroutineHandle coroutineHandle && 
                   _activeCoroutines.ContainsKey(coroutineHandle.Id) && 
                   coroutineHandle.IsRunning;
        }

        /// <inheritdoc />
        public int GetActiveCoroutineCount(string tag = null)
        {
            if (string.IsNullOrEmpty(tag))
                return _activeCoroutines.Count;

            return _coroutinesByTag.TryGetValue(tag, out var taggedCoroutines) ? taggedCoroutines.Count : 0;
        }

        #endregion

        #region Convenience Methods

        /// <inheritdoc />
        public ICoroutineHandle StartDelayedAction(float delay, Action action, string tag = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return StartCoroutine(DelayedActionCoroutine(delay, action), tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartRepeatingAction(float interval, Action action, string tag = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return StartCoroutine(RepeatingActionCoroutine(interval, action), tag);
        }

        /// <inheritdoc />
        public ICoroutineHandle StartConditionalAction(Func<bool> condition, Action action, float checkInterval = 0f, string tag = null)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return StartCoroutine(ConditionalActionCoroutine(condition, action, checkInterval), tag);
        }

        private IEnumerator DelayedActionCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        private IEnumerator RepeatingActionCoroutine(float interval, Action action)
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                action();
            }
        }

        private IEnumerator ConditionalActionCoroutine(Func<bool> condition, Action action, float checkInterval)
        {
            while (!condition())
            {
                if (checkInterval > 0f)
                    yield return new WaitForSeconds(checkInterval);
                else
                    yield return null;
            }
            action();
        }

        #endregion

        #region Message Handlers

        /// <summary>
        /// Handles coroutine profiler session completed messages.
        /// </summary>
        private void OnCoroutineSessionCompleted(CoroutineProfilerSessionCompletedMessage message)
        {
            // Only process messages for this runner
            if (message.RunnerId != _runnerId)
                return;

            Debug.Log($"[{RunnerName}] Coroutine session completed - Operation: {message.OperationType}, " +
                     $"Duration: {message.DurationMs:F2}ms, Success: {message.Success}");
        }

        /// <summary>
        /// Handles coroutine metric alert messages.
        /// </summary>
        private void OnCoroutineMetricAlert(CoroutineMetricAlertMessage message)
        {
            // Only process messages for this runner
            if (message.RunnerId != _runnerId)
                return;

            var severityText = message.Severity switch
            {
                AlertSeverity.Warning => "WARNING",
                AlertSeverity.Critical => "CRITICAL",
                _ => "INFO"
            };

            Debug.LogWarning($"[{RunnerName}] {severityText}: {message.MetricName} = {message.CurrentValue:F2} " +
                           $"(threshold: {message.Threshold:F2})");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up a completed or cancelled coroutine.
        /// </summary>
        private void CleanupCoroutine(int coroutineId)
        {
            if (!_activeCoroutines.TryGetValue(coroutineId, out var handle))
                return;

            _activeCoroutines.Remove(coroutineId);
    
            // Remove from native collection - NativeParallelHashMap uses Remove, not TryRemove
            if (_activeCoroutineIds.IsCreated)
            {
                _activeCoroutineIds.Remove(coroutineId);
            }

            // Remove from tag tracking
            if (!string.IsNullOrEmpty(handle.Tag) && _coroutinesByTag.TryGetValue(handle.Tag, out var taggedCoroutines))
            {
                taggedCoroutines.Remove(coroutineId);
                if (taggedCoroutines.Count == 0)
                    _coroutinesByTag.Remove(handle.Tag);
            }
        }

        /// <summary>
        /// Pauses all coroutine execution.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            Debug.Log($"[{RunnerName}] Coroutine execution paused");
        }

        /// <summary>
        /// Resumes coroutine execution.
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            Debug.Log($"[{RunnerName}] Coroutine execution resumed");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the coroutine runner and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Stop all running coroutines
            StopAllCoroutines();

            // Dispose of message subscriptions
            _sessionCompletedSubscription?.Dispose();
            _metricAlertSubscription?.Dispose();

            // Dispose of native collections
            if (_activeCoroutineIds.IsCreated)
                _activeCoroutineIds.Dispose();

            // Clean up statistics
            _statistics?.Dispose();

            _activeCoroutines.Clear();
            _coroutinesByTag.Clear();

            Debug.Log($"[{RunnerName}] Coroutine runner disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}