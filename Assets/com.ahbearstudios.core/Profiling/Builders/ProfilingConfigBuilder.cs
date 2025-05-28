using UnityEngine;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Configuration;

namespace AhBearStudios.Core.Profiling.Builders
{
    /// <summary>
    /// Builder for creating and configuring ProfilingConfig instances using a fluent interface.
    /// Provides methods to set all properties of a profiling configuration.
    /// </summary>
    public sealed class ProfilingConfigBuilder : IProfilingConfigBuilder<IProfilingConfig, ProfilingConfigBuilder>
    {
        private readonly IProfilingConfig _config;
        private IProfilingConfig _optimizedConfig; // Added for storing optimized config

        /// <summary>
        /// Initializes a new instance of the ProfilingConfigBuilder with a default configuration.
        /// </summary>
        public ProfilingConfigBuilder()
        {
            _config = ScriptableObject.CreateInstance<ProfilingConfig>();
            _optimizedConfig = null;
        }

        /// <summary>
        /// Initializes a new instance of the ProfilingConfigBuilder with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to start with</param>
        public ProfilingConfigBuilder(IProfilingConfig config)
        {
            _config = config.Clone();
            _optimizedConfig = null;
        }
        
        /// <summary>
        /// Creates a new builder initialized with settings from an existing configuration.
        /// </summary>
        /// <param name="config">The existing configuration to use as a starting point</param>
        /// <returns>A new builder instance with copied settings</returns>
        public static ProfilingConfigBuilder FromExisting(IProfilingConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config), "Cannot create builder from null configuration");
            }
            
            return new ProfilingConfigBuilder(config);
        }

        /// <summary>
        /// Sets the configuration ID.
        /// </summary>
        /// <param name="configId">The unique identifier for this configuration</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithConfigId(string configId)
        {
            GetActiveConfig().ConfigId = configId;
            return this;
        }

        /// <summary>
        /// Sets whether profiling is enabled.
        /// </summary>
        /// <param name="enable">True to enable profiling, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithProfiling(bool enable)
        {
            GetActiveConfig().EnableProfiling = enable;
            return this;
        }

        /// <summary>
        /// Sets whether profiling should start automatically on initialization.
        /// </summary>
        /// <param name="enable">True to enable automatic startup, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithAutoStartup(bool enable)
        {
            GetActiveConfig().EnableOnStartup = enable;
            return this;
        }

        /// <summary>
        /// Sets whether profiling results should be logged to console.
        /// </summary>
        /// <param name="enable">True to enable console logging, false otherwise</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithConsoleLogging(bool enable)
        {
            GetActiveConfig().LogToConsole = enable;
            return this;
        }

        /// <summary>
        /// Sets the update interval for profiling data collection.
        /// </summary>
        /// <param name="intervalSeconds">The interval in seconds between data collection (min: 0.001)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithUpdateInterval(float intervalSeconds)
        {
            GetActiveConfig().UpdateInterval = intervalSeconds;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of samples to process per frame.
        /// </summary>
        /// <param name="maxSamples">The maximum number of samples (min: 1)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithMaxSamplesPerFrame(int maxSamples)
        {
            GetActiveConfig().MaxSamplesPerFrame = maxSamples;
            return this;
        }

        /// <summary>
        /// Sets the size of the history buffer for storing profiling data.
        /// </summary>
        /// <param name="bufferSize">The size of the history buffer (min: 1)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithHistoryBufferSize(int bufferSize)
        {
            GetActiveConfig().HistoryBufferSize = bufferSize;
            return this;
        }

        /// <summary>
        /// Configures memory profiling options.
        /// </summary>
        /// <param name="enableMemoryProfiling">Whether to enable memory profiling</param>
        /// <param name="trackGCAllocations">Whether to track garbage collection allocations</param>
        /// <param name="enablePoolMetrics">Whether to collect pool metrics</param>
        /// <param name="maxPoolEntries">Maximum number of pool metrics entries to store</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithMemoryProfiling(bool enableMemoryProfiling, bool trackGCAllocations = true, 
            bool enablePoolMetrics = true, int maxPoolEntries = 1000)
        {
            IProfilingConfig config = GetActiveConfig();
            config.EnableMemoryProfiling = enableMemoryProfiling;
            config.TrackGCAllocations = trackGCAllocations;
            config.EnablePoolMetrics = enablePoolMetrics;
            config.MaxPoolMetricsEntries = maxPoolEntries;
            return this;
        }

        /// <summary>
        /// Configures system metrics tracking options.
        /// </summary>
        /// <param name="enableSystemMetrics">Whether to enable system metrics tracking</param>
        /// <param name="trackCPU">Whether to track CPU usage</param>
        /// <param name="trackMemory">Whether to track memory usage</param>
        /// <param name="trackFrameTime">Whether to track frame time</param>
        /// <param name="trackRenderTime">Whether to track render time</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithSystemMetrics(bool enableSystemMetrics, bool trackCPU = true, 
            bool trackMemory = true, bool trackFrameTime = true, bool trackRenderTime = true)
        {
            IProfilingConfig config = GetActiveConfig();
            config.EnableSystemMetrics = enableSystemMetrics;
            config.TrackCPUUsage = trackCPU;
            config.TrackMemoryUsage = trackMemory;
            config.TrackFrameTime = trackFrameTime;
            config.TrackRenderTime = trackRenderTime;
            return this;
        }

        /// <summary>
        /// Configures performance alert options.
        /// </summary>
        /// <param name="enableAlerts">Whether to enable performance alerts</param>
        /// <param name="metricThreshold">The default threshold for metric alerts (in seconds)</param>
        /// <param name="sessionThreshold">The default threshold for session alerts (in seconds)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithAlerts(bool enableAlerts, double metricThreshold = 0.1, 
            double sessionThreshold = 0.05)
        {
            IProfilingConfig config = GetActiveConfig();
            config.EnableAlerts = enableAlerts;
            config.DefaultMetricThreshold = metricThreshold;
            config.DefaultSessionThreshold = sessionThreshold;
            return this;
        }

        /// <summary>
        /// Configures data collection options.
        /// </summary>
        /// <param name="enableDetailedMetrics">Whether to enable detailed metrics collection</param>
        /// <param name="enableHierarchicalProfiling">Whether to enable hierarchical profiling</param>
        /// <param name="enableBurstCompatibleProfiling">Whether to enable Burst-compatible profiling</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithDataCollection(bool enableDetailedMetrics, 
            bool enableHierarchicalProfiling = true, bool enableBurstCompatibleProfiling = true)
        {
            IProfilingConfig config = GetActiveConfig();
            config.EnableDetailedMetrics = enableDetailedMetrics;
            config.EnableHierarchicalProfiling = enableHierarchicalProfiling;
            config.EnableBurstCompatibleProfiling = enableBurstCompatibleProfiling;
            return this;
        }

        /// <summary>
        /// Configures data export options.
        /// </summary>
        /// <param name="enableExport">Whether to enable data export</param>
        /// <param name="exportPath">The path to export data to</param>
        /// <param name="autoExportOnStop">Whether to automatically export data when profiling stops</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithDataExport(bool enableExport, string exportPath = "ProfilingData", 
            bool autoExportOnStop = false)
        {
            IProfilingConfig config = GetActiveConfig();
            config.EnableDataExport = enableExport;
            config.ExportPath = exportPath;
            config.AutoExportOnStop = autoExportOnStop;
            return this;
        }

        /// <summary>
        /// Configures platform optimization options.
        /// </summary>
        /// <param name="enableMobileOptimizations">Whether to enable mobile-specific optimizations</param>
        /// <param name="enableConsoleOptimizations">Whether to enable console-specific optimizations</param>
        /// <param name="enableEditorDebugging">Whether to enable additional debugging in the Unity Editor</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder WithPlatformOptimizations(bool enableMobileOptimizations = true,
            bool enableConsoleOptimizations = true, bool enableEditorDebugging = true)
        {
            if (GetActiveConfig() is ProfilingConfig config)
            {
                // Use reflection-free property access for ProfilingConfig
                var property = typeof(ProfilingConfig).GetProperty("EnableMobileOptimizations");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, enableMobileOptimizations);
                }
                
                property = typeof(ProfilingConfig).GetProperty("EnableConsoleOptimizations");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, enableConsoleOptimizations);
                }
                
                property = typeof(ProfilingConfig).GetProperty("EnableEditorDebugging");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, enableEditorDebugging);
                }
            }
            
            return this;
        }

        /// <summary>
        /// Creates an optimized configuration for the current platform.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilingConfigBuilder OptimizeForCurrentPlatform()
        {
            if (GetActiveConfig() is ProfilingConfig config)
            {
                _optimizedConfig = config.GetPlatformOptimizedConfig();
            }
            
            return this;
        }

        /// <summary>
        /// Builds the final profiling configuration.
        /// </summary>
        /// <returns>The configured profiling configuration</returns>
        public IProfilingConfig Build()
        {
            return GetActiveConfig().Clone();
        }
        
        /// <summary>
        /// Gets the active configuration (optimized if available, original otherwise).
        /// </summary>
        /// <returns>The active configuration</returns>
        private IProfilingConfig GetActiveConfig()
        {
            return _optimizedConfig ?? _config;
        }
    }
}