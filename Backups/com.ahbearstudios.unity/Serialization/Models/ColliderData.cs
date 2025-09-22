using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing Collider component data.
    /// </summary>
    [MemoryPackable]
    public partial class ColliderData
    {
        public bool Enabled { get; set; }
        public bool IsTrigger { get; set; }
        public string Material { get; set; }
        public SerializableBounds Bounds { get; set; }
    }
}