namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Alert rule configuration for monitoring.
/// </summary>
public sealed class AlertRule
{
    /// <summary>
    /// Gets or sets the alert rule name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the alert description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Gets or sets the metric name to monitor.
    /// </summary>
    public string MetricName { get; set; }
}