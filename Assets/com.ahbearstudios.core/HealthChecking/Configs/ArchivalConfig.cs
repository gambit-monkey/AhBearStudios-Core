using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Archival configuration for long-term storage
/// </summary>
public sealed record ArchivalConfig
{
    /// <summary>
    /// Whether archival is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Age threshold for archiving data
    /// </summary>
    public TimeSpan ArchivalThreshold { get; init; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Archival storage type
    /// </summary>
    public ArchivalStorageType StorageType { get; init; } = ArchivalStorageType.LocalFileSystem;

    /// <summary>
    /// Archival storage configuration
    /// </summary>
    public Dictionary<string, object> StorageConfig { get; init; } = new();

    /// <summary>
    /// Creates archival configuration for production environments
    /// </summary>
    /// <returns>Production archival configuration</returns>
    public static ArchivalConfig ForProduction()
    {
        return new ArchivalConfig
        {
            Enabled = true,
            ArchivalThreshold = TimeSpan.FromDays(90),
            StorageType = ArchivalStorageType.CloudStorage,
            StorageConfig = new Dictionary<string, object>
            {
                ["Provider"] = "AWS",
                ["Bucket"] = "health-check-archives",
                ["StorageClass"] = "GLACIER"
            }
        };
    }

    /// <summary>
    /// Creates archival configuration for compliance requirements
    /// </summary>
    /// <returns>Compliance archival configuration</returns>
    public static ArchivalConfig ForCompliance()
    {
        return new ArchivalConfig
        {
            Enabled = true,
            ArchivalThreshold = TimeSpan.FromDays(365),
            StorageType = ArchivalStorageType.CloudStorage,
            StorageConfig = new Dictionary<string, object>
            {
                ["Provider"] = "AWS",
                ["Bucket"] = "compliance-archives",
                ["StorageClass"] = "DEEP_ARCHIVE",
                ["Encryption"] = true,
                ["RetentionPolicy"] = "7_YEARS"
            }
        };
    }

    /// <summary>
    /// Validates archival configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (ArchivalThreshold < TimeSpan.Zero)
            errors.Add("ArchivalThreshold must be non-negative");

        if (!Enum.IsDefined(typeof(ArchivalStorageType), StorageType))
            errors.Add($"Invalid archival storage type: {StorageType}");

        return errors;
    }
}