using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Integrates the logging system with Unity's profiler by subscribing to logging events
    /// through the MessageBus and creating performance markers.
    /// </summary>
    public class UnityProfilerIntegration : IDisposable
    {
        #region Private Fields
        
        /// <summary>
        /// Dictionary of named profiler markers to avoid repeated creation.
        /// </summary>
        private readonly Dictionary<string, ProfilerMarker> _markers = new Dictionary<string, ProfilerMarker>();
        
        /// <summary>
        /// Stopwatch for precise timing measurements.
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();
        
        /// <summary>
        /// Flag indicating if profiling is enabled.
        /// </summary>
        private bool _isEnabled = true;
        
        /// <summary>
        /// Marker for general message processing operations.
        /// </summary>
        private readonly ProfilerMarker _messageProcessingMarker = new ProfilerMarker("Logging.MessageProcessing");
        
        /// <summary>
        /// Marker for queue flush operations.
        /// </summary>
        private readonly ProfilerMarker _queueFlushMarker = new ProfilerMarker("Logging.QueueFlush");
        
        /// <summary>
        /// Marker for log target write operations.
        /// </summary>
        private readonly ProfilerMarker _targetWriteMarker = new ProfilerMarker("Logging.TargetWrite");
        
        /// <summary>
        /// The message bus instance for subscribing to log events.
        /// </summary>
        private readonly IMessageBus _messageBus;
        
        /// <summary>
        /// Message subscription tokens for cleanup.
        /// </summary>
        private readonly List<IMessageSubscription> _subscriptions = new List<IMessageSubscription>();
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new Unity profiler integration with the specified message bus.
        /// </summary>
        /// <param name="messageBus">The message bus to subscribe to for logging events.</param>
        /// <exception cref="ArgumentNullException">Thrown if messageBus is null.</exception>
        public UnityProfilerIntegration(IMessageBus messageBus)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            // Subscribe to log-related messages
            SubscribeToMessages();
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets or sets whether profiling is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
        
        #endregion
        
        #region Message Subscription
        
        /// <summary>
        /// Subscribes to all relevant log-related messages from the message bus.
        /// </summary>
        private void SubscribeToMessages()
        {
            // Subscribe to message creation events
            var entryReceivedSub = _messageBus.Subscribe<LogEntryReceivedMessage>(OnLogEntryReceived);
            _subscriptions.Add(entryReceivedSub);
            
            // Subscribe to message processing events
            var processingMessageSub = _messageBus.Subscribe<LogProcessingMessage>(OnLogProcessing);
            _subscriptions.Add(processingMessageSub);
            
            // Subscribe to message written events
            var entryWrittenSub = _messageBus.Subscribe<LogEntryWrittenMessage>(OnLogEntryWritten);
            _subscriptions.Add(entryWrittenSub);
            
            // Subscribe to flush events
            var flushMessageSub = _messageBus.Subscribe<LogFlushMessage>(OnLogFlush);
            _subscriptions.Add(flushMessageSub);
        }
        
        #endregion
        
        #region Message Handlers
        
        /// <summary>
        /// Handles log entry received messages.
        /// </summary>
        /// <param name="message">The log entry received message.</param>
        private void OnLogEntryReceived(LogEntryReceivedMessage message)
        {
            if (!_isEnabled) return;
            
            // This would be a very high-frequency event, so we just count it rather than creating markers
            Profiler.BeginSample("Logging.MessageCreated");
            Profiler.EndSample();
        }
        
        /// <summary>
        /// Handles log processing messages.
        /// </summary>
        /// <param name="message">The log processing message.</param>
        private void OnLogProcessing(LogProcessingMessage message)
        {
            if (!_isEnabled) return;
            
            _messageProcessingMarker.Begin();
            
            // Specific tag-based profiling if the message contains a tag
            if (message.Entry != null)
            {
                FixedString32Bytes fixedTagName = message.Entry.GetTagString();
                string tagName = fixedTagName.ToString();
                
                if (!string.IsNullOrEmpty(tagName))
                {
                    GetOrCreateMarker($"Logging.Tag.{tagName}").Begin();
                    GetOrCreateMarker($"Logging.Tag.{tagName}").End();
                }
            }
            
            _messageProcessingMarker.End();
        }
        
        /// <summary>
        /// Handles log entry written messages.
        /// </summary>
        /// <param name="message">The log entry written message.</param>
        private void OnLogEntryWritten(LogEntryWrittenMessage message)
        {
            if (!_isEnabled) return;
            
            _targetWriteMarker.Begin();
            
            // Add any target-specific profiling here if needed
            if (!string.IsNullOrEmpty(message.TargetName))
            {
                GetOrCreateMarker($"Logging.Target.{message.TargetName}").Begin();
                GetOrCreateMarker($"Logging.Target.{message.TargetName}").End();
            }
            
            _targetWriteMarker.End();
        }
        
        /// <summary>
        /// Handles log flush messages.
        /// </summary>
        /// <param name="message">The log flush message.</param>
        private void OnLogFlush(LogFlushMessage message)
        {
            if (!_isEnabled) return;
            
            _queueFlushMarker.Begin();
            
            // Record performance metrics for the flush operation
            Profiler.BeginSample($"Logging.Flush.{message.ProcessedCount}Messages");
            Profiler.EndSample();
            
            _queueFlushMarker.End();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Begins a custom profiling section.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        public void BeginSample(string name)
        {
            if (!_isEnabled || string.IsNullOrEmpty(name)) return;
            GetOrCreateMarker(name).Begin();
        }
        
        /// <summary>
        /// Ends the most recently started profiling section.
        /// </summary>
        /// <param name="name">The name of the section.</param>
        public void EndSample(string name)
        {
            if (!_isEnabled || string.IsNullOrEmpty(name)) return;
            if (_markers.TryGetValue(name, out var marker))
            {
                marker.End();
            }
        }
        
        /// <summary>
        /// Creates a scoped profiler marker that automatically closes when disposed.
        /// </summary>
        /// <param name="name">The name of the marker.</param>
        /// <returns>A disposable profiler scope.</returns>
        public IDisposable CreateScopedMarker(string name)
        {
            if (!_isEnabled || string.IsNullOrEmpty(name))
                return new DummyDisposable();
                
            return new ProfilerScope(GetOrCreateMarker(name));
        }
        
        /// <summary>
        /// Resets all profiling state, clearing cached markers.
        /// </summary>
        public void Reset()
        {
            _markers.Clear();
            _stopwatch.Reset();
        }
        
        /// <summary>
        /// Disposes of resources and unsubscribes from messages.
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from all message bus subscriptions
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            
            _subscriptions.Clear();
            _markers.Clear();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets or creates a profiler marker with the specified name.
        /// </summary>
        /// <param name="name">The name of the marker.</param>
        /// <returns>The profiler marker.</returns>
        private ProfilerMarker GetOrCreateMarker(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Marker name cannot be null or empty", nameof(name));
                
            if (!_markers.TryGetValue(name, out var marker))
            {
                marker = new ProfilerMarker(name);
                _markers[name] = marker;
            }
            return marker;
        }
        
        #endregion
        
        #region Nested Types
        
        /// <summary>
        /// A scope that automatically ends a profiler marker when disposed.
        /// </summary>
        private sealed class ProfilerScope : IDisposable
        {
            private readonly ProfilerMarker _marker;
            private bool _disposed;
            
            /// <summary>
            /// Creates a new profiler scope with the specified marker.
            /// </summary>
            /// <param name="marker">The profiler marker to use.</param>
            public ProfilerScope(ProfilerMarker marker)
            {
                _marker = marker;
                _marker.Begin();
            }
            
            /// <summary>
            /// Ends the profiler marker when disposed.
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _marker.End();
                    _disposed = true;
                }
            }
        }
        
        /// <summary>
        /// A dummy disposable that does nothing when disposed.
        /// Used when profiling is disabled to avoid null checks.
        /// </summary>
        private sealed class DummyDisposable : IDisposable
        {
            /// <summary>
            /// Does nothing when disposed.
            /// </summary>
            public void Dispose() { }
        }
        
        #endregion
    }
}