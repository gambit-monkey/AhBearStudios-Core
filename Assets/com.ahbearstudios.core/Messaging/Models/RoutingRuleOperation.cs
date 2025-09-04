namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Operations performed on routing rules.
/// </summary>
public enum RoutingRuleOperation
{
    /// <summary>
    /// Rule was added.
    /// </summary>
    Added,

    /// <summary>
    /// Rule was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// Rule was modified.
    /// </summary>
    Modified
}