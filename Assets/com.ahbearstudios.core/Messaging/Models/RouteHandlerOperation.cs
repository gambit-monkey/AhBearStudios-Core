namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Operations performed on route handlers.
/// </summary>
public enum RouteHandlerOperation
{
    /// <summary>
    /// Handler was registered.
    /// </summary>
    Registered,

    /// <summary>
    /// Handler was unregistered.
    /// </summary>
    Unregistered
}