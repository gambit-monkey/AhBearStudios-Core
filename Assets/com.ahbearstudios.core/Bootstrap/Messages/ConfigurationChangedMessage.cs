using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Messages;

/// <summary>
/// Message for configuration changes in hot-reload scenarios.
/// </summary>
public record struct ConfigurationChangedMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public string AffectedInstaller { get; init; }
    public string ConfigurationPath { get; init; }
    public string ChangeType { get; init; }
}