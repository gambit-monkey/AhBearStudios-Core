using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AhBearStudios.Core.HealthCheck.Messages;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Factories;

/// <summary>
/// Factory for creating health check message instances with proper validation and defaults.
/// Provides centralized creation logic following the Builder → Config → Factory → Service pattern.
/// Ensures consistent timestamp generation, correlation ID management, and field validation.
/// </summary>
public static class HealthCheckMessageFactory
{
    #region Core Creation Methods

    /// <summary>
    /// Creates a new HealthCheckCreatedMessage with the current timestamp and auto-generated correlation ID.
    /// </summary>
    /// <param name="healthCheckName">The name of the created health check.</param>
    /// <param name="healthCheckType">The type of the created health check.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="factoryId">The identifier of the factory that created the health check.</param>
    /// <param name="configurationId">Optional configuration identifier.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <returns>A new HealthCheckCreatedMessage instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative.</exception>
    public static HealthCheckCreatedMessage CreateHealthCheckCreated(
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        TimeSpan creationDuration,
        FixedString64Bytes factoryId,
        FixedString128Bytes configurationId = default,
        FixedString64Bytes correlationId = default)
    {
        ValidateRequiredFields(healthCheckName, healthCheckType, factoryId);
        ValidateCreationDuration(creationDuration);

        return new HealthCheckCreatedMessage(
            Id: Guid.NewGuid(),
            TimestampTicks: DateTime.UtcNow.Ticks,
            TypeCode: 0, // Should be set by message registry
            HealthCheckName: healthCheckName,
            HealthCheckType: healthCheckType,
            ConfigurationId: configurationId.IsEmpty ? "default" : configurationId,
            CreationDurationTicks: creationDuration.Ticks,
            FactoryId: factoryId,
            CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId);
    }

    /// <summary>
    /// Creates a new HealthCheckCreatedMessage with extended context and metadata.
    /// </summary>
    /// <param name="healthCheckName">The name of the created health check.</param>
    /// <param name="healthCheckType">The type of the created health check.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="factoryId">The identifier of the factory that created the health check.</param>
    /// <param name="configurationId">Optional configuration identifier.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="creationContext">Additional creation context.</param>
    /// <param name="severity">Message severity level.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    /// <param name="environment">Environment where creation occurred.</param>
    /// <param name="instanceId">Application instance identifier.</param>
    /// <returns>A new HealthCheckCreatedMessage instance with extended metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative.</exception>
    public static HealthCheckCreatedMessage CreateHealthCheckCreatedWithContext(
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        TimeSpan creationDuration,
        FixedString64Bytes factoryId,
        FixedString128Bytes configurationId = default,
        FixedString64Bytes correlationId = default,
        FixedString128Bytes creationContext = default,
        MessageSeverity severity = MessageSeverity.Info,
        FixedString64Bytes tags = default,
        FixedString32Bytes environment = default,
        FixedString32Bytes instanceId = default)
    {
        ValidateRequiredFields(healthCheckName, healthCheckType, factoryId);
        ValidateCreationDuration(creationDuration);

        return new HealthCheckCreatedMessage(
            Id: Guid.NewGuid(),
            TimestampTicks: DateTime.UtcNow.Ticks,
            TypeCode: 0, // Should be set by message registry
            HealthCheckName: healthCheckName,
            HealthCheckType: healthCheckType,
            ConfigurationId: configurationId.IsEmpty ? "default" : configurationId,
            CreationDurationTicks: creationDuration.Ticks,
            FactoryId: factoryId,
            CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId,
            CreationContext: creationContext,
            Severity: severity,
            Tags: tags,
            Environment: environment,
            InstanceId: instanceId);
    }

    #endregion

    #region Specialized Creation Methods

