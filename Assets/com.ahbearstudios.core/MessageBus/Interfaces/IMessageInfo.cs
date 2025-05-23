using System;
using AhBearStudios.Core.MessageBus.Attributes;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for message information that provides details about a registered message type.
    /// </summary>
    public interface IMessageInfo
    {
        /// <summary>
        /// Gets the message type.
        /// </summary>
        Type MessageType { get; }
        
        /// <summary>
        /// Gets the message attribute.
        /// </summary>
        MessageAttribute Attribute { get; }
        
        /// <summary>
        /// Gets the type code assigned to this message type.
        /// </summary>
        ushort TypeCode { get; }
        
        /// <summary>
        /// Gets whether this message type supports Burst compilation.
        /// </summary>
        bool IsBurstCompatible { get; }
        
        /// <summary>
        /// Gets whether this message type supports reliable delivery.
        /// </summary>
        bool SupportsReliableDelivery { get; }
        
        /// <summary>
        /// Gets whether this message type can be serialized for network transmission.
        /// </summary>
        bool IsNetworkSerializable { get; }
        
        /// <summary>
        /// Gets the domain this message belongs to.
        /// </summary>
        string Domain { get; }
    }
}