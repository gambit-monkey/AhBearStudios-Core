using System.Collections.Generic;
using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing complete GameObject data.
    /// </summary>
    [MemoryPackable]
    public partial class GameObjectData
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public int Layer { get; set; }
        public bool IsActive { get; set; }
        public long Timestamp { get; set; }

        public TransformData Transform { get; set; }
        public List<ComponentData> Components { get; set; }
        public List<GameObjectData> Children { get; set; }
    }
}