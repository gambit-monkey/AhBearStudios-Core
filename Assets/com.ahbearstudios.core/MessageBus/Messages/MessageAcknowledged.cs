// MessageAcknowledged.cs
using System;
using AhBearStudios.Core.Messaging.Attributes;
using MemoryPack;

namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Message sent to acknowledge receipt of another message.
    /// </summary>
    [MemoryPackable]
    [Message("System", "Acknowledgment message for reliable delivery", logOnPublish: false)]
    [MessageTypeCode(1000)] // Reserve a specific type code for system messages
    public partial class MessageAcknowledged : MessageBase
    {
        /// <summary>
        /// Gets or sets the ID of the message being acknowledged.
        /// </summary>
        [MemoryPackInclude]
        public Guid AcknowledgedMessageId { get; set; }
        
        /// <summary>
        /// Gets or sets the delivery ID of the message being acknowledged.
        /// </summary>
        [MemoryPackInclude]
        public Guid AcknowledgedDeliveryId { get; set; }
        
        /// <summary>
        /// Gets or sets the time when the acknowledgment was sent.
        /// </summary>
        [MemoryPackInclude]
        public DateTime AcknowledgmentTime { get; set; } = DateTime.UtcNow;
    }
}