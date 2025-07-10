namespace AhBearStudios.Core.Bootstrap.Interfaces;

/// <summary>
/// Bootstrap configuration interface providing system enablement and settings.
/// Supports platform optimization and conditional system installation.
/// </summary>
public interface IBootstrapConfig
{
    #region Core System Configuration

    /// <summary>Gets whether the logging system should be installed and configured.</summary>
    bool EnableLogging { get; }

    /// <summary>Gets whether the profiling system should be installed and configured.</summary>
    bool EnableProfiling { get; }

    /// <summary>Gets whether the object pooling system should be installed and configured.</summary>
    bool EnablePooling { get; }

    /// <summary>Gets whether the message bus system should be installed and configured.</summary>
    bool EnableMessageBus { get; }

    /// <summary>Gets whether the health check system should be installed and configured.</summary>
    bool EnableHealthChecks { get; }

    /// <summary>Gets whether the alert system should be installed and configured.</summary>
    bool EnableAlerts { get; }

    #endregion

    #region Optional System Configuration

    /// <summary>Gets whether the audio system should be installed and configured.</summary>
    bool EnableAudio { get; }

    /// <summary>Gets whether the input system should be installed and configured.</summary>
    bool EnableInput { get; }

    /// <summary>Gets whether the save system should be installed and configured.</summary>
    bool EnableSaveSystem { get; }

    /// <summary>Gets whether the scene management system should be installed and configured.</summary>
    bool EnableSceneManagement { get; }

    /// <summary>Gets whether the UI framework should be installed and configured.</summary>
    bool EnableUIFramework { get; }

    #endregion

    #region Development System Configuration

    /// <summary>Gets whether development systems should be installed.</summary>
    bool EnableDevelopmentSystems { get; }

    /// <summary>Gets whether the debug console should be installed and configured.</summary>
    bool EnableDebugConsole { get; }

    /// <summary>Gets whether the performance HUD should be installed and configured.</summary>
    bool EnablePerformanceHUD { get; }

    /// <summary>Gets whether system diagnostics should be installed and configured.</summary>
    bool EnableSystemDiagnostics { get; }

    #endregion

    #region Bootstrap Behavior Configuration

    /// <summary>Gets whether bootstrap operations should be logged for debugging and monitoring.</summary>
    bool EnableBootstrapLogging { get; }

    /// <summary>Gets whether to validate all system dependencies before installation begins.</summary>
    bool ValidateSystemDependencies { get; }

    /// <summary>Gets whether to continue installation when non-critical systems fail.</summary>
    bool ContinueOnNonCriticalFailures { get; }

    /// <summary>Gets the maximum time allowed for bootstrap operations before timeout.</summary>
    float BootstrapTimeoutSeconds { get; }

    /// <summary>Gets whether to enable graceful fallback scenarios for failed systems.</summary>
    bool EnableGracefulFallbacks { get; }

    #endregion

    #region Platform and Performance Configuration

    /// <summary>Gets platform-specific optimizations for the current deployment target.</summary>
    /// <returns>Optimized configuration for the current platform.</returns>
    IBootstrapConfig GetPlatformOptimizedConfig();

    /// <summary>Validates system dependencies and configuration consistency.</summary>
    /// <param name="errors">Output array of validation error messages.</param>
    /// <returns>True if validation passed, false if errors were found.</returns>
    bool ValidateConfiguration(out string[] errors);

    /// <summary>Gets the estimated total memory overhead for all enabled systems.</summary>
    /// <returns>Estimated memory usage in bytes for capacity planning.</returns>
    long GetEstimatedMemoryUsage();

    #endregion
}
