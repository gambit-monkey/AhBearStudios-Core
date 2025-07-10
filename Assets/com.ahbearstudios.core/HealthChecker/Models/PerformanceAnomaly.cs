namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents a detected performance anomaly in factory operations.
/// </summary>
public readonly record struct PerformanceAnomaly(
    AnomalyType Type,
    string Description,
    AnomalySeverity Severity,
    DateTime DetectedAt);