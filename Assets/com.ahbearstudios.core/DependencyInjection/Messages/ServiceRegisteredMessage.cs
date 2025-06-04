
using System;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Messages
{
    /// <summary>
    /// Message published when a service is registered in a dependency container.
    /// </summary>
    public readonly struct ServiceRegisteredMessage : IMessage
    {
        public Guid Id { get; }
        public long TimestampTicks { get; }
        public ushort TypeCode => 2001; // Unique type code for service registration
        
        public string ContainerName { get; }
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
        public bool IsFactoryRegistration { get; }
        
        public ServiceRegisteredMessage(string containerName, Type serviceType, Type implementationType, ServiceLifetime lifetime, bool isFactoryRegistration = false)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ContainerName = containerName;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            IsFactoryRegistration = isFactoryRegistration;
        }
    }
}