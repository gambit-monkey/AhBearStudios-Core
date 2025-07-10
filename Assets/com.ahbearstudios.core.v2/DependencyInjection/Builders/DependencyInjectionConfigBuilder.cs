using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Builders
{
    /// <summary>
    /// Builder implementation for creating DI configurations with fluent API.
    /// Optimized for minimal allocations and high performance.
    /// </summary>
    public sealed class DependencyInjectionConfigBuilder : IDependencyInjectionConfigBuilder
    {
        private ContainerFramework _framework = ContainerFramework.VContainer;
        private bool _enableValidation = true;
        private bool _enableDebugLogging = false;
        private bool _enablePerformanceMetrics = false;
        private bool _throwOnValidationFailure = false;
        private int _maxBuildTimeWarningMs = 100;
        private Dictionary<string, object> _frameworkOptions;
        private bool _enableScoping = true;
        private bool _enableNamedServices = false;
        
        /// <summary>
        /// Sets the preferred DI framework.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithFramework(ContainerFramework framework)
        {
            _framework = framework;
            return this;
        }
        
        /// <summary>
        /// Enables or disables container validation.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithValidation(bool enabled = true)
        {
            _enableValidation = enabled;
            return this;
        }
        
        /// <summary>
        /// Enables or disables debug logging.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithDebugLogging(bool enabled = true)
        {
            _enableDebugLogging = enabled;
            return this;
        }
        
        /// <summary>
        /// Enables or disables performance metrics collection.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithPerformanceMetrics(bool enabled = true)
        {
            _enablePerformanceMetrics = enabled;
            return this;
        }
        
        /// <summary>
        /// Sets whether to throw exceptions on validation failures.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithThrowOnValidationFailure(bool enabled = true)
        {
            _throwOnValidationFailure = enabled;
            return this;
        }
        
        /// <summary>
        /// Sets the maximum build time before warnings.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithMaxBuildTimeWarning(int milliseconds)
        {
            _maxBuildTimeWarningMs = Math.Max(0, milliseconds);
            return this;
        }
        
        /// <summary>
        /// Adds framework-specific configuration option.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithFrameworkOption(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Framework option key cannot be null or empty", nameof(key));
            
            _frameworkOptions ??= new Dictionary<string, object>();
            _frameworkOptions[key] = value;
            return this;
        }
        
        /// <summary>
        /// Enables or disables container scoping.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithScoping(bool enabled = true)
        {
            _enableScoping = enabled;
            return this;
        }
        
        /// <summary>
        /// Enables or disables named service resolution.
        /// </summary>
        public IDependencyInjectionConfigBuilder WithNamedServices(bool enabled = true)
        {
            _enableNamedServices = enabled;
            return this;
        }
        
        /// <summary>
        /// Builds the configuration.
        /// </summary>
        public IDependencyInjectionConfig Build()
        {
            return new DependencyInjectionConfig(
                _framework,
                _enableValidation,
                _enableDebugLogging,
                _enablePerformanceMetrics,
                _throwOnValidationFailure,
                _maxBuildTimeWarningMs,
                _frameworkOptions,
                _enableScoping,
                _enableNamedServices);
        }
    }
}