namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Defines the high-level types of alert channels available in the system.
/// Each type corresponds to a specific alert delivery mechanism category.
/// </summary>
public enum AlertChannelType : byte
{
    /// <summary>
    /// Log-based alert channel that writes alerts to the logging system.
    /// Suitable for persistent storage and audit trails.
    /// </summary>
    Log = 0,

    /// <summary>
    /// Console-based alert channel that displays alerts in the system console.
    /// Suitable for real-time monitoring and debugging.
    /// </summary>
    Console = 1,

    /// <summary>
    /// Network-based alert channel that sends alerts via HTTP/HTTPS webhooks.
    /// Suitable for integration with external monitoring systems and APIs.
    /// </summary>
    Network = 2,

    /// <summary>
    /// Email-based alert channel that sends alerts via SMTP.
    /// Suitable for critical alerts that require human attention.
    /// </summary>
    Email = 3,

    /// <summary>
    /// Unity console-based alert channel that displays alerts in Unity's debug console.
    /// Suitable for development and Unity-specific debugging scenarios.
    /// </summary>
    UnityConsole = 4,

    /// <summary>
    /// Unity notification-based alert channel for in-game notifications.
    /// Suitable for runtime alerts visible to users within Unity applications.
    /// </summary>
    UnityNotification = 5
}