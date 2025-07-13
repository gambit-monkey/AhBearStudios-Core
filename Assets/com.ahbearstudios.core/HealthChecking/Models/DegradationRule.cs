using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Unified degradation rule for complex scenarios and system-specific logic
/// </summary>
public sealed record DegradationRule
{
    /// <summary>
    /// Unique identifier for this rule
    /// </summary>
    public FixedString64Bytes Id { get; init; } = GenerateId();

    /// <summary>
    /// Name of this degradation rule
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Rule priority (higher numbers execute first)
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Health check patterns that trigger this rule
    /// </summary>
    public List<string> HealthCheckPatterns { get; init; } = new();

    /// <summary>
    /// Required health check states to trigger this rule
    /// </summary>
    public Dictionary<FixedString64Bytes, HealthStatus> RequiredStates { get; init; } = new();

    /// <summary>
    /// Degradation level to apply when rule is triggered
    /// </summary>
    public DegradationLevel TargetDegradationLevel { get; init; } = DegradationLevel.Minor;

    /// <summary>
    /// Time window for evaluating this rule
    /// </summary>
    public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Custom condition expression for complex logic
    /// </summary>
    public string ConditionExpression { get; init; } = string.Empty;

    /// <summary>
    /// Whether this rule is currently enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Custom actions to execute when rule is triggered
    /// </summary>
    public List<string> Actions { get; init; } = new();

    /// <summary>
    /// System type for system-specific logic
    /// </summary>
    public string SystemType { get; init; } = string.Empty;

    /// <summary>
    /// Threshold for minor degradation level
    /// </summary>
    public double MinorThreshold { get; init; } = 0.1;

    /// <summary>
    /// Threshold for moderate degradation level
    /// </summary>
    public double ModerateThreshold { get; init; } = 0.25;

    /// <summary>
    /// Threshold for severe degradation level
    /// </summary>
    public double SevereThreshold { get; init; } = 0.5;

    /// <summary>
    /// Threshold for disabled degradation level
    /// </summary>
    public double DisabledThreshold { get; init; } = 0.75;

    /// <summary>
    /// Custom logic function for calculating degradation level
    /// </summary>
    public Func<DegradationLevel, DegradationLevel>? CustomLogic { get; init; }

    /// <summary>
    /// Calculates the degradation level using custom logic if available
    /// </summary>
    /// <param name="overallLevel">The overall degradation level</param>
    /// <returns>The calculated degradation level</returns>
    public DegradationLevel CalculateDegradationLevel(DegradationLevel overallLevel)
    {
        return CustomLogic?.Invoke(overallLevel) ?? overallLevel;
    }

    /// <summary>
    /// Validates degradation rule
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("DegradationRule Name cannot be null or empty");

        if (!Enum.IsDefined(typeof(DegradationLevel), TargetDegradationLevel))
            errors.Add($"Invalid target degradation level: {TargetDegradationLevel}");

        if (EvaluationWindow <= TimeSpan.Zero)
            errors.Add("DegradationRule EvaluationWindow must be greater than zero");

        if (HealthCheckPatterns.Count == 0 && RequiredStates.Count == 0 &&
            string.IsNullOrWhiteSpace(ConditionExpression))
            errors.Add("DegradationRule must specify at least one condition");

        // Validate threshold values
        if (MinorThreshold < 0 || MinorThreshold > 1)
            errors.Add("MinorThreshold must be between 0 and 1");

        if (ModerateThreshold < 0 || ModerateThreshold > 1)
            errors.Add("ModerateThreshold must be between 0 and 1");

        if (SevereThreshold < 0 || SevereThreshold > 1)
            errors.Add("SevereThreshold must be between 0 and 1");

        if (DisabledThreshold < 0 || DisabledThreshold > 1)
            errors.Add("DisabledThreshold must be between 0 and 1");

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