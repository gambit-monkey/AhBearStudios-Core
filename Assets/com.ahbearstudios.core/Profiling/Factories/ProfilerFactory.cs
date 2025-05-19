using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Factory for creating IProfiler instances
    /// </summary>
    public static class ProfilerFactory
    {
        private static IProfiler _defaultInstance;
        private static readonly NullProfiler _nullInstance = new NullProfiler();

        /// <summary>
        /// Get the default IProfiler instance
        /// </summary>
        public static IProfiler Default
        {
            get
            {
                if (_defaultInstance == null)
                {
                    _defaultInstance = new DefaultProfiler();
                }

                return _defaultInstance;
            }
        }

        /// <summary>
        /// Get a null profiler (no-op implementation for when profiling is disabled)
        /// </summary>
        public static IProfiler Null => _nullInstance;

        /// <summary>
        /// Create a new DefaultProfiler instance
        /// </summary>
        public static IProfiler CreateDefault()
        {
            return new DefaultProfiler();
        }

        /// <summary>
        /// Create a profiler based on enabled state
        /// </summary>
        /// <param name="enabled">Whether profiling is enabled</param>
        /// <returns>Default profiler if enabled, null profiler if disabled</returns>
        public static IProfiler Create(bool enabled)
        {
            return enabled ? CreateDefault() : Null;
        }
    }
}