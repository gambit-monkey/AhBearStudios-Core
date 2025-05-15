using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Wrapper for Unity's ProfilerMarker that provides scoped profiling with tagging
    /// </summary>
    public class ProfilerSession : IDisposable
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly RuntimeProfilerManager _manager;
        private bool _isDisposed;
        private long _startTimeNs;

        /// <summary>
        /// Creates a new ProfilerSession
        /// </summary>
        internal ProfilerSession(ProfilerTag tag, RuntimeProfilerManager manager)
        {
            _tag = tag;
            _manager = manager;
            _marker = new ProfilerMarker(_tag.FullName);
            _isDisposed = false;
            
            // Begin the profiler marker
            _marker.Begin();
            _startTimeNs = GetHighPrecisionTimestampNs();
            
            // Notify manager that session started
            _manager.OnSessionStarted(this);
        }

        /// <summary>
        /// Get the tag associated with this session
        /// </summary>
        public ProfilerTag Tag => _tag;

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _marker.End();
            _isDisposed = true;
            
            // Calculate duration in milliseconds
            long durationNs = GetHighPrecisionTimestampNs() - _startTimeNs;
            double durationMs = durationNs / 1000000.0;
            
            // Notify manager that session ended
            _manager.OnSessionEnded(this, durationMs);
        }

        /// <summary>
        /// Gets high precision timestamp in nanoseconds
        /// </summary>
        private static long GetHighPrecisionTimestampNs()
        {
            long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            long frequency = System.Diagnostics.Stopwatch.Frequency;
            return (long)((double)timestamp / frequency * 1_000_000_000);
        }
    }
    
    /// <summary>
    /// Static helper class for profiling utility methods
    /// </summary>
    public static class ProfilerSessionExtensions
    {
        /// <summary>
        /// Profile a block of code with a custom tag
        /// </summary>
        public static void Profile(this Action action, ProfilerTag tag)
        {
            using (RuntimeProfilerManager.Instance.BeginScope(tag))
            {
                action();
            }
        }
        
        /// <summary>
        /// Profile a function with a return value
        /// </summary>
        public static T Profile<T>(this Func<T> func, ProfilerTag tag)
        {
            using (RuntimeProfilerManager.Instance.BeginScope(tag))
            {
                return func();
            }
        }
    }
}