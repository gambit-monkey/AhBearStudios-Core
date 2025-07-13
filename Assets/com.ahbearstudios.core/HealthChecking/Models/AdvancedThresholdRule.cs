using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Advanced threshold rule for complex health evaluation scenarios
/// </summary>
public sealed record AdvancedThresholdRule
{
    /// <summary>
    /// Unique identifier for this rule
    /// </summary>
    public FixedString64Bytes Id { get; init; } = GenerateId();

    /// <summary>
    /// Name of this threshold rule
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Priority of this rule (higher numbers execute first)
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Condition expression for when this rule applies
    /// </summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>
    /// Custom thresholds to apply when condition is met
    /// </summary>
    public HealthThresholds CustomThresholds { get; init; }

    /// <summary>
    /// Whether this rule is currently enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Time window for evaluating this rule
    /// </summary>
    public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Custom metadata for this rule
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Validates the advanced threshold rule
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Advanced threshold rule Name cannot be null or empty");

        if (string.IsNullOrWhiteSpace(Condition))
            errors.Add("Advanced threshold rule Condition cannot be null or empty");

        if (EvaluationWindow <= TimeSpan.Zero)
            errors.Add("Advanced threshold rule EvaluationWindow must be greater than zero");

        if (CustomThresholds != null)
        {
            errors.AddRange(CustomThresholds.Validate());
        }

        return errors;
    }

    /// <summary>
    /// Generates a unique identifier for rules
    /// </summary>
    /// <returns>Unique rule ID</returns>
    private static FixedString64Bytes GenerateId()
    {
        return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
    }
}