namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Types of export destinations
/// </summary>
public enum ExportDestinationType
{
    Http,
    Ftp,
    CloudStorage,
    Database,
    MessageQueue,
    Email,
    Webhook
}