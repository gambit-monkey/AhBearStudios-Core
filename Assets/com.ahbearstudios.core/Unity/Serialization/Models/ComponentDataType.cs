namespace AhBearStudios.Unity.Serialization.Models
{
    /// <summary>
    /// Enum to identify which type of component data is stored.
    /// </summary>
    public enum ComponentDataType
    {
        None = 0,
        Renderer = 1,
        Collider = 2,
        Rigidbody = 3,
        MonoBehaviour = 4,
        Other = 5
    }
}