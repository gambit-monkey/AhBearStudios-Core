﻿namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Defines the priority levels for message processing.
/// Higher values indicate higher priority.
/// </summary>
public enum MessagePriority : byte
{
    /// <summary>
    /// Low priority messages that can be processed when resources are available.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority messages for regular system operations.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority messages that should be processed quickly.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority messages that require immediate processing.
    /// </summary>
    Critical = 3
}