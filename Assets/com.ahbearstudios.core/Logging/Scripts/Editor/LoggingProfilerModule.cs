using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using AhBearStudios.Core.Logging.Events;
using Unity.Profiling.Editor;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom profiler module for the logging system.
    /// </summary>
    [Serializable]
    public class LoggingProfilerModule : ProfilerModule
    {
        // Stats tracking
        private Queue<float> _flushDurations = new Queue<float>(100);
        private Queue<int> _messageRates = new Queue<int>(100);
        private int _processedSinceLastUpdate = 0;
        private float _lastUpdateTime = 0;
        private float _maxFlushDuration = 0;
        private int _maxMessageRate = 0;
        
        // Graphs
        private ProfilerCounterValue<float> _flushDurationCounter;
        private ProfilerCounterValue<int> _messageRateCounter;
        
        // Constructor required by ProfilerModule base class
        public LoggingProfilerModule() : base("Logging Performance")
        {
            // Create counters
            _flushDurationCounter = AddCounter("Flush Duration (ms)", ProfilerCategory.Scripts, 
                ProfilerMarkerDataUnit.TimeNanoseconds, ProfilerCounterOptions.FluidMovement, true);
            
            _messageRateCounter = AddCounter("Messages/sec", ProfilerCategory.Scripts,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FluidMovement, true);
            
            // Subscribe to logging events
            LogEvents.OnLogFlushed += OnLogFlushed;
            LogEvents.OnMessageWritten += OnMessageWritten;
        }
        
        protected override void DisposeInternal()
        {
            // Unsubscribe from events
            LogEvents.OnLogFlushed -= OnLogFlushed;
            LogEvents.OnMessageWritten -= OnMessageWritten;
            
            base.DisposeInternal();
        }
        
        private void OnLogFlushed(object sender, LogFlushEventArgs e)
        {
            // Track flush duration
            float duration = e.DurationMs;
            
            _flushDurations.Enqueue(duration);
            if (_flushDurations.Count > 100)
            {
                _flushDurations.Dequeue();
            }
            
            _maxFlushDuration = Math.Max(_maxFlushDuration, duration);
            
            // Update the profiler counter
            _flushDurationCounter.Value = duration;
        }
        
        private void OnMessageWritten(object sender, LogMessageWrittenEventArgs e)
        {
            // Track message rate
            _processedSinceLastUpdate++;
            
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastUpdateTime >= 1.0f)
            {
                int rate = Mathf.RoundToInt(_processedSinceLastUpdate / (currentTime - _lastUpdateTime));
                
                _messageRates.Enqueue(rate);
                if (_messageRates.Count > 100)
                {
                    _messageRates.Dequeue();
                }
                
                _maxMessageRate = Math.Max(_maxMessageRate, rate);
                
                // Update the profiler counter
                _messageRateCounter.Value = rate;
                
                // Reset counters
                _processedSinceLastUpdate = 0;
                _lastUpdateTime = currentTime;
            }
        }
        
        public override void OnGUI(Rect position, ProfilerTimeline timeline)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUI.LabelField(position, "Logging profiler data is only available during play mode.");
                return;
            }
            
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
            EditorGUILayout.LabelField("Flush Duration History (ms):", EditorStyles.boldLabel);
            DrawMiniGraph(_flushDurations.ToArray(), 0, _maxFlushDuration, Color.cyan);
            
            EditorGUILayout.LabelField("Message Rate History (msgs/sec):", EditorStyles.boldLabel);
            DrawMiniGraph(_messageRates.ToArray(), 0, _maxMessageRate, Color.green);
            
            GUILayout.EndArea();
        }
        
        private void DrawMiniGraph<T>(T[] values, T min, T max, Color color) where T : IComparable
        {
            if (values.Length == 0)
                return;
                
            Rect graphRect = EditorGUILayout.GetControlRect(GUILayout.Height(50));
            
            // Background
            EditorGUI.DrawRect(graphRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            
            float graphWidth = graphRect.width;
            float graphHeight = graphRect.height;
            
            // Draw the lines
            for (int i = 1; i < values.Length; i++)
            {
                float x1 = graphRect.x + (i - 1) * graphWidth / values.Length;
                float x2 = graphRect.x + i * graphWidth / values.Length;
                
                // Calculate normalized values for height
                float normalizedPrev = NormalizeValue(values[i - 1], min, max);
                float normalizedCurr = NormalizeValue(values[i], min, max);
                
                float y1 = graphRect.y + graphHeight - normalizedPrev * graphHeight;
                float y2 = graphRect.y + graphHeight - normalizedCurr * graphHeight;
                
                Handles.color = color;
                Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
            }
        }
        
        private float NormalizeValue<T>(T value, T min, T max) where T : IComparable
        {
            // Convert to float for normalization
            float fValue = Convert.ToSingle(value);
            float fMin = Convert.ToSingle(min);
            float fMax = Convert.ToSingle(max);
            
            if (Math.Abs(fMax - fMin) < 0.001f)
                return 0.5f; // Avoid division by zero
                
            return (fValue - fMin) / (fMax - fMin);
        }
    }
}