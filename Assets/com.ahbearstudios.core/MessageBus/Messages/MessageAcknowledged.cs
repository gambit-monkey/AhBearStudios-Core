using System;
using AhBearStudios.Core.MessageBus.Attributes;
using AhBearStudios.Core.MessageBus.Interfaces;
using MemoryPack;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Record struct for message acknowledgments.
    /// Provides immutable acknowledgment semantics for reliable delivery.
    /// </summary>
    [MemoryPackable]
    [Message("System", "Acknowledgment message for reliable delivery", logOnPublish: false)]
    [MessageTypeCode(1000)] // Reserve a specific type code for system messages
    public readonly partial record struct MessageAcknowledged : IMessage
    {
        /// <summary>
        /// The base message data.
        /// </summary>
        [MemoryPackInclude]
        public MessageBase BaseMessage { get; init; }
        
        /// <inheritdoc />
        public Guid Id => BaseMessage.Id;
        
        /// <inheritdoc />
        public long TimestampTicks => BaseMessage.TimestampTicks;
        
        /// <inheritdoc />
        public ushort TypeCode => BaseMessage.TypeCode;
        
        /// <summary>
        /// Gets the ID of the message being acknowledged.
        /// </summary>
        [MemoryPackInclude]
        public Guid AcknowledgedMessageId { get; init; }
        
        /// <summary>
        /// Gets the delivery ID of the message being acknowledged.
        /// </summary>
        [MemoryPackInclude]
        public Guid AcknowledgedDeliveryId { get; init; }
        
        /// <summary>
        /// Gets the time when the acknowledgment was sent.
        /// </summary>
        [MemoryPackInclude]
        public DateTime AcknowledgmentTime { get; init; }
        
        /// <summary>
        /// Initializes a new instance of the MessageAcknowledged record struct.
        /// </summary>
        /// <param name="acknowledgedMessageId">The ID of the message being acknowledged.</param>
        /// <param name="acknowledgedDeliveryId">The delivery ID of the message being acknowledged.</param>
        public MessageAcknowledged(Guid acknowledgedMessageId, Guid acknowledgedDeliveryId)
        {
            BaseMessage = new MessageBase(1000); // System message type code
            AcknowledgedMessageId = acknowledgedMessageId;
            AcknowledgedDeliveryId = acknowledgedDeliveryId;
            AcknowledgmentTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Constructor for MemoryPack serialization.
        /// </summary>
        [MemoryPackConstructor]
        public MessageAcknowledged(MessageBase baseMessage, Guid acknowledgedMessageId, 
            Guid acknowledgedDeliveryId, DateTime acknowledgmentTime)
        {
            BaseMessage = baseMessage;
            AcknowledgedMessageId = acknowledgedMessageId;
            AcknowledgedDeliveryId = acknowledgedDeliveryId;
            AcknowledgmentTime = acknowledgmentTime;
        }
    }
}