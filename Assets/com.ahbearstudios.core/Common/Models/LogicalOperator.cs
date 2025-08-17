namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Logical operators for combining evaluation results.
/// Used across multiple systems for consistent logical operations.
/// </summary>
public enum LogicalOperator : byte
{
    /// <summary>
    /// All conditions must pass (logical AND).
    /// </summary>
    And = 0,

    /// <summary>
    /// At least one condition must pass (logical OR).
    /// </summary>
    Or = 1,

    /// <summary>
    /// Exactly one condition must pass (logical XOR).
    /// </summary>
    Xor = 2,

    /// <summary>
    /// No conditions must pass (logical NOT).
    /// </summary>
    Not = 3
}