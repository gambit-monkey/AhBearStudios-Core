namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for profiling configuration that controls behavior and performance characteristics.
    /// </summary>
    public interface IProfilingConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        string ConfigId { get; set; }
        
        /// <summary>
        /// Gets or sets whether profiling is enabled.
        /// </summary>
        bool EnableProfiling { get; set; }
        
        /// <summary>
        /// Gets or sets whether profiling should start automatically on initialization.
        /// </summary>
        bool EnableOnStartup { get; set; }
        
        /// <summary>
        /// Gets or sets whether profiling results should be logged to console.
        /// </summary>
        bool LogToConsole { get; set; }
        
        /// <summary>
        /// Gets or sets the update interval in seconds for profiling data collection.
        /// </summary>
        float UpdateInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of samples to process per frame.
        /// </summary>
        int MaxSamplesPerFrame { get; set; }
        
        /// <summary>
        /// Gets or sets the size of the history buffer for storing profiling data.
        /// </summary>
        int HistoryBufferSize { get; set; }
        
        /// <summary>
        /// Gets or sets whether memory profiling is enabled.
        /// </summary>
        bool EnableMemoryProfiling { get; set; }
        
        /// <summary>
        /// Gets or sets whether garbage collection allocations should be tracked.
        /// </summary>
        bool TrackGCAllocations { get; set; }
        
        /// <summary>
        /// Gets or sets whether pool metrics should be collected.
        /// </summary>
        bool EnablePoolMetrics { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of pool metrics entries to store.
        /// </summary>
        int MaxPoolMetricsEntries { get; set; }
        
        /// <summary>
        /// Gets or sets whether system metrics collection is enabled.
        /// </summary>
        bool EnableSystemMetrics { get; set; }
        
        /// <summary>
        /// Gets or sets whether CPU usage should be tracked.
        /// </summary>
        bool TrackCPUUsage { get; set; }
        
        /// <summary>
        /// Gets or sets whether memory usage should be tracked.
        /// </summary>
        bool TrackMemoryUsage { get; set; }
        
        /// <summary>
        /// Gets or sets whether frame time should be tracked.
        /// </summary>
        bool TrackFrameTime { get; set; }
        
        /// <summary>
        /// Gets or sets whether render time should be tracked.
        /// </summary>
        bool TrackRenderTime { get; set; }
        
        /// <summary>
        /// Gets or sets whether performance alerts are enabled.
        /// </summary>
        bool EnableAlerts { get; set; }
        
        /// <summary>
        /// Gets or sets the default threshold for metric alerts.
        /// </summary>
        double DefaultMetricThreshold { get; set; }
        
        /// <summary>
        /// Gets or sets the default threshold for session alerts.
        /// </summary>
        double DefaultSessionThreshold { get; set; }
        
        /// <summary>
        /// Gets or sets whether detailed metrics collection is enabled.
        /// </summary>
        bool EnableDetailedMetrics { get; set; }
        
        /// <summary>
        /// Gets or sets whether hierarchical profiling is enabled.
        /// </summary>
        bool EnableHierarchicalProfiling { get; set; }
        
        /// <summary>
        /// Gets or sets whether Burst-compatible profiling is enabled.
        /// </summary>
        bool EnableBurstCompatibleProfiling { get; set; }
        
        /// <summary>
        /// Gets or sets whether profiling data export is enabled.
        /// </summary>
        bool EnableDataExport { get; set; }
        
        /// <summary>
        /// Gets or sets the export path for profiling data.
        /// </summary>
        string ExportPath { get; set; }
        
        /// <summary>
        /// Gets or sets whether data should be automatically exported when profiling stops.
        /// </summary>
        bool AutoExportOnStop { get; set; }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        IProfilingConfig Clone();
    }
}