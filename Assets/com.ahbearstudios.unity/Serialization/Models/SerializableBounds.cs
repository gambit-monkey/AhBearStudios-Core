using UnityEngine;
using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible struct for serializing Bounds data.
    /// Provides implicit conversion operators for seamless Unity integration.
    /// </summary>
    [MemoryPackable]
    public partial struct SerializableBounds
    {
        public SerializableVector3 Center;
        public SerializableVector3 Size;

        public static implicit operator Bounds(SerializableBounds b) => new Bounds(b.Center, b.Size);
        public static implicit operator SerializableBounds(Bounds b) => new SerializableBounds { Center = b.center, Size = b.size };
    }
}