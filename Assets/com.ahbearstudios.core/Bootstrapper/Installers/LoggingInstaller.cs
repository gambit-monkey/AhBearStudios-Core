using System;
using VContainer;
using AhBearStudios.Core.DependencyInjection;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    /// <summary>
    /// Installer for the logging system that registers all logging-related dependencies.
    /// </summary>
    public sealed class LoggingInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Logging System";
        
        public override int Priority => 10; // Install first - no dependencies
        
        public override Type[] Dependencies => Array.Empty<Type>();
        
        public override bool ValidateInstaller()
        {
            // Logging system has no external dependencies, always valid
            return true;
        }
        
        protected override void InstallCore(IContainerBuilder builder)
        {
            LogInstallation("Installing logging system components");
            
            // Register configuration interface
            RegisterFactory<ILoggerConfig>(builder, resolver =>
            {
                var coreConfig = resolver.Resolve<Bootstrap.Configuration.CoreSystemsConfig>();
                return coreConfig.LoggingConfig ?? CreateDefaultLoggingConfig();
            });
            
            // Register logger factory
            RegisterSingleton<ILoggerFactory, DefaultLoggerFactory>(builder);
            
            // Register main logger
            RegisterFactory<IBurstLogger>(builder, resolver =>
            {
                var factory = resolver.Resolve<ILoggerFactory>();
                var config = resolver.Resolve<ILoggerConfig>();
                return factory.CreateLogger(config);
            });
            
            // Register log targets based on configuration
            RegisterLogTargets(builder);
            
            // Register batch processor for job system integration
            RegisterFactory<Jobs.LogBatchProcessor>(builder, resolver =>
            {
                var logger = resolver.Resolve<IBurstLogger>();
                var config = resolver.Resolve<ILoggerConfig>();
                var factory = resolver.Resolve<ILoggerFactory>();
                return factory.CreateBatchProcessor(logger, config);
            });
            
            LogInstallation("Logging system installation completed");
        }
        
        private void RegisterLogTargets(IContainerBuilder builder)
        {
            LogInstallation("Registering log targets");
            
            // Register console target
            RegisterSingleton<ILogTarget, ConsoleLogTarget>(builder);
            
            // Register Unity log target for editor integration
            RegisterSingleton<ILogTarget, UnityLogTarget>(builder);
            
            // Register file target with platform-specific handling
#if !UNITY_WEBGL
            RegisterSingleton<ILogTarget, FileLogTarget>(builder);
#endif
            
            // Register memory target for debugging
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterSingleton<ILogTarget, MemoryLogTarget>(builder);
#endif
        }
        
        private ILoggerConfig CreateDefaultLoggingConfig()
        {
            LogWarning("No logging configuration provided, creating default configuration");
            
            var defaultConfig = UnityEngine.ScriptableObject.CreateInstance<LoggingConfig>();
            
            // Set sensible defaults based on platform
#if UNITY_EDITOR
            defaultConfig.MinimumLevel = 0; // Log everything in editor
            defaultConfig.MaxMessagesPerBatch = 100;
#elif DEVELOPMENT_BUILD
            defaultConfig.MinimumLevel = 1; // Skip verbose in development
            defaultConfig.MaxMessagesPerBatch = 50;
#else
            defaultConfig.MinimumLevel = 2; // Only warnings and errors in production
            defaultConfig.MaxMessagesPerBatch = 25;
#endif
            
            return defaultConfig;
        }
    }
    
    // Placeholder implementations for log targets - these would be actual implementations
    internal class ConsoleLogTarget : ILogTarget
    {
        public string Name => "Console";
        public byte MinimumLevel { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        
        public void WriteBatch(Unity.Collections.NativeList<Data.LogMessage> entries) { /* Implementation */ }
        public void Write(in Data.LogMessage entry) { /* Implementation */ }
        public void Flush() { /* Implementation */ }
        public bool IsLevelEnabled(byte level) => IsEnabled && level >= MinimumLevel;
        public void AddTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void RemoveTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void ClearTagFilters() { /* Implementation */ }
        public void Dispose() { /* Implementation */ }
    }
    
    internal class UnityLogTarget : ILogTarget
    {
        public string Name => "Unity";
        public byte MinimumLevel { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        
        public void WriteBatch(Unity.Collections.NativeList<Data.LogMessage> entries) { /* Implementation */ }
        public void Write(in Data.LogMessage entry) { /* Implementation */ }
        public void Flush() { /* Implementation */ }
        public bool IsLevelEnabled(byte level) => IsEnabled && level >= MinimumLevel;
        public void AddTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void RemoveTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void ClearTagFilters() { /* Implementation */ }
        public void Dispose() { /* Implementation */ }
    }
    
    internal class FileLogTarget : ILogTarget
    {
        public string Name => "File";
        public byte MinimumLevel { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        
        public void WriteBatch(Unity.Collections.NativeList<Data.LogMessage> entries) { /* Implementation */ }
        public void Write(in Data.LogMessage entry) { /* Implementation */ }
        public void Flush() { /* Implementation */ }
        public bool IsLevelEnabled(byte level) => IsEnabled && level >= MinimumLevel;
        public void AddTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void RemoveTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void ClearTagFilters() { /* Implementation */ }
        public void Dispose() { /* Implementation */ }
    }
    
    internal class MemoryLogTarget : ILogTarget
    {
        public string Name => "Memory";
        public byte MinimumLevel { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        
        public void WriteBatch(Unity.Collections.NativeList<Data.LogMessage> entries) { /* Implementation */ }
        public void Write(in Data.LogMessage entry) { /* Implementation */ }
        public void Flush() { /* Implementation */ }
        public bool IsLevelEnabled(byte level) => IsEnabled && level >= MinimumLevel;
        public void AddTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void RemoveTagFilter(Tags.Tagging.TagCategory tagCategory) { /* Implementation */ }
        public void ClearTagFilters() { /* Implementation */ }
        public void Dispose() { /* Implementation */ }
    }
    
    // Placeholder implementations for missing types
    internal class DefaultLoggerFactory : ILoggerFactory
    {
        public IBurstLogger CreateLogger(ILoggerConfig config) { return new BurstLogger(); }
        public Jobs.LogBatchProcessor CreateBatchProcessor(IBurstLogger burstLogger, ILoggerConfig config) { return new Jobs.LogBatchProcessor(); }
        public Jobs.JobLogger CreateJobLogger(Unity.Collections.NativeQueue<Data.LogMessage> queue, ILoggerConfig config) { return new Jobs.JobLogger(); }
    }
    
    internal class BurstLogger : IBurstLogger
    {
        public void Log(byte level, string message, string tag) { /* Implementation */ }
        public void Log(byte level, string message, string tag, Data.LogProperties properties) { /* Implementation */ }
        public bool IsEnabled(byte level) { return true; }
    }
}