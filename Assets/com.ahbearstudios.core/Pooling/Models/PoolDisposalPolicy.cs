namespace AhBearStudios.Core.Pooling.Models;

/// <summary>
/// Disposal policy for pooled objects.
/// </summary>
public enum PoolDisposalPolicy
{
    /// <summary>
    /// Return the object to the pool for reuse.
    /// </summary>
    ReturnToPool,

    /// <summary>
    /// Dispose the object immediately.
    /// </summary>
    DisposeOnReturn,

    /// <summary>
    /// Let the pool decide based on capacity and usage.
    /// </summary>
    PoolDecision
}