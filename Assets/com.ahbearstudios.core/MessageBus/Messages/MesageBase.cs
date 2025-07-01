using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Registration;
using MemoryPack;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Base record struct for managed messages that don't need to be Burst-compatible.
    /// Provides immutable value semantics while supporting full serialization capabilities.
    /// </summary>
    [MemoryPackable]
    public readonly partial record struct MessageBase : IMessage
    {
        /// <inheritdoc />
        [MemoryPackInclude]
        public Guid Id { get; init; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public long TimestampTicks { get; init; }
        
        /// <inheritdoc />
        [MemoryPackInclude]
        public ushort TypeCode { get; init; }
        
        /// <summary>
        /// Gets the timestamp of this message as a DateTime.
        /// </summary>
        public DateTime Timestamp => new(TimestampTicks, DateTimeKind.Utc);
        
        /// <summary>
        /// Initializes a new instance of the MessageBase record struct.
        /// </summary>
        /// <param name="typeCode">The type code that identifies this message type.</param>
        public MessageBase(ushort typeCode)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = typeCode;
        }
        
        /// <summary>
        /// Constructor for MemoryPack serialization.
        /// </summary>
        [MemoryPackConstructor]
        public MessageBase(Guid id, long timestampTicks, ushort typeCode)
        {
            Id = id;
            TimestampTicks = timestampTicks;
            TypeCode = typeCode;
        }
    }
}