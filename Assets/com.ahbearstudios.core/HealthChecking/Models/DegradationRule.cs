using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
    /// Custom degradation rule for complex scenarios
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