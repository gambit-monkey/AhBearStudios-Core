using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Events;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Default implementation of the IProfilerManager interface that integrates with
    /// Unity's profiling system and the MessageBus
    /// </summary>
    public class DefaultRuntimeProfilerManager : IProfilerManager
    {
        private readonly IMessageBus _messageBus;
        
        // Dictionary of active profiling sessions
        private readonly Dictionary<ProfilerTag, List<IProfilerSession>> _activeSessions = 
            new Dictionary<ProfilerTag, List<IProfilerSession>>();
        
        // Dictionary of historical profiling data
        private readonly Dictionary<ProfilerTag, CircularBuffer<double>> _sessionHistory = 
            new Dictionary<ProfilerTag, CircularBuffer<double>>();
            
        // Stats for custom code profiling
        private readonly Dictionary<ProfilerTag, ProfileStats> _profilingStats = 
            new Dictionary<ProfilerTag, ProfileStats>();
            
        // Constant for history buffer size
        private const int SessionHistorySize = 120;
        
        // Event handlers for profiling events
        private event EventHandler<ProfilerSessionEventArgs> SessionCompleted;
        private event EventHandler<ProfilerSessionEventArgs> SessionAlertTriggered;
        private event EventHandler<MetricEventArgs> MetricAlertTriggered;

        /// <summary>
        /// Gets whether profiling is currently enabled
        /// </summary>
        public bool IsEnabled { get; private set; }
        
        /// <summary>
        /// Gets or sets whether to log profiling events to the console
        /// </summary>
        public bool LogToConsole { get; set; }
        
        /// <summary>
        /// Gets the system metrics tracker
        /// </summary>
        public SystemMetricsTracker SystemMetrics { get; }
        
        /// <summary>
        /// Gets the threshold alert system
        /// </summary>
        public ThresholdAlertSystem AlertSystem { get; }

        /// <summary>
        /// Creates a new default runtime profiler manager
        /// </summary>
        /// <param name="messageBus">The message bus to use for publishing profiling messages</param>
        /// <param name="startEnabled">Whether to start profiling immediately</param>
        /// <param name="logToConsole">Whether to log profiling events to the console</param>
        public DefaultRuntimeProfilerManager(IMessageBus messageBus, bool startEnabled = true, bool logToConsole = true)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            SystemMetrics = new SystemMetricsTracker();
            AlertSystem = new ThresholdAlertSystem(this);
            
            IsEnabled = startEnabled;
            LogToConsole = logToConsole;
            
            // Register profiler message types with the message registry
            RegisterProfilerMessages(_messageBus.GetMessageRegistry());
            
            if (startEnabled)
            {
                SystemMetrics.Start();
                AlertSystem.Start();
            }
        }

        /// <summary>
        /// Registers all profiler messages with the message registry
        /// </summary>
        private void RegisterProfilerMessages(IMessageRegistry registry)
        {
            if (registry == null)
                return;
                
            // Register individual message types with their type codes
            registry.RegisterMessageType(typeof(ProfilerSessionCompletedMessage), 1001);
            registry.RegisterMessageType(typeof(ProfilingStartedMessage), 1002);
            registry.RegisterMessageType(typeof(ProfilingStoppedMessage), 1003);
            registry.RegisterMessageType(typeof(StatsResetMessage), 1004);
            registry.RegisterMessageType(typeof(MetricAlertMessage), 1005);
            registry.RegisterMessageType(typeof(SessionAlertMessage), 1006);
            
            if (LogToConsole)
            {
                Debug.Log("[Profiler] Registered profiler message types with message registry");
            }
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            if (IsEnabled)
                return;
                
            IsEnabled = true;
            SystemMetrics?.Start();
            AlertSystem?.Start();
            
            // Publish message
            _messageBus.PublishMessage(new ProfilingStartedMessage());
            
            if (LogToConsole)
            {
                Debug.Log("[Profiler] Profiling started");
            }
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            if (!IsEnabled)
                return;
                
            IsEnabled = false;
            SystemMetrics?.Stop();
            AlertSystem?.Stop();
            
            // Publish message
            _messageBus.PublishMessage(new ProfilingStoppedMessage());
            
            if (LogToConsole)
            {
                Debug.Log("[Profiler] Profiling stopped");
            }
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        public IProfilerSession BeginScope(ProfilerTag tag)
        {
            if (!IsEnabled)
                return null;
                
            var session = new ProfilerSession(tag, this);
            return session;
        }
        
        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        public IProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return BeginScope(new ProfilerTag(category, name));
        }

        /// <summary>
        /// Notifies that a session has started
        /// </summary>
        public void OnSessionStarted(IProfilerSession session)
        {
            if (!_activeSessions.TryGetValue(session.Tag, out var sessions))
            {
                sessions = new List<IProfilerSession>();
                _activeSessions[session.Tag] = sessions;
            }
            
            sessions.Add(session);
        }

        /// <summary>
        /// Notifies that a session has ended
        /// </summary>
        public void OnSessionEnded(IProfilerSession session, double durationMs)
        {
            // Update active sessions
            if (_activeSessions.TryGetValue(session.Tag, out var sessions))
            {
                sessions.Remove(session);
                if (sessions.Count == 0)
                {
                    _activeSessions.Remove(session.Tag);
                }
            }
            
            // Update stats
            if (!_profilingStats.TryGetValue(session.Tag, out var stats))
            {
                stats = new ProfileStats();
                _profilingStats[session.Tag] = stats;
            }
            
            stats.AddSample(durationMs);
            
            // Update history
            if (!_sessionHistory.TryGetValue(session.Tag, out var history))
            {
                history = new CircularBuffer<double>(SessionHistorySize);
                _sessionHistory[session.Tag] = history;
            }
            
            history.Add(durationMs);
            
            // Publish message
            _messageBus.PublishMessage(new ProfilerSessionCompletedMessage(session.Tag, durationMs));
            
            // Raise event
            var args = new ProfilerSessionEventArgs(session.Tag, durationMs);
            SessionCompleted?.Invoke(this, args);
            
            if (LogToConsole)
            {
                Debug.Log($"[Profiler] Session {session.Tag.FullName} completed in {durationMs:F2}ms");
            }
        }

        /// <summary>
        /// Get stats for a specific profiling tag
        /// </summary>
        public ProfileStats GetStats(ProfilerTag tag)
        {
            if (_profilingStats.TryGetValue(tag, out var stats))
            {
                return stats;
            }
            
            return new ProfileStats(); // Empty stats
        }

        /// <summary>
        /// Get all profiling stats
        /// </summary>
        public IReadOnlyDictionary<ProfilerTag, ProfileStats> GetAllStats()
        {
            return _profilingStats;
        }

        /// <summary>
        /// Get history for a specific profiling tag
        /// </summary>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            if (_sessionHistory.TryGetValue(tag, out var history))
            {
                return history.ToArray();
            }
            
            return Array.Empty<double>();
        }

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _profilingStats.Clear();
            _sessionHistory.Clear();
            
            // Publish message
            _messageBus.PublishMessage(new StatsResetMessage());
            
            if (LogToConsole)
            {
                Debug.Log("[Profiler] All stats reset");
            }
        }

        /// <summary>
        /// Get all active profiling sessions
        /// </summary>
        public IReadOnlyDictionary<ProfilerTag, List<IProfilerSession>> GetActiveSessions()
        {
            return _activeSessions;
        }

        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold, Action<MetricEventArgs> callback)
        {
            if (callback != null)
            {
                MetricAlertTriggered += (sender, args) =>
                {
                    if (args.MetricTag.Equals(metricTag))
                    {
                        callback(args);
                    }
                };
            }
            
            AlertSystem.RegisterMetricThreshold(metricTag, threshold, OnInternalMetricAlert);
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs, Action<ProfilerSessionEventArgs> callback)
        {
            if (callback != null)
            {
                SessionAlertTriggered += (sender, args) =>
                {
                    if (args.Tag.Equals(sessionTag))
                    {
                        callback(args);
                    }
                };
            }
            
            AlertSystem.RegisterSessionThreshold(sessionTag, thresholdMs, OnInternalSessionAlert);
        }

        /// <summary>
        /// Update the profiler manager (should be called regularly)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsEnabled)
                return;
                
            SystemMetrics?.Update();
            AlertSystem?.Update(deltaTime);
        }

        /// <summary>
        /// Handler for internal session alerts
        /// </summary>
        private void OnInternalSessionAlert(ProfilerSessionEventArgs args)
        {
            // Get the threshold that was exceeded
            AlertSystem.GetSessionThreshold(args.Tag, out double thresholdMs);
            
            // Publish message
            _messageBus.PublishMessage(new SessionAlertMessage(args, thresholdMs));
            
            // Raise event
            SessionAlertTriggered?.Invoke(this, args);
            
            if (LogToConsole)
            {
                Debug.LogWarning($"[Profiler] Session alert: {args.Tag.FullName} took {args.DurationMs:F2}ms (threshold: {thresholdMs:F2}ms)");
            }
        }
    }
}