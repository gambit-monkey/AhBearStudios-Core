namespace AhBearStudios.Core.Bootstrap.Interfaces
{
    /// <summary>
    /// Interface for installer configuration that controls which systems are used during bootstrapping.
    /// Provides fine-grained control over system installation and feature enablement.
    /// </summary>
    public interface IInstallerConfig
    {
        #region Core Systems
        
        /// <summary>
        /// Gets or sets whether the logging system should be installed.
        /// </summary>
        bool EnableLogging { get; set; }
        
        /// <summary>
        /// Gets or sets whether the profiling system should be installed.
        /// </summary>
        bool EnableProfiling { get; set; }
        
        /// <summary>
        /// Gets or sets whether the object pooling system should be installed.
        /// </summary>
        bool EnablePooling { get; set; }
        
        /// <summary>
        /// Gets or sets whether the message bus system should be installed.
        /// </summary>
        bool EnableMessageBus { get; set; }
        
        #endregion
        
        #region Optional Systems
        
        /// <summary>
        /// Gets or sets whether the networking system should be installed.
        /// </summary>
        bool EnableNetworking { get; set; }
        
        /// <summary>
        /// Gets or sets whether the audio system should be installed.
        /// </summary>
        bool EnableAudio { get; set; }
        
        /// <summary>
        /// Gets or sets whether the input system should be installed.
        /// </summary>
        bool EnableInput { get; set; }
        
        /// <summary>
        /// Gets or sets whether the save system should be installed.
        /// </summary>
        bool EnableSaveSystem { get; set; }
        
        /// <summary>
        /// Gets or sets whether the scene management system should be installed.
        /// </summary>
        bool EnableSceneManagement { get; set; }
        
        /// <summary>
        /// Gets or sets whether the UI framework should be installed.
        /// </summary>
        bool EnableUIFramework { get; set; }
        
        #endregion
        
        #region Development Systems
        
        /// <summary>
        /// Gets or sets whether the debug console should be installed.
        /// </summary>
        bool EnableDebugConsole { get; set; }
        
        /// <summary>
        /// Gets or sets whether the cheat system should be installed.
        /// </summary>
        bool EnableCheatSystem { get; set; }
        
        /// <summary>
        /// Gets or sets whether the performance HUD should be installed.
        /// </summary>
        bool EnablePerformanceHUD { get; set; }
        
        /// <summary>
        /// Gets or sets whether the memory profiler should be installed.
        /// </summary>
        bool EnableMemoryProfiler { get; set; }
        
        /// <summary>
        /// Gets or sets whether the system diagnostics tools should be installed.
        /// </summary>
        bool EnableSystemDiagnostics { get; set; }
        
        #endregion
        
        #region Platform-Specific Systems
        
        /// <summary>
        /// Gets or sets whether mobile-specific systems should be installed.
        /// </summary>
        bool EnableMobileSpecificSystems { get; set; }
        
        /// <summary>
        /// Gets or sets whether console-specific systems should be installed.
        /// </summary>
        bool EnableConsoleSpecificSystems { get; set; }
        
        /// <summary>
        /// Gets or sets whether PC-specific systems should be installed.
        /// </summary>
        bool EnablePCSpecificSystems { get; set; }
        
        /// <summary>
        /// Gets or sets whether cloud systems should be installed.
        /// </summary>
        bool EnableCloudSystems { get; set; }
        
        #endregion
        
        #region Third-Party Integrations
        
        /// <summary>
        /// Gets or sets whether analytics systems should be installed.
        /// </summary>
        bool EnableAnalytics { get; set; }
        
        /// <summary>
        /// Gets or sets whether crash reporting systems should be installed.
        /// </summary>
        bool EnableCrashReporting { get; set; }
        
        /// <summary>
        /// Gets or sets whether remote configuration systems should be installed.
        /// </summary>
        bool EnableRemoteConfig { get; set; }
        
        /// <summary>
        /// Gets or sets whether push notification systems should be installed.
        /// </summary>
        bool EnablePushNotifications { get; set; }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Gets or sets whether dependencies between systems should be validated during installation.
        /// </summary>
        bool ValidateDependencies { get; set; }
        
        /// <summary>
        /// Gets or sets whether fallback systems should be enabled when primary systems fail.
        /// </summary>
        bool EnableFallbackSystems { get; set; }
        
        /// <summary>
        /// Gets or sets whether strict validation should be performed (fails on warnings).
        /// </summary>
        bool StrictValidation { get; set; }
        
        #endregion
        
        /// <summary>
        /// Validates that all enabled systems have their dependencies satisfied.
        /// </summary>
        /// <param name="errors">Array of validation error messages if any are found.</param>
        /// <returns>True if all dependencies are satisfied, false otherwise.</returns>
        bool ValidateSystemDependencies(out string[] errors);
        
        /// <summary>
        /// Gets the total number of systems that will be installed.
        /// </summary>
        /// <returns>The count of enabled systems.</returns>
        int GetEnabledSystemCount();
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        IInstallerConfig Clone();
    }
}