using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Registration;
using MemoryPack;

namespace AhBearStudios.Core.MessageBus.Messages
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
        public Guid Id { get; protected set; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public long TimestampTicks { get; protected set; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public ushort TypeCode { get; protected set; }
        
        /// <summary>
        /// Initializes a new instance of the MessageBase class.
        /// </summary>
        public MessageBase()
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = MessageTypeRegistry.GetTypeCode(GetType());
        }
        
        /// <summary>
        /// Constructor for MemoryPack serialization.
        /// </summary>
        [MemoryPackConstructor]
        protected MessageBase(Guid id, long timestampTicks, ushort typeCode)
        {
            Id = id;
            TimestampTicks = timestampTicks;
            TypeCode = typeCode;
        }
        
        /// <summary>
        /// Gets the timestamp of this message as a DateTime.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);
    }
}