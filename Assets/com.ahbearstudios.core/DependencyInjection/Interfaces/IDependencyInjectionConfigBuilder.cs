using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Builder interface for creating DI configurations with fluent API.
    /// </summary>
    public interface IDependencyInjectionConfigBuilder
    {
        /// <summary>
        /// Sets the preferred DI framework.
        /// </summary>
        IDependencyInjectionConfigBuilder WithFramework(ContainerFramework framework);
        
        /// <summary>
        /// Enables or disables container validation.
        /// </summary>
        IDependencyInjectionConfigBuilder WithValidation(bool enabled = true);
        
        /// <summary>
        /// Enables or disables debug logging.
        /// </summary>
        IDependencyInjectionConfigBuilder WithDebugLogging(bool enabled = true);
        
        /// <summary>
        /// Enables or disables performance metrics collection.
        /// </summary>
        IDependencyInjectionConfigBuilder WithPerformanceMetrics(bool enabled = true);
        
        /// <summary>
        /// Sets whether to throw exceptions on validation failures.
        /// </summary>
        IDependencyInjectionConfigBuilder WithThrowOnValidationFailure(bool enabled = true);
        
        /// <summary>
        /// Sets the maximum build time before warnings.
        /// </summary>
        IDependencyInjectionConfigBuilder WithMaxBuildTimeWarning(int milliseconds);
        
        /// <summary>
        /// Adds framework-specific configuration option.
        /// </summary>
        IDependencyInjectionConfigBuilder WithFrameworkOption(string key, object value);
        
        /// <summary>
        /// Enables or disables container scoping.
        /// </summary>
        IDependencyInjectionConfigBuilder WithScoping(bool enabled = true);
        
        /// <summary>
        /// Enables or disables named service resolution.
        /// </summary>
        IDependencyInjectionConfigBuilder WithNamedServices(bool enabled = true);
        
        /// <summary>
        /// Builds the configuration.
        /// </summary>
        IDependencyInjectionConfig Build();
    }
}