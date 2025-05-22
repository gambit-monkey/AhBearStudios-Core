using System;
using MemoryPack;

namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Base class for messages that don't need to be Burst-compatible.
    /// Used for regular managed messages that support full serialization.
    /// </summary>
    [MemoryPackable]
    public partial class MessageBase : IMessage
    {
        /// <inheritdoc />
        [MemoryPackInclude]
        public Guid Id { get; private set; } = Guid.NewGuid();
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public long TimestampTicks { get; private set; } = DateTime.UtcNow.Ticks;
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public ushort TypeCode { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the MessageBase class.
        /// </summary>
        public MessageBase()
        {
            TypeCode = MessageTypeRegistry.GetTypeCode(GetType());
        }
        
        /// <summary>
        /// Gets the timestamp of this message as a DateTime.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);
    }
}