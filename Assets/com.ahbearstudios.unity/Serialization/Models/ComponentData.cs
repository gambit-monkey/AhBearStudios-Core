using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// MemoryPack-compatible structure for serializing component data.
    /// </summary>
    [MemoryPackable]
    public partial class ComponentData
    {
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        public bool IsEnabled { get; set; }
        
        // Union approach for different component data types
        public ComponentDataType DataType { get; set; }
        public RendererData RendererData { get; set; }
        public ColliderData ColliderData { get; set; }
        public RigidbodyData RigidbodyData { get; set; }
        public MonoBehaviourData MonoBehaviourData { get; set; }
    }
}