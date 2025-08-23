using UnityEngine;
using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible struct for serializing Vector3 data.
    /// Provides implicit conversion operators for seamless Unity integration.
    /// </summary>
    [MemoryPackable]
    public partial struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public static implicit operator Vector3(SerializableVector3 v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3 { x = v.x, y = v.y, z = v.z };
    }
}