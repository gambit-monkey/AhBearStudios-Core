using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// Compressed quaternion representation using smallest-three encoding.
    /// Reduces quaternion size from 16 bytes to 7 bytes with minimal precision loss.
    /// </summary>
    [MemoryPackable]
    public partial struct CompressedQuaternion
    {
        public byte LargestIndex;  // Which component is largest (0=x, 1=y, 2=z, 3=w)
        public short A;            // First compressed component
        public short B;            // Second compressed component
        public short C;            // Third compressed component
    }
}