using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Messages
{
    /// <summary>
    /// Message published when a container is built.
    /// </summary>
    public readonly struct ContainerBuiltMessage : IMessage
    {
        public Guid Id { get; }
        public long TimestampTicks { get; }
        public ushort TypeCode => 2004; // Unique type code for container built
        
        public string ContainerName { get; }
        public int RegisteredServicesCount { get; }
        public TimeSpan BuildTime { get; }
        
        public ContainerBuiltMessage(string containerName, int registeredServicesCount, TimeSpan buildTime)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ContainerName = containerName;
            RegisteredServicesCount = registeredServicesCount;
            BuildTime = buildTime;
        }
    }
}