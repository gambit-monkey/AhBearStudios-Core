using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Messages
{
    /// <summary>
    /// Message published when a container is disposed.
    /// </summary>
    public readonly struct ContainerDisposedMessage : IMessage
    {
        public Guid Id { get; }
        public long TimestampTicks { get; }
        public ushort TypeCode => 2005; // Unique type code for container disposal
        
        public string ContainerName { get; }
        public TimeSpan Lifetime { get; }
        
        public ContainerDisposedMessage(string containerName, TimeSpan lifetime)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ContainerName = containerName;
            Lifetime = lifetime;
        }
    }
}