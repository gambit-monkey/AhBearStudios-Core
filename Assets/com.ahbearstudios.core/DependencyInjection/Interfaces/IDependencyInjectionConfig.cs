using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Configuration interface for dependency injection system behavior.
    /// Supports multiple DI frameworks with performance and validation options.
    /// </summary>
    public interface IDependencyInjectionConfig
    {
        /// <summary>
        /// Gets the preferred DI framework to use.
        /// </summary>
        ContainerFramework PreferredFramework { get; }
        
        /// <summary>
        /// Gets whether to enable container validation during build.
        /// </summary>
        bool EnableValidation { get; }
        
        /// <summary>
        /// Gets whether to enable debug logging for DI operations.
        /// </summary>
        bool EnableDebugLogging { get; }
        
        /// <summary>
        /// Gets whether to enable performance metrics collection.
        /// </summary>
        bool EnablePerformanceMetrics { get; }
        
        /// <summary>
        /// Gets whether to throw exceptions on validation failures.
        /// </summary>
        bool ThrowOnValidationFailure { get; }
        
        /// <summary>
        /// Gets the maximum container build time in milliseconds before warnings.
        /// </summary>
        int MaxBuildTimeWarningMs { get; }
        
        /// <summary>
        /// Gets framework-specific configuration options.
        /// </summary>
        IReadOnlyDictionary<string, object> FrameworkSpecificOptions { get; }
        
        /// <summary>
        /// Gets whether to enable container scoping support.
        /// </summary>
        bool EnableScoping { get; }
        
        /// <summary>
        /// Gets whether to enable named service resolution.
        /// </summary>
        bool EnableNamedServices { get; }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        IDependencyInjectionConfig Clone();
    }
}