namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Categorizes different types of subscriptions for monitoring and management.
/// </summary>
public enum SubscriptionCategory : byte
{
    /// <summary>
    /// Standard synchronous subscription.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Asynchronous subscription.
    /// </summary>
    Async = 1,

    /// <summary>
    /// Subscription with filtering conditions.
    /// </summary>
    Filtered = 2,

    /// <summary>
    /// Asynchronous subscription with filtering conditions.
    /// </summary>
    AsyncFiltered = 3,

    /// <summary>
    /// Subscription with priority filtering.
    /// </summary>
    Priority = 4,

    /// <summary>
    /// Subscription with source filtering.
    /// </summary>
    Source = 5,

    /// <summary>
    /// Subscription with correlation ID filtering.
    /// </summary>
    Correlation = 6,

    /// <summary>
    /// Subscription with custom error handling.
    /// </summary>
    ErrorHandling = 7
}