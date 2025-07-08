namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Installation state enumeration for tracking installer progress.
/// </summary>
public enum InstallationState : byte
{
    /// <summary>Installer has not been processed yet.</summary>
    NotStarted = 0,
        
    /// <summary>Installer validation is in progress.</summary>
    Validating = 1,
        
    /// <summary>Pre-installation phase is in progress.</summary>
    PreInstalling = 2,
        
    /// <summary>Core installation phase is in progress.</summary>
    Installing = 3,
        
    /// <summary>Post-installation phase is in progress.</summary>
    PostInstalling = 4,
        
    /// <summary>Installation completed successfully.</summary>
    Completed = 5,
        
    /// <summary>Installation failed with errors.</summary>
    Failed = 6,
        
    /// <summary>Installation was skipped due to configuration.</summary>
    Skipped = 7
}