    /// <summary>
    /// Creates a message for a slow health check creation with warning severity.
    /// Automatically sets warning severity and adds performance-related tags.
    /// </summary>
    /// <param name="healthCheckName">The name of the created health check.</param>
    /// <param name="healthCheckType">The type of the created health check.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="factoryId">The identifier of the factory that created the health check.</param>
    /// <param name="configurationId">Optional configuration identifier.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="slowThresholdMs">The threshold in milliseconds above which creation is considered slow.</param>
    /// <returns>A new HealthCheckCreatedMessage with warning severity.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative or slowThresholdMs is not positive.</exception>
    public static HealthCheckCreatedMessage CreateSlowCreationWarning(
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        TimeSpan creationDuration,
        FixedString64Bytes factoryId,
        FixedString128Bytes configurationId = default,
        FixedString64Bytes correlationId = default,
        double slowThresholdMs = 1000.0)
    {
        ValidateRequiredFields(healthCheckName, healthCheckType, factoryId);
        ValidateCreationDuration(creationDuration);

        if (slowThresholdMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(slowThresholdMs), "Slow threshold must be positive");

        var durationMs = creationDuration.TotalMilliseconds;
        var context = $"Slow creation: {durationMs:F1}ms (threshold: {slowThresholdMs:F1}ms)";
        
        return CreateHealthCheckCreatedWithContext(
            healthCheckName: healthCheckName,
            healthCheckType: healthCheckType,
            creationDuration: creationDuration,
            factoryId: factoryId,
            configurationId: configurationId,
            correlationId: correlationId,
            creationContext: context,
            severity: MessageSeverity.Warning,
            tags: "slow,performance");
    }

    /// <summary>
    /// Creates a message for an exceptionally fast health check creation with debug severity.
    /// Useful for tracking performance optimizations and unusual creation patterns.
    /// </summary>
    /// <param name="healthCheckName">The name of the created health check.</param>
    /// <param name="healthCheckType">The type of the created health check.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="factoryId">The identifier of the factory that created the health check.</param>
    /// <param name="configurationId">Optional configuration identifier.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="fastThresholdMs">The threshold in milliseconds below which creation is considered very fast.</param>
    /// <returns>A new HealthCheckCreatedMessage with debug severity.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative or fastThresholdMs is negative.</exception>
    public static HealthCheckCreatedMessage CreateFastCreationDebug(
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        TimeSpan creationDuration,
        FixedString64Bytes factoryId,
        FixedString128Bytes configurationId = default,
        FixedString64Bytes correlationId = default,
        double fastThresholdMs = 10.0)
    {
        ValidateRequiredFields(healthCheckName, healthCheckType, factoryId);
        ValidateCreationDuration(creationDuration);

        if (fastThresholdMs < 0)
            throw new ArgumentOutOfRangeException(nameof(fastThresholdMs), "Fast threshold cannot be negative");

        var durationMs = creationDuration.TotalMilliseconds;
        var context = $"Very fast creation: {durationMs:F2}ms (threshold: {fastThresholdMs:F1}ms)";
        
        return CreateHealthCheckCreatedWithContext(
            healthCheckName: healthCheckName,
            healthCheckType: healthCheckType,
            creationDuration: creationDuration,
            factoryId: factoryId,
            configurationId: configurationId,
            correlationId: correlationId,
            creationContext: context,
            severity: MessageSeverity.Debug,
            tags: "fast,performance");
    }

    /// <summary>
    /// Creates a message for a health check creation that failed validation but was still created.
    /// Used when creation succeeds despite configuration or dependency issues.
    /// </summary>
    /// <param name="healthCheckName">The name of the created health check.</param>
    /// <param name="healthCheckType">The type of the created health check.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="factoryId">The identifier of the factory that created the health check.</param>
    /// <param name="validationWarnings">Description of validation warnings encountered.</param>
    /// <param name="configurationId">Optional configuration identifier.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <returns>A new HealthCheckCreatedMessage with warning severity and validation context.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative.</exception>
    public static HealthCheckCreatedMessage CreateWithValidationWarnings(
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        TimeSpan creationDuration,
        FixedString64Bytes factoryId,
        string validationWarnings,
        FixedString128Bytes configurationId = default,
        FixedString64Bytes correlationId = default)
    {
        ValidateRequiredFields(healthCheckName, healthCheckType, factoryId);
        ValidateCreationDuration(creationDuration);

        if (string.IsNullOrEmpty(validationWarnings))
            throw new ArgumentException("Validation warnings cannot be null or empty", nameof(validationWarnings));

        var context = $"Created with warnings: {validationWarnings}";
        
        return CreateHealthCheckCreatedWithContext(
            healthCheckName: healthCheckName,
            healthCheckType: healthCheckType,
            creationDuration: creationDuration,
            factoryId: factoryId,
            configurationId: configurationId,
            correlationId: correlationId,
            creationContext: context,
            severity: MessageSeverity.Warning,
            tags: "validation,warnings");
    }

