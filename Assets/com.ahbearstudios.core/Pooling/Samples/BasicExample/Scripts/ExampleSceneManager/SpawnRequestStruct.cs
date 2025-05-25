using UnityEngine;

/// <summary>
/// Request for spawning an object with position and rotation
/// </summary>
public struct SpawnRequest
{
    /// <summary>
    /// Type of object to spawn
    /// </summary>
    public ObjectType Type;
    
    /// <summary>
    /// Position where the object should be spawned
    /// </summary>
    public Vector3 Position;
    
    /// <summary>
    /// Rotation of the spawned object
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// Creates a new spawn request
    /// </summary>
    /// <param name="type">Type of object to spawn</param>
    /// <param name="position">Position where the object should be spawned</param>
    /// <param name="rotation">Rotation of the spawned object</param>
    public SpawnRequest(ObjectType type, Vector3 position, Quaternion rotation)
    {
        Type = type;
        Position = position;
        Rotation = rotation;
    }
}