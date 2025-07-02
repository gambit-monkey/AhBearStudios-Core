using AhBearStudios.Core.MessageBus.Interfaces;
using MemoryPack;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Base record struct for unmanaged messages that need to be Burst-compatible.
    /// Uses blittable types only for full Burst compatibility and provides immutable value semantics.
    /// </summary>
    [MemoryPackable]
    public readonly partial record struct BlittableMessageBase : IUnmanagedMessage
    {
        /// <summary>
        /// Internal storage for the message ID using math types for unmanaged compatibility.
        /// </summary>
        private readonly uint4 _idStorage;
        
        /// <summary>
        /// UTC timestamp ticks when the message was created.
        /// </summary>
        [MemoryPackInclude]
        public long TimestampTicks { get; init; }
        
        /// <summary>
        /// Type code for the message.
        /// </summary>
        [MemoryPackInclude]
        public ushort TypeCode { get; init; }
        
        /// <inheritdoc />
        public Guid Id => new(
            _idStorage.x,
            (ushort)(_idStorage.y & 0xFFFF),
            (ushort)((_idStorage.y >> 16) & 0xFFFF),
            (byte)(_idStorage.z & 0xFF),
            (byte)((_idStorage.z >> 8) & 0xFF),
            (byte)((_idStorage.z >> 16) & 0xFF),
            (byte)((_idStorage.z >> 24) & 0xFF),
            (byte)(_idStorage.w & 0xFF),
            (byte)((_idStorage.w >> 8) & 0xFF),
            (byte)((_idStorage.w >> 16) & 0xFF),
            (byte)((_idStorage.w >> 24) & 0xFF)
        );
        
        /// <summary>
        /// Initializes a new instance of the BlittableMessageBase record struct.
        /// </summary>
        /// <param name="typeCode">The type code that identifies this message type.</param>
        public BlittableMessageBase(ushort typeCode)
        {
            // Generate a pseudo-random ID using the current timestamp
            var now = DateTime.UtcNow.Ticks;
            var random = new Random((uint)now);
            
            _idStorage = new uint4(
                random.NextUInt(),
                random.NextUInt(),
                random.NextUInt(),
                random.NextUInt()
            );
            
            TimestampTicks = now;
            TypeCode = typeCode;
        }
        
        /// <summary>
        /// Constructor for MemoryPack serialization.
        /// </summary>
        public BlittableMessageBase(uint4 idStorage, long timestampTicks, ushort typeCode)
        {
            _idStorage = idStorage;
            TimestampTicks = timestampTicks;
            TypeCode = typeCode;
        }
    }
}