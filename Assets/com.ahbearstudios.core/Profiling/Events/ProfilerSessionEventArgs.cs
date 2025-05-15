using System;

namespace AhBearStudios.Core.Profiling.Events
{
    /// <summary>
    /// Event data for profiler session events
    /// </summary>
    public class ProfilerSessionEventArgs : EventArgs
    {
        /// <summary>
        /// Tag of the profiler session
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// Create new profiler session event args
        /// </summary>
        public ProfilerSessionEventArgs(ProfilerTag tag, double durationMs)
        {
            Tag = tag;
            DurationMs = durationMs;
        }
    }
}