using UnityEngine;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Bootstrap.Configuration;

namespace AhBearStudios.Core.Bootstrap.Builders
{
    /// <summary>
    /// Builder for creating and configuring installer configurations using a fluent interface.
    /// Provides methods to set all properties of an installer configuration.
    /// </summary>
    public sealed class InstallerConfigBuilder : IInstallerConfigBuilder<IInstallerConfig, InstallerConfigBuilder>
    {
        private readonly IInstallerConfig _config;
        private IInstallerConfig _optimizedConfig;

        /// <summary>
        /// Initializes a new instance of the InstallerConfigBuilder with a default configuration.
        /// </summary>
        public InstallerConfigBuilder()
        {
            _config = ScriptableObject.CreateInstance<InstallerConfig>();
            _optimizedConfig = null;
        }

        /// <summary>
        /// Initializes a new instance of the InstallerConfigBuilder with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to start with</param>
        public InstallerConfigBuilder(IInstallerConfig config)
        {
            _config = config.Clone();
            _optimizedConfig = null;
        }
        
        /// <summary>
        /// Creates a new builder initialized with settings from an existing configuration.
        /// </summary>
        /// <param name="config">The existing configuration to use as a starting point</param>
        /// <returns>A new builder instance with copied settings</returns>
        public static InstallerConfigBuilder FromExisting(IInstallerConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config), "Cannot create builder from null configuration");
            }
            
            return new InstallerConfigBuilder(config);
        }

        #region Core Systems

        /// <summary>
        /// Configures core systems settings.
        /// </summary>
        /// <param name="enableLogging">Whether to enable the logging system</param>
        /// <param name="enableProfiling">Whether to enable the profiling system</param>
        /// <param name="enablePooling">Whether to enable the object pooling system</param>
        /// <param name="enableMessageBus">Whether to enable the message bus system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithCoreSystems(bool enableLogging = true, bool enableProfiling = true, 
            bool enablePooling = true, bool enableMessageBus = true)
        {
            IInstallerConfig config = GetActiveConfig();
            config.EnableLogging = enableLogging;
            config.EnableProfiling = enableProfiling;
            config.EnablePooling = enablePooling;
            config.EnableMessageBus = enableMessageBus;
            return this;
        }

        /// <summary>
        /// Sets whether the logging system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the logging system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithLogging(bool enable)
        {
            GetActiveConfig().EnableLogging = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the profiling system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the profiling system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithProfiling(bool enable)
        {
            GetActiveConfig().EnableProfiling = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the object pooling system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the object pooling system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithPooling(bool enable)
        {
            GetActiveConfig().EnablePooling = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the message bus system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the message bus system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithMessageBus(bool enable)
        {
            GetActiveConfig().EnableMessageBus = enable;
            return this;
        }

        #endregion

        #region Optional Systems

        /// <summary>
        /// Configures optional systems settings.
        /// </summary>
        /// <param name="enableNetworking">Whether to enable the networking system</param>
        /// <param name="enableAudio">Whether to enable the audio system</param>
        /// <param name="enableInput">Whether to enable the input system</param>
        /// <param name="enableSaveSystem">Whether to enable the save system</param>
        /// <param name="enableSceneManagement">Whether to enable the scene management system</param>
        /// <param name="enableUIFramework">Whether to enable the UI framework</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithOptionalSystems(bool enableNetworking = false, bool enableAudio = true, 
            bool enableInput = true, bool enableSaveSystem = true, bool enableSceneManagement = true, 
            bool enableUIFramework = true)
        {
            IInstallerConfig config = GetActiveConfig();
            config.EnableNetworking = enableNetworking;
            config.EnableAudio = enableAudio;
            config.EnableInput = enableInput;
            config.EnableSaveSystem = enableSaveSystem;
            config.EnableSceneManagement = enableSceneManagement;
            config.EnableUIFramework = enableUIFramework;
            return this;
        }

        /// <summary>
        /// Sets whether the networking system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the networking system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithNetworking(bool enable)
        {
            GetActiveConfig().EnableNetworking = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the audio system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the audio system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithAudio(bool enable)
        {
            GetActiveConfig().EnableAudio = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the input system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the input system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithInput(bool enable)
        {
            GetActiveConfig().EnableInput = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the save system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the save system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithSaveSystem(bool enable)
        {
            GetActiveConfig().EnableSaveSystem = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the scene management system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the scene management system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithSceneManagement(bool enable)
        {
            GetActiveConfig().EnableSceneManagement = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the UI framework should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the UI framework</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithUIFramework(bool enable)
        {
            GetActiveConfig().EnableUIFramework = enable;
            return this;
        }

        #endregion

        #region Development Systems

        /// <summary>
        /// Configures development systems settings.
        /// </summary>
        /// <param name="enableDebugConsole">Whether to enable the debug console</param>
        /// <param name="enableCheatSystem">Whether to enable the cheat system</param>
        /// <param name="enablePerformanceHUD">Whether to enable the performance HUD</param>
        /// <param name="enableMemoryProfiler">Whether to enable the memory profiler</param>
        /// <param name="enableSystemDiagnostics">Whether to enable system diagnostics tools</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithDevelopmentSystems(bool enableDebugConsole = true, bool enableCheatSystem = false, 
            bool enablePerformanceHUD = true, bool enableMemoryProfiler = false, bool enableSystemDiagnostics = true)
        {
            IInstallerConfig config = GetActiveConfig();
            config.EnableDebugConsole = enableDebugConsole;
            config.EnableCheatSystem = enableCheatSystem;
            config.EnablePerformanceHUD = enablePerformanceHUD;
            config.EnableMemoryProfiler = enableMemoryProfiler;
            config.EnableSystemDiagnostics = enableSystemDiagnostics;
            return this;
        }

        /// <summary>
        /// Sets whether the debug console should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the debug console</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithDebugConsole(bool enable)
        {
            GetActiveConfig().EnableDebugConsole = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the cheat system should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the cheat system</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithCheatSystem(bool enable)
        {
            GetActiveConfig().EnableCheatSystem = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the performance HUD should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the performance HUD</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithPerformanceHUD(bool enable)
        {
            GetActiveConfig().EnablePerformanceHUD = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the memory profiler should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable the memory profiler</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithMemoryProfiler(bool enable)
        {
            GetActiveConfig().EnableMemoryProfiler = enable;
            return this;
        }

        /// <summary>
        /// Sets whether the system diagnostics tools should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable system diagnostics tools</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithSystemDiagnostics(bool enable)
        {
            GetActiveConfig().EnableSystemDiagnostics = enable;
            return this;
        }

        #endregion

        #region Platform-Specific Systems

        /// <summary>
        /// Configures platform-specific systems settings.
        /// </summary>
        /// <param name="enableMobile">Whether to enable mobile-specific systems</param>
        /// <param name="enableConsole">Whether to enable console-specific systems</param>
        /// <param name="enablePC">Whether to enable PC-specific systems</param>
        /// <param name="enableCloud">Whether to enable cloud systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithPlatformSystems(bool enableMobile = false, bool enableConsole = false, 
            bool enablePC = false, bool enableCloud = false)
        {
            IInstallerConfig config = GetActiveConfig();
            config.EnableMobileSpecificSystems = enableMobile;
            config.EnableConsoleSpecificSystems = enableConsole;
            config.EnablePCSpecificSystems = enablePC;
            config.EnableCloudSystems = enableCloud;
            return this;
        }

        /// <summary>
        /// Sets whether mobile-specific systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable mobile-specific systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithMobileSystems(bool enable)
        {
            GetActiveConfig().EnableMobileSpecificSystems = enable;
            return this;
        }

        /// <summary>
        /// Sets whether console-specific systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable console-specific systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithConsoleSystems(bool enable)
        {
            GetActiveConfig().EnableConsoleSpecificSystems = enable;
            return this;
        }

        /// <summary>
        /// Sets whether PC-specific systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable PC-specific systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithPCSystems(bool enable)
        {
            GetActiveConfig().EnablePCSpecificSystems = enable;
            return this;
        }

        /// <summary>
        /// Sets whether cloud systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable cloud systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithCloudSystems(bool enable)
        {
            GetActiveConfig().EnableCloudSystems = enable;
            return this;
        }

        #endregion

        #region Third-Party Integrations

        /// <summary>
        /// Configures third-party integration settings.
        /// </summary>
        /// <param name="enableAnalytics">Whether to enable analytics systems</param>
        /// <param name="enableCrashReporting">Whether to enable crash reporting systems</param>
        /// <param name="enableRemoteConfig">Whether to enable remote configuration systems</param>
        /// <param name="enablePushNotifications">Whether to enable push notification systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithThirdPartyIntegrations(bool enableAnalytics = false, bool enableCrashReporting = false, 
            bool enableRemoteConfig = false, bool enablePushNotifications = false)
        {
            IInstallerConfig config = GetActiveConfig();
            config.EnableAnalytics = enableAnalytics;
            config.EnableCrashReporting = enableCrashReporting;
            config.EnableRemoteConfig = enableRemoteConfig;
            config.EnablePushNotifications = enablePushNotifications;
            return this;
        }

        /// <summary>
        /// Sets whether analytics systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable analytics systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithAnalytics(bool enable)
        {
            GetActiveConfig().EnableAnalytics = enable;
            return this;
        }

        /// <summary>
        /// Sets whether crash reporting systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable crash reporting systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithCrashReporting(bool enable)
        {
            GetActiveConfig().EnableCrashReporting = enable;
            return this;
        }

        /// <summary>
        /// Sets whether remote configuration systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable remote configuration systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithRemoteConfig(bool enable)
        {
            GetActiveConfig().EnableRemoteConfig = enable;
            return this;
        }

        /// <summary>
        /// Sets whether push notification systems should be installed.
        /// </summary>
        /// <param name="enable">Whether to enable push notification systems</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithPushNotifications(bool enable)
        {
            GetActiveConfig().EnablePushNotifications = enable;
            return this;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Configures validation settings.
        /// </summary>
        /// <param name="validateDependencies">Whether to validate dependencies between systems</param>
        /// <param name="enableFallbackSystems">Whether to enable fallback systems when primary systems fail</param>
        /// <param name="strictValidation">Whether to perform strict validation (fails on warnings)</param>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithValidation(bool validateDependencies = true, bool enableFallbackSystems = true, 
            bool strictValidation = false)
        {
            IInstallerConfig config = GetActiveConfig();
            config.ValidateDependencies = validateDependencies;
            config.EnableFallbackSystems = enableFallbackSystems;
            config.StrictValidation = strictValidation;
            return this;
        }

        #endregion

        #region Presets

        /// <summary>
        /// Applies mobile-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithMobileOptimizations()
        {
            return WithCoreSystems(true, true, true, true)
                .WithOptionalSystems(false, true, true, true, true, true)
                .WithDevelopmentSystems(false, false, false, false, false)
                .WithPlatformSystems(true, false, false, false)
                .WithThirdPartyIntegrations(false, true, false, true)
                .WithValidation(true, true, false);
        }

        /// <summary>
        /// Applies console-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithConsoleOptimizations()
        {
            return WithCoreSystems(true, true, true, true)
                .WithOptionalSystems(true, true, true, true, true, true)
                .WithDevelopmentSystems(false, false, true, false, true)
                .WithPlatformSystems(false, true, false, false)
                .WithThirdPartyIntegrations(true, true, true, false)
                .WithValidation(true, true, false);
        }

        /// <summary>
        /// Applies PC-optimized settings.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithPCOptimizations()
        {
            return WithCoreSystems(true, true, true, true)
                .WithOptionalSystems(true, true, true, true, true, true)
                .WithDevelopmentSystems(true, false, true, false, true)
                .WithPlatformSystems(false, false, true, false)
                .WithThirdPartyIntegrations(true, true, true, false)
                .WithValidation(true, true, false);
        }

        /// <summary>
        /// Applies development-optimized settings with additional debugging tools.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithDevelopmentOptimizations()
        {
            return WithCoreSystems(true, true, true, true)
                .WithOptionalSystems(true, true, true, true, true, true)
                .WithDevelopmentSystems(true, true, true, true, true)
                .WithPlatformSystems(false, false, false, false)
                .WithThirdPartyIntegrations(false, true, false, false)
                .WithValidation(true, true, true);
        }

        /// <summary>
        /// Applies minimal settings for performance-critical environments.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder WithMinimalSystems()
        {
            return WithCoreSystems(true, false, true, true)
                .WithOptionalSystems(false, true, true, true, true, false)
                .WithDevelopmentSystems(false, false, false, false, false)
                .WithPlatformSystems(false, false, false, false)
                .WithThirdPartyIntegrations(false, false, false, false)
                .WithValidation(false, false, false);
        }

        #endregion

        /// <summary>
        /// Creates an optimized configuration for the current platform.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public InstallerConfigBuilder OptimizeForCurrentPlatform()
        {
            if (GetActiveConfig() is InstallerConfig config)
            {
                _optimizedConfig = config.GetPlatformOptimizedConfig();
            }
            
            return this;
        }

        /// <summary>
        /// Builds the final installer configuration.
        /// </summary>
        /// <returns>The configured installer configuration</returns>
        public IInstallerConfig Build()
        {
            return GetActiveConfig().Clone();
        }
        
        /// <summary>
        /// Gets the active configuration (optimized if available, original otherwise).
        /// </summary>
        /// <returns>The active configuration</returns>
        private IInstallerConfig GetActiveConfig()
        {
            return _optimizedConfig ?? _config;
        }
    }
}