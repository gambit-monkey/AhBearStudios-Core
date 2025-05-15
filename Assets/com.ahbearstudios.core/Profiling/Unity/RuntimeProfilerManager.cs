using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Events;
using UnityEngine;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Central manager for all profiling activities
    /// </summary>
    public class RuntimeProfilerManager : MonoBehaviour
    {
        #region Singleton
        private static RuntimeProfilerManager _instance;
        
        /// <summary>
        /// Get the singleton instance of the profiler manager
        /// </summary>
        public static RuntimeProfilerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[RuntimeProfiler]");
                    _instance = go.AddComponent<RuntimeProfilerManager>();
                    DontDestroyOnLoad(go);
                }
                
                return _instance;
            }
        }
        #endregion
        
        /// <summary>
        /// Whether profiling is currently enabled
        /// </summary>
        public bool IsEnabled { get; private set; }
        
        /// <summary>
        /// Whether to log profiling events to the console
        /// </summary>
        public bool LogToConsole { get; set; }
        
        /// <summary>
        /// System metrics tracker for ProfilerRecorder metrics
        /// </summary>
        public SystemMetricsTracker SystemMetrics { get; set; }
        
        /// <summary>
        /// ThresholdAlerting system for monitoring metrics
        /// </summary>
        public ThresholdAlertSystem AlertSystem { get; private set; }
        
        // Dictionary of active profiling sessions
        private readonly Dictionary<ProfilerTag, List<ProfilerSession>> _activeSessions = new Dictionary<ProfilerTag, List<ProfilerSession>>();
        
        // Dictionary of historical profiling data
        private readonly Dictionary<ProfilerTag, CircularBuffer<double>> _sessionHistory = new Dictionary<ProfilerTag, CircularBuffer<double>>();
        
        // Circular buffer size for session history
        private const int SessionHistorySize = 120;
        
        // Stats for custom code profiling
        private readonly Dictionary<ProfilerTag, ProfileStats> _profilingStats = new Dictionary<ProfilerTag, ProfileStats>();
        
        /// <summary>
        /// Event fired when a profiling session ends
        /// </summary>
        public event EventHandler<ProfilerSessionEventArgs> SessionCompleted;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            SystemMetrics = new SystemMetricsTracker();
            AlertSystem = new ThresholdAlertSystem();
            
            IsEnabled = true;
            LogToConsole = Debug.isDebugBuild;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            SystemMetrics?.Dispose();
            AlertSystem?.Dispose();
        }
        
        private void Update()
        {
            if (!IsEnabled)
                return;
                
            // Update system metrics
            SystemMetrics?.Update(Time.deltaTime);
            
            // Update threshold alerts
            AlertSystem?.Update(Time.deltaTime);
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
        }
        
        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            if (!IsEnabled)
                return null;
                
            var session = new ProfilerSession(tag, this);
            return session;
        }
        
        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return BeginScope(new ProfilerTag(category, name));
        }
        
        /// <summary>
        /// Notifies that a session has started
        /// </summary>
        internal void OnSessionStarted(ProfilerSession session)
        {
            if (!_activeSessions.TryGetValue(session.Tag, out var sessions))
            {
                sessions = new List<ProfilerSession>();
                _activeSessions[session.Tag] = sessions;
            }
            
            sessions.Add(session);
        }
        
        /// <summary>
        /// Notifies that a session has ended
        /// </summary>
        internal void OnSessionEnded(ProfilerSession session, double durationMs)
        {
            // Remove from active sessions
            if (_activeSessions.TryGetValue(session.Tag, out var sessions))
            {
                sessions.Remove(session);
                
                if (sessions.Count == 0)
                {
                    _activeSessions.Remove(session.Tag);
                }
            }
            
            // Add to history
            if (!_sessionHistory.TryGetValue(session.Tag, out var history))
            {
                history = new CircularBuffer<double>(SessionHistorySize);
                _sessionHistory[session.Tag] = history;
            }
            
            history.Add(durationMs);
            
            // Update stats
            if (!_profilingStats.TryGetValue(session.Tag, out var stats))
            {
                stats = new ProfileStats();
                _profilingStats[session.Tag] = stats;
            }
            
            stats.AddSample(durationMs);
            
            // Log if enabled
            if (LogToConsole)
            {
                Debug.Log($"[Profiler] {session.Tag.FullName}: {durationMs:F3}ms");
            }
            
            // Fire event
            SessionCompleted?.Invoke(this, new ProfilerSessionEventArgs(session.Tag, durationMs));
            
            // Check for alerts
            AlertSystem?.CheckSessionAlert(session.Tag, durationMs);
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
            
            return null;
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
            foreach (var stats in _profilingStats.Values)
            {
                stats.Reset();
            }
            
            _sessionHistory.Clear();
        }
        
        /// <summary>
        /// Get all active profiling sessions
        /// </summary>
        public IReadOnlyDictionary<ProfilerTag, List<ProfilerSession>> GetActiveSessions()
        {
            return _activeSessions;
        }
        
        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold, Action<MetricEventArgs> callback)
        {
            var metric = SystemMetrics.GetMetric(metricTag);
            if (metric != null)
            {
                AlertSystem.RegisterMetricAlert(metric, threshold, callback);
            }
        }
        
        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs, Action<ProfilerSessionEventArgs> callback)
        {
            AlertSystem.RegisterSessionAlert(sessionTag, thresholdMs, callback);
        }
    }
}