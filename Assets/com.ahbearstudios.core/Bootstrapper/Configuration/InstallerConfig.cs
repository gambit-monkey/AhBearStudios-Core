using UnityEngine;
using AhBearStudios.Core.Bootstrap.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Configuration
{
    /// <summary>
    /// Configuration that controls which installers are used during bootstrapping.
    /// Provides fine-grained control over system installation and feature enablement.
    /// </summary>
    [CreateAssetMenu(menuName = "AhBear/Core/Installer Config", fileName = "InstallerConfig", order = 1)]
    public sealed class InstallerConfig : ScriptableObject, IInstallerConfig
    {
        [Header("Core Systems")]
        [SerializeField]
        private bool enableLogging = true;
        
        [SerializeField]
        private bool enableProfiling = true;
        
        [SerializeField]
        private bool enablePooling = true;
        
        [SerializeField]
        private bool enableMessageBus = true;
        
        [Header("Optional Systems")]
        [SerializeField]
        private bool enableNetworking = false;
        
        [SerializeField]
        private bool enableAudio = true;
        
        [SerializeField]
        private bool enableInput = true;
        
        [SerializeField]
        private bool enableSaveSystem = true;
        
        [SerializeField]
        private bool enableSceneManagement = true;
        
        [SerializeField]
        private bool enableUIFramework = true;
        
        [Header("Development Systems")]
        [SerializeField]
        private bool enableDebugConsole = true;
        
        [SerializeField]
        private bool enableCheatSystem = false;
        
        [SerializeField]
        private bool enablePerformanceHUD = true;
        
        [SerializeField]
        private bool enableMemoryProfiler = false;
        
        [SerializeField]
        private bool enableSystemDiagnostics = true;
        
        [Header("Platform-Specific Systems")]
        [SerializeField]
        private bool enableMobileSpecificSystems = false;
        
        [SerializeField]
        private bool enableConsoleSpecificSystems = false;
        
        [SerializeField]
        private bool enablePCSpecificSystems = false;
        
        [SerializeField]
        private bool enableCloudSystems = false;
        
        [Header("Third-Party Integrations")]
        [SerializeField]
        private bool enableAnalytics = false;
        
        [SerializeField]
        private bool enableCrashReporting = false;
        
        [SerializeField]
        private bool enableRemoteConfig = false;
        
        [SerializeField]
        private bool enablePushNotifications = false;
        
        [Header("Validation")]
        [SerializeField]
        private bool validateDependencies = true;
        
        [SerializeField]
        private bool enableFallbackSystems = true;
        
        [SerializeField]
        private bool strictValidation = false;
        
        // IInstallerConfig implementation - Core systems
        public bool EnableLogging 
        { 
            get => enableLogging;
            set => enableLogging = value;
        }
        
        public bool EnableProfiling 
        { 
            get => enableProfiling;
            set => enableProfiling = value;
        }
        
        public bool EnablePooling 
        { 
            get => enablePooling;
            set => enablePooling = value;
        }
        
        public bool EnableMessageBus 
        { 
            get => enableMessageBus;
            set => enableMessageBus = value;
        }
        
        // IInstallerConfig implementation - Optional systems
        public bool EnableNetworking 
        { 
            get => enableNetworking;
            set => enableNetworking = value;
        }
        
        public bool EnableAudio 
        { 
            get => enableAudio;
            set => enableAudio = value;
        }
        
        public bool EnableInput 
        { 
            get => enableInput;
            set => enableInput = value;
        }
        
        public bool EnableSaveSystem 
        { 
            get => enableSaveSystem;
            set => enableSaveSystem = value;
        }
        
        public bool EnableSceneManagement 
        { 
            get => enableSceneManagement;
            set => enableSceneManagement = value;
        }
        
        public bool EnableUIFramework 
        { 
            get => enableUIFramework;
            set => enableUIFramework = value;
        }
        
        // IInstallerConfig implementation - Development systems
        public bool EnableDebugConsole 
        { 
            get => enableDebugConsole;
            set => enableDebugConsole = value;
        }
        
        public bool EnableCheatSystem 
        { 
            get => enableCheatSystem;
            set => enableCheatSystem = value;
        }
        
        public bool EnablePerformanceHUD 
        { 
            get => enablePerformanceHUD;
            set => enablePerformanceHUD = value;
        }
        
        public bool EnableMemoryProfiler 
        { 
            get => enableMemoryProfiler;
            set => enableMemoryProfiler = value;
        }
        
        public bool EnableSystemDiagnostics 
        { 
            get => enableSystemDiagnostics;
            set => enableSystemDiagnostics = value;
        }
        
        // IInstallerConfig implementation - Platform-specific systems
        public bool EnableMobileSpecificSystems 
        { 
            get => enableMobileSpecificSystems;
            set => enableMobileSpecificSystems = value;
        }
        
        public bool EnableConsoleSpecificSystems 
        { 
            get => enableConsoleSpecificSystems;
            set => enableConsoleSpecificSystems = value;
        }
        
        public bool EnablePCSpecificSystems 
        { 
            get => enablePCSpecificSystems;
            set => enablePCSpecificSystems = value;
        }
        
        public bool EnableCloudSystems 
        { 
            get => enableCloudSystems;
            set => enableCloudSystems = value;
        }
        
        // IInstallerConfig implementation - Third-party integrations
        public bool EnableAnalytics 
        { 
            get => enableAnalytics;
            set => enableAnalytics = value;
        }
        
        public bool EnableCrashReporting 
        { 
            get => enableCrashReporting;
            set => enableCrashReporting = value;
        }
        
        public bool EnableRemoteConfig 
        { 
            get => enableRemoteConfig;
            set => enableRemoteConfig = value;
        }
        
        public bool EnablePushNotifications 
        { 
            get => enablePushNotifications;
            set => enablePushNotifications = value;
        }
        
        // IInstallerConfig implementation - Validation
        public bool ValidateDependencies 
        { 
            get => validateDependencies;
            set => validateDependencies = value;
        }
        
        public bool EnableFallbackSystems 
        { 
            get => enableFallbackSystems;
            set => enableFallbackSystems = value;
        }
        
        public bool StrictValidation 
        { 
            get => strictValidation;
            set => strictValidation = value;
        }
        
        private void OnValidate()
        {
            ValidateConfiguration();
        }
        
        private void ValidateConfiguration()
        {
            // Auto-enable dependencies for core systems
            if (enableMessageBus && !enableLogging)
            {
                Debug.LogWarning("MessageBusService requires Logging - auto-enabling");
                enableLogging = true;
            }
            
            if (enableProfiling && !enableLogging)
            {
                Debug.LogWarning("Profiling requires Logging - auto-enabling");
                enableLogging = true;
            }
            
            // Validate optional system dependencies
            if (enableDebugConsole && !enableUIFramework)
            {
                Debug.LogWarning("Debug Console requires UI Framework - consider enabling");
            }
            
            if (enablePerformanceHUD && !enableProfiling)
            {
                Debug.LogWarning("Performance HUD requires Profiling - consider enabling");
            }
            
            if (enableMemoryProfiler && !enableProfiling)
            {
                Debug.LogWarning("Memory Profiler requires Profiling - auto-enabling");
                enableProfiling = true;
            }
            
            if (enableSystemDiagnostics && !enableProfiling)
            {
                Debug.LogWarning("System Diagnostics requires Profiling - consider enabling");
            }
            
            // Platform-specific validation
#if UNITY_ANDROID || UNITY_IOS
            if (!enableMobileSpecificSystems)
            {
                Debug.LogInfo("Consider enabling Mobile Specific Systems for mobile builds");
            }
#elif UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
            if (!enableConsoleSpecificSystems)
            {
                Debug.LogInfo("Consider enabling Console Specific Systems for console builds");
            }
#elif UNITY_STANDALONE
            if (!enablePCSpecificSystems)
            {
                Debug.LogInfo("Consider enabling PC Specific Systems for PC builds");
            }
#endif
            
            // Development vs production validation
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // Development builds should have debugging enabled
            if (!enableDebugConsole && !enablePerformanceHUD)
            {
                Debug.LogInfo("Consider enabling debug tools for development builds");
            }
#else
            // Production builds should disable development features
            if (enableCheatSystem)
            {
                Debug.LogWarning("Cheat System should be disabled in production builds");
                enableCheatSystem = false;
            }
            
            if (enableMemoryProfiler)
            {
                Debug.LogWarning("Memory Profiler should be disabled in production builds");
                enableMemoryProfiler = false;
            }
#endif
        }
        
        /// <summary>
        /// Creates a platform-optimized version of this configuration.
        /// </summary>
        public InstallerConfig GetPlatformOptimizedConfig()
        {
            var optimized = Instantiate(this);
            
#if UNITY_EDITOR
            // Editor optimizations
            optimized.enableDebugConsole = true;
            optimized.enablePerformanceHUD = true;
            optimized.enableSystemDiagnostics = true;
            optimized.enableMemoryProfiler = true;
#elif UNITY_ANDROID || UNITY_IOS
            // Mobile optimizations
            optimized.enableMobileSpecificSystems = true;
            optimized.enableDebugConsole = false;
            optimized.enablePerformanceHUD = false;
            optimized.enableMemoryProfiler = false;
            optimized.enableCheatSystem = false;
            optimized.enableSystemDiagnostics = false;
#elif UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
            // Console optimizations
            optimized.enableConsoleSpecificSystems = true;
            optimized.enablePerformanceHUD = true;
            optimized.enableSystemDiagnostics = true;
#elif UNITY_STANDALONE
            // PC optimizations
            optimized.enablePCSpecificSystems = true;
            optimized.enableDebugConsole = true;
            optimized.enablePerformanceHUD = true;
            optimized.enableSystemDiagnostics = true;
#endif
            
            return optimized;
        }
        
        /// <summary>
        /// Validates that all enabled systems have their dependencies satisfied.
        /// </summary>
        public bool ValidateSystemDependencies(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();
            
            // Core system dependencies
            if (enableMessageBus && !enableLogging)
            {
                errorList.Add("MessageBusService requires Logging system");
            }
            
            if (enableProfiling && !enableLogging)
            {
                errorList.Add("Profiling requires Logging system");
            }
            
            // Optional system dependencies
            if (enableMemoryProfiler && !enableProfiling)
            {
                errorList.Add("Memory Profiler requires Profiling system");
            }
            
            if (enablePerformanceHUD && !enableProfiling)
            {
                errorList.Add("Performance HUD requires Profiling system");
            }
            
            if (enableSystemDiagnostics && !enableProfiling)
            {
                errorList.Add("System Diagnostics requires Profiling system");
            }
            
            if (enableDebugConsole && !enableUIFramework)
            {
                errorList.Add("Debug Console requires UI Framework");
            }
            
            errors = errorList.ToArray();
            return errorList.Count == 0;
        }
        
        /// <summary>
        /// Gets the total number of systems that will be installed.
        /// </summary>
        public int GetEnabledSystemCount()
        {
            int count = 0;
            
            if (enableLogging) count++;
            if (enableProfiling) count++;
            if (enablePooling) count++;
            if (enableMessageBus) count++;
            if (enableNetworking) count++;
            if (enableAudio) count++;
            if (enableInput) count++;
            if (enableSaveSystem) count++;
            if (enableSceneManagement) count++;
            if (enableUIFramework) count++;
            if (enableDebugConsole) count++;
            if (enableCheatSystem) count++;
            if (enablePerformanceHUD) count++;
            if (enableMemoryProfiler) count++;
            if (enableSystemDiagnostics) count++;
            if (enableMobileSpecificSystems) count++;
            if (enableConsoleSpecificSystems) count++;
            if (enablePCSpecificSystems) count++;
            if (enableCloudSystems) count++;
            if (enableAnalytics) count++;
            if (enableCrashReporting) count++;
            if (enableRemoteConfig) count++;
            if (enablePushNotifications) count++;
            
            return count;
        }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        public IInstallerConfig Clone()
        {
            return Instantiate(this);
        }
    }
}