using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Base struct for messages that need to be Burst-compatible.
    /// Uses blittable types only for full Burst compatibility.
    /// </summary>
    public struct BlittableMessageBase : IUnmanagedMessage
    {
        /// <summary>
        /// Internal storage for the message ID using math types for unmanaged compatibility.
        /// </summary>
        private readonly uint4 _idStorage;
        
        /// <summary>
        /// UTC timestamp ticks when the message was created.
        /// </summary>
        private readonly long _timestamp;
        
        /// <summary>
        /// Type code for the message.
        /// </summary>
        private readonly ushort _typeCode;
        
        /// <inheritdoc />
        public Guid Id => new Guid(
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
        
        /// <inheritdoc />
        public long TimestampTicks => _timestamp;
        
        /// <inheritdoc />
        public ushort TypeCode => _typeCode;
        
        /// <summary>
        /// Initializes a new instance of the UnmanagedMessageBase struct.
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
            
            _timestamp = now;
            _typeCode = typeCode;
        }
    }
}