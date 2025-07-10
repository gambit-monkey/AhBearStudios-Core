using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for profiling configuration builders that implement the fluent pattern.
    /// </summary>
    /// <typeparam name="TConfig">The profiling configuration type being built</typeparam>
    /// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
    public interface IProfilingConfigBuilder<TConfig, TBuilder> 
        where TConfig : IProfilingConfig
        where TBuilder : IProfilingConfigBuilder<TConfig, TBuilder>
    {
        /// <summary>
        /// Builds the profiling configuration.
        /// </summary>
        /// <returns>The configured profiling configuration</returns>
        TConfig Build();
    }
}