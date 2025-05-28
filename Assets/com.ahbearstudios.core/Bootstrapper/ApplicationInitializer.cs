using System;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using AhBearStudios.Core.Bootstrap.Configuration;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Bootstrap
{
    /// <summary>
    /// Handles application initialization after dependency injection is complete.
    /// Runs as a VContainer EntryPoint and performs post-DI setup tasks.
    /// </summary>
    public sealed class ApplicationInitializer : IStartable, IDisposable
    {
        private readonly CoreSystemsConfig coreConfig;
        private readonly InstallerConfig installerConfig;
        private readonly IBurstLogger logger;
        private readonly IMessageBus messageBus;
        private readonly IProfiler profiler;
        private readonly IPoolRegistry poolRegistry;
        
        private bool isInitialized = false;
        private float gcTimer = 0f;
        
        public ApplicationInitializer(
            CoreSystemsConfig coreConfig,
            InstallerConfig installerConfig,
            IBurstLogger logger,
            IMessageBus messageBus = null,
            IProfiler profiler = null,
            IPoolRegistry poolRegistry = null)
        {
            this.coreConfig = coreConfig ?? throw new ArgumentNullException(nameof(coreConfig));
            this.installerConfig = installerConfig ?? throw new ArgumentNullException(nameof(installerConfig));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.messageBus = messageBus;
            this.profiler = profiler;
            this.poolRegistry = poolRegistry;
        }
        
        public void Start()
        {
            try
            {
                using (profiler?.BeginSample("ApplicationInitialization"))
                {
                    logger.Log(Logging.LogLevel.Info, "Starting application initialization", "Bootstrap");
                    
                    InitializeSubsystems();
                    RegisterForApplicationEvents();
                    ValidateSystemHealth();
                    PerformPostInitializationTasks();
                    
                    isInitialized = true;
                    
                    logger.Log(Logging.LogLevel.Info, "Application initialization completed successfully", "Bootstrap");
                    
                    // Publish application ready event
                    PublishApplicationReadyEvent();
                }
            }
            catch (Exception ex)
            {
                logger.Log(Logging.LogLevel.Error, $"Application initialization failed: {ex.Message}", "Bootstrap");
                throw;
            }
        }
        
        private void InitializeSubsystems()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing subsystems", "Bootstrap");
            
            // Initialize pooling system
            if (coreConfig.EnablePooling && poolRegistry != null)
            {
                InitializePoolingSystem();
            }
            
            // Initialize profiling system
            if (coreConfig.EnableProfiling && profiler != null)
            {
                InitializeProfilingSystem();
            }
            
            // Initialize message bus system
            if (coreConfig.EnableMessageBus && messageBus != null)
            {
                InitializeMessageBusSystem();
            }
            
            // Initialize platform-specific systems
            InitializePlatformSystems();
        }
        
        private void InitializePoolingSystem()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing pooling system", "Bootstrap");
            
            try
            {
                // Prewarm commonly used pools if enabled
                if (coreConfig.PoolingConfig?.PrewarmOnInit == true)
                {
                    PrewarmCommonPools();
                }
                
                // Setup pool metrics if enabled
                if (coreConfig.PoolingConfig?.CollectMetrics == true && profiler != null)
                {
                    // Register pool performance alerts
                    profiler.RegisterMetricAlert(new ProfilerTag("Pool", "HitRatio"), 0.8);
                    profiler.RegisterMetricAlert(new ProfilerTag("Pool", "Efficiency"), 0.7);
                }
                
                logger.Log(Logging.LogLevel.Info, "Pooling system initialized successfully", "Bootstrap");
            }
            catch (Exception ex)
            {
                logger.Log(Logging.LogLevel.Error, $"Failed to initialize pooling system: {ex.Message}", "Bootstrap");
                throw;
            }
        }
        
        private void InitializeProfilingSystem()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing profiling system", "Bootstrap");
            
            try
            {
                // Start profiling if enabled
                if (coreConfig.ProfilingConfig?.EnableOnStartup == true)
                {
                    profiler.StartProfiling();
                    logger.Log(Logging.LogLevel.Info, "Profiling started automatically", "Bootstrap");
                }
                
                // Setup performance monitoring alerts
                if (coreConfig.EnableMemoryProfiling)
                {
                    SetupMemoryProfilingAlerts();
                }
                
                // Setup system performance alerts
                SetupSystemPerformanceAlerts();
                
                logger.Log(Logging.LogLevel.Info, "Profiling system initialized successfully", "Bootstrap");
            }
            catch (Exception ex)
            {
                logger.Log(Logging.LogLevel.Error, $"Failed to initialize profiling system: {ex.Message}", "Bootstrap");
                throw;
            }
        }
        
        private void InitializeMessageBusSystem()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing message bus system", "Bootstrap");
            
            try
            {
                // Discover and register message types
                var registry = messageBus.GetMessageRegistry();
                registry.DiscoverMessages();
                
                // Setup message processing monitoring
                if (profiler != null)
                {
                    profiler.RegisterMetricAlert(new ProfilerTag("MessageBus", "QueueSize"), 1000);
                    profiler.RegisterMetricAlert(new ProfilerTag("MessageBus", "ProcessingTime"), 0.016); // 16ms
                }
                
                // Subscribe to system-level events
                SubscribeToSystemEvents();
                
                logger.Log(Logging.LogLevel.Info, "Message bus system initialized successfully", "Bootstrap");
            }
            catch (Exception ex)
            {
                logger.Log(Logging.LogLevel.Error, $"Failed to initialize message bus system: {ex.Message}", "Bootstrap");
                throw;
            }
        }
        
        private void InitializePlatformSystems()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing platform-specific systems", "Bootstrap");
            
