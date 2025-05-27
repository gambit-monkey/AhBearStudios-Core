using UnityEngine;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Configuration
{
    /// <summary>
    /// Configuration for the profiling system that implements IProfilingConfig.
    /// Provides ScriptableObject-based configuration with validation and platform optimization.
    /// </summary>
    [CreateAssetMenu(menuName = "AhBear/Core/Profiling Config", fileName = "ProfilingConfig", order = 4)]
    public sealed class ProfilingConfig : ScriptableObject, IProfilingConfig
    {
        [Header("Configuration")]
        [SerializeField]
        private string configId = "DefaultProfilingConfig";
    {
        [Header("General")]
        [SerializeField]
        private bool enableProfiling = true;
        
        [SerializeField]
        private bool enableOnStartup = true;
        
        [SerializeField]
        private bool logToConsole = false;
        
        [Header("Performance")]
        [SerializeField, Range(0.001f, 1f)]
        private float updateInterval = 0.016f; // 60fps
        
        [SerializeField, Range(10, 10000)]
        private int maxSamplesPerFrame = 100;
        
        [SerializeField, Range(1, 1000)]
        private int historyBufferSize = 100;
        
        [Header("Memory Management")]
        [SerializeField]
        private bool enableMemoryProfiling = true;
        
        [SerializeField]
        private bool trackGCAllocations = true;
        
        [SerializeField]
        private bool enablePoolMetrics = true;
        
        [SerializeField, Range(100, 10000)]
        private int maxPoolMetricsEntries = 1000;
        
        [Header("System Metrics")]
        [SerializeField]
        private bool enableSystemMetrics = true;
        
        [SerializeField]
        private bool trackCPUUsage = true;
        
        [SerializeField]
        private bool trackMemoryUsage = true;
        
        [SerializeField]
        private bool trackFrameTime = true;
        
        [SerializeField]
        private bool trackRenderTime = true;
        
        [Header("Alerts")]
        [SerializeField]
        private bool enableAlerts = true;
        
        [SerializeField, Range(0.001f, 1f)]
        private double defaultMetricThreshold = 0.1; // 100ms
        
        [SerializeField, Range(0.001f, 1f)]
        private double defaultSessionThreshold = 0.05; // 50ms
        
        [Header("Data Collection")]
        [SerializeField]
        private bool enableDetailedMetrics = false;
        
        [SerializeField]
        private bool enableHierarchicalProfiling = true;
        
        [SerializeField]
        private bool enableBurstCompatibleProfiling = true;
        
        [Header("Export")]
        [SerializeField]
        private bool enableDataExport = false;
        
        [SerializeField]
        private string exportPath = "ProfilingData";
        
        [SerializeField]
        private bool autoExportOnStop = false;
        
        [Header("Platform Optimizations")]
        [SerializeField]
        private bool enableMobileOptimizations = true;
        
        [SerializeField]
        private bool enableConsoleOptimizations = true;
        
        [SerializeField]
        private bool enableEditorDebugging = true;
        
        // IProfilingConfig implementation
        public string ConfigId 
        { 
            get => configId; 
            set => configId = value; 
        }
        
        public bool EnableProfiling 
        { 
            get => enableProfiling; 
            set => enableProfiling = value; 
        }
        
        public bool EnableOnStartup 
        { 
            get => enableOnStartup; 
            set => enableOnStartup = value; 
        }
        
        public bool LogToConsole 
        { 
            get => logToConsole; 
            set => logToConsole = value; 
        }
        
        public float UpdateInterval 
        { 
            get => updateInterval; 
            set => updateInterval = Mathf.Max(0.001f, value); 
        }
        
        public int MaxSamplesPerFrame 
        { 
            get => maxSamplesPerFrame; 
            set => maxSamplesPerFrame = Mathf.Max(1, value); 
        }
        
        public int HistoryBufferSize 
        { 
            get => historyBufferSize; 
            set => historyBufferSize = Mathf.Max(1, value); 
        }
        
        public bool EnableMemoryProfiling 
        { 
            get => enableMemoryProfiling; 
            set => enableMemoryProfiling = value; 
        }
        
        public bool TrackGCAllocations 
        { 
            get => trackGCAllocations; 
            set => trackGCAllocations = value; 
        }
        
        public bool EnablePoolMetrics 
        { 
            get => enablePoolMetrics; 
            set => enablePoolMetrics = value; 
        }
        
        public int MaxPoolMetricsEntries 
        { 
            get => maxPoolMetricsEntries; 
            set => maxPoolMetricsEntries = Mathf.Max(0, value); 
        }
        
        public bool EnableSystemMetrics 
        { 
            get => enableSystemMetrics; 
            set => enableSystemMetrics = value; 
        }
        
        public bool TrackCPUUsage 
        { 
            get => trackCPUUsage; 
            set => trackCPUUsage = value; 
        }
        
        public bool TrackMemoryUsage 
        { 
            get => trackMemoryUsage; 
            set => trackMemoryUsage = value; 
        }
        
        public bool TrackFrameTime 
        { 
            get => trackFrameTime; 
            set => trackFrameTime = value; 
        }
        
        public bool TrackRenderTime 
        { 
            get => trackRenderTime; 
            set => trackRenderTime = value; 
        }
        
        public bool EnableAlerts 
        { 
            get => enableAlerts; 
            set => enableAlerts = value; 
        }
        
        public double DefaultMetricThreshold 
        { 
            get => defaultMetricThreshold; 
            set => defaultMetricThreshold = System.Math.Max(0.001, value); 
        }
        
        public double DefaultSessionThreshold 
        { 
            get => defaultSessionThreshold; 
            set => defaultSessionThreshold = System.Math.Max(0.001, value); 
        }
        
        public bool EnableDetailedMetrics 
        { 
            get => enableDetailedMetrics; 
            set => enableDetailedMetrics = value; 
        }
        
        public bool EnableHierarchicalProfiling 
        { 
            get => enableHierarchicalProfiling; 
            set => enableHierarchicalProfiling = value; 
        }
        
        public bool EnableBurstCompatibleProfiling 
        { 
            get => enableBurstCompatibleProfiling; 
            set => enableBurstCompatibleProfiling = value; 
        }
        
        public bool EnableDataExport 
        { 
            get => enableDataExport; 
            set => enableDataExport = value; 
        }
        
        public string ExportPath 
        { 
            get => exportPath; 
            set => exportPath = value ?? "ProfilingData"; 
        }
        
        public bool AutoExportOnStop 
        { 
            get => autoExportOnStop; 
            set => autoExportOnStop = value; 
        }