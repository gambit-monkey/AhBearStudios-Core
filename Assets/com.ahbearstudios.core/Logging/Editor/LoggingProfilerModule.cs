using System;
using Unity.Collections;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using AhBearStudios.Core.Logging.Events;
using Unity.Profiling.Editor;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom profiler module for the logging system that tracks and visualizes performance metrics.
    /// Uses Unity.Collections v2 for memory-efficient, allocation-free operation.
    /// </summary>
    [Serializable]
    public class LoggingProfilerModule : ProfilerModule, IDisposable
    {
        // Configuration
        private const int MAX_HISTORY_SIZE = 100;
        
        // Native containers for metrics
        private NativeRingQueue<float> _flushDurations;
        private NativeRingQueue<int> _messageRates;
        
        // Stats tracking
        private int _processedSinceLastUpdate;
        private float _lastUpdateTime;
        private float _maxFlushDuration;
        private int _maxMessageRate;
        private bool _isInitialized;
        
        // Metrics collection status
        private bool _isCollecting;
        
        // Counters for Unity Profiler integration
        private readonly ProfilerCounterValue<float> _flushDurationCounter;
        private readonly ProfilerCounterValue<int> _messageRateCounter;
        
        // Synchronization object for thread safety
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingProfilerModule"/> class.
        /// </summary>
        public LoggingProfilerModule() : base("Logging Performance")
        {
            // Create profiler counters - these are registered with Unity's profiler
            _flushDurationCounter = AddCounter(
                "Flush Duration (ms)", 
                ProfilerCategory.Scripts, 
                ProfilerMarkerDataUnit.TimeNanoseconds, 
                ProfilerCounterOptions.FluidMovement, 
                true
            );
            
            _messageRateCounter = AddCounter(
                "Messages/sec", 
                ProfilerCategory.Scripts,
                ProfilerMarkerDataUnit.Count, 
                ProfilerCounterOptions.FluidMovement, 
                true
            );
            
            // Initialize state
            _processedSinceLastUpdate = 0;
            _lastUpdateTime = 0;
            _maxFlushDuration = 0;
            _maxMessageRate = 0;
            _isInitialized = false;
            _isCollecting = false;
            
            // Initialize collections lazily when first needed
            InitializeIfNeeded();
            
            // Start collecting metrics
            StartCollection();
        }
        
        /// <summary>
        /// Initializes native collections if they haven't been initialized yet.
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (_isInitialized)
                return;
                
            try
            {
                _flushDurations = new NativeRingQueue<float>(MAX_HISTORY_SIZE, Allocator.Persistent);
                _messageRates = new NativeRingQueue<int>(MAX_HISTORY_SIZE, Allocator.Persistent);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize LoggingProfilerModule: {ex.Message}");
                _isInitialized = false;
            }
        }
        
        /// <summary>
        /// Starts collecting logging metrics by subscribing to events.
        /// </summary>
        public void StartCollection()
        {
            if (_isCollecting)
                return;
                
            // Subscribe to logging events
            LogEvents.OnLogFlushed += OnLogFlushed;
            LogEvents.OnMessageWritten += OnMessageWritten;
            
            _isCollecting = true;
        }
        
        /// <summary>
        /// Stops collecting logging metrics by unsubscribing from events.
        /// </summary>
        public void StopCollection()
        {
            if (!_isCollecting)
                return;
                
            // Unsubscribe from events
            LogEvents.OnLogFlushed -= OnLogFlushed;
            LogEvents.OnMessageWritten -= OnMessageWritten;
            
            _isCollecting = false;
        }
        
        /// <summary>
        /// Handles the OnLogFlushed event to track flush performance.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnLogFlushed(object sender, LogFlushEventArgs e)
        {
            if (!_isInitialized)
                return;
                
            try
            {
                // Track flush duration under thread-safe lock
                lock (_syncRoot)
                {
                    float duration = e.DurationMs;
                    
                    if (_flushDurations.Length == _flushDurations.Capacity)
                    {
                        _ = _flushDurations.TryDequeue(out _);
                    }
                    
                    _flushDurations.TryEnqueue(duration);
                    _maxFlushDuration = Math.Max(_maxFlushDuration, duration);
                    
                    // Update the profiler counter immediately
                    _flushDurationCounter.Value = duration;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing flush metrics: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles the OnMessageWritten event to track message throughput.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMessageWritten(object sender, LogMessageWrittenEventArgs e)
        {
            if (!_isInitialized)
                return;
                
            try
            {
                // Track message rate under thread-safe lock
                lock (_syncRoot)
                {
                    _processedSinceLastUpdate++;
                    
                    float currentTime = Time.realtimeSinceStartup;
                    if (currentTime - _lastUpdateTime >= 1.0f)
                    {
                        int rate = Mathf.RoundToInt(_processedSinceLastUpdate / (currentTime - _lastUpdateTime));
                        
                        if (_messageRates.Length == _messageRates.Capacity)
                        {
                            _ = _messageRates.TryDequeue(out _);
                        }
                        
                        _messageRates.TryEnqueue(rate);
                        _maxMessageRate = Math.Max(_maxMessageRate, rate);
                        
                        // Update the profiler counter
                        _messageRateCounter.Value = rate;
                        
                        // Reset counters
                        _processedSinceLastUpdate = 0;
                        _lastUpdateTime = currentTime;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing message rate metrics: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Renders the profiler module UI.
        /// </summary>
        /// <param name="position">The rect position for rendering.</param>
        /// <param name="timeline">The profiler timeline.</param>
        public void OnGUI(Rect position, ProfilerTimeline timeline)
        {
            if (!_isInitialized)
            {
                EditorGUI.LabelField(position, "Logging profiler is not initialized. Please check console for errors.");
                return;
            }
            
            if (!EditorApplication.isPlaying)
            {
                EditorGUI.LabelField(position, "Logging profiler data is only available during play mode.");
                return;
            }
            
            lock (_syncRoot)
            {
                GUILayout.BeginArea(position);
                
                EditorGUILayout.LabelField("Logging System Performance", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                
                // Draw stats
                EditorGUILayout.LabelField($"Current Flush Duration: {_flushDurationCounter.Value:F2} ms");
                EditorGUILayout.LabelField($"Max Flush Duration: {_maxFlushDuration:F2} ms");
                EditorGUILayout.LabelField($"Current Message Rate: {_messageRateCounter.Value} msgs/sec");
                EditorGUILayout.LabelField($"Max Message Rate: {_maxMessageRate} msgs/sec");
                
                EditorGUILayout.Space();
                
                // Draw mini graphs
                if (_flushDurations.Length > 0)
                {
                    EditorGUILayout.LabelField("Flush Duration History (ms):", EditorStyles.boldLabel);
                    DrawMiniGraph(_flushDurations, 0, _maxFlushDuration, Color.cyan);
                }
                
                if (_messageRates.Length > 0)
                {
                    EditorGUILayout.LabelField("Message Rate History (msgs/sec):", EditorStyles.boldLabel);
                    DrawMiniGraph(_messageRates, 0, _maxMessageRate, Color.green);
                }
                
                GUILayout.EndArea();
            }
        }
        
        /// <summary>
        /// Draws a mini graph from a NativeRingQueue of values.
        /// </summary>
        /// <typeparam name="T">The type of values in the queue.</typeparam>
        /// <param name="values">The values to graph.</param>
        /// <param name="min">The minimum value (for scaling).</param>
        /// <param name="max">The maximum value (for scaling).</param>
        /// <param name="color">The color to use for the graph lines.</param>
        private void DrawMiniGraph<T>(NativeRingQueue<T> values, T min, T max, Color color) where T : unmanaged, IComparable<T>
        {
            if (values.Length == 0)
                return;
                
            Rect graphRect = EditorGUILayout.GetControlRect(GUILayout.Height(50));
            
            // Background
            EditorGUI.DrawRect(graphRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            
            float graphWidth = graphRect.width;
            float graphHeight = graphRect.height;
            
            // Get values in order from oldest to newest
            using var tempArray = new NativeArray<T>(values.Length, Allocator.Temp);
            CopyRingQueueToArray(values, tempArray);
            
            // Draw the lines
            for (int i = 1; i < tempArray.Length; i++)
            {
                float x1 = graphRect.x + (i - 1) * graphWidth / tempArray.Length;
                float x2 = graphRect.x + i * graphWidth / tempArray.Length;
                
                // Calculate normalized values for height
                float normalizedPrev = NormalizeValue(tempArray[i - 1], min, max);
                float normalizedCurr = NormalizeValue(tempArray[i], min, max);
                
                float y1 = graphRect.y + graphHeight - normalizedPrev * graphHeight;
                float y2 = graphRect.y + graphHeight - normalizedCurr * graphHeight;
                
                Handles.color = color;
                Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
            }
        }
        
        /// <summary>
        /// Copies values from a NativeRingQueue to a NativeArray in chronological order.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="queue">The source queue.</param>
        /// <param name="array">The destination array.</param>
        private unsafe void CopyRingQueueToArray<T>(NativeRingQueue<T> queue, NativeArray<T> array) where T : unmanaged
        {
            if (queue.Length == 0 || array.Length == 0)
                return;
                
            int count = Math.Min(queue.Length, array.Length);
            
            // Create a temporary array to hold the queue content
            using var tempArray = new NativeArray<T>(count, Allocator.Temp);
            
            // Dequeue items to temp array (reversed order)
            int tempIndex = 0;
            NativeRingQueue<T> tempQueue = queue;
            
            while (tempIndex < count && tempQueue.TryDequeue(out T value))
            {
                tempArray[tempIndex++] = value;
            }
            
            // Copy back in reverse to get chronological order
            for (int i = 0; i < count; i++)
            {
                array[i] = tempArray[count - i - 1];
            }
        }
        
        /// <summary>
        /// Normalizes a value between 0 and 1 based on the provided min and max.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to normalize.</param>
        /// <param name="min">The minimum possible value.</param>
        /// <param name="max">The maximum possible value.</param>
        /// <returns>A normalized value between 0 and 1.</returns>
        private float NormalizeValue<T>(T value, T min, T max) where T : IComparable<T>
        {
            // Convert to float for normalization
            float fValue = Convert.ToSingle(value);
            float fMin = Convert.ToSingle(min);
            float fMax = Convert.ToSingle(max);
            
            if (Math.Abs(fMax - fMin) < 0.001f)
                return 0.5f; // Avoid division by zero
                
            return (fValue - fMin) / (fMax - fMin);
        }
        
        /// <summary>
        /// Cleans up native resources when the ProfilerModule is disposed.
        /// Called by Unity automatically when the profiler module is removed.
        /// </summary>
        protected override void DisposeInternal()
        {
            // Stop collecting data
            StopCollection();
            
            // Clean up native collections
            DisposeCollections();
            
            base.DisposeInternal();
        }
        
        /// <summary>
        /// Disposes native collections if they are initialized.
        /// </summary>
        private void DisposeCollections()
        {
            if (!_isInitialized)
                return;
                
            if (_flushDurations.IsCreated)
            {
                _flushDurations.Dispose();
            }
            
            if (_messageRates.IsCreated)
            {
                _messageRates.Dispose();
            }
            
            _isInitialized = false;
        }
        
        /// <summary>
        /// Implements IDisposable to ensure proper cleanup of unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            StopCollection();
            DisposeCollections();
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer to ensure resource cleanup if Dispose isn't called.
        /// </summary>
        ~LoggingProfilerModule()
        {
            Dispose();
        }
    }
}