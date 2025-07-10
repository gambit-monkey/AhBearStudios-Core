using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Builders;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Factory for creating common profiling configurations using the builder pattern.
    /// </summary>
    public static class ProfilingConfigFactory
    {
        /// <summary>
        /// Creates a development-focused configuration with comprehensive profiling enabled.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A profiling configuration optimized for development</returns>
        public static IProfilingConfig CreateDevelopmentConfig(string configId = "DevelopmentProfilingConfig")
        {
            return new ProfilingConfigBuilder()
                .WithConfigId(configId)
                .WithProfiling(true)
                .WithAutoStartup(true)
                .WithConsoleLogging(true)
                .WithUpdateInterval(0.016f)
                .WithMaxSamplesPerFrame(200)
                .WithHistoryBufferSize(200)
                .WithMemoryProfiling(true, true, true, 2000)
                .WithSystemMetrics(true, true, true, true, true)
                .WithAlerts(true, 0.05, 0.025)
                .WithDataCollection(true, true, true)
                .WithDataExport(true, "ProfilingData/Dev", false)
                .WithPlatformOptimizations(true, true, true)
                .Build();
        }

        /// <summary>
        /// Creates a production-focused configuration with minimal performance impact.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A profiling configuration optimized for production</returns>
        public static IProfilingConfig CreateProductionConfig(string configId = "ProductionProfilingConfig")
        {
            return new ProfilingConfigBuilder()
                .WithConfigId(configId)
                .WithProfiling(true)
                .WithAutoStartup(true)
                .WithConsoleLogging(false)
                .WithUpdateInterval(0.05f)
                .WithMaxSamplesPerFrame(50)
                .WithHistoryBufferSize(50)
                .WithMemoryProfiling(false, false, false, 100)
                .WithSystemMetrics(true, true, true, true, false)
                .WithAlerts(true, 0.2, 0.1)
                .WithDataCollection(false, false, true)
                .WithDataExport(false)
                .WithPlatformOptimizations(true, true, false)
                .OptimizeForCurrentPlatform()
                .Build();
        }

        /// <summary>
        /// Creates a minimal configuration suitable for testing or performance-critical scenarios.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A minimal profiling configuration</returns>
        public static IProfilingConfig CreateMinimalConfig(string configId = "MinimalProfilingConfig")
        {
            return new ProfilingConfigBuilder()
                .WithConfigId(configId)
                .WithProfiling(true)
                .WithAutoStartup(false)
                .WithConsoleLogging(false)
                .WithUpdateInterval(0.1f)
                .WithMaxSamplesPerFrame(20)
                .WithHistoryBufferSize(20)
                .WithMemoryProfiling(false)
                .WithSystemMetrics(true, false, false, true, false)
                .WithAlerts(false)
                .WithDataCollection(false, false, true)
                .WithDataExport(false)
                .WithPlatformOptimizations(true, true, false)
                .OptimizeForCurrentPlatform()
                .Build();
        }
    }
}