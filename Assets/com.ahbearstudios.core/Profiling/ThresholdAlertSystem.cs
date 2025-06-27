// File: Assets/com.ahbearstudios.core/Profiling/ThresholdAlertSystem.cs
using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using UnityEngine;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// System for monitoring and alerting on metric and session thresholds
    /// using the MessageBusService for communication
    /// </summary>
    public class ThresholdAlertSystem : IDisposable
    {
        /// <summary>
        /// Reference to the profiler manager
        /// </summary>
        private readonly IProfilerManager _profilerManager;
        
        /// <summary>
        /// Reference to the message bus for publishing alerts
        /// </summary>
        private readonly IMessageBusService _messageBusService;
        
        /// <summary>
        /// Metric alerts mapped by ProfilerTag
        /// </summary>
        private readonly Dictionary<ProfilerTag, List<MetricThreshold>> _metricThresholds = 
            new Dictionary<ProfilerTag, List<MetricThreshold>>();
        
        /// <summary>
        /// Session alerts mapped by ProfilerTag
        /// </summary>
        private readonly Dictionary<ProfilerTag, List<SessionThreshold>> _sessionThresholds = 
            new Dictionary<ProfilerTag, List<SessionThreshold>>();
        
        /// <summary>
        /// Default cooldown period for alerts in seconds
        /// </summary>
        private const float DefaultAlertCooldown = 5.0f;
        
        /// <summary>
        /// Is the alert system running
        /// </summary>
        private bool _isRunning = false;
        
        /// <summary>
        /// Whether to log alert events to the console
        /// </summary>
        public bool LogToConsole { get; set; }
        
        /// <summary>
        /// Gets whether the alert system is currently enabled
        /// </summary>
        public bool IsEnabled => _isRunning;
        
        /// <summary>
        /// Creates a new threshold alert system connected to a profiler manager
        /// </summary>
        /// <param name="profilerManager">Reference to the profiler manager implementation</param>
        /// <param name="messageBusService">Reference to the message bus for publishing alerts</param>
        /// <param name="logToConsole">Whether to log alerts to the console</param>
        public ThresholdAlertSystem(IProfilerManager profilerManager, IMessageBusService messageBusService, bool logToConsole = true)
        {
            _profilerManager = profilerManager ?? throw new ArgumentNullException(nameof(profilerManager));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            LogToConsole = logToConsole;
            _isRunning = false;
            
            // Subscribe to session completed messages
            _messageBusService.GetSubscriber<ProfilerSessionCompletedMessage>().Subscribe(OnSessionCompleted);
        }
        
        /// <summary>
        /// Start the alert system
        /// </summary>
        public void Start()
        {
            _isRunning = true;
        }
        
        /// <summary>
        /// Stop the alert system
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }
        
        /// <summary>
        /// Update the alert system
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last update</param>
        public void Update(float deltaTime)
        {
            if (!_isRunning)
                return;
                
            // Check for metric alerts
            CheckMetricThresholds();
            
            // Update cooldowns
            UpdateCooldowns(deltaTime);
        }
        
        /// <summary>
        /// Register a metric threshold for alerting
        /// </summary>
        /// <param name="metricTag">The tag identifying the metric to monitor</param>
        /// <param name="threshold">The threshold value to trigger an alert</param>
        /// <param name="cooldownSeconds">Cooldown period in seconds before allowing another alert</param>
        public void RegisterMetricThreshold(ProfilerTag metricTag, double threshold,
            float cooldownSeconds = DefaultAlertCooldown)
        {
            if (metricTag == null)
                throw new ArgumentNullException(nameof(metricTag));
                
            if (!_metricThresholds.TryGetValue(metricTag, out var thresholds))
            {
                thresholds = new List<MetricThreshold>();
                _metricThresholds[metricTag] = thresholds;
            }
            
            var alert = new MetricThreshold
            {
                MetricTag = metricTag,
                ThresholdValue = threshold,
                CooldownPeriod = cooldownSeconds,
                CurrentCooldown = 0f
            };
            
            thresholds.Add(alert);
        }
        
        /// <summary>
        /// Register a session threshold for alerting
        /// </summary>
        /// <param name="sessionTag">The tag identifying the session to monitor</param>
        /// <param name="thresholdMs">The threshold duration in milliseconds to trigger an alert</param>
        /// <param name="cooldownSeconds">Cooldown period in seconds before allowing another alert</param>
        public void RegisterSessionThreshold(ProfilerTag sessionTag, double thresholdMs, 
            float cooldownSeconds = DefaultAlertCooldown)
        {
            if (sessionTag == null)
                throw new ArgumentNullException(nameof(sessionTag));
                
            if (!_sessionThresholds.TryGetValue(sessionTag, out var thresholds))
            {
                thresholds = new List<SessionThreshold>();
                _sessionThresholds[sessionTag] = thresholds;
            }
            
            var alert = new SessionThreshold
            {
                SessionTag = sessionTag,
                ThresholdMs = thresholdMs,
                CooldownPeriod = cooldownSeconds,
                CurrentCooldown = 0f
            };
            
            thresholds.Add(alert);
        }
        
        /// <summary>
        /// Retrieves the threshold value for a specific session tag
        /// </summary>
        /// <param name="sessionTag">The session tag to lookup</param>
        /// <param name="thresholdMs">Output parameter that will contain the threshold value in ms</param>
        /// <returns>True if a threshold was found, false otherwise</returns>
        public bool GetSessionThreshold(ProfilerTag sessionTag, out double thresholdMs)
        {
            thresholdMs = 0;
            
            if (sessionTag == null || !_sessionThresholds.TryGetValue(sessionTag, out var thresholds) || thresholds.Count == 0)
                return false;
                
            // Return the lowest threshold value (most sensitive)
            double lowestThreshold = double.MaxValue;
            foreach (var threshold in thresholds)
            {
                if (threshold.ThresholdMs < lowestThreshold)
                {
                    lowestThreshold = threshold.ThresholdMs;
                }
            }
            
            thresholdMs = lowestThreshold;
            return true;
        }
        
        /// <summary>
        /// Retrieves the threshold value for a specific metric tag
        /// </summary>
        /// <param name="metricTag">The metric tag to lookup</param>
        /// <param name="thresholdValue">Output parameter that will contain the threshold value</param>
        /// <returns>True if a threshold was found, false otherwise</returns>
        public bool GetMetricThreshold(ProfilerTag metricTag, out double thresholdValue)
        {
            thresholdValue = 0;
            
            if (metricTag == null || !_metricThresholds.TryGetValue(metricTag, out var thresholds) || thresholds.Count == 0)
                return false;
                
            // Return the lowest threshold value (most sensitive)
            double lowestThreshold = double.MaxValue;
            foreach (var threshold in thresholds)
            {
                if (threshold.ThresholdValue < lowestThreshold)
                {
                    lowestThreshold = threshold.ThresholdValue;
                }
            }
            
            thresholdValue = lowestThreshold;
            return true;
        }
        
        /// <summary>
        /// Handler for session completed messages
        /// </summary>
        private void OnSessionCompleted(ProfilerSessionCompletedMessage message)
        {
            if (!_isRunning)
                return;
                
            CheckSessionThreshold(message.Tag, message.DurationMs, message.SessionId);
        }
        
        /// <summary>
        /// Check a completed session against registered thresholds
        /// </summary>
        /// <param name="sessionTag">The tag of the completed session</param>
        /// <param name="durationMs">The duration of the session in milliseconds</param>
        /// <param name="sessionId">The unique identifier of the session</param>
        private void CheckSessionThreshold(ProfilerTag sessionTag, double durationMs, Guid sessionId)
        {
            if (!_isRunning || sessionTag == null)
                return;
                
            if (!_sessionThresholds.TryGetValue(sessionTag, out var thresholds))
                return;
                
            foreach (var threshold in thresholds)
            {
                if (threshold.CurrentCooldown <= 0f && durationMs >= threshold.ThresholdMs)
                {
                    // Trigger alert
                    threshold.CurrentCooldown = threshold.CooldownPeriod;
                    
                    // Create and publish message via MessageBusService
                    _messageBusService.PublishMessage(new SessionAlertMessage(
                        sessionTag, 
                        sessionId,
                        durationMs, 
                        threshold.ThresholdMs));
                    
                    // Log if enabled
                    if (LogToConsole)
                    {
                        Debug.LogWarning($"[Profiler] Session alert: {sessionTag.FullName} took {durationMs:F2}ms (threshold: {threshold.ThresholdMs:F2}ms)");
                    }
                }
            }
        }
        
        /// <summary>
        /// Check all metric values against registered thresholds
        /// </summary>
        private void CheckMetricThresholds()
        {
            if (!_isRunning)
                return;
                
            // Check system metrics against thresholds
            if (_profilerManager?.SystemMetrics != null)
            {
                foreach (var metric in _profilerManager.SystemMetrics.GetAllMetrics())
                {
                    CheckMetricValue(metric.Tag, metric.LastValue, metric.Unit);
                }
            }
        }
        
        /// <summary>
        /// Check a metric value against registered thresholds
        /// </summary>
        /// <param name="metricTag">The tag of the metric</param>
        /// <param name="value">The current value of the metric</param>
        /// <param name="unit">The unit of the metric value (e.g., "MB", "ms")</param>
        public void CheckMetricValue(ProfilerTag metricTag, double value, string unit = "")
        {
            if (!_isRunning || metricTag == null)
                return;
            
            if (!_metricThresholds.TryGetValue(metricTag, out var thresholds))
                return;
            
            foreach (var threshold in thresholds)
            {
                if (threshold.CurrentCooldown <= 0f && value >= threshold.ThresholdValue)
                {
                    // Trigger alert
                    threshold.CurrentCooldown = threshold.CooldownPeriod;
                    
                    // Create and publish message via MessageBusService
                    _messageBusService.PublishMessage(new MetricAlertMessage(
                        metricTag, 
                        value, 
                        threshold.ThresholdValue));
                    
                    // Log if enabled
                    if (LogToConsole)
                    {
                        Debug.LogWarning($"[Profiler] Metric alert: {metricTag.FullName} = {value:F2}{unit} (threshold: {threshold.ThresholdValue:F2}{unit})");
                    }
                }
            }
        }
        
        /// <summary>
        /// Update cooldowns for all alert thresholds
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update in seconds</param>
        private void UpdateCooldowns(float deltaTime)
        {
            // Update metric threshold cooldowns
            foreach (var thresholds in _metricThresholds.Values)
            {
                foreach (var threshold in thresholds)
                {
                    if (threshold.CurrentCooldown > 0f)
                    {
                        threshold.CurrentCooldown -= deltaTime;
                    }
                }
            }
            
            // Update session threshold cooldowns
            foreach (var thresholds in _sessionThresholds.Values)
            {
                foreach (var threshold in thresholds)
                {
                    if (threshold.CurrentCooldown > 0f)
                    {
                        threshold.CurrentCooldown -= deltaTime;
                    }
                }
            }
        }
        
        /// <summary>
        /// Dispose all resources used by the threshold alert system
        /// </summary>
        public void Dispose()
        {
            Stop();
            _metricThresholds.Clear();
            _sessionThresholds.Clear();
        }
        
        /// <summary>
        /// Configuration for a metric threshold
        /// </summary>
        private class MetricThreshold
        {
            /// <summary>
            /// The tag identifying the metric to monitor
            /// </summary>
            public ProfilerTag MetricTag;
            
            /// <summary>
            /// The threshold value to trigger an alert
            /// </summary>
            public double ThresholdValue;
            
            /// <summary>
            /// Cooldown period in seconds before allowing another alert
            /// </summary>
            public float CooldownPeriod;
            
            /// <summary>
            /// Current cooldown time remaining
            /// </summary>
            public float CurrentCooldown;
        }
        
        /// <summary>
        /// Configuration for a session threshold
        /// </summary>
        private class SessionThreshold
        {
            /// <summary>
            /// The tag identifying the session to monitor
            /// </summary>
            public ProfilerTag SessionTag;
            
            /// <summary>
            /// The threshold duration in milliseconds to trigger an alert
            /// </summary>
            public double ThresholdMs;
            
            /// <summary>
            /// Cooldown period in seconds before allowing another alert
            /// </summary>
            public float CooldownPeriod;
            
            /// <summary>
            /// Current cooldown time remaining
            /// </summary>
            public float CurrentCooldown;
        }
    }
}