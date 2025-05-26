using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.Profiling.Profilers;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Factory for creating IProfiler instances
    /// </summary>
    public class ProfilerFactory
    {
        private readonly IDependencyProvider _dependencyProvider;
        private static IProfiler _nullInstance;

        /// <summary>
        /// Creates a new ProfilerFactory instance
        /// </summary>
        /// <param name="dependencyProvider">Dependency provider for resolving dependencies</param>
        public ProfilerFactory(IDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider;
        }

        /// <summary>
        /// Create a new profiler instance
        /// </summary>
        /// <returns>Configured profiler instance</returns>
        public IProfiler CreateProfiler()
        {
            var messageBus = _dependencyProvider.Resolve<IMessageBus>();
            return new DefaultProfiler(messageBus);
        }

        /// <summary>
        /// Get a null profiler (no-op implementation for when profiling is disabled)
        /// </summary>
        public IProfiler GetNullProfiler()
        {
            if (_nullInstance == null)
            {
                // Create a NullProfiler that implements IProfiler but does nothing
                _nullInstance = new NullProfiler();
            }
            return _nullInstance;
        }

        /// <summary>
        /// Create a profiler based on enabled state
        /// </summary>
        /// <param name="enabled">Whether profiling is enabled</param>
        /// <returns>Real profiler if enabled, null profiler if disabled</returns>
        public IProfiler Create(bool enabled)
        {
            return enabled ? CreateProfiler() : GetNullProfiler();
        }
    }
}