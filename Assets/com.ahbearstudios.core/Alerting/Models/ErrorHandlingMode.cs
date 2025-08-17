namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Error handling modes for filters.
/// </summary>
public enum ErrorHandlingMode : byte
{
    /// <summary>
    /// Allow all alerts on error.
    /// </summary>
    AllowOnError = 0,

    /// <summary>
    /// Suppress all alerts on error.
    /// </summary>
    SuppressOnError = 1,

    /// <summary>
    /// Log error and continue processing.
    /// </summary>
    LogAndContinue = 2,

    /// <summary>
    /// Disable filter on error.
    /// </summary>
    DisableOnError = 3
}