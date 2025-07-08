using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Messages;

/// <summary>
/// Message published when an installer completes.
/// </summary>
public record struct InstallerCompletedMessage : IMessage
{
    public Guid Id { get; init; }
    public long TimestampTicks { get; init; }
    public ushort TypeCode { get; init; }
    public string InstallerName { get; init; }
    public int Priority { get; init; }
    public TimeSpan InstallDuration { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int RecoveryAttempts { get; init; }
    public bool IsSuccessful { get; init; }
    public bool SupportsHotReload { get; init; }
    public bool IsHotReloadEnabled { get; init; }
}