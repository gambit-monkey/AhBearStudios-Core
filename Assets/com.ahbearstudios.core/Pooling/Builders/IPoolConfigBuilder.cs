using AhBearStudios.Pooling.Configurations;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Interface for pool configuration builders that implement the fluent pattern
    /// </summary>
    /// <typeparam name="TConfig">The configuration type being built</typeparam>
    /// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
    public interface IPoolConfigBuilder<TConfig, TBuilder> 
        where TConfig : IPoolConfig
        where TBuilder : IPoolConfigBuilder<TConfig, TBuilder>
    {
        /// <summary>
        /// Builds the configuration
        /// </summary>
        /// <returns>The configured pool configuration</returns>
        TConfig Build();
    }
}