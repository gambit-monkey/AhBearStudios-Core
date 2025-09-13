using System;
using AhBearStudios.Core.Profiling.Configs;
using AhBearStudios.Core.Pooling;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Simple factory for creating ProfilerService instances following the CLAUDE.md factory pattern.
    /// Provides stateless creation utilities with no lifecycle management or object tracking.
    /// </summary>
    /// <remarks>
    /// ProfilerServiceFactory follows the established CLAUDE.md factory guidelines:
    /// - Factories only create, never destroy (no lifecycle management)
    /// - Take validated configs and create instances
    /// - DO NOT implement IDisposable (per CLAUDE.md)
    /// - Keep factory methods simple and focused on instantiation
    /// - Stateless creation utilities with clear separation between creation and lifecycle
    /// 
    /// Object lifecycle management is handled by:
    /// - IPoolingService manages object lifecycle (not factories)
    /// - Services manage their own disposal when needed
    /// - Clear separation: Creation (Factory) vs Lifecycle (Pooling/Service)
    /// </remarks>
    public static class ProfilerServiceFactory
    {
        #region Core Factory Methods

        /// <summary>
        /// Creates a new ProfilerService instance using the specified configuration.
        /// This is the primary factory method for ProfilerService creation.
        /// </summary>
        /// <param name="configuration">Validated profiler configuration</param>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails</exception>
        public static ProfilerService CreateProfilerService(ProfilerConfig configuration, IPoolingService poolingService = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Validate configuration before creating service
            if (!configuration.ValidateForProduction() && configuration.IsEnabled)
            {
                throw new InvalidOperationException(
                    "Configuration validation failed for production use. Use ProfilerConfigBuilder.ValidateConfiguration() to check for issues.");
            }

            // Simple instantiation - no lifecycle management or tracking
            return new ProfilerService(configuration, poolingService);
        }

        /// <summary>
        /// Creates a new ProfilerService instance optimized for Unity development scenarios.
        /// Uses development-friendly settings with comprehensive monitoring enabled.
        /// </summary>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance configured for development</returns>
        public static ProfilerService CreateDevelopmentService(IPoolingService poolingService = null)
        {
            var config = new Builders.ProfilerConfigBuilder()
                .UseUnityDevelopmentPreset()
                .SetSource("DevelopmentFactory")
                .Build();

            return CreateProfilerService(config, poolingService);
        }

        /// <summary>
        /// Creates a new ProfilerService instance optimized for production environments.
        /// Uses performance-friendly settings with reduced overhead while maintaining visibility.
        /// </summary>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance configured for production</returns>
        public static ProfilerService CreateProductionService(IPoolingService poolingService = null)
        {
            var config = new Builders.ProfilerConfigBuilder()
                .UseProductionPreset()
                .SetSource("ProductionFactory")
                .Build();

            return CreateProfilerService(config, poolingService);
        }

        /// <summary>
        /// Creates a new ProfilerService instance optimized for performance testing scenarios.
        /// Uses comprehensive monitoring with strict thresholds for detailed performance analysis.
        /// </summary>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance configured for performance testing</returns>
        public static ProfilerService CreatePerformanceTestingService(IPoolingService poolingService = null)
        {
            var config = new Builders.ProfilerConfigBuilder()
                .UsePerformanceTestingPreset()
                .SetSource("PerformanceTestingFactory")
                .Build();

            return CreateProfilerService(config, poolingService);
        }

        /// <summary>
        /// Creates a new ProfilerService instance with minimal overhead for performance-critical scenarios.
        /// Uses minimal monitoring to reduce impact on frame rate while maintaining basic Unity integration.
        /// </summary>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance configured for minimal overhead</returns>
        public static ProfilerService CreateMinimalOverheadService(IPoolingService poolingService = null)
        {
            var config = new Builders.ProfilerConfigBuilder()
                .UseMinimalOverheadPreset()
                .SetSource("MinimalOverheadFactory")
                .Build();

            return CreateProfilerService(config, poolingService);
        }

        #endregion

        #region Specialized Factory Methods

        /// <summary>
        /// Creates a new ProfilerService instance with custom sampling rate and threshold settings.
        /// Useful for fine-tuned profiling scenarios with specific performance requirements.
        /// </summary>
        /// <param name="samplingRate">Sampling rate between 0.0 and 1.0</param>
        /// <param name="thresholdMs">Performance threshold in milliseconds</param>
        /// <param name="enableUnityIntegration">Whether to enable Unity ProfilerMarker integration</param>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance with custom settings</returns>
        /// <exception cref="ArgumentException">Thrown when sampling rate is out of valid range</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative</exception>
        public static ProfilerService CreateCustomService(
            float samplingRate,
            double thresholdMs,
            bool enableUnityIntegration = true,
            IPoolingService poolingService = null)
        {
            if (samplingRate < 0.0f || samplingRate > 1.0f)
                throw new ArgumentException("Sampling rate must be between 0.0 and 1.0", nameof(samplingRate));

            if (thresholdMs < 0.0)
                throw new ArgumentOutOfRangeException(nameof(thresholdMs), "Threshold cannot be negative");

            var config = new Builders.ProfilerConfigBuilder()
                .SetSamplingRate(samplingRate)
                .SetDefaultThreshold(thresholdMs)
                .SetUnityProfilerIntegration(enableUnityIntegration)
                .SetSource("CustomFactory")
                .Build();

            return CreateProfilerService(config, poolingService);
        }

        /// <summary>
        /// Creates a new ProfilerService instance configured for specific Unity target frame rates.
        /// Automatically sets appropriate thresholds based on the target FPS.
        /// </summary>
        /// <param name="targetFps">Target frame rate (30, 60, 120, or other)</param>
        /// <param name="samplingRate">Sampling rate between 0.0 and 1.0 (default: 1.0)</param>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <returns>New ProfilerService instance configured for the target frame rate</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when target FPS is not positive</exception>
        /// <exception cref="ArgumentException">Thrown when sampling rate is out of valid range</exception>
        public static ProfilerService CreateForTargetFrameRate(
            int targetFps,
            float samplingRate = 1.0f,
            IPoolingService poolingService = null)
        {
            if (targetFps <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetFps), "Target FPS must be positive");

            if (samplingRate < 0.0f || samplingRate > 1.0f)
                throw new ArgumentException("Sampling rate must be between 0.0 and 1.0", nameof(samplingRate));

            // Calculate threshold based on target FPS
            var thresholdMs = 1000.0 / targetFps; // Frame budget in milliseconds

            var config = new Builders.ProfilerConfigBuilder()
                .SetSamplingRate(samplingRate)
                .SetDefaultThreshold(thresholdMs)
                .SetSource("FrameRateFactory")
                .Build();

            return CreateProfilerService(config, poolingService);
        }

        /// <summary>
        /// Creates a new ProfilerService instance with disabled profiling for scenarios where
        /// you want the interface but no actual profiling overhead.
        /// </summary>
        /// <returns>New ProfilerService instance with profiling disabled</returns>
        public static ProfilerService CreateDisabledService()
        {
            var config = new Builders.ProfilerConfigBuilder()
                .SetEnabled(false)
                .SetSource("DisabledFactory")
                .Build();

            return CreateProfilerService(config, null);
        }

        #endregion

        #region Configuration Factory Methods

        /// <summary>
        /// Creates a ProfilerService instance from a pre-built configuration with validation.
        /// This method provides additional validation and error handling beyond the basic factory method.
        /// </summary>
        /// <param name="configuration">Pre-built profiler configuration</param>
        /// <param name="poolingService">Optional pooling service for scope object management</param>
        /// <param name="validateForProduction">Whether to validate configuration for production readiness</param>
        /// <returns>New ProfilerService instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        public static ProfilerService CreateFromConfigurationWithValidation(
            ProfilerConfig configuration,
            IPoolingService poolingService = null,
            bool validateForProduction = true)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Perform comprehensive validation if requested
            if (validateForProduction && !configuration.ValidateForProduction())
            {
                throw new InvalidOperationException(
                    $"Configuration validation failed for production use. Configuration: {configuration}");
            }

            return CreateProfilerService(configuration, poolingService);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates that the provided configuration is suitable for ProfilerService creation.
        /// This is a static validation method that can be used independently of service creation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>True if configuration is valid for service creation, false otherwise</returns>
        public static bool ValidateConfigurationForCreation(ProfilerConfig configuration)
        {
            if (configuration == null)
                return false;

            try
            {
                // Test basic configuration properties
                if (configuration.DefaultSamplingRate < 0.0f || configuration.DefaultSamplingRate > 1.0f)
                    return false;

                if (configuration.MaxActiveScopeCount < 0)
                    return false;

                if (configuration.MaxMetricSnapshots < 0)
                    return false;

                if (configuration.DefaultThresholdMs < 0.0)
                    return false;

                if (configuration.ScopePoolSize < 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the recommended configuration preset name based on current environment conditions.
        /// This is a helper method for selecting appropriate preset configurations.
        /// </summary>
        /// <param name="isProduction">Whether running in production environment</param>
        /// <param name="isPerformanceTesting">Whether running performance tests</param>
        /// <param name="requiresMinimalOverhead">Whether minimal overhead is required</param>
        /// <returns>Name of the recommended configuration preset</returns>
        public static string GetRecommendedPreset(
            bool isProduction = false,
            bool isPerformanceTesting = false,
            bool requiresMinimalOverhead = false)
        {
            if (requiresMinimalOverhead)
                return "MinimalOverhead";

            if (isPerformanceTesting)
                return "PerformanceTesting";

            if (isProduction)
                return "Production";

            return "Development";
        }

        #endregion
    }
}