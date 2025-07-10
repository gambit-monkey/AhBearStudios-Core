using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Represents a timeline of profiler data for the logging system.
    /// This class manages time-series performance data and visualization state.
    /// </summary>
    public class ProfilerTimeline : IDisposable
    {
        // Timeline configuration
        private const int DEFAULT_HISTORY_SIZE = 120; // 2 minutes worth of data at 1 sample/second
        
        // Timeline state and view settings
        private float _viewStartTime;
        private float _viewDuration = 60f; // 1 minute default view window
        private bool _autoScroll = true;
        
        // Timeline data
        private readonly float _timelineStartTime;
        private float _timelineCurrentTime;
        
        // Native collections for timeline data points
        private NativeList<float> _timePoints;
        private Dictionary<string, NativeList<float>> _metricSeries;
        
        /// <summary>
        /// Gets or sets whether the timeline should automatically scroll to show the most recent data.
        /// </summary>
        public bool AutoScroll
        {
            get => _autoScroll;
            set => _autoScroll = value;
        }
        
        /// <summary>
        /// Gets the start time of the visible part of the timeline in seconds since app start.
        /// </summary>
        public float ViewStartTime => _viewStartTime;
        
        /// <summary>
        /// Gets the duration of the visible part of the timeline in seconds.
        /// </summary>
        public float ViewDuration => _viewDuration;
        
        /// <summary>
        /// Gets the end time of the visible part of the timeline in seconds since app start.
        /// </summary>
        public float ViewEndTime => _viewStartTime + _viewDuration;
        
        /// <summary>
        /// Gets the total duration of the recorded timeline in seconds.
        /// </summary>
        public float TotalDuration => _timelineCurrentTime - _timelineStartTime;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerTimeline"/> class.
        /// </summary>
        public ProfilerTimeline()
        {
            _timelineStartTime = Time.realtimeSinceStartup;
            _timelineCurrentTime = _timelineStartTime;
            _viewStartTime = _timelineStartTime;
            
            InitializeCollections();
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerTimeline"/> class with a specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity for timeline data points.</param>
        public ProfilerTimeline(int capacity)
        {
            _timelineStartTime = Time.realtimeSinceStartup;
            _timelineCurrentTime = _timelineStartTime;
            _viewStartTime = _timelineStartTime;
            
            InitializeCollections(capacity);
        }
        
        /// <summary>
        /// Initializes the native collections used to store timeline data.
        /// </summary>
        /// <param name="capacity">The initial capacity for collections.</param>
        private void InitializeCollections(int capacity = DEFAULT_HISTORY_SIZE)
        {
            _timePoints = new NativeList<float>(capacity, Allocator.Persistent);
            _metricSeries = new Dictionary<string, NativeList<float>>();
        }
        
        /// <summary>
        /// Registers a new metric series to be tracked in the timeline.
        /// </summary>
        /// <param name="metricName">The name of the metric series.</param>
        /// <returns>True if the metric was registered successfully; otherwise, false.</returns>
        public bool RegisterMetricSeries(string metricName)
        {
            if (_metricSeries.ContainsKey(metricName))
                return false;
                
            _metricSeries[metricName] = new NativeList<float>(_timePoints.Capacity, Allocator.Persistent);
            return true;
        }
        
        /// <summary>
        /// Adds a data point to the timeline for a specific metric.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="value">The value to record.</param>
        /// <param name="timestamp">The timestamp for the data point. If null, uses the current time.</param>
        public void AddDataPoint(string metricName, float value, float? timestamp = null)
        {
            if (!_metricSeries.TryGetValue(metricName, out var series))
            {
                RegisterMetricSeries(metricName);
                series = _metricSeries[metricName];
            }
            
            _timelineCurrentTime = timestamp ?? Time.realtimeSinceStartup;
            
            // Ensure all series have the same number of data points
            if (_timePoints.Length > series.Length)
            {
                // Fill in gaps with zeros if we missed some data points
                while (series.Length < _timePoints.Length - 1)
                {
                    series.Add(0f);
                }
            }
            
            // If this is a new time point, add it to the time series
            if (_timePoints.Length == 0 || _timelineCurrentTime > _timePoints[_timePoints.Length - 1])
            {
                _timePoints.Add(_timelineCurrentTime);
                
                // Add data to all other series to maintain alignment
                foreach (var kvp in _metricSeries)
                {
                    if (kvp.Key != metricName && kvp.Value.Length < _timePoints.Length)
                    {
                        kvp.Value.Add(0f);
                    }
                }
            }
            
            // Add the data point
            series.Add(value);
            
            // Handle auto-scrolling
            if (_autoScroll)
            {
                _viewStartTime = Mathf.Max(_timelineCurrentTime - _viewDuration, _timelineStartTime);
            }
        }
        
        /// <summary>
        /// Gets data points for a specific metric within the current view.
        /// </summary>
        /// <param name="metricName">The name of the metric to retrieve.</param>
        /// <param name="normalizedValues">Whether to normalize the values between 0 and 1.</param>
        /// <returns>A tuple containing time points and corresponding values within the view window.</returns>
        public (NativeArray<float> times, NativeArray<float> values) GetViewData(string metricName, bool normalizedValues = false)
        {
            if (!_metricSeries.TryGetValue(metricName, out var series) || _timePoints.Length == 0)
            {
                return (new NativeArray<float>(0, Allocator.Temp), new NativeArray<float>(0, Allocator.Temp));
            }
            
            // Find time points within the view
            int startIndex = 0;
            int endIndex = _timePoints.Length - 1;
            
            // Find first time point >= view start
            while (startIndex < _timePoints.Length && _timePoints[startIndex] < _viewStartTime)
            {
                startIndex++;
            }
            
            // Find last time point <= view end
            while (endIndex >= 0 && _timePoints[endIndex] > ViewEndTime)
            {
                endIndex--;
            }
            
            // If no points are in view, return empty arrays
            if (startIndex > endIndex || startIndex >= _timePoints.Length)
            {
                return (new NativeArray<float>(0, Allocator.Temp), new NativeArray<float>(0, Allocator.Temp));
            }
            
            int count = endIndex - startIndex + 1;
            var timeSubset = new NativeArray<float>(count, Allocator.Temp);
            var valueSubset = new NativeArray<float>(count, Allocator.Temp);
            
            // Copy data within the view
            for (int i = 0; i < count; i++)
            {
                int sourceIndex = startIndex + i;
                timeSubset[i] = _timePoints[sourceIndex];
                
                // Ensure series has enough data (should always be true, but just to be safe)
                if (sourceIndex < series.Length)
                {
                    valueSubset[i] = series[sourceIndex];
                }
            }
            
            // Normalize values if requested
            if (normalizedValues && count > 0)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                
                // Find min/max
                for (int i = 0; i < valueSubset.Length; i++)
                {
                    min = Mathf.Min(min, valueSubset[i]);
                    max = Mathf.Max(max, valueSubset[i]);
                }
                
                // Normalize only if we have a valid range
                if (max > min)
                {
                    float range = max - min;
                    for (int i = 0; i < valueSubset.Length; i++)
                    {
                        valueSubset[i] = (valueSubset[i] - min) / range;
                    }
                }
            }
            
            return (timeSubset, valueSubset);
        }
        
        /// <summary>
        /// Sets the visible time window for the timeline.
        /// </summary>
        /// <param name="startTime">The start time in seconds since app start.</param>
        /// <param name="duration">The duration of the window in seconds.</param>
        public void SetViewWindow(float startTime, float duration)
        {
            _viewStartTime = Mathf.Max(startTime, _timelineStartTime);
            _viewDuration = Mathf.Max(duration, 1f); // Minimum duration of 1 second
            _autoScroll = false; // Disable auto-scroll when manually setting the window
        }
        
        /// <summary>
        /// Zooms the view to a specific time range.
        /// </summary>
        /// <param name="centerTime">The center time for the zoom operation.</param>
        /// <param name="zoomFactor">Factor by which to zoom (> 1 zooms in, < 1 zooms out).</param>
        public void Zoom(float centerTime, float zoomFactor)
        {
            float newDuration = _viewDuration / zoomFactor;
            float newStartTime = centerTime - newDuration / 2;
            
            SetViewWindow(newStartTime, newDuration);
        }
        
        /// <summary>
        /// Pans the view by a specified amount.
        /// </summary>
        /// <param name="deltaTime">The time delta to pan by, in seconds.</param>
        public void Pan(float deltaTime)
        {
            SetViewWindow(_viewStartTime + deltaTime, _viewDuration);
        }
        
        /// <summary>
        /// Clears all timeline data and resets the timeline.
        /// </summary>
        public void Clear()
        {
            // Update time tracking
            _timelineCurrentTime = Time.realtimeSinceStartup;
            // Can't assign to _timelineStartTime as it's readonly
            _viewStartTime = _timelineCurrentTime; // Use current time as view start
    
            // Clear time points safely
            if (_timePoints.IsCreated)
            {
                _timePoints.Clear();
            }
    
            // Clear all metric series safely
            foreach (var series in _metricSeries.Values)
            {
                if (series.IsCreated)
                {
                    series.Clear();
                }
            }
    
            // Reset auto-scroll state
            _autoScroll = true;
        }
        
        /// <summary>
        /// Updates the timeline for the current frame.
        /// Call this during the GUI update to maintain timeline state.
        /// </summary>
        public void Update()
        {
            _timelineCurrentTime = Time.realtimeSinceStartup;
            
            if (_autoScroll)
            {
                _viewStartTime = Mathf.Max(_timelineCurrentTime - _viewDuration, _timelineStartTime);
            }
        }
        
        /// <summary>
        /// Disposes all native collections used by the timeline.
        /// </summary>
        public void Dispose()
        {
            if (_timePoints.IsCreated)
            {
                _timePoints.Dispose();
            }
            
            foreach (var series in _metricSeries.Values)
            {
                if (series.IsCreated)
                {
                    series.Dispose();
                }
            }
            
            _metricSeries.Clear();
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer to ensure proper cleanup of native resources.
        /// </summary>
        ~ProfilerTimeline()
        {
            Dispose();
        }
    }
}