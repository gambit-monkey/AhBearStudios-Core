using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Messages
{
    /// <summary>
    /// Message published when a child container is created.
    /// </summary>
    public readonly struct ChildContainerCreatedMessage : IMessage
    {
        public Guid Id { get; }
        public long TimestampTicks { get; }
        public ushort TypeCode => 2006; // Unique type code for child container creation
        
        public string ParentContainerName { get; }
        public string ChildContainerName { get; }
        
        public ChildContainerCreatedMessage(string parentContainerName, string childContainerName)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ParentContainerName = parentContainerName;
            ChildContainerName = childContainerName;
        }
    }
}