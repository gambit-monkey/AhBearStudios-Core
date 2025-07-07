using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Configuration
{
    /// <summary>
    /// Default implementation of IDependencyInjectionConfig with reasonable defaults.
    /// Optimized for performance with minimal allocations.
    /// </summary>
    public sealed class DependencyInjectionConfig : IDependencyInjectionConfig
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyOptions = 
            new Dictionary<string, object>(0);
        
        /// <summary>
        /// Gets the preferred DI framework to use.
        /// </summary>
        public ContainerFramework PreferredFramework { get; private set; }
        
        /// <summary>
        /// Gets whether to enable container validation during build.
        /// </summary>
        public bool EnableValidation { get; private set; }
        
        /// <summary>
        /// Gets whether to enable debug logging for DI operations.
        /// </summary>
        public bool EnableDebugLogging { get; private set; }
        
        /// <summary>
        /// Gets whether to enable performance metrics collection.
        /// </summary>
        public bool EnablePerformanceMetrics { get; private set; }
        
        /// <summary>
        /// Gets whether to throw exceptions on validation failures.
        /// </summary>
        public bool ThrowOnValidationFailure { get; private set; }
        
        /// <summary>
        /// Gets the maximum container build time in milliseconds before warnings.
        /// </summary>
        public int MaxBuildTimeWarningMs { get; private set; }
        
        /// <summary>
        /// Gets framework-specific configuration options.
        /// </summary>
        public IReadOnlyDictionary<string, object> FrameworkSpecificOptions { get; private set; }
        
        /// <summary>
        /// Gets whether to enable container scoping support.
        /// </summary>
        public bool EnableScoping { get; private set; }
        
        /// <summary>
        /// Gets whether to enable named service resolution.
        /// </summary>
        public bool EnableNamedServices { get; private set; }
        
        /// <summary>
        /// Initializes a new instance with default values optimized for performance.
        /// </summary>
        public DependencyInjectionConfig()
        {
            PreferredFramework = ContainerFramework.VContainer;
            EnableValidation = true;
            EnableDebugLogging = false;
            EnablePerformanceMetrics = false;
            ThrowOnValidationFailure = false;
            MaxBuildTimeWarningMs = 100;
            FrameworkSpecificOptions = EmptyOptions;
            EnableScoping = true;
            EnableNamedServices = false;
        }
        
        /// <summary>
        /// Initializes a new instance with specified values.
        /// </summary>
        public DependencyInjectionConfig(
            ContainerFramework preferredFramework = ContainerFramework.VContainer,
            bool enableValidation = true,
            bool enableDebugLogging = false,
            bool enablePerformanceMetrics = false,
            bool throwOnValidationFailure = false,
            int maxBuildTimeWarningMs = 100,
            IReadOnlyDictionary<string, object> frameworkSpecificOptions = null,
            bool enableScoping = true,
            bool enableNamedServices = false)
        {
            PreferredFramework = preferredFramework;
            EnableValidation = enableValidation;
            EnableDebugLogging = enableDebugLogging;
            EnablePerformanceMetrics = enablePerformanceMetrics;
            ThrowOnValidationFailure = throwOnValidationFailure;
            MaxBuildTimeWarningMs = Math.Max(0, maxBuildTimeWarningMs);
            FrameworkSpecificOptions = frameworkSpecificOptions ?? EmptyOptions;
            EnableScoping = enableScoping;
            EnableNamedServices = enableNamedServices;
        }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        public IDependencyInjectionConfig Clone()
        {
            var optionsCopy = FrameworkSpecificOptions.Count > 0 
                ? new Dictionary<string, object>(FrameworkSpecificOptions)
                : EmptyOptions;
                
            return new DependencyInjectionConfig(
                PreferredFramework,
                EnableValidation,
                EnableDebugLogging,
                EnablePerformanceMetrics,
                ThrowOnValidationFailure,
                MaxBuildTimeWarningMs,
                optionsCopy,
                EnableScoping,
                EnableNamedServices);
        }
        
        /// <summary>
        /// Gets a default configuration optimized for production use.
        /// </summary>
        public static IDependencyInjectionConfig Production => new DependencyInjectionConfig(
            enableDebugLogging: false,
            enablePerformanceMetrics: false,
            throwOnValidationFailure: true);
        
        /// <summary>
        /// Gets a default configuration optimized for development use.
        /// </summary>
        public static IDependencyInjectionConfig Development => new DependencyInjectionConfig(
            enableDebugLogging: true,
            enablePerformanceMetrics: true,
            throwOnValidationFailure: false);
        
        /// <summary>
        /// Gets a default configuration optimized for testing use.
        /// </summary>
        public static IDependencyInjectionConfig Testing => new DependencyInjectionConfig(
            enableValidation: true,
            enableDebugLogging: false,
            enablePerformanceMetrics: false,
            throwOnValidationFailure: true,
            enableScoping: true,
            enableNamedServices: true);
    }
}