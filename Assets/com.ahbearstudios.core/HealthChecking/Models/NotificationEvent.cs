namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Events that can trigger notifications
/// </summary>
public enum NotificationEvent
{
    ReportGenerated,
    ReportFailed,
    HealthStatusChanged,
    AlertTriggered,
    DegradationDetected,
    SystemRecovered,
    ExportCompleted,
    ExportFailed
}