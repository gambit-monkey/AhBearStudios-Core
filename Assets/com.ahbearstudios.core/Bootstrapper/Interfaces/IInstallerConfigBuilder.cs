using AhBearStudios.Core.Bootstrap.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Interfaces
{
    /// <summary>
    /// Interface for installer configuration builders that implement the fluent pattern.
    /// </summary>
    /// <typeparam name="TConfig">The installer configuration type being built</typeparam>
    /// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
    public interface IInstallerConfigBuilder<TConfig, TBuilder> 
        where TConfig : IInstallerConfig
        where TBuilder : IInstallerConfigBuilder<TConfig, TBuilder>
    {
        /// <summary>
        /// Builds the installer configuration.
        /// </summary>
        /// <returns>The configured installer configuration</returns>
        TConfig Build();
    }
}