    #endregion

    #region Batch Creation Methods

    /// <summary>
    /// Creates multiple health check created messages with shared correlation ID for batch operations.
    /// Useful when creating multiple health checks in a single factory operation.
    /// </summary>
    /// <param name="healthCheckInfos">Collection of health check creation information.</param>
    /// <param name="factoryId">The identifier of the factory that created the health checks.</param>
    /// <param name="batchCorrelationId">Optional correlation ID for the entire batch.</param>
    /// <returns>A collection of HealthCheckCreatedMessage instances with shared correlation ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckInfos is null.</exception>
    /// <exception cref="ArgumentException">Thrown when healthCheckInfos is empty.</exception>
    public static IReadOnlyList<HealthCheckCreatedMessage> CreateBatchHealthCheckCreated(
        IEnumerable<HealthCheckCreationInfo> healthCheckInfos,
        FixedString64Bytes factoryId,
        FixedString64Bytes batchCorrelationId = default)
    {
        if (healthCheckInfos == null)
            throw new ArgumentNullException(nameof(healthCheckInfos));

        var infos = healthCheckInfos.ToList();
        if (infos.Count == 0)
            throw new ArgumentException("Health check infos cannot be empty", nameof(healthCheckInfos));

        if (factoryId.IsEmpty)
            throw new ArgumentException("Factory ID cannot be empty", nameof(factoryId));

        var correlationId = batchCorrelationId.IsEmpty ? GenerateCorrelationId() : batchCorrelationId;
        var messages = new List<HealthCheckCreatedMessage>(infos.Count);

        foreach (var info in infos)
        {
            var message = CreateHealthCheckCreated(
                healthCheckName: info.HealthCheckName,
                healthCheckType: info.HealthCheckType,
                creationDuration: info.CreationDuration,
                factoryId: factoryId,
                configurationId: info.ConfigurationId,
                correlationId: correlationId);

            messages.Add(message);
        }

        return messages;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Creates a message from an existing message with a new correlation ID.
    /// Useful for linking related operations or updating correlation tracking.
    /// </summary>
    /// <param name="source">The source message to copy from.</param>
    /// <param name="newCorrelationId">The new correlation ID to assign.</param>
    /// <returns>A new message with the updated correlation ID.</returns>
    /// <exception cref="ArgumentException">Thrown when newCorrelationId is null or empty.</exception>
    public static HealthCheckCreatedMessage WithUpdatedCorrelationId(
        HealthCheckCreatedMessage source,
        FixedString64Bytes newCorrelationId)
    {
        if (newCorrelationId.IsEmpty)
            throw new ArgumentException("Correlation ID cannot be empty", nameof(newCorrelationId));

        return source.WithCorrelationId(newCorrelationId);
    }

    /// <summary>
    /// Generates a unique correlation ID for tracking related operations.
    /// </summary>
    /// <returns>A unique correlation ID as a FixedString64Bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedString64Bytes GenerateCorrelationId()
    {
        var guid = Guid.NewGuid();
        var shortId = guid.ToString("N")[..12]; // Take first 12 characters
        return new FixedString64Bytes(shortId);
    }

    #endregion

    #region Private Validation Methods

    /// <summary>
    /// Validates that required fields are not empty.
    /// </summary>
    /// <param name="healthCheckName">Health check name to validate.</param>
    /// <param name="healthCheckType">Health check type to validate.</param>
    /// <param name="factoryId">Factory ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown when any required field is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateRequiredFields(
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        FixedString64Bytes factoryId)
    {
        if (healthCheckName.IsEmpty)
            throw new ArgumentException("Health check name cannot be empty", nameof(healthCheckName));
        if (healthCheckType.IsEmpty)
            throw new ArgumentException("Health check type cannot be empty", nameof(healthCheckType));
        if (factoryId.IsEmpty)
            throw new ArgumentException("Factory ID cannot be empty", nameof(factoryId));
    }

    /// <summary>
    /// Validates that the creation duration is not negative.
    /// </summary>
    /// <param name="creationDuration">The creation duration to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateCreationDuration(TimeSpan creationDuration)
    {
        if (creationDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(creationDuration), "Creation duration cannot be negative");
    }

    #endregion
}