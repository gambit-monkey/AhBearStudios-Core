using System.Collections.Generic;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Messages;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Extensions;

/// <summary>
/// Extension methods for IHealthCheckConfig to provide additional functionality.
/// </summary>
public static class HealthCheckConfigExtensions
{
    /// <summary>
    /// Updates a configuration property and publishes a change notification.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="config">The configuration to update.</param>
    /// <param name="propertyName">The name of the property being changed.</param>
    /// <param name="oldValue">The previous value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related changes.</param>
    public static void NotifyConfigurationChanged<T>(
        this IHealthCheckConfig config,
        string propertyName,
        T oldValue,
        T newValue,
        FixedString64Bytes correlationId = default)
    {
        if (config.MessageBusService != null && !EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            var message = new HealthCheckConfigurationChangedMessage(
                Id: Guid.NewGuid(),
                TimestampTicks: DateTime.UtcNow.Ticks,
                TypeCode: 0, // Should be set by message registry
                HealthCheckName: config.DisplayName ?? config.ConfigId,
                ConfigurationId: config.ConfigId,
                ChangeType: "PropertyUpdated",
                PropertyName: propertyName,
                OldValue: oldValue?.ToString() ?? "null",
                NewValue: newValue?.ToString() ?? "null",
                CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            );

            config.MessageBusService.PublishMessage(message);
        }
    }

    /// <summary>
    /// Publishes a validation failed message when configuration validation fails.
    /// </summary>
    /// <param name="config">The configuration that failed validation.</param>
    /// <param name="validationResult">The validation result containing errors.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related operations.</param>
    public static void NotifyValidationFailed(
        this IHealthCheckConfig config,
        ConfigurationValidationResult validationResult,
        FixedString64Bytes correlationId = default)
    {
        if (config.MessageBusService != null && !validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors ?? Array.Empty<string>());
            var warnings = string.Join("; ", validationResult.Warnings ?? Array.Empty<string>());
            
            var message = new HealthCheckValidationFailedMessage(
                Id: Guid.NewGuid(),
                TimestampTicks: DateTime.UtcNow.Ticks,
                TypeCode: 0, // Should be set by message registry
                HealthCheckName: config.DisplayName ?? config.ConfigId,
                ConfigurationId: config.ConfigId,
                ValidationErrors: errors,
                ValidationWarnings: warnings,
                ErrorCount: validationResult.Errors?.Count ?? 0,
                WarningCount: validationResult.Warnings?.Count ?? 0,
                CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            );

            config.MessageBusService.PublishMessage(message);
        }
    }

    /// <summary>
    /// Generates a correlation ID for tracking related operations.
    /// </summary>
    /// <returns>A unique correlation ID as a FixedString64Bytes.</returns>
    private static FixedString64Bytes GenerateCorrelationId()
    {
        var guid = Guid.NewGuid();
        var shortId = guid.ToString("N")[..12]; // Take first 12 characters
        return new FixedString64Bytes(shortId);
    }
}