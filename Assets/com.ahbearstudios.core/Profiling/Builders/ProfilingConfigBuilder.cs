using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Profiling.Configuration
{
    /// <summary>
    /// Builder for creating Profiling configurations using fluent API pattern.
    /// </summary>
    public sealed class ProfilingConfigBuilder : IPoolConfigBuilder<IProfilingConfig, ProfilingConfigBuilder>
    {
        private readonly ProfilingConfig config;
        
        /// <summary>
        /// Initializes a new instance of the ProfilingConfigBuilder.
        /// </summary>
        public ProfilingConfigBuilder()
        {
            config = new ProfilingConfig();
        }
        
        /// <summary>
        /// Sets the configuration identifier.
        /// </summary>
        /// <param name="configId">The unique identifier for this configuration.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithConfigId(string configId)
        {
            config.ConfigId = configId;
            return this;
        }
        
        /// <summary>
        /// Configures basic profiling settings.
        /// </summary>
        /// <param name="enabled">Whether profiling is enabled.</param>
        /// <param name="enableOnStartup">Whether profiling should start automatically on initialization.</param>
        /// <param name="logToConsole">Whether profiling results should be logged to console.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithBasicSettings(bool enabled = true, bool enableOnStartup = true, bool logToConsole = false)
        {
            config.EnableProfiling = enabled;
            config.EnableOnStartup = enableOnStartup;
            config.LogToConsole = logToConsole;
            return this;
        }
        
        /// <summary>
        /// Configures performance settings.
        /// </summary>
        /// <param name="updateInterval">The update interval in seconds for profiling data collection.</param>
        /// <param name="maxSamplesPerFrame">The maximum number of samples to process per frame.</param>
        /// <param name="historyBufferSize">The size of the history buffer for storing profiling data.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithPerformanceSettings(float updateInterval = 0.016f, int maxSamplesPerFrame = 100, int historyBufferSize = 100)
        {
            config.UpdateInterval = updateInterval;
            config.MaxSamplesPerFrame = maxSamplesPerFrame;
            config.HistoryBufferSize = historyBufferSize;
            return this;
        }
        
        /// <summary>
        /// Configures memory profiling settings.
        /// </summary>
        /// <param name="enableMemoryProfiling">Whether memory profiling is enabled.</param>
        /// <param name="trackGCAllocations">Whether garbage collection allocations should be tracked.</param>
        /// <param name="enablePoolMetrics">Whether pool metrics should be collected.</param>
        /// <param name="maxPoolMetricsEntries">The maximum number of pool metrics entries to store.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithMemoryProfiling(bool enableMemoryProfiling = true, bool trackGCAllocations = true, bool enablePoolMetrics = true, int maxPoolMetricsEntries = 1000)
        {
            config.EnableMemoryProfiling = enableMemoryProfiling;
            config.TrackGCAllocations = trackGCAllocations;
            config.EnablePoolMetrics = enablePoolMetrics;
            config.MaxPoolMetricsEntries = maxPoolMetricsEntries;
            return this;
        }
        
        /// <summary>
        /// Configures system metrics tracking.
        /// </summary>
        /// <param name="enableSystemMetrics">Whether system metrics collection is enabled.</param>
        /// <param name="trackCPU">Whether CPU usage should be tracked.</param>
        /// <param name="trackMemory">Whether memory usage should be tracked.</param>
        /// <param name="trackFrameTime">Whether frame time should be tracked.</param>
        /// <param name="trackRenderTime">Whether render time should be tracked.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithSystemMetrics(bool enableSystemMetrics = true, bool trackCPU = true, bool trackMemory = true, bool trackFrameTime = true, bool trackRenderTime = true)
        {
            config.EnableSystemMetrics = enableSystemMetrics;
            config.TrackCPUUsage = trackCPU;
            config.TrackMemoryUsage = trackMemory;
            config.TrackFrameTime = trackFrameTime;
            config.TrackRenderTime = trackRenderTime;
            return this;
        }
        
        /// <summary>
        /// Configures alert settings.
        /// </summary>
        /// <param name="enableAlerts">Whether performance alerts are enabled.</param>
        /// <param name="metricThreshold">The default threshold for metric alerts.</param>
        /// <param name="sessionThreshold">The default threshold for session alerts.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithAlerts(bool enableAlerts = true, double metricThreshold = 0.1, double sessionThreshold = 0.05)
        {
            config.EnableAlerts = enableAlerts;
            config.DefaultMetricThreshold = metricThreshold;
            config.DefaultSessionThreshold = sessionThreshold;
            return this;
        }
        
        /// <summary>
        /// Configures data collection settings.
        /// </summary>
        /// <param name="enableDetailedMetrics">Whether detailed metrics collection is enabled.</param>
        /// <param name="enableHierarchical">Whether hierarchical profiling is enabled.</param>
        /// <param name="enableBurstCompatible">Whether Burst-compatible profiling is enabled.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithDataCollection(bool enableDetailedMetrics = false, bool enableHierarchical = true, bool enableBurstCompatible = true)
        {
            config.EnableDetailedMetrics = enableDetailedMetrics;
            config.EnableHierarchicalProfiling = enableHierarchical;
            config.EnableBurstCompatibleProfiling = enableBurstCompatible;
            return this;
        }
        
        /// <summary>
        /// Configures data export settings.
        /// </summary>
        /// <param name="enableExport">Whether profiling data export is enabled.</param>
        /// <param name="exportPath">The export path for profiling data.</param>
        /// <param name="autoExportOnStop">Whether data should be automatically exported when profiling stops.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithDataExport(bool enableExport = false, string exportPath = "ProfilingData", bool autoExportOnStop = false)
        {
            config.EnableDataExport = enableExport;
            config.ExportPath = exportPath;
            config.AutoExportOnStop = autoExportOnStop;
            return this;
        }
        
        /// <summary>
        /// Applies mobile-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithMobileOptimizations()
        {
            return WithPerformanceSettings(0.033f, 50, 50)
                .WithMemoryProfiling(false, false, false, 100)
                .WithSystemMetrics(false, false, true, true, false)
                .WithDataCollection(false, false, true)
                .WithDataExport(false);
        }
        
        /// <summary>
        /// Applies console-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithConsoleOptimizations()
        {
            return WithPerformanceSettings(0.016f, 200, 200)
                .WithMemoryProfiling(true, true, true, 2000)
                .WithSystemMetrics(true, true, true, true, true)
                .WithDataCollection(true, true, true)
                .WithAlerts(true, 0.05, 0.02);
        }
        
        /// <summary>
        /// Applies development-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithDevelopmentOptimizations()
        {
            return WithBasicSettings(true, true, true)
                .WithMemoryProfiling(true, true, true, 5000)
                .WithSystemMetrics(true, true, true, true, true)
                .WithDataCollection(true, true, true)
                .WithDataExport(true, "Development/ProfilingData", true)
                .WithAlerts(true, 0.02, 0.01);
        }
        
        /// <summary>
        /// Applies minimal profiling settings for production.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        public ProfilingConfigBuilder WithProductionOptimizations()
        {
            return WithBasicSettings(false, false, false)
                .WithMemoryProfiling(false, false, false, 0)
                .WithSystemMetrics(false, false, false, false, false)
                .WithDataCollection(false, false, false)
                .WithDataExport(false)
                .WithAlerts(false);
        }
        
        /// <summary>
        /// Builds the final configuration.
        /// </summary>
        /// <returns>The configured Profiling configuration.</returns>
        public IProfilingConfig Build()
        {
            return config.Clone();
        }
    }
}