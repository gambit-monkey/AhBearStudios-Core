using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing Transform component data.
    /// Supports various optimization options like compressed quaternions and precision control.
    /// </summary>
    [MemoryPackable]
    public partial class TransformData
    {
        public bool HasPosition { get; set; }
        public bool HasRotation { get; set; }
        public bool HasScale { get; set; }
        public bool SerializeLocalSpace { get; set; }
        public bool UseCompressedRotation { get; set; }
        public long Timestamp { get; set; }
        
        public SerializableVector3 Position { get; set; }
        public SerializableQuaternion Rotation { get; set; }
        public SerializableVector3 Scale { get; set; }
        public CompressedQuaternion CompressedRotation { get; set; }
    }
}