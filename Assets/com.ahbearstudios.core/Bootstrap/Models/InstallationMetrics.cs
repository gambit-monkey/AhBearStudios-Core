namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Installation metrics data structure.
/// </summary>
public struct InstallationMetrics
{
    public DateTime ValidationStartTime { get; set; }
    public DateTime ValidationEndTime { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    public DateTime PreInstallStartTime { get; set; }
    public DateTime PreInstallEndTime { get; set; }
    public TimeSpan PreInstallDuration { get; set; }
    public DateTime InstallStartTime { get; set; }
    public DateTime InstallEndTime { get; set; }
    public TimeSpan InstallDuration { get; set; }
    public DateTime PostInstallStartTime { get; set; }
    public DateTime PostInstallEndTime { get; set; }
    public TimeSpan PostInstallDuration { get; set; }
    public TimeSpan TotalInstallDuration { get; set; }
    public bool IsValidationSuccessful { get; set; }
    public bool IsInstallationSuccessful { get; set; }
    public int ValidationErrorCount { get; set; }
    public int ValidationWarningCount { get; set; }
    public int RecoveryAttempts { get; set; }
}