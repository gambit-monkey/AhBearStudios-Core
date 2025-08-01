using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing Renderer component data.
    /// </summary>
    [MemoryPackable]
    public partial class RendererData
    {
        public bool Enabled { get; set; }
        public int CastShadows { get; set; }
        public bool ReceiveShadows { get; set; }
        public int MaterialCount { get; set; }
    }
}