using System;
using VContainer;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Configuration;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    /// <summary>
    /// Installer for the profiling system that registers all profiling-related dependencies.
    /// </summary>
    public sealed class ProfilingInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Profiling System";
        
        public override int Priority => 20; // Install after logging
        
        public override Type[] Dependencies => new[] { typeof(LoggingInstaller) };
        
        public override bool ValidateInstaller()
        {
            // Profiling requires logging system
            return true; // Dependencies are validated by the bootstrap system
        }
        
        protected override void InstallCore(IContainerBuilder builder)
        {
            LogInstallation("Installing profiling system components");
            
            // Register configuration interface
            RegisterFactory<IProfilingConfig>(builder, resolver =>
            {
                var coreConfig = resolver.Resolve<Bootstrap.Configuration.CoreSystemsConfig>();
                return coreConfig.ProfilingConfig ?? CreateDefaultProfilingConfig();
            });
            
            // Register profiler manager
            RegisterSingleton<IProfilerManager, ProfilerManager>(builder);
            
            // Register main profiler interface
            RegisterFactory<IProfiler>(builder, resolver =>
            {
                var manager = resolver.Resolve<IProfilerManager>();
                return new ProfilerAdapter(manager);
            });
            
            // Register metrics tracking systems
            RegisterMetricsComponents(builder);
            
            // Register performance monitoring
            RegisterPerformanceComponents(builder);
            
            LogInstallation("Profiling system installation completed");
        }
        
        private void RegisterMetricsComponents(IContainerBuilder builder)
        {
            LogInstallation("Registering metrics components");
            
            // Register pool metrics for managed pools
            RegisterSingleton<IPoolMetrics, PoolMetrics>(builder);
            
            // Register native pool metrics for burst-compatible pools
            RegisterFactory<INativePoolMetrics>(builder, resolver =>
            {
                var config = resolver.Resolve<IProfilingConfig>();
                return new NativePoolMetrics(Unity.Collections.Allocator.Persistent, config.MaxPoolMetricsEntries);
            });
            
            // Register serializer metrics
            RegisterSingleton<ISerializerMetrics, SerializerMetrics>(builder);
            
            // Register system metrics tracker
            RegisterSingleton<SystemMetricsTracker, SystemMetricsTracker>(builder);
        }
        
        private void RegisterPerformanceComponents(IContainerBuilder builder)
        {
            LogInstallation("Registering performance monitoring components");
            
            // Register performance session factory
            RegisterSingleton<IProfilerSessionFactory, ProfilerSessionFactory>(builder);
            
            // Register alert system
            RegisterSingleton<IAlertSystem, AlertSystem>(builder);
            
            // Register data export system (if enabled)
            RegisterFactory<IDataExportSystem>(builder, resolver =>
            {
                var config = resolver.Resolve<IProfilingConfig>();
                if (config.EnableDataExport)
                {
                    return new DataExportSystem(config.ExportPath);
                }
                return new NullDataExportSystem();
            });
        }
        
        private IProfilingConfig CreateDefaultProfilingConfig()
        {
            LogWarning("No profiling configuration provided, creating default configuration");
            
            var defaultConfig = UnityEngine.ScriptableObject.CreateInstance<ProfilingConfig>();
            
            // Set sensible defaults based on platform and build type
#if UNITY_EDITOR
            defaultConfig.EnableProfiling = true;
            defaultConfig.EnableOnStartup = true;
            defaultConfig.LogToConsole = true;
            defaultConfig.EnableDetailedMetrics = true;
            defaultConfig.EnableMemoryProfiling = true;
            defaultConfig.TrackGCAllocations = true;
            defaultConfig.EnableDataExport = true;
#elif DEVELOPMENT_BUILD
            defaultConfig.EnableProfiling = true;
            defaultConfig.EnableOnStartup = true;
            defaultConfig.LogToConsole = false;
            defaultConfig.EnableDetailedMetrics = false;
            defaultConfig.EnableMemoryProfiling = true;
            defaultConfig.TrackGCAllocations = false;
            defaultConfig.EnableDataExport = false;
#else
            defaultConfig.EnableProfiling = false;
            defaultConfig.EnableOnStartup = false;
            defaultConfig.LogToConsole = false;
            defaultConfig.EnableDetailedMetrics = false;
            defaultConfig.EnableMemoryProfiling = false;
            defaultConfig.TrackGCAllocations = false;
            defaultConfig.EnableDataExport = false;
#endif
            
            return defaultConfig;
        }
    }
    
    // Placeholder implementations - these would be actual implementations
    internal class ProfilerManager : IProfilerManager
    {
        public bool IsEnabled { get; private set; }
        public bool LogToConsole { get; set; }
        public MessageBus.Interfaces.IMessageBusService MessageBusService { get; private set; }
        public SystemMetricsTracker SystemMetrics { get; private set; }
        
        public void StartProfiling() { IsEnabled = true; }
        public void StopProfiling() { IsEnabled = false; }
        public IProfilerSession BeginScope(ProfilerTag tag) { return new ProfilerSession(tag); }
        public IProfilerSession BeginScope(Unity.Profiling.ProfilerCategory category, string name) { return new ProfilerSession(new ProfilerTag(category, name)); }
        public ProfileStats GetStats(ProfilerTag tag) { return new ProfileStats(); }
        public System.Collections.Generic.IReadOnlyDictionary<ProfilerTag, ProfileStats> GetAllStats() { return new System.Collections.Generic.Dictionary<ProfilerTag, ProfileStats>(); }
        public System.Collections.Generic.IReadOnlyList<double> GetHistory(ProfilerTag tag) { return new System.Collections.Generic.List<double>(); }
        public void ResetStats() { }
        public System.Collections.Generic.IReadOnlyDictionary<ProfilerTag, System.Collections.Generic.List<IProfilerSession>> GetActiveSessions() { return new System.Collections.Generic.Dictionary<ProfilerTag, System.Collections.Generic.List<IProfilerSession>>(); }
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold) { }
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs) { }
        public void Update(float deltaTime) { }
    }
    
    internal class ProfilerAdapter : IProfiler
    {
        private readonly IProfilerManager manager;
        
        public ProfilerAdapter(IProfilerManager manager)
        {
            this.manager = manager;
        }
        
        public bool IsEnabled => manager.IsEnabled;
        public MessageBus.Interfaces.IMessageBusService MessageBusService => manager.MessageBusService;
        
        public IDisposable BeginSample(string name) { return manager.BeginScope(Unity.Profiling.ProfilerCategory.Scripts, name); }
        public ProfilerSession BeginScope(ProfilerTag tag) { return (ProfilerSession)manager.BeginScope(tag); }
        public ProfilerSession BeginScope(Unity.Profiling.ProfilerCategory category, string name) { return (ProfilerSession)manager.BeginScope(category, name); }
        public Data.DefaultMetricsData GetMetrics(ProfilerTag tag) { return new Data.DefaultMetricsData(); }
        public System.Collections.Generic.IReadOnlyDictionary<ProfilerTag, Data.DefaultMetricsData> GetAllMetrics() { return new System.Collections.Generic.Dictionary<ProfilerTag, Data.DefaultMetricsData>(); }
        public System.Collections.Generic.IReadOnlyList<double> GetHistory(ProfilerTag tag) { return manager.GetHistory(tag); }
        public void ResetStats() { manager.ResetStats(); }
        public void StartProfiling() { manager.StartProfiling(); }
        public void StopProfiling() { manager.StopProfiling(); }
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold) { manager.RegisterMetricAlert(metricTag, threshold); }
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs) { manager.RegisterSessionAlert(sessionTag, thresholdMs); }
    }
    
    internal class ProfilerSession : IProfilerSession
    {
        public ProfilerTag Tag { get; private set; }
        public double ElapsedMilliseconds { get; private set; }
        public long ElapsedNanoseconds { get; private set; }
        public bool IsDisposed { get; private set; }
        
        public ProfilerSession(ProfilerTag tag)
        {
            Tag = tag;
        }
        
        public void RecordMetric(string metricName, double value) { }
        public void Dispose() { IsDisposed = true; }
    }
    
    internal class PoolMetrics : IPoolMetrics
    {
        public bool IsCreated => true;
        
        public Data.PoolMetricsData GetMetricsData(Guid poolId) { return new Data.PoolMetricsData(); }
        public Data.PoolMetricsData? GetPoolMetrics(Guid poolId) { return new Data.PoolMetricsData(); }
        public Data.PoolMetricsData GetGlobalMetricsData() { return new Data.PoolMetricsData(); }
        public void RecordAcquire(Guid poolId, int activeCount, float acquireTimeMs) { }
        public void RecordRelease(Guid poolId, int activeCount, float releaseTimeMs, float lifetimeSeconds = 0) { }
        public void UpdatePoolConfiguration(Guid poolId, int capacity, int minCapacity = 0, int maxCapacity = 0, string poolType = null, int itemSizeBytes = 0) { }
        public void UpdatePoolConfiguration(Guid poolId, int capacity) { }
        public System.Collections.Generic.Dictionary<Guid, Data.PoolMetricsData> GetAllPoolMetrics() { return new System.Collections.Generic.Dictionary<Guid, Data.PoolMetricsData>(); }
        public void ResetPoolStats(Guid poolId) { }
        public void ResetAllPoolStats() { }
        public void ResetStats() { }
        public float GetPoolHitRatio(Guid poolId) { return 1.0f; }
        public float GetPoolEfficiency(Guid poolId) { return 1.0f; }
        public void RecordCreate(Guid poolId, int createdCount, long memoryOverheadBytes = 0) { }
        public void RecordFragmentation(Guid poolId, int fragmentCount, float fragmentationRatio) { }
        public void RecordOperationResults(Guid poolId, int acquireSuccessCount = 0, int acquireFailureCount = 0, int releaseSuccessCount = 0, int releaseFailureCount = 0) { }
        public void RecordResize(Guid poolId, int oldCapacity, int newCapacity, float resizeTimeMs = 0) { }
        public System.Collections.Generic.Dictionary<string, string> GetPerformanceSnapshot(Guid poolId) { return new System.Collections.Generic.Dictionary<string, string>(); }
        public void RegisterAlert(Guid poolId, string metricName, double threshold) { }
    }
    
    internal class NativePoolMetrics : INativePoolMetrics
    {
        public bool IsCreated => true;
        public Unity.Collections.Allocator Allocator { get; private set; }
        
        public NativePoolMetrics(Unity.Collections.Allocator allocator, int capacity)
        {
            Allocator = allocator;
        }
        
        public Data.PoolMetricsData GetMetricsData(Unity.Collections.FixedString64Bytes poolId) { return new Data.PoolMetricsData(); }
        public Data.PoolMetricsData GetGlobalMetricsData() { return new Data.PoolMetricsData(); }
        public Unity.Jobs.JobHandle RecordAcquire(Unity.Collections.FixedString64Bytes poolId, int activeCount, float acquireTimeMs, Unity.Jobs.JobHandle dependencies = default) { return dependencies; }
        public Unity.Jobs.JobHandle RecordRelease(Unity.Collections.FixedString64Bytes poolId, int activeCount, float releaseTimeMs, float lifetimeSeconds = 0, Unity.Jobs.JobHandle dependencies = default) { return dependencies; }
        public void UpdatePoolConfiguration(Unity.Collections.FixedString64Bytes poolId, int capacity, int minCapacity = 0, int maxCapacity = 0, Unity.Collections.FixedString64Bytes poolType = default, int itemSizeBytes = 0) { }
        public Unity.Collections.NativeArray<Data.PoolMetricsData> GetAllPoolMetrics(Unity.Collections.Allocator allocator) { return new Unity.Collections.NativeArray<Data.PoolMetricsData>(); }
        public void ResetPoolStats(Unity.Collections.FixedString64Bytes poolId) { }
        public void ResetAllPoolStats() { }
        public void ResetStats() { }
        public float GetPoolHitRatio(Unity.Collections.FixedString64Bytes poolId) { return 1.0f; }
        public float GetPoolEfficiency(Unity.Collections.FixedString64Bytes poolId) { return 1.0f; }
        public void RegisterAlert(Unity.Collections.FixedString64Bytes poolId, Unity.Collections.FixedString64Bytes metricName, double threshold) { }
        public void Dispose() { }
    }
    
    internal class SerializerMetrics : ISerializerMetrics
    {
        public long TotalSerializations { get; private set; }
        public long TotalDeserializations { get; private set; }
        public long FailedSerializations { get; private set; }
        public long FailedDeserializations { get; private set; }
        public double AverageSerializationTimeMs { get; private set; }
        public double AverageDeserializationTimeMs { get; private set; }
        public long TotalBytesSeralized { get; private set; }
        public long TotalBytesDeserialized { get; private set; }
        
        public void RecordSerialization(TimeSpan duration, int dataSize, bool success) { }
        public void RecordDeserialization(TimeSpan duration, int dataSize, bool success) { }
        public void Reset() { }
    }
    
    internal class SystemMetricsTracker
    {
        // Implementation for tracking system-level metrics
    }
    
    internal class ProfilerSessionFactory : IProfilerSessionFactory
    {
        public IProfilerSession CreateSession(ProfilerTag tag) { return new ProfilerSession(tag); }
    }
    
    internal class AlertSystem : IAlertSystem
    {
        public void RegisterAlert(string name, double threshold) { }
        public void CheckAlerts() { }
    }
    
    internal class DataExportSystem : IDataExportSystem
    {
        public DataExportSystem(string exportPath) { }
        public void ExportData() { }
    }
    
    internal class NullDataExportSystem : IDataExportSystem
    {
        public void ExportData() { }
    }
    
    // Placeholder interfaces
    internal interface IProfilerSessionFactory
    {
        IProfilerSession CreateSession(ProfilerTag tag);
    }
    
    internal interface IAlertSystem
    {
        void RegisterAlert(string name, double threshold);
        void CheckAlerts();
    }
    
    internal interface IDataExportSystem
    {
        void ExportData();
    }
    
    // Placeholder structs and classes
    internal struct ProfilerTag
    {
        public Unity.Profiling.ProfilerCategory Category;
        public string Name;
        
        public ProfilerTag(Unity.Profiling.ProfilerCategory category, string name)
        {
            Category = category;
            Name = name;
        }
    }
    
    internal struct ProfileStats
    {
        // Implementation for profile statistics
    }
}