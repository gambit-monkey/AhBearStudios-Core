namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Types of notification channels
/// </summary>
public enum NotificationChannelType
{
    Email,
    Sms,
    Slack,
    Teams,
    Discord,
    Webhook,
    PushNotification
}