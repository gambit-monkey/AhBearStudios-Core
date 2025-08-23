using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing Rigidbody component data.
    /// </summary>
    [MemoryPackable]
    public partial class RigidbodyData
    {
        public float Mass { get; set; }
        public float Drag { get; set; }
        public float AngularDrag { get; set; }
        public bool UseGravity { get; set; }
        public bool IsKinematic { get; set; }
        public SerializableVector3 Velocity { get; set; }
        public SerializableVector3 AngularVelocity { get; set; }
    }
}