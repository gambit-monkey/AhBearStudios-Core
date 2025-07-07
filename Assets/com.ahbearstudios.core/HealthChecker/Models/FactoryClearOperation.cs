namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the type of factory clear operation performed.
/// </summary>
public enum FactoryClearOperation : byte
{
    /// <summary>
    /// Complete clear of all factory components.
    /// </summary>
    Complete = 0,

    /// <summary>
    /// Partial clear of specific factory components.
    /// </summary>
    Partial = 1,

    /// <summary>
    /// Selective clear based on criteria.
    /// </summary>
    Selective = 2,

    /// <summary>
    /// Emergency clear due to critical issues.
    /// </summary>
    Emergency = 3,

    /// <summary>
    /// Maintenance clear for routine cleanup.
    /// </summary>
    Maintenance = 4,

    /// <summary>
    /// Reset clear to restore factory to initial state.
    /// </summary>
    Reset = 5
}