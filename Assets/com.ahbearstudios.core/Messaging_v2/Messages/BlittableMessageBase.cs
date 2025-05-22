using System;
using Unity.Mathematics;

namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Base struct for messages that need to be Burst-compatible.
    /// Uses blittable types only for full Burst compatibility.
    /// </summary>
    public struct BlittableMessageBase : IMessage
    {
        /// <summary>
        /// Internal storage for the message ID.
        /// </summary>
        private readonly Random.State _idState;
        
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
            (uint)math.hash(new int2(_idState.state, 0)),
            (ushort)math.hash(new int2(_idState.state, 1)),
            (ushort)math.hash(new int2(_idState.state, 2)),
            (byte)math.hash(new int2(_idState.state, 3)),
            (byte)math.hash(new int2(_idState.state, 4)),
            (byte)math.hash(new int2(_idState.state, 5)),
            (byte)math.hash(new int2(_idState.state, 6)),
            (byte)math.hash(new int2(_idState.state, 7)),
            (byte)math.hash(new int2(_idState.state, 8)),
            (byte)math.hash(new int2(_idState.state, 9)),
            (byte)math.hash(new int2(_idState.state, 10))
        );
        
        /// <inheritdoc />
        public long TimestampTicks => _timestamp;
        
        /// <inheritdoc />
        public ushort TypeCode => _typeCode;
        
        /// <summary>
        /// Initializes a new instance of the BlittableMessageBase struct.
        /// </summary>
        /// <param name="typeCode">The type code that identifies this message type.</param>
        public BlittableMessageBase(ushort typeCode)
        {
            _idState = new Unity.Mathematics.Random.State((uint)System.DateTime.UtcNow.Ticks);
            _timestamp = System.DateTime.UtcNow.Ticks;
            _typeCode = typeCode;
        }
    }
}