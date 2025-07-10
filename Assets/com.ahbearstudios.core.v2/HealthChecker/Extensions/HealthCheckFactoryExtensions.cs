using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Messages;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Extensions;

/// <summary>
/// Extension methods for IHealthCheckFactory to provide additional functionality and message publishing support.
/// </summary>
public static class HealthCheckFactoryExtensions
{
    /// <summary>
    /// Publishes a health check created message when a health check is successfully created.
    /// </summary>
    /// <param name="factory">The factory that created the health check.</param>
    /// <param name="healthCheckName">The name of the created health check.</param>
    /// <param name="healthCheckType">The type of the created health check.</param>
    /// <param name="configurationId">The configuration ID used for creation.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related operations.</param>
    public static void PublishHealthCheckCreated(
        this IHealthCheckFactory factory,
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        FixedString128Bytes configurationId,
        TimeSpan creationDuration,
        FixedString64Bytes correlationId = default)
    {
        if (factory.MessageBusService != null)
        {
            var message = new HealthCheckCreatedMessage(
                Id: Guid.NewGuid(),
                TimestampTicks: DateTime.UtcNow.Ticks,
                TypeCode: 0, // Should be set by message registry
                HealthCheckName: healthCheckName,
                HealthCheckType: healthCheckType,
                ConfigurationId: configurationId,
                CreationDuration: creationDuration,
                FactoryId: GetFactoryId(factory),
                CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            );

            factory.MessageBusService.PublishMessage(message);
        }
    }

    /// <summary>
    /// Publishes a health check creation failed message when creation fails.
    /// </summary>
    /// <param name="factory">The factory that attempted to create the health check.</param>
    /// <param name="healthCheckName">The name of the health check that failed to create.</param>
    /// <param name="healthCheckType">The type of the health check that failed to create.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="exceptionType">The type of exception that caused the failure.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related operations.</param>
    public static void PublishHealthCheckCreationFailed(
        this IHealthCheckFactory factory,
        FixedString64Bytes healthCheckName,
        FixedString64Bytes healthCheckType,
        FixedString512Bytes errorMessage,
        FixedString128Bytes exceptionType,
        FixedString64Bytes correlationId = default)
    {
        if (factory.MessageBusService != null)
        {
            var message = new HealthCheckCreationFailedMessage(
                Id: Guid.NewGuid(),
                TimestampTicks: DateTime.UtcNow.Ticks,
                TypeCode: 0, // Should be set by message registry
                HealthCheckName: healthCheckName,
                HealthCheckType: healthCheckType,
                ErrorMessage: errorMessage,
                ExceptionType: exceptionType,
                FactoryId: GetFactoryId(factory),
                CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            );

            factory.MessageBusService.PublishMessage(message);
        }
    }

    /// <summary>
    /// Publishes a health check service created message when a service is successfully created.
    /// </summary>
    /// <param name="factory">The factory that created the service.</param>
    /// <param name="serviceType">The type of service that was created.</param>
    /// <param name="configurationId">The configuration ID used for creation.</param>
    /// <param name="creationDuration">The time taken to create the service.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related operations.</param>
    public static void PublishHealthCheckServiceCreated(
        this IHealthCheckFactory factory,
        FixedString64Bytes serviceType,
        FixedString128Bytes configurationId,
        TimeSpan creationDuration,
        FixedString64Bytes correlationId = default)
    {
        if (factory.MessageBusService != null)
        {
            var message = new HealthCheckServiceCreatedMessage(
                Id: Guid.NewGuid(),
                TimestampTicks: DateTime.UtcNow.Ticks,
                TypeCode: 0, // Should be set by message registry
                ServiceType: serviceType,
                ConfigurationId: configurationId,
                CreationDuration: creationDuration,
                FactoryId: GetFactoryId(factory),
                CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            );

            factory.MessageBusService.PublishMessage(message);
        }
    }

    /// <summary>
    /// Publishes a factory cleared message when the factory cache is cleared.
    /// </summary>
    /// <param name="factory">The factory that was cleared.</param>
    /// <param name="instancesDisposed">Whether instances were disposed during clearing.</param>
    /// <param name="clearedInstanceCount">The number of instances that were cleared.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related operations.</param>
    public static void PublishFactoryCleared(
        this IHealthCheckFactory factory,
        bool instancesDisposed,
        int clearedInstanceCount,
        FixedString64Bytes correlationId = default)
    {
        if (factory.MessageBusService != null)
        {
            var message = new HealthCheckFactoryClearedMessage(
                Id: Guid.NewGuid(),
                TimestampTicks: DateTime.UtcNow.Ticks,
                TypeCode: 0, // Should be set by message registry
                FactoryId: GetFactoryId(factory),
                InstancesDisposed: instancesDisposed,
                ClearedInstanceCount: clearedInstanceCount,
                CorrelationId: correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            );

            factory.MessageBusService.PublishMessage(message);
        }
    }

    /// <summary>
    /// Gets a unique identifier for the factory instance.
    /// </summary>
    /// <param name="factory">The factory to get the ID for.</param>
    /// <returns>A unique factory identifier.</returns>
    private static FixedString64Bytes GetFactoryId(IHealthCheckFactory factory)
    {
        // Use the factory's hash code as a simple identifier
        var hashCode = factory.GetHashCode();
        return new FixedString64Bytes($"Factory_{hashCode:X8}");
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