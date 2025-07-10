using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Unity.Configuration
{
    /// <summary>
    /// ScriptableObject configuration for the profiling system.
    /// Contains all settings and presets for profiler behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "ProfilerConfiguration", menuName = "AhBear Studios/Profiling/Profiler Configuration")]
    public class ProfilerConfiguration : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] private bool _enableProfiling = true;
        [SerializeField] private bool _logToConsole = true;
        [SerializeField] private bool _logAlertsToConsole = true;
        [SerializeField] private float _updateInterval = 0.1f;
        
        [Header("System Metrics")]
        [SerializeField] private List<SystemMetricConfig> _systemMetrics = new List<SystemMetricConfig>();
        
        [Header("Threshold Alerts")]
        [SerializeField] private List<ThresholdAlertConfig> _thresholdAlerts = new List<ThresholdAlertConfig>();
        
        [Header("UI Settings")]
        [SerializeField] private bool _enableRuntimeUI = true;
        [SerializeField] private bool _showUIOnStart = false;
        [SerializeField] private KeyCode _toggleUIKey = KeyCode.F3;
        [SerializeField] private float _uiUpdateInterval = 0.5f;
        
        [Header("Performance")]
        [SerializeField] private int _historyBufferSize = 120;
        [SerializeField] private int _maxConcurrentSessions = 1000;
        [SerializeField] private bool _enableBurstOptimizations = true;
        
        /// <summary>
        /// Gets whether profiling is enabled
        /// </summary>
        public bool EnableProfiling => _enableProfiling;
        
        /// <summary>
        /// Gets whether to log to console
        /// </summary>
        public bool LogToConsole => _logToConsole;
        
        /// <summary>
        /// Gets whether to log alerts to console
        /// </summary>
        public bool LogAlertsToConsole => _logAlertsToConsole;
        
        /// <summary>
        /// Gets the update interval in seconds
        /// </summary>
        public float UpdateInterval => _updateInterval;
        
        /// <summary>
        /// Gets the system metrics configuration
        /// </summary>
        public IReadOnlyList<SystemMetricConfig> SystemMetrics => _systemMetrics;
        
        /// <summary>
        /// Gets the threshold alerts configuration
        /// </summary>
        public IReadOnlyList<ThresholdAlertConfig> ThresholdAlerts => _thresholdAlerts;
        
        /// <summary>
        /// Gets whether runtime UI is enabled
        /// </summary>
        public bool EnableRuntimeUI => _enableRuntimeUI;
        
        /// <summary>
        /// Gets whether to show UI on start
        /// </summary>
        public bool ShowUIOnStart => _showUIOnStart;
        
        /// <summary>
        /// Gets the UI toggle key
        /// </summary>
        public KeyCode ToggleUIKey => _toggleUIKey;
        
        /// <summary>
        /// Gets the UI update interval
        /// </summary>
        public float UIUpdateInterval => _uiUpdateInterval;
        
        /// <summary>
        /// Gets the history buffer size
        /// </summary>
        public int HistoryBufferSize => _historyBufferSize;
        
        /// <summary>
        /// Gets the maximum concurrent sessions
        /// </summary>
        public int MaxConcurrentSessions => _maxConcurrentSessions;
        
        /// <summary>
        /// Gets whether Burst optimizations are enabled
        /// </summary>
        public bool EnableBurstOptimizations => _enableBurstOptimizations;
        
        /// <summary>
        /// Initializes the configuration with default values
        /// </summary>
        public void InitializeDefaults()
        {
            if (_systemMetrics.Count == 0)
            {
                AddDefaultSystemMetrics();
            }
            
            if (_thresholdAlerts.Count == 0)
            {
                AddDefaultThresholdAlerts();
            }
        }
        
        /// <summary>
        /// Adds default system metrics
        /// </summary>
        private void AddDefaultSystemMetrics()
        {
            _systemMetrics.Clear();
            
            _systemMetrics.Add(new SystemMetricConfig
            {
                Name = "Frame Time",
                Category = ProfilerCategory.Render,
                StatName = "FrameTime",
                Unit = "ms",
                Enabled = true
            });
            
            _systemMetrics.Add(new SystemMetricConfig
            {
                Name = "Main Thread",
                Category = ProfilerCategory.Render,
                StatName = "Main Thread",
                Unit = "ms",
                Enabled = true
            });
            
            _systemMetrics.Add(new SystemMetricConfig
            {
                Name = "GC Alloc",
                Category = ProfilerCategory.Memory,
                StatName = "GC.Alloc.Size",
                Unit = "KB",
                Enabled = true
            });
            
            _systemMetrics.Add(new SystemMetricConfig
            {
                Name = "GC Count",
                Category = ProfilerCategory.Memory,
                StatName = "GC.Alloc.Count",
                Unit = "count",
                Enabled = true
            });
            
            _systemMetrics.Add(new SystemMetricConfig
            {
                Name = "Draw Calls",
                Category = ProfilerCategory.Render,
                StatName = "Batches Count",
                Unit = "count",
                Enabled = true
            });
            
            _systemMetrics.Add(new SystemMetricConfig
            {
                Name = "Physics Step",
                Category = ProfilerCategory.Physics,
                StatName = "Physics.Step",
                Unit = "ms",
                Enabled = true
            });
        }
        
        /// <summary>
        /// Adds default threshold alerts
        /// </summary>
        private void AddDefaultThresholdAlerts()
        {
            _thresholdAlerts.Clear();
            
            _thresholdAlerts.Add(new ThresholdAlertConfig
            {
                Name = "Frame Time",
                Category = ProfilerCategory.Render,
                Threshold = 33.3, // 30 FPS
                CooldownSeconds = 5.0f,
                IsSessionAlert = false,
                Enabled = true
            });
            
            _thresholdAlerts.Add(new ThresholdAlertConfig
            {
                Name = "GC Alloc",
                Category = ProfilerCategory.Memory,
                Threshold = 1000.0, // 1MB
                CooldownSeconds = 10.0f,
                IsSessionAlert = false,
                Enabled = true
            });
            
            _thresholdAlerts.Add(new ThresholdAlertConfig
            {
                Name = "Slow Update",
                Category = ProfilerCategory.Scripts,
                Threshold = 10.0, // 10ms
                CooldownSeconds = 5.0f,
                IsSessionAlert = true,
                Enabled = false
            });
        }
        
        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool Validate()
        {
            if (_updateInterval <= 0)
            {
                Debug.LogError("Update interval must be greater than 0");
                return false;
            }
            
            if (_historyBufferSize <= 0)
            {
                Debug.LogError("History buffer size must be greater than 0");
                return false;
            }
            
            if (_maxConcurrentSessions <= 0)
            {
                Debug.LogError("Max concurrent sessions must be greater than 0");
                return false;
            }
            
            return true;
        }
        
        private void OnValidate()
        {
            _updateInterval = Mathf.Max(0.01f, _updateInterval);
            _historyBufferSize = Mathf.Max(1, _historyBufferSize);
            _maxConcurrentSessions = Mathf.Max(1, _maxConcurrentSessions);
            _uiUpdateInterval = Mathf.Max(0.1f, _uiUpdateInterval);
        }
    }
}