#if UNITY_ANDROID || UNITY_IOS
            if (installerConfig.EnableMobileSpecificSystems)
            {
                InitializeMobileSystems();
            }
#elif UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
            if (installerConfig.EnableConsoleSpecificSystems)
            {
                InitializeConsoleSystems();
            }
#elif UNITY_STANDALONE
            if (installerConfig.EnablePCSpecificSystems)
            {
                InitializePCSystems();
            }
#endif
        }
        
        private void PrewarmCommonPools()
        {
            logger.Log(Logging.LogLevel.Info, "Prewarming common pools", "Bootstrap");
            
            // This would prewarm commonly used object pools
            // Implementation would depend on your specific pooled types
            
            // Example:
            // var gameObjectPool = poolRegistry.GetPoolByType<GameObject>();
            // gameObjectPool?.EnsureCapacity(50);
        }
        
        private void SetupMemoryProfilingAlerts()
        {
            if (profiler == null) return;
            
            // Setup memory-related performance alerts
            profiler.RegisterMetricAlert(new ProfilerTag("Memory", "GCAlloc"), 1048576); // 1MB per frame
            profiler.RegisterMetricAlert(new ProfilerTag("Memory", "UsedHeap"), 536870912); // 512MB
        }
        
        private void SetupSystemPerformanceAlerts()
        {
            if (profiler == null) return;
            
            // Setup system performance alerts
            profiler.RegisterSessionAlert(new ProfilerTag("System", "FrameTime"), 33.33); // 30fps threshold
            profiler.RegisterSessionAlert(new ProfilerTag("System", "UpdateTime"), 16.67); // 60fps threshold
        }
        
        private void SubscribeToSystemEvents()
        {
            if (messageBus == null) return;
            
            // Subscribe to application lifecycle events
            messageBus.SubscribeToMessage<ApplicationPauseMessage>(OnApplicationPause);
            messageBus.SubscribeToMessage<ApplicationFocusMessage>(OnApplicationFocus);
            messageBus.SubscribeToMessage<MemoryWarningMessage>(OnMemoryWarning);
        }
        
        private void RegisterForApplicationEvents()
        {
            logger.Log(Logging.LogLevel.Info, "Registering for application events", "Bootstrap");
            
            Application.quitting += OnApplicationQuitting;
            Application.lowMemory += OnLowMemory;
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }
        
        private void ValidateSystemHealth()
        {
            logger.Log(Logging.LogLevel.Info, "Performing system health validation", "Bootstrap");
            
            // Validate core systems are responding
            try
            {
                if (messageBus != null)
                {
                    // Test message bus responsiveness
                    var testMessage = new SystemHealthCheckMessage
                    {
                        Id = Guid.NewGuid(),
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        TypeCode = 9999 // Test message type code
                    };
                    messageBus.PublishMessage(testMessage);
                }
                
                if (poolRegistry != null)
                {
                    // Validate pool registry is accessible
                    var poolCount = poolRegistry.Count;
                    logger.Log(Logging.LogLevel.Info, $"Pool registry accessible with {poolCount} registered pools", "Bootstrap");
                }
                
                logger.Log(Logging.LogLevel.Info, "System health validation completed successfully", "Bootstrap");
            }
            catch (Exception ex)
            {
                logger.Log(Logging.LogLevel.Warning, $"System health validation encountered issues: {ex.Message}", "Bootstrap");
            }
        }
        
        private void PerformPostInitializationTasks()
        {
            logger.Log(Logging.LogLevel.Info, "Performing post-initialization tasks", "Bootstrap");
            
            // Setup automatic garbage collection if enabled
            if (coreConfig.EnableAutoGarbageCollection)
            {
                logger.Log(Logging.LogLevel.Info, $"Automatic GC enabled with {coreConfig.GCInterval}s interval", "Bootstrap");
            }
            
            // Initialize development features if enabled
            if (coreConfig.EnableDevelopmentFeatures)
            {
                InitializeDevelopmentFeatures();
            }
            
            // Log system configuration summary
            LogSystemConfigurationSummary();
        }
        
        private void InitializeDevelopmentFeatures()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing development features", "Bootstrap");
            
            // Development-specific initialization
            if (installerConfig.EnableDebugConsole)
            {
                logger.Log(Logging.LogLevel.Info, "Debug console available", "Bootstrap");
            }
            
            if (installerConfig.EnablePerformanceHUD)
            {
                logger.Log(Logging.LogLevel.Info, "Performance HUD available", "Bootstrap");
            }
        }
        
        private void LogSystemConfigurationSummary()
        {
            var enabledSystems = new System.Collections.Generic.List<string>();
            
            if (coreConfig.EnableLogging) enabledSystems.Add("Logging");
            if (coreConfig.EnableProfiling) enabledSystems.Add("Profiling");
            if (coreConfig.EnablePooling) enabledSystems.Add("Pooling");
            if (coreConfig.EnableMessageBus) enabledSystems.Add("MessageBus");
            
            logger.Log(Logging.LogLevel.Info, 
                $"System configuration summary - Enabled systems: [{string.Join(", ", enabledSystems)}], " +
                $"Update interval: {coreConfig.SystemUpdateInterval:F3}s, " +
                $"Max operations/frame: {coreConfig.MaxOperationsPerFrame}", 
                "Bootstrap");
        }
        
        private void PublishApplicationReadyEvent()
        {
            if (messageBus == null) return;
            
            try
            {
                var readyMessage = new ApplicationReadyMessage
                {
                    Id = Guid.NewGuid(),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = 1000, // Application ready message type code
                    InitializationTimeMs = Time.realtimeSinceStartup * 1000f
                };
                
                messageBus.PublishMessage(readyMessage);
                logger.Log(Logging.LogLevel.Info, "Application ready event published", "Bootstrap");
            }
            catch (Exception ex)
            {
                logger.Log(Logging.LogLevel.Warning, $"Failed to publish application ready event: {ex.Message}", "Bootstrap");
            }
        }
        
        // Application event handlers
        private void OnApplicationQuitting()
        {
            logger?.Log(Logging.LogLevel.Info, "Application quitting", "Bootstrap");
            
            // Graceful shutdown of systems
            if (profiler != null && profiler.IsEnabled)
            {
                profiler.StopProfiling();
            }
            
            // Flush any pending operations
            if (messageBus != null)
            {
                // Allow message bus to process remaining messages
            }
        }
        
        private void OnLowMemory()
        {
            logger?.Log(Logging.LogLevel.Warning, "Low memory warning received", "Bootstrap");
            
            // Trigger memory cleanup
            if (messageBus != null)
            {
                var memoryWarning = new MemoryWarningMessage
                {
                    Id = Guid.NewGuid(),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = 1001,
                    MemoryLevel = MemoryLevel.Critical
                };
                messageBus.PublishMessage(memoryWarning);
            }
            
            // Force garbage collection
            System.GC.Collect();
            
            // Request pool shrinking
            if (poolRegistry != null)
            {
                RequestPoolShrinking();
            }
        }
        
        private void OnApplicationPause(ApplicationPauseMessage message)
        {
            logger?.Log(Logging.LogLevel.Info, $"Application pause state changed: {message.IsPaused}", "Bootstrap");
            
            if (message.IsPaused)
            {
                // Pause non-essential systems
                if (profiler != null && profiler.IsEnabled)
                {
                    profiler.StopProfiling();
                }
            }
            else
            {
                // Resume systems
                if (profiler != null && coreConfig.ProfilingConfig?.EnableOnStartup == true)
                {
                    profiler.StartProfiling();
                }
            }
        }
        
        private void OnApplicationFocus(ApplicationFocusMessage message)
        {
            logger?.Log(Logging.LogLevel.Info, $"Application focus state changed: {message.HasFocus}", "Bootstrap");
            
            // Adjust system performance based on focus
            if (!message.HasFocus)
            {
                // Reduce performance when unfocused
                ReduceSystemPerformance();
            }
            else
            {
                // Restore full performance when focused
                RestoreSystemPerformance();
            }
        }
        
        private void OnMemoryWarning(MemoryWarningMessage message)
        {
            logger?.Log(Logging.LogLevel.Warning, $"Memory warning received: {message.MemoryLevel}", "Bootstrap");
            
            switch (message.MemoryLevel)
            {
                case MemoryLevel.Low:
                    // Trigger pool shrinking
                    RequestPoolShrinking();
                    break;
                    
                case MemoryLevel.Critical:
                    // Aggressive memory cleanup
                    RequestPoolShrinking();
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                    break;
            }
        }
        
#if UNITY_EDITOR
        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            switch (state)
            {
                case UnityEditor.PlayModeStateChange.ExitingPlayMode:
                    logger?.Log(Logging.LogLevel.Info, "Exiting play mode", "Bootstrap");
                    // Cleanup before exiting play mode
                    break;
                    
                case UnityEditor.PlayModeStateChange.EnteredEditMode:
                    logger?.Log(Logging.LogLevel.Info, "Entered edit mode", "Bootstrap");
                    break;
            }
        }
#endif
        
        // Platform-specific initialization methods
        private void InitializeMobileSystems()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing mobile-specific systems", "Bootstrap");
            
            // Mobile-specific optimizations
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            // Setup mobile input handling
            // Setup mobile-specific memory management
            // Setup mobile-specific performance monitoring
        }
        
        private void InitializeConsoleSystems()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing console-specific systems", "Bootstrap");
            
            // Console-specific optimizations
            Application.targetFrameRate = -1; // Use platform default
            
            // Setup console-specific input handling
            // Setup console-specific memory management
            // Setup console-specific networking
        }
        
        private void InitializePCSystems()
        {
            logger.Log(Logging.LogLevel.Info, "Initializing PC-specific systems", "Bootstrap");
            
            // PC-specific optimizations
            Application.targetFrameRate = -1; // Uncapped
            
            // Setup PC-specific input handling
            // Setup PC-specific graphics options
            // Setup PC-specific file system access
        }
        
        // System management methods
        private void RequestPoolShrinking()
        {
            if (poolRegistry == null) return;
            
            try
            {
                var pools = poolRegistry.GetAllPools();
                foreach (var pool in pools)
                {
                    if (pool is IShrinkablePool shrinkablePool)
                    {
                        shrinkablePool.TryShrink(0.5f); // Shrink if usage below 50%
                    }
                }
                
                logger?.Log(Logging.LogLevel.Info, "Pool shrinking requested", "Bootstrap");
            }
            catch (Exception ex)
            {
                logger?.Log(Logging.LogLevel.Warning, $"Pool shrinking failed: {ex.Message}", "Bootstrap");
            }
        }
        
        private void ReduceSystemPerformance()
        {
            // Reduce update frequencies
            // Disable non-essential visual effects
            // Reduce physics simulation quality
            logger?.Log(Logging.LogLevel.Info, "System performance reduced", "Bootstrap");
        }
        
        private void RestoreSystemPerformance()
        {
            // Restore normal update frequencies
            // Re-enable visual effects
            // Restore physics simulation quality
            logger?.Log(Logging.LogLevel.Info, "System performance restored", "Bootstrap");
        }
        
        // Update method for ongoing maintenance tasks
        public void Update()
        {
            if (!isInitialized) return;
            
            // Handle automatic garbage collection
            if (coreConfig.EnableAutoGarbageCollection)
            {
                gcTimer += Time.deltaTime;
                if (gcTimer >= coreConfig.GCInterval)
                {
                    gcTimer = 0f;
                    System.GC.Collect();
                    logger?.Log(Logging.LogLevel.Debug, "Automatic garbage collection triggered", "Bootstrap");
                }
            }
        }
        
        public void Dispose()
        {
            // Unregister from application events
            Application.quitting -= OnApplicationQuitting;
            Application.lowMemory -= OnLowMemory;
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
            
            // Unsubscribe from message bus events
            // Note: In a real implementation, you'd store subscription tokens and dispose them
            
            logger?.Log(Logging.LogLevel.Info, "Application initializer disposed", "Bootstrap");
        }
        
        /// <summary>
        /// Gets whether the application has been fully initialized.
        /// </summary>
        public bool IsInitialized => isInitialized;
    }
    
    // Message types for application lifecycle
    public struct ApplicationReadyMessage : IMessage
    {
        public Guid Id { get; set; }
        public long TimestampTicks { get; set; }
        public ushort TypeCode { get; set; }
        public float InitializationTimeMs { get; set; }
    }
    
    public struct ApplicationPauseMessage : IMessage
    {
        public Guid Id { get; set; }
        public long TimestampTicks { get; set; }
        public ushort TypeCode { get; set; }
        public bool IsPaused { get; set; }
    }
    
    public struct ApplicationFocusMessage : IMessage
    {
        public Guid Id { get; set; }
        public long TimestampTicks { get; set; }
        public ushort TypeCode { get; set; }
        public bool HasFocus { get; set; }
    }
    
    public struct MemoryWarningMessage : IMessage
    {
        public Guid Id { get; set; }
        public long TimestampTicks { get; set; }
        public ushort TypeCode { get; set; }
        public MemoryLevel MemoryLevel { get; set; }
    }
    
    public struct SystemHealthCheckMessage : IMessage
    {
        public Guid Id { get; set; }
        public long TimestampTicks { get; set; }
        public ushort TypeCode { get; set; }
    }
    
    public enum MemoryLevel
    {
        Normal = 0,
        Low = 1,
        Critical = 2
    }
    
    // Placeholder ProfilerTag struct
    internal struct ProfilerTag
    {
        public string Category;
        public string Name;
        
        public ProfilerTag(string category, string name)
        {
            Category = category;
            Name = name;
        }
    }
    
    // Placeholder LogLevel enum
    namespace Logging
    {
        public enum LogLevel : byte
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Fatal = 4
        }
    }
}