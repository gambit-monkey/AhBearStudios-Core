using System;
using System.Reflection;
using AhBearStudios.Core.MessageBus.Attributes;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;
using MemoryPack;

namespace AhBearStudios.Core.MessageBus.Data
{
    /// <summary>
    /// Implementation of IMessageInfo that provides information about a registered message type.
    /// </summary>
    public sealed class MessageInfo : IMessageInfo
    {
        /// <inheritdoc />
        public Type MessageType { get; }
        
        /// <inheritdoc />
        public MessageAttribute Attribute { get; }
        
        /// <inheritdoc />
        public ushort TypeCode { get; }
        
        /// <inheritdoc />
        public bool IsBurstCompatible { get; }
        
        /// <inheritdoc />
        public bool SupportsReliableDelivery { get; }
        
        /// <inheritdoc />
        public bool IsNetworkSerializable { get; }
        
        /// <inheritdoc />
        public string Domain { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageInfo class.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="attribute">The message attribute.</param>
        /// <param name="typeCode">The type code assigned to this message type.</param>
        public MessageInfo(Type messageType, MessageAttribute attribute, ushort typeCode)
        {
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            TypeCode = typeCode;
            
            // Determine capabilities based on type characteristics
            IsBurstCompatible = messageType.IsValueType && 
                               typeof(BlittableMessageBase).IsAssignableFrom(messageType);
            
            SupportsReliableDelivery = typeof(IReliableMessage).IsAssignableFrom(messageType);
            
            // Check if the type has MemoryPackable attribute or is blittable
            IsNetworkSerializable = messageType.GetCustomAttribute<MemoryPackableAttribute>() != null ||
                                   IsBurstCompatible;
            
            // Get domain from MessageDomainAttribute on the containing class
            var containingType = messageType.DeclaringType;
            if (containingType != null)
            {
                var domainAttr = containingType.GetCustomAttribute<MessageDomainAttribute>();
                Domain = domainAttr?.Name ?? "Unknown";
            }
            else
            {
                Domain = "Global";
            }
        }
    }
}