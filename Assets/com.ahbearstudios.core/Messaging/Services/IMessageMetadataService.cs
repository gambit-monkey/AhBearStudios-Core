using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Service interface for MessageMetadata business logic and operations.
/// Provides validation, querying, and utility methods for metadata.
/// </summary>
public interface IMessageMetadataService
{
    /// <summary>
    /// Validates the metadata for consistency and correctness.
    /// </summary>
    /// <param name="metadata">The metadata to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValid(MessageMetadata metadata);

    /// <summary>
    /// Checks if a message has expired based on its metadata.
    /// </summary>
    /// <param name="metadata">The metadata to check</param>
    /// <returns>True if expired, false otherwise</returns>
    bool IsExpired(MessageMetadata metadata);

    /// <summary>
    /// Checks if a message is ready for delivery considering delivery delay.
    /// </summary>
    /// <param name="metadata">The metadata to check</param>
    /// <returns>True if ready for delivery, false otherwise</returns>
    bool IsReadyForDelivery(MessageMetadata metadata);

    /// <summary>
    /// Gets a custom property value by key.
    /// </summary>
    /// <typeparam name="T">The expected value type</typeparam>
    /// <param name="metadata">The metadata containing the property</param>
    /// <param name="key">The property key</param>
    /// <returns>The property value, or default if not found</returns>
    T GetCustomProperty<T>(MessageMetadata metadata, string key);

    /// <summary>
    /// Gets a custom header value by key.
    /// </summary>
    /// <param name="metadata">The metadata containing the header</param>
    /// <param name="key">The header key</param>
    /// <returns>The header value, or null if not found</returns>
    string GetCustomHeader(MessageMetadata metadata, string key);


    /// <summary>
    /// Checks if the metadata has any custom properties.
    /// </summary>
    /// <param name="metadata">The metadata to check</param>
    /// <returns>True if has custom properties, false otherwise</returns>
    bool HasCustomProperties(MessageMetadata metadata);

    /// <summary>
    /// Checks if the metadata has any custom headers.
    /// </summary>
    /// <param name="metadata">The metadata to check</param>
    /// <returns>True if has custom headers, false otherwise</returns>
    bool HasCustomHeaders(MessageMetadata metadata);

    /// <summary>
    /// Estimates the total size in bytes for this metadata.
    /// </summary>
    /// <param name="metadata">The metadata to estimate</param>
    /// <returns>Estimated size in bytes</returns>
    int EstimateSize(MessageMetadata metadata);

    /// <summary>
    /// Creates a summary string of the metadata for logging and debugging.
    /// </summary>
    /// <param name="metadata">The metadata to summarize</param>
    /// <returns>Formatted summary string</returns>
    string ToSummary(MessageMetadata metadata);

    /// <summary>
    /// Gets the DateTime representation of the creation timestamp.
    /// </summary>
    /// <param name="metadata">The metadata containing the timestamp</param>
    /// <returns>The creation DateTime in UTC</returns>
    DateTime GetCreatedAt(MessageMetadata metadata);

    /// <summary>
    /// Gets the DateTime representation of the expiration timestamp.
    /// </summary>
    /// <param name="metadata">The metadata containing the timestamp</param>
    /// <returns>The expiration DateTime in UTC, or null if no expiration</returns>
    DateTime? GetExpiresAt(MessageMetadata metadata);
}