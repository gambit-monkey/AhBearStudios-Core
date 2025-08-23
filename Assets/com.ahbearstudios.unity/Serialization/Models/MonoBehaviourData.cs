using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing MonoBehaviour component data.
    /// </summary>
    [MemoryPackable]
    public partial class MonoBehaviourData
    {
        public bool Enabled { get; set; }
        public bool HasCustomData { get; set; }
        public byte[] CustomDataBytes { get; set; } // Store custom data as serialized bytes
        public string CustomDataTypeName { get; set; } // Store the type name for deserialization
    }
}