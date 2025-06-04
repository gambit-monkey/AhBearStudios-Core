using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Messages
{
    /// <summary>
    /// Message published when a service resolution attempt fails.
    /// </summary>
    public readonly struct ServiceResolutionFailedMessage : IMessage
    {
        public Guid Id { get; }
        public long TimestampTicks { get; }
        public ushort TypeCode => 2003; // Unique type code for resolution failure
        
        public string ContainerName { get; }
        public Type ServiceType { get; }
        public string ErrorMessage { get; }
        public TimeSpan AttemptedResolutionTime { get; }
        
        public ServiceResolutionFailedMessage(string containerName, Type serviceType, string errorMessage, TimeSpan attemptedResolutionTime)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ContainerName = containerName;
            ServiceType = serviceType;
            ErrorMessage = errorMessage;
            AttemptedResolutionTime = attemptedResolutionTime;
        }
    }
}