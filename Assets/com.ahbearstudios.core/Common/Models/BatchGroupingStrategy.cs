using System;

namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Defines strategies for grouping health checks into batches for execution.
/// Used across multiple systems for batch processing optimization.
/// Designed for Unity game development with performance considerations.
/// </summary>
public enum BatchGroupingStrategy : byte
{
    /// <summary>
    /// Group by priority level - high priority checks execute together
    /// </summary>
    Priority = 0,

    /// <summary>
    /// Group by system type - all database checks together, all network checks together, etc.
    /// </summary>
    SystemType = 1,

    /// <summary>
    /// Group by expected execution time - fast checks together, slow checks together
    /// </summary>
    ExecutionTime = 2,

    /// <summary>
    /// Group by resource requirements - memory-intensive checks together, CPU-intensive together
    /// </summary>
    ResourceRequirement = 3,

    /// <summary>
    /// Group by dependencies - checks that depend on each other are grouped together
    /// </summary>
    Dependency = 4,

    /// <summary>
    /// No specific grouping strategy - fill batches as they come
    /// </summary>
    None = 5
}