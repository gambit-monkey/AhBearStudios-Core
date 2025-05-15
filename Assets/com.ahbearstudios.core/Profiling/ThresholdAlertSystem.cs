using System;
using System.Collections.Generic;
using UnityEngine;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// System for monitoring and alerting on metric thresholds
    /// </summary>
    public class ThresholdAlertSystem : IDisposable
    {
        // Metric alerts
        private readonly Dictionary<SystemMetric, List<MetricAlert>> _metricAlerts = new Dictionary<SystemMetric, List<MetricAlert>>();
        
        // Session alerts
        private readonly Dictionary<ProfilerTag, List<SessionAlert>> _sessionAlerts = new Dictionary<ProfilerTag, List<SessionAlert>>();
        
        // Cooldown period for alerts
        private const float DefaultAlertCooldown = 5.0f;
        
        // Is the alert system running
        private bool _isRunning = false;
        
        /// <summary>
        /// Create a new threshold alert system
        /// </summary>
        public ThresholdAlertSystem()
        {
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
        public void Update(float deltaTime)
        {
            if (!_isRunning)
                return;
                
            // Check for metric alerts
            CheckMetricAlerts();
            
            // Update cooldowns
            UpdateCooldowns(deltaTime);
        }
        
        /// <summary>
        /// Register a metric alert
        /// </summary>
        public void RegisterMetricAlert(SystemMetric metric, double threshold, Action<MetricEventArgs> callback, float cooldownSeconds = DefaultAlertCooldown)
        {
            if (!_metricAlerts.TryGetValue(metric, out var alerts))
            {
                alerts = new List<MetricAlert>();
                _metricAlerts[metric] = alerts;
            }
            
            var alert = new MetricAlert
            {
                Metric = metric,
                Threshold = threshold,
                Callback = callback,
                Cooldown = cooldownSeconds,
                CurrentCooldown = 0f
            };
            
            alerts.Add(alert);
        }
        
        /// <summary>
        /// Register a session alert
        /// </summary>
        public void RegisterSessionAlert(ProfilerTag tag, double thresholdMs, Action<ProfilerSessionEventArgs> callback, float cooldownSeconds = DefaultAlertCooldown)
        {
            if (!_sessionAlerts.TryGetValue(tag, out var alerts))
            {
                alerts = new List<SessionAlert>();
                _sessionAlerts[tag] = alerts;
            }
            
            var alert = new SessionAlert
            {
                Tag = tag,
                ThresholdMs = thresholdMs,
                Callback = callback,
                Cooldown = cooldownSeconds,
                CurrentCooldown = 0f
            };
            
            alerts.Add(alert);
        }
        
        /// <summary>
        /// Check a session against registered alerts
        /// </summary>
        public void CheckSessionAlert(ProfilerTag tag, double durationMs)
        {
            if (!_isRunning)
                return;
                
            if (!_sessionAlerts.TryGetValue(tag, out var alerts))
                return;
                
            foreach (var alert in alerts)
            {
                if (alert.CurrentCooldown <= 0f && durationMs >= alert.ThresholdMs)
                {
                    // Trigger alert
                    alert.CurrentCooldown = alert.Cooldown;
                    
                    try
                    {
                        var eventArgs = new ProfilerSessionEventArgs(tag, durationMs);
                        alert.Callback?.Invoke(eventArgs);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in session alert callback: {e.Message}");
                    }
                    
                    // Log alert
                    Debug.LogWarning($"[Profiler Alert] Session {tag.FullName} exceeded threshold: {durationMs:F2}ms > {alert.ThresholdMs:F2}ms");
                }
            }
        }
        
        /// <summary>
        /// Check all metrics against registered alerts
        /// </summary>
        private void CheckMetricAlerts()
        {
            if (!_isRunning)
                return;
                
            foreach (var kvp in _metricAlerts)
            {
                var metric = kvp.Key;
                var alerts = kvp.Value;
                
                foreach (var alert in alerts)
                {
                    if (alert.CurrentCooldown <= 0f && metric.LastValue >= alert.Threshold)
                    {
                        // Trigger alert
                        alert.CurrentCooldown = alert.Cooldown;
                        
                        try
                        {
                            var eventArgs = new MetricEventArgs(metric, metric.LastValue);
                            alert.Callback?.Invoke(eventArgs);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error in metric alert callback: {e.Message}");
                        }
                        
                        // Log alert
                        Debug.LogWarning($"[Profiler Alert] Metric {metric.Tag.FullName} exceeded threshold: {metric.GetFormattedLastValue()} > {alert.Threshold:F2} {metric.Unit}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Update cooldowns for all alerts
        /// </summary>
        private void UpdateCooldowns(float deltaTime)
        {
            // Update metric alert cooldowns
            foreach (var alerts in _metricAlerts.Values)
            {
                foreach (var alert in alerts)
                {
                    if (alert.CurrentCooldown > 0f)
                    {
                        alert.CurrentCooldown -= deltaTime;
                    }
                }
            }
            
            // Update session alert cooldowns
            foreach (var alerts in _sessionAlerts.Values)
            {
                foreach (var alert in alerts)
                {
                    if (alert.CurrentCooldown > 0f)
                    {
                        alert.CurrentCooldown -= deltaTime;
                    }
                }
            }
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            _metricAlerts.Clear();
            _sessionAlerts.Clear();
        }
        
        /// <summary>
        /// Metric alert configuration
        /// </summary>
        private class MetricAlert
        {
            public SystemMetric Metric;
            public double Threshold;
            public Action<MetricEventArgs> Callback;
            public float Cooldown;
            public float CurrentCooldown;
        }
        
        /// <summary>
        /// Session alert configuration
        /// </summary>
        private class SessionAlert
        {
            public ProfilerTag Tag;
            public double ThresholdMs;
            public Action<ProfilerSessionEventArgs> Callback;
            public float Cooldown;
            public float CurrentCooldown;
        }
    }
}