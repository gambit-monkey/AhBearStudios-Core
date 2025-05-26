using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Events;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using UnityEngine;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// System for monitoring and alerting on metric and session thresholds
    /// using the MessageBus for communication
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
        private readonly IMessageBus _messageBus;
        
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
        /// <param name="messageBus">Reference to the message bus for publishing alerts</param>
        /// <param name="logToConsole">Whether to log alerts to the console</param>
        public ThresholdAlertSystem(IProfilerManager profilerManager, IMessageBus messageBus, bool logToConsole = true)
        {
            _profilerManager = profilerManager ?? throw new ArgumentNullException(nameof(profilerManager));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            LogToConsole = logToConsole;
            _isRunning = false;
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
        /// <param name="callback">Optional callback to invoke when the threshold is exceeded (for backward compatibility)</param>
        /// <param name="cooldownSeconds">Cooldown period in seconds before allowing another alert</param>
        public void RegisterMetricThreshold(ProfilerTag metricTag, double threshold, 
            Action<MetricEventArgs> callback = null, float cooldownSeconds = DefaultAlertCooldown)
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
                Callback = callback, // Can be null if using MessageBus exclusively
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
        /// <param name="callback">Optional callback to invoke when the threshold is exceeded (for backward compatibility)</param>
        /// <param name="cooldownSeconds">Cooldown period in seconds before allowing another alert</param>
        public void RegisterSessionThreshold(ProfilerTag sessionTag, double thresholdMs, 
            Action<ProfilerSessionEventArgs> callback = null, float cooldownSeconds = DefaultAlertCooldown)
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
                Callback = callback, // Can be null if using MessageBus exclusively
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
        /// Check a completed session against registered thresholds
        /// </summary>
        /// <param name="sessionTag">The tag of the completed session</param>
        /// <param name="durationMs">The duration of the session in milliseconds</param>
        public void CheckSessionThreshold(ProfilerTag sessionTag, double durationMs)
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
                    
                    // Create event args for backward compatibility
                    var eventArgs = new ProfilerSessionEventArgs(sessionTag, durationMs);
                    
                    // Create and publish message via MessageBus
                    var message = new SessionAlertMessage(eventArgs, threshold.ThresholdMs);
                    _messageBus.PublishMessage(message);
                    
                    // Log if enabled
                    if (LogToConsole)
                    {
                        Debug.LogWarning($"[Profiler] Session alert: {sessionTag.FullName} took {durationMs:F2}ms (threshold: {threshold.ThresholdMs:F2}ms)");
                    }
                    
                    // Call legacy callback if provided (for backward compatibility)
                    try
                    {
                        threshold.Callback?.Invoke(eventArgs);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in session threshold callback: {e.Message}");
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
                    
                    // Create event args for backward compatibility
                    var eventArgs = new MetricEventArgs(metricTag, value, threshold.ThresholdValue, unit);
                    
                    // Create and publish message via MessageBus
                    var message = new MetricThresholdExceededMessage(metricTag, value, threshold.ThresholdValue, unit);
                    _messageBus.PublishMessage(message);
                    
                    // Log if enabled
                    if (LogToConsole)
                    {
                        Debug.LogWarning($"[Profiler] Metric alert: {metricTag.FullName} = {value:F2}{unit} (threshold: {threshold.ThresholdValue:F2}{unit})");
                    }
                    
                    // Call legacy callback if provided (for backward compatibility)
                    try
                    {
                        threshold.Callback?.Invoke(eventArgs);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in metric threshold callback: {e.Message}");
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
            /// Callback to invoke when the threshold is exceeded (legacy support)
            /// </summary>
            public Action<MetricEventArgs> Callback;
            
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
            /// Callback to invoke when the threshold is exceeded (legacy support)
            /// </summary>
            public Action<ProfilerSessionEventArgs> Callback;
            
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