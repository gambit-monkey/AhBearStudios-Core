namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Actions that can be taken by content filters when patterns match.
/// Used across multiple systems for consistent filter behavior.
/// </summary>
public enum FilterAction : byte
{
    /// <summary>
    /// Allow the alert to pass through (no action taken).
    /// </summary>
    Allow = 0,

    /// <summary>
    /// Suppress the alert (block it from further processing).
    /// </summary>
    Suppress = 1,

    /// <summary>
    /// Modify the alert content before passing it through.
    /// </summary>
    Modify = 2,

    /// <summary>
    /// Flag the alert for special attention.
    /// </summary>
    Flag = 3
}