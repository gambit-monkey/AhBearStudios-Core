using UnityEngine;
using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible struct for serializing Quaternion data.
    /// Provides implicit conversion operators for seamless Unity integration.
    /// </summary>
    [MemoryPackable]
    public partial struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static implicit operator Quaternion(SerializableQuaternion q) => new Quaternion(q.x, q.y, q.z, q.w);
        public static implicit operator SerializableQuaternion(Quaternion q) => new SerializableQuaternion { x = q.x, y = q.y, z = q.z, w = q.w };
    }
}