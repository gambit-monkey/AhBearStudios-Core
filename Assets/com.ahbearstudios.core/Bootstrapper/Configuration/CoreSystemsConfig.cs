using UnityEngine;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Pooling.Configuration;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.Profiling.Configuration;

namespace AhBearStudios.Core.Bootstrap.Configuration
{
    /// <summary>
    /// Master configuration for all core systems.
    /// Provides centralized configuration management with platform optimization and validation.
    /// </summary>
    [CreateAssetMenu(menuName = "AhBear/Core/Core Systems Config", fileName = "CoreSystemsConfig", order = 0)]
    public sealed class CoreSystemsConfig : ScriptableObject
    {
        [Header("System Toggles")]
        [SerializeField]
        private bool enableLogging = true;
        
        [SerializeField]
        private bool enableProfiling = true;
        
        [SerializeField]
        private bool enablePooling = true;
        
        [SerializeField]
        private bool enableMessageBus = true;
        
        [Header("Performance")]
        [SerializeField, Range(0.008f, 0.1f)]
        private float systemUpdateInterval = 0.016f; // 60fps
        
        [SerializeField, Range(10, 10000)]
        private int maxOperationsPerFrame = 1000;
        
        [SerializeField]
        private bool enableBurstOptimizations = true;
        
        [SerializeField]
        private bool enableJobSystemOptimizations = true;
        
        [Header("Memory Management")]
        [SerializeField]
        private bool enableAutoGarbageCollection = false;
        
        [SerializeField, Range(0.1f, 60f)]
        private float gcInterval = 30f;
        
        [SerializeField]
        private bool enableMemoryProfiling = true;
        
        [Header("System Configurations")]
        [SerializeField]
        private LoggingConfig loggingConfig;
        
        [SerializeField]
        private PoolingConfig poolingConfig;
        
        [SerializeField]
        private MessageBusConfig messageBusConfig;
        
        [SerializeField]
        private ProfilingConfig profilingConfig;
        
        [Header("Bootstrap Settings")]
        [SerializeField]
        private bool enableDevelopmentFeatures = true;
        
        [SerializeField]
        private bool enableBootstrapLogging = true;
        
        [SerializeField]
        private bool validateSystemDependencies = true;
        
        [SerializeField]
        private bool enableGracefulFallbacks = true;
        
        [Header("Platform Overrides")]
        [SerializeField]
        private PlatformConfigOverrides platformOverrides;
        
        // Public properties
        public bool EnableLogging => enableLogging;
        public bool EnableProfiling => enableProfiling;
        public bool EnablePooling => enablePooling;
        public bool EnableMessageBus => enableMessageBus;
        
        public float SystemUpdateInterval => systemUpdateInterval;
        public int MaxOperationsPerFrame => maxOperationsPerFrame;
        public bool EnableBurstOptimizations => enableBurstOptimizations;
        public bool EnableJobSystemOptimizations => enableJobSystemOptimizations;
        
        public bool EnableAutoGarbageCollection => enableAutoGarbageCollection;
        public float GCInterval => gcInterval;
        public bool EnableMemoryProfiling => enableMemoryProfiling;
        
        public LoggingConfig LoggingConfig => loggingConfig;
        public PoolingConfig PoolingConfig => poolingConfig;
        public MessageBusConfig MessageBusConfig => messageBusConfig;
        public ProfilingConfig ProfilingConfig => profilingConfig;
        
        public bool EnableDevelopmentFeatures => enableDevelopmentFeatures;
        public bool EnableBootstrapLogging => enableBootstrapLogging;
        public bool ValidateSystemDependencies => validateSystemDependencies;
        public bool EnableGracefulFallbacks => enableGracefulFallbacks;
        
        public PlatformConfigOverrides PlatformOverrides => platformOverrides;
        
        private void OnValidate()
        {
            ValidateConfiguration();
        }
        
