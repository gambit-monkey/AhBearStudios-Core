namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// Statistics about GameObject serialization.
    /// </summary>
    public struct GameObjectSerializationStats
    {
        public int ComponentCount;
        public int ChildrenCount;
        public int TotalDepth;
        public int EstimatedSizeBytes;
    }
}