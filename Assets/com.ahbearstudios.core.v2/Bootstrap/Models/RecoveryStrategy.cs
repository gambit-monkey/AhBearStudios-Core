namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Recovery strategy enumeration for different failure scenarios.
/// </summary>
public enum RecoveryStrategy : byte
{
    /// <summary>Skip this installer and continue with others.</summary>
    Skip = 0,
        
    /// <summary>Install minimal subset of services.</summary>
    MinimalInstall = 1,
        
    /// <summary>Use fallback implementations.</summary>
    UseFallbacks = 2,
        
    /// <summary>Retry installation with different configuration.</summary>
    RetryWithConfig = 3,
        
    /// <summary>Install mock implementations for testing.</summary>
    UseMocks = 4
}