using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Messages;

public readonly record struct HealthCheckConfigurationChangedMessage(
    Guid Id,
    long TimestampTicks,
    ushort TypeCode,
    FixedString64Bytes HealthCheckName,
    FixedString128Bytes ConfigurationId,
    FixedString64Bytes ChangeType, // "Updated", "Created", "Deleted"
    FixedString512Bytes ChangedProperties,
    FixedString64Bytes CorrelationId
) : IMessage;