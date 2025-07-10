namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Installation metrics data structure.
/// </summary>
public readonly record struct InstallationMetrics
{
    public DateTime ValidationStartTime { get; init; }
    public DateTime ValidationEndTime { get; init; }
    public TimeSpan ValidationDuration { get; init; }
    public DateTime PreInstallStartTime { get; init; }
    public DateTime PreInstallEndTime { get; init; }
    public TimeSpan PreInstallDuration { get; init; }
    public DateTime InstallStartTime { get; init; }
    public DateTime InstallEndTime { get; init; }
    public TimeSpan InstallDuration { get; init; }
    public DateTime PostInstallStartTime { get; init; }
    public DateTime PostInstallEndTime { get; init; }
    public TimeSpan PostInstallDuration { get; init; }
    public TimeSpan TotalInstallDuration { get; init; }
    public bool IsValidationSuccessful { get; init; }
    public bool IsInstallationSuccessful { get; init; }
    public int ValidationErrorCount { get; init; }
    public int ValidationWarningCount { get; init; }
    public int RecoveryAttempts { get; init; }
}