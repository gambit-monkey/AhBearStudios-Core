namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Defines types of performance anomalies that can be detected.
/// </summary>
public enum AnomalyType : byte
{
    /// <summary>
    /// High failure rate anomaly.
    /// </summary>
    HighFailureRate = 0,

    /// <summary>
    /// Slow performance anomaly.
    /// </summary>
    SlowPerformance = 1,

    /// <summary>
    /// Performance spike anomaly.
    /// </summary>
    PerformanceSpike = 2,

    /// <summary>
    /// Low activity anomaly.
    /// </summary>
    LowActivity = 3,

    /// <summary>
    /// Resource exhaustion anomaly.
    /// </summary>
    ResourceExhaustion = 4,

    /// <summary>
    /// Pattern deviation anomaly.
    /// </summary>
    PatternDeviation = 5
}