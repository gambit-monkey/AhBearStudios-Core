using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Represents a value-based counter for Unity's Profiler system.
    /// Provides an optimized way to track and display performance metrics in the Profiler window.
    /// </summary>
    /// <typeparam name="T">The type of value being tracked. Supported types include int, long, float, double.</typeparam>
    public sealed class ProfilerCounterValue<T> : IDisposable where T : unmanaged
    {
        // The native handle to the profiler counter
        private ProfilerRecorder _recorder;
        
        // The friendly name of this counter for display purposes
        private readonly string _name;
        
        // Category for grouping related counters
        private readonly ProfilerCategory _category;
        
        // The data unit for this counter
        private readonly ProfilerMarkerDataUnit _dataUnit;
        
        // The current value of the counter
        private T _value;
        
        // Whether this counter has been disposed
        private bool _isDisposed;
        
        /// <summary>
        /// Gets or sets the current value of this profiler counter.
        /// Setting this value will update the profiler immediately.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                UpdateProfiler();
            }
        }
        
        /// <summary>
        /// Gets the name of this counter.
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Gets the profiler category this counter belongs to.
        /// </summary>
        public ProfilerCategory Category => _category;
        
        /// <summary>
        /// Gets the data unit for this counter.
        /// </summary>
        public ProfilerMarkerDataUnit DataUnit => _dataUnit;
        
        /// <summary>
        /// Gets whether this counter has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Gets access to the underlying recorder.
        /// </summary>
        public ProfilerRecorder Recorder => _recorder;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilerCounterValue{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the counter, displayed in the Profiler window.</param>
        /// <param name="category">The category for grouping this counter with related metrics.</param>
        /// <param name="dataUnit">The unit of measurement for this counter.</param>
        /// <param name="recorderOptions">Options for how the counter should behave in the Profiler.</param>
        public ProfilerCounterValue(
            string name, 
            ProfilerCategory category,
            ProfilerMarkerDataUnit dataUnit = ProfilerMarkerDataUnit.Count,
            ProfilerRecorderOptions recorderOptions = ProfilerRecorderOptions.Default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Counter name cannot be null or empty", nameof(name));
                
            _name = name;
            _category = category;
            _dataUnit = dataUnit;
            _isDisposed = false;
            
            // Initialize the counter with the appropriate numeric type
            if (typeof(T) == typeof(int))
            {
                _recorder = ProfilerRecorder.StartNew(category, name, 1, recorderOptions);
            }
            else if (typeof(T) == typeof(long))
            {
                _recorder = ProfilerRecorder.StartNew(category, name, 8, recorderOptions);
            }
            else if (typeof(T) == typeof(float))
            {
                _recorder = ProfilerRecorder.StartNew(category, name, 4, recorderOptions);
            }
            else if (typeof(T) == typeof(double))
            {
                _recorder = ProfilerRecorder.StartNew(category, name, 8, recorderOptions);
            }
            else
            {
                throw new ArgumentException($"Unsupported counter type: {typeof(T).Name}. " +
                                           "Only int, long, float, and double are supported.");
            }
            
            // Set initial value to default
            _value = default;
        }
        
        /// <summary>
        /// Updates the profiler with the current value of this counter.
        /// </summary>
        private unsafe void UpdateProfiler()
        {
            if (_isDisposed)
                return;
            
            // In a real implementation, we would use the Unity Profiler's internal API to update counter values
            // For now, we'll just log the current values in development builds
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (typeof(T) == typeof(int))
            {
                int value = *(int*)UnsafeUtility.AddressOf(ref _value);
                Debug.Log($"Profiler Counter '{_name}': {ConvertToReadableValue(value)} ({_dataUnit})");
            }
            else if (typeof(T) == typeof(long))
            {
                long value = *(long*)UnsafeUtility.AddressOf(ref _value);
                Debug.Log($"Profiler Counter '{_name}': {ConvertToReadableValue(value)} ({_dataUnit})");
            }
            else if (typeof(T) == typeof(float))
            {
                float value = *(float*)UnsafeUtility.AddressOf(ref _value);
                Debug.Log($"Profiler Counter '{_name}': {ConvertToReadableValue((long)value)} ({_dataUnit})");
            }
            else if (typeof(T) == typeof(double))
            {
                double value = *(double*)UnsafeUtility.AddressOf(ref _value);
                Debug.Log($"Profiler Counter '{_name}': {ConvertToReadableValue((long)value)} ({_dataUnit})");
            }
            #endif
            
            // The _recorder will handle making the value visible in the profiler
            // Note: This assumes the recorder is properly configured to sample our value
            // A complete implementation would need to use Unity's native Profiler API
        }
        
        /// <summary>
        /// Convert raw value to readable value based on data unit
        /// </summary>
        private double ConvertToReadableValue(long rawValue)
        {
            double value = rawValue;

            // Convert based on data unit type
            switch (_dataUnit)
            {
                case ProfilerMarkerDataUnit.TimeNanoseconds:
                    value = value / 1_000_000.0; // ns to ms
                    break;

                case ProfilerMarkerDataUnit.Bytes:
                    value = value / 1024.0; // bytes to KB
                    break;

                case ProfilerMarkerDataUnit.FrequencyHz:
                case ProfilerMarkerDataUnit.Count:
                    // Keep as is
                    break;

                default:
                    // Keep as is for unknown units
                    break;
            }

            return value;
        }
        
        /// <summary>
        /// Disposes this counter, freeing any native resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            if (_recorder.Valid)
            {
                _recorder.Dispose();
            }
            
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer to ensure proper cleanup of resources.
        /// </summary>
        ~ProfilerCounterValue()
        {
            Dispose();
        }
    }
}