        private void ValidateConfiguration()
        {
            // Validate system dependencies
            if (enableMessageBus && !enableLogging)
            {
                Debug.LogWarning("MessageBus requires Logging system. Auto-enabling Logging.");
                enableLogging = true;
            }
            
            if (enableProfiling && !enableLogging)
            {
                Debug.LogWarning("Profiling requires Logging system. Auto-enabling Logging.");
                enableLogging = true;
            }
            
            if (enablePooling && !enableLogging)
            {
                Debug.LogWarning("Pooling benefits from Logging system. Consider enabling Logging.");
            }
            
            // Validate performance settings
            if (maxOperationsPerFrame < 10)
            {
                maxOperationsPerFrame = 10;
                Debug.LogWarning("MaxOperationsPerFrame too low. Reset to 10.");
            }
            
            if (systemUpdateInterval < 0.008f)
            {
                systemUpdateInterval = 0.008f;
                Debug.LogWarning("SystemUpdateInterval too low. Reset to 0.008 (125fps max).");
            }
            
            // Validate configurations exist
            if (enableLogging && loggingConfig == null)
            {
                Debug.LogError("LoggingConfig is required when Logging is enabled.");
            }
            
            if (enablePooling && poolingConfig == null)
            {
                Debug.LogError("PoolingConfig is required when Pooling is enabled.");
            }
            
            if (enableMessageBus && messageBusConfig == null)
            {
                Debug.LogError("MessageBusConfig is required when MessageBus is enabled.");
            }
            
            if (enableProfiling && profilingConfig == null)
            {
                Debug.LogError("ProfilingConfig is required when Profiling is enabled.");
            }
        }
        
        /// <summary>
        /// Creates a platform-optimized version of this configuration.
        /// </summary>
        public CoreSystemsConfig GetPlatformOptimizedConfig()
        {
            var optimized = Instantiate(this);
            
            // Apply platform-specific optimizations
            var platformSettings = GetCurrentPlatformSettings();
            ApplyPlatformOptimizations(optimized, platformSettings);
            
            // Optimize individual system configurations
            if (optimized.loggingConfig != null)
                optimized.loggingConfig = optimized.loggingConfig.GetPlatformOptimizedConfig();
                
            if (optimized.poolingConfig != null)
                optimized.poolingConfig = optimized.poolingConfig.GetPlatformOptimizedConfig();
                
            if (optimized.messageBusConfig != null)
                optimized.messageBusConfig = optimized.messageBusConfig.GetPlatformOptimizedConfig();
                
            if (optimized.profilingConfig != null)
                optimized.profilingConfig = optimized.profilingConfig.GetPlatformOptimizedConfig();
            
            return optimized;
        }
        
        private PlatformSettings GetCurrentPlatformSettings()
        {
#if UNITY_EDITOR
            return platformOverrides.EditorSettings;
#elif UNITY_ANDROID || UNITY_IOS
            return platformOverrides.MobileSettings;
#elif UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
            return platformOverrides.ConsoleSettings;
#elif UNITY_STANDALONE
            return platformOverrides.DesktopSettings;
#else
            return platformOverrides.DefaultSettings;
#endif
        }
        
        private void ApplyPlatformOptimizations(CoreSystemsConfig config, PlatformSettings settings)
        {
            if (settings.OverrideSystemUpdateInterval)
                config.systemUpdateInterval = settings.SystemUpdateInterval;
                
            if (settings.OverrideMaxOperationsPerFrame)
                config.maxOperationsPerFrame = settings.MaxOperationsPerFrame;
                
            if (settings.DisableDevelopmentFeatures)
                config.enableDevelopmentFeatures = false;
                
            config.enableBurstOptimizations = settings.EnableBurstOptimizations;
            config.enableJobSystemOptimizations = settings.EnableJobSystemOptimizations;
            config.enableMemoryProfiling = settings.EnableMemoryProfiling;
        }
        
        /// <summary>
        /// Validates that all required system dependencies are satisfied.
        /// </summary>
        public bool ValidateSystemDependencies(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();
            
            // Check critical dependencies
            if (enableMessageBus && !enableLogging)
            {
                errorList.Add("MessageBus requires Logging system to be enabled");
            }
            
            if (enableProfiling && !enableLogging)
            {
                errorList.Add("Profiling requires Logging system to be enabled");
            }
            
            // Check configuration completeness
            if (enableLogging && loggingConfig == null)
            {
                errorList.Add("LoggingConfig is missing but Logging system is enabled");
            }
            
            if (enablePooling && poolingConfig == null)
            {
                errorList.Add("PoolingConfig is missing but Pooling system is enabled");
            }
            
            if (enableMessageBus && messageBusConfig == null)
            {
                errorList.Add("MessageBusConfig is missing but MessageBus system is enabled");
            }
            
            if (enableProfiling && profilingConfig == null)
            {
                errorList.Add("ProfilingConfig is missing but Profiling system is enabled");
            }
            
            errors = errorList.ToArray();
            return errorList.Count == 0;
        }
    }
    
    /// <summary>
    /// Platform-specific configuration overrides.
    /// </summary>
    [System.Serializable]
    public sealed class PlatformConfigOverrides
    {
        [Header("Mobile Settings")]
        [SerializeField]
        private PlatformSettings mobileSettings = new PlatformSettings
        {
            SystemUpdateInterval = 0.033f, // 30fps
            MaxOperationsPerFrame = 500,
            EnableBurstOptimizations = true,
            EnableJobSystemOptimizations = false,
            EnableMemoryProfiling = false,
            DisableDevelopmentFeatures = true,
            OverrideSystemUpdateInterval = true,
            OverrideMaxOperationsPerFrame = true
        };
        
        [Header("Console Settings")]
        [SerializeField]
        private PlatformSettings consoleSettings = new PlatformSettings
        {
            SystemUpdateInterval = 0.016f, // 60fps
            MaxOperationsPerFrame = 2000,
            EnableBurstOptimizations = true,
            EnableJobSystemOptimizations = true,
            EnableMemoryProfiling = true,
            DisableDevelopmentFeatures = false,
            OverrideSystemUpdateInterval = true,
            OverrideMaxOperationsPerFrame = true
        };
        
        [Header("Desktop Settings")]
        [SerializeField]
        private PlatformSettings desktopSettings = new PlatformSettings
        {
            SystemUpdateInterval = 0.008f, // 125fps
            MaxOperationsPerFrame = 5000,
            EnableBurstOptimizations = true,
            EnableJobSystemOptimizations = true,
            EnableMemoryProfiling = true,
            DisableDevelopmentFeatures = false,
            OverrideSystemUpdateInterval = false,
            OverrideMaxOperationsPerFrame = false
        };
        
        [Header("Editor Settings")]
        [SerializeField]
        private PlatformSettings editorSettings = new PlatformSettings
        {
            SystemUpdateInterval = 0.016f, // 60fps
            MaxOperationsPerFrame = 1000,
            EnableBurstOptimizations = false, // Easier debugging
            EnableJobSystemOptimizations = true,
            EnableMemoryProfiling = true,
            DisableDevelopmentFeatures = false,
            OverrideSystemUpdateInterval = false,
            OverrideMaxOperationsPerFrame = false
        };
        
        [Header("Default Settings")]
        [SerializeField]
        private PlatformSettings defaultSettings = new PlatformSettings
        {
            SystemUpdateInterval = 0.016f,
            MaxOperationsPerFrame = 1000,
            EnableBurstOptimizations = true,
            EnableJobSystemOptimizations = true,
            EnableMemoryProfiling = false,
            DisableDevelopmentFeatures = true,
            OverrideSystemUpdateInterval = false,
            OverrideMaxOperationsPerFrame = false
        };
        
        public PlatformSettings MobileSettings => mobileSettings;
        public PlatformSettings ConsoleSettings => consoleSettings;
        public PlatformSettings DesktopSettings => desktopSettings;
        public PlatformSettings EditorSettings => editorSettings;
        public PlatformSettings DefaultSettings => defaultSettings;
    }
    
    /// <summary>
    /// Platform-specific settings that can override core configuration values.
    /// </summary>
    [System.Serializable]
    public sealed class PlatformSettings
    {
        [SerializeField]
        private float systemUpdateInterval = 0.016f;
        
        [SerializeField]
        private int maxOperationsPerFrame = 1000;
        
        [SerializeField]
        private bool enableBurstOptimizations = true;
        
        [SerializeField]
        private bool enableJobSystemOptimizations = true;
        
        [SerializeField]
        private bool enableMemoryProfiling = true;
        
        [SerializeField]
        private bool disableDevelopmentFeatures = false;
        
        [SerializeField]
        private bool overrideSystemUpdateInterval = false;
        
        [SerializeField]
        private bool overrideMaxOperationsPerFrame = false;
        
        public float SystemUpdateInterval => systemUpdateInterval;
        public int MaxOperationsPerFrame => maxOperationsPerFrame;
        public bool EnableBurstOptimizations => enableBurstOptimizations;
        public bool EnableJobSystemOptimizations => enableJobSystemOptimizations;
        public bool EnableMemoryProfiling => enableMemoryProfiling;
        public bool DisableDevelopmentFeatures => disableDevelopmentFeatures;
        public bool OverrideSystemUpdateInterval => overrideSystemUpdateInterval;
        public bool OverrideMaxOperationsPerFrame => overrideMaxOperationsPerFrame;
    }
}