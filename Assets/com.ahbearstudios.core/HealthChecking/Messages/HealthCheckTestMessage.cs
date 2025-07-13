using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Simple test message for health check functional testing.
/// </summary>
internal sealed record HealthCheckTestMessage : BaseMessage
{
    /// <inheritdoc />
    public override ushort TypeCode => 65535; // Reserved for health checks

    /// <summary>
    /// Initializes a new instance of the HealthCheckTestMessage class.
    /// </summary>
    public HealthCheckTestMessage() : base("HealthCheck", MessagePriority.Low)
    {
    }
}