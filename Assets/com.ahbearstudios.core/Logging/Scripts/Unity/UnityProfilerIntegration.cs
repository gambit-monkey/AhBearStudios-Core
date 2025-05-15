using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;
using AhBearStudios.Core.Logging.Events;
using Unity.Collections;
using Unity.Profiling;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Integrates the logging system with Unity's profiler.
    /// </summary>
    public class UnityProfiler
    {
        private readonly Dictionary<string, ProfilerMarker> _markers = new Dictionary<string, ProfilerMarker>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _isEnabled = true;
        
        // Sample markers for common operations
        private readonly ProfilerMarker _messageProcessingMarker = new ProfilerMarker("Logging.MessageProcessing");
        private readonly ProfilerMarker _queueFlushMarker = new ProfilerMarker("Logging.QueueFlush");
        private readonly ProfilerMarker _targetWriteMarker = new ProfilerMarker("Logging.TargetWrite");
        
        /// <summary>
        /// Creates a new Unity profiler integration.
        /// </summary>
        public UnityProfiler()
        {
            // Subscribe to logging events
            LogEvents.OnMessageCreated += OnMessageCreated;
            LogEvents.OnMessageProcessed += OnMessageProcessed;
            LogEvents.OnMessageWritten += OnMessageWritten;
            LogEvents.OnLogFlushed += OnLogFlushed;
            // TODO Add the Other Events to be profiled
        }
        
        /// <summary>
        /// Gets or sets whether profiling is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
        
        /// <summary>
        /// Called when a message is created.
        /// </summary>
        private void OnMessageCreated(object sender, LogMessageEventArgs e)
        {
            if (!_isEnabled) return;
            
            // This would be a very high-frequency event, so we just count it rather than creating markers
            Profiler.BeginSample("Logging.MessageCreated");
            Profiler.EndSample();
        }
        
        /// <summary>
        /// Called when a message is processed.
        /// </summary>
        private void OnMessageProcessed(object sender, LogMessageEventArgs e)
        {
            if (!_isEnabled) return;
    
            _messageProcessingMarker.Begin();
    
            // Specific tag-based profiling
            FixedString32Bytes fixedTagName = e.Message.GetTagString();
            string tagName = fixedTagName.ToString();
            if (!string.IsNullOrEmpty(tagName))
            {
                GetOrCreateMarker($"Logging.Tag.{tagName}").Begin();
                GetOrCreateMarker($"Logging.Tag.{tagName}").End();
            }
    
            _messageProcessingMarker.End();
        }

        
        /// <summary>
        /// Called when a message is written to targets.
        /// </summary>
        private void OnMessageWritten(object sender, LogMessageWrittenEventArgs e)
        {
            if (!_isEnabled) return;
            
            _targetWriteMarker.Begin();
            _targetWriteMarker.End();
        }
        
        /// <summary>
        /// Called when the log queue is flushed.
        /// </summary>
        private void OnLogFlushed(object sender, LogFlushEventArgs e)
        {
            if (!_isEnabled) return;
            
            _queueFlushMarker.Begin();
            
            // Record performance metrics
            Profiler.BeginSample($"Logging.Flush.{e.ProcessedCount}Messages");
            Profiler.EndSample();
            
            _queueFlushMarker.End();
        }
        
        /// <summary>
        /// Gets or creates a profiler marker with the specified name.
        /// </summary>
        /// <param name="name">The name of the marker.</param>
        /// <returns>The profiler marker.</returns>
        private ProfilerMarker GetOrCreateMarker(string name)
        {
            if (!_markers.TryGetValue(name, out var marker))
            {
                marker = new ProfilerMarker(name);
                _markers[name] = marker;
            }
            return marker;
        }
        
        /// <summary>
        /// Begins a custom profiling section.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        public void BeginSample(string name)
        {
            if (!_isEnabled) return;
            GetOrCreateMarker(name).Begin();
        }
        
        /// <summary>
        /// Ends the most recently started profiling section.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        public void EndSample(string name)
        {
            if (!_isEnabled) return;
            if (_markers.TryGetValue(name, out var marker))
            {
                marker.End();
            }
        }
        
        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            LogEvents.OnMessageCreated -= OnMessageCreated;
            LogEvents.OnMessageProcessed -= OnMessageProcessed;
            LogEvents.OnMessageWritten -= OnMessageWritten;
            LogEvents.OnLogFlushed -= OnLogFlushed;
            
            _markers.Clear();
        }
    }
}