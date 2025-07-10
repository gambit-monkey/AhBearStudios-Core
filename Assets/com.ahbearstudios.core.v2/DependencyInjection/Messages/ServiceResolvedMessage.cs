using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Messages
{
    /// <summary>
    /// Message published when a service is resolved from a dependency container.
    /// </summary>
    public readonly struct ServiceResolvedMessage : IMessage
    {
        public Guid Id { get; }
        public long TimestampTicks { get; }
        public ushort TypeCode => 2002; // Unique type code for service resolution
        
        public string ContainerName { get; }
        public Type ServiceType { get; }
        public object Instance { get; }
        public TimeSpan ResolutionTime { get; }
        public bool WasSuccessful { get; }
        
        public ServiceResolvedMessage(string containerName, Type serviceType, object instance, TimeSpan resolutionTime, bool wasSuccessful = true)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ContainerName = containerName;
            ServiceType = serviceType;
            Instance = instance;
            ResolutionTime = resolutionTime;
            WasSuccessful = wasSuccessful;
        }
    }
}