using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Manages and samples Unity's built-in ProfilerRecorder metrics
    /// </summary>
    public class SystemMetricsTracker : IDisposable
    {
        private readonly Dictionary<ProfilerTag, SystemMetric> _metrics = new Dictionary<ProfilerTag, SystemMetric>();
        private bool _isRunning;
        private float _sampleInterval;
        private float _timeSinceLastSample;
        
        /// <summary>
        /// Create a new SystemMetricsTracker
        /// </summary>
        /// <param name="sampleInterval">Interval in seconds to sample metrics</param>
        public SystemMetricsTracker(float sampleInterval = 0.5f)
        {
            _sampleInterval = sampleInterval;
            _isRunning = false;
            _timeSinceLastSample = 0f;
        }
        
        /// <summary>
        /// Start tracking metrics
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;
                
            _isRunning = true;
            _timeSinceLastSample = 0f;
            
            // Start all recorders
            foreach (var metric in _metrics.Values)
            {
                if (!metric.Recorder.Valid)
                {
                    metric.Reset();
                }
            }
        }
        
        /// <summary>
        /// Stop tracking metrics
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;
                
            _isRunning = false;
        }
        
        /// <summary>
        /// Update metrics based on sample interval
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isRunning)
                return;
                
            _timeSinceLastSample += deltaTime;
            
            if (_timeSinceLastSample >= _sampleInterval)
            {
                SampleAllMetrics();
                _timeSinceLastSample = 0f;
            }
        }
        
        /// <summary>
        /// Sample all metrics immediately
        /// </summary>
        public void SampleAllMetrics()
        {
            if (!_isRunning)
                return;
                
            foreach (var metric in _metrics.Values)
            {
                metric.Update();
            }
        }
        
        /// <summary>
        /// Register a new custom metric to track
        /// </summary>
        public SystemMetric RegisterMetric(ProfilerTag tag, string statName, string unit = "ms")
        {
            if (_metrics.TryGetValue(tag, out var existingMetric))
            {
                return existingMetric;
            }
            
            ProfilerRecorder recorder;
            
            try
            {
                recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, statName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create ProfilerRecorder for {tag.FullName} ({statName}): {e.Message}");
                return null;
            }
            
            var metric = new SystemMetric(tag, recorder, unit);
            _metrics[tag] = metric;
            
            return metric;
        }
        
        /// <summary>
        /// Register common default metrics
        /// </summary>
        public void RegisterDefaultMetrics()
        {
            RegisterMetric(new ProfilerTag(ProfilerCategory.Memory, "GC.Alloc"), 
                "GC.Alloc.Size", "KB");
            RegisterMetric(new ProfilerTag(ProfilerCategory.Memory, "GC.Count"), 
                "GC.Alloc.Count", "count");
            RegisterMetric(new ProfilerTag(ProfilerCategory.Render, "Main Thread"), 
                "Main Thread", "ms");
            RegisterMetric(new ProfilerTag(ProfilerCategory.Render, "Frame Time"), 
                "FrameTime", "ms");
            RegisterMetric(new ProfilerTag(ProfilerCategory.Render, "Draw Calls"), 
                "Batches Count", "count");
            RegisterMetric(new ProfilerTag(ProfilerCategory.Physics, "Physics.Step"), 
                "Physics.Step", "ms");
        }
        
        /// <summary>
        /// Get metric by tag
        /// </summary>
        public SystemMetric GetMetric(ProfilerTag tag)
        {
            if (_metrics.TryGetValue(tag, out var metric))
            {
                return metric;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all current metrics
        /// </summary>
        public IReadOnlyCollection<SystemMetric> GetAllMetrics()
        {
            return _metrics.Values;
        }
        
        /// <summary>
        /// Dispose and clean up all recorders
        /// </summary>
        public void Dispose()
        {
            Stop();
            
            foreach (var metric in _metrics.Values)
            {
                metric.Recorder.Dispose();
            }
            
            _metrics.Clear();
        }
        
        #region Static Factory
        private static SystemMetricsTracker _defaultInstance;
        
        /// <summary>
        /// Get default system metrics tracker instance
        /// </summary>
        public static SystemMetricsTracker Default
        {
            get
            {
                if (_defaultInstance == null)
                {
                    _defaultInstance = new SystemMetricsTracker();
                }
                
                return _defaultInstance;
            }
        }
        
        /// <summary>
        /// Start default metrics tracking with common Unity metrics
        /// </summary>
        public static void StartDefault()
        {
            if (_defaultInstance == null)
            {
                _defaultInstance = new SystemMetricsTracker();
            }
            
            _defaultInstance.RegisterDefaultMetrics();
            _defaultInstance.Start();
        }
        
        /// <summary>
        /// Register a custom metric to track with the default tracker
        /// </summary>
        public static SystemMetric RegisterCustomMetric(string name, ProfilerCategory category, string statName, string unit = "ms")
        {
            if (_defaultInstance == null)
            {
                _defaultInstance = new SystemMetricsTracker();
            }
            
            var tag = new ProfilerTag(category, name);
            return _defaultInstance.RegisterMetric(tag, statName, unit);
        }
        
        /// <summary>
        /// Clean up default instance
        /// </summary>
        public static void ShutdownDefault()
        {
            if (_defaultInstance != null)
            {
                _defaultInstance.Dispose();
                _defaultInstance = null;
            }
        }
        #endregion
    }
}