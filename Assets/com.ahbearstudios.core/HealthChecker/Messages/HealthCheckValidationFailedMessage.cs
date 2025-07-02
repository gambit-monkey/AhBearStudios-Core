using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Messages;

public readonly record struct HealthCheckValidationFailedMessage(
    Guid Id,
    long TimestampTicks, 
    ushort TypeCode,
    FixedString64Bytes HealthCheckName,
    FixedString128Bytes ConfigurationId,
    FixedString512Bytes ValidationErrors,
    HealthSeverity Severity,
    FixedString64Bytes CorrelationId
) : IMessage;