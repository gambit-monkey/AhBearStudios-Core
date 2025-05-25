/// <summary>
/// Enumeration of available pool types for the example scene
/// </summary>
public enum PoolType
{
    /// <summary>
    /// Standard GameObject pool
    /// </summary>
    Standard,
    
    /// <summary>
    /// Thread-safe pool implementation
    /// </summary>
    ThreadSafe,
    
    /// <summary>
    /// Native pool using Unity Collections v2
    /// </summary>
    Native,
    
    /// <summary>
    /// Burst-compatible native pool
    /// </summary>
    BurstCompatible,
    
    /// <summary>
    /// Job-compatible native pool
    /// </summary>
    JobCompatible
}