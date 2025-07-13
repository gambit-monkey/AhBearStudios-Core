namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Reasons for subscription disposal for diagnostic and monitoring purposes.
/// </summary>
public enum DisposalReason : byte
{
    /// <summary>
    /// Subscription was explicitly disposed by caller.
    /// </summary>
    Explicit = 0,

    /// <summary>
    /// Subscription was disposed during scope cleanup.
    /// </summary>
    ScopeCleanup = 1,

    /// <summary>
    /// Subscription was disposed during service shutdown.
    /// </summary>
    ServiceShutdown = 2,

    /// <summary>
    /// Subscription was disposed due to an error condition.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Subscription was disposed due to timeout.
    /// </summary>
    Timeout = 4,

    /// <summary>
    /// Subscription was disposed due to resource limits.
    /// </summary>
    ResourceLimit = 5,

    /// <summary>
    /// Subscription was disposed for unknown reasons.
    /// </summary>
    Unknown = 255
}