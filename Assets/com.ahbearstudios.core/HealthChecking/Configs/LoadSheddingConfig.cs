using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Load shedding configuration for managing system load during degradation
/// </summary>
public sealed record LoadSheddingConfig
{
    /// <summary>
    /// Whether load shedding is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Load shedding strategy to use
    /// </summary>
    public LoadSheddingStrategy Strategy { get; init; } = LoadSheddingStrategy.Priority;

    /// <summary>
    /// Percentage of load to shed at minor degradation
    /// </summary>
    public double MinorLoadShedding { get; init; } = 0.1;

    /// <summary>
    /// Percentage of load to shed at moderate degradation
    /// </summary>
    public double ModerateLoadShedding { get; init; } = 0.3;

    /// <summary>
    /// Percentage of load to shed at severe degradation
    /// </summary>
    public double SevereLoadShedding { get; init; } = 0.6;

    /// <summary>
    /// Request priorities for priority-based load shedding
    /// </summary>
    public Dictionary<string, int> RequestPriorities { get; init; } = new();

    /// <summary>
    /// User tiers for tier-based load shedding
    /// </summary>
    public Dictionary<string, int> UserTiers { get; init; } = new();

    /// <summary>
    /// Creates load shedding configuration for high availability systems
    /// </summary>
    /// <returns>High availability load shedding configuration</returns>
    public static LoadSheddingConfig ForHighAvailability()
    {
        return new LoadSheddingConfig
        {
            Enabled = true,
            Strategy = LoadSheddingStrategy.Priority,
            MinorLoadShedding = 0.05,
            ModerateLoadShedding = 0.2,
            SevereLoadShedding = 0.5,
            RequestPriorities = new Dictionary<string, int>
            {
                ["Critical"] = 1000,
                ["High"] = 800,
                ["Normal"] = 500,
                ["Low"] = 200,
                ["Background"] = 100
            }
        };
    }

    /// <summary>
    /// Validates load shedding configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(typeof(LoadSheddingStrategy), Strategy))
            errors.Add($"Invalid load shedding strategy: {Strategy}");

        if (MinorLoadShedding < 0.0 || MinorLoadShedding > 1.0)
            errors.Add("MinorLoadShedding must be between 0.0 and 1.0");

        if (ModerateLoadShedding < 0.0 || ModerateLoadShedding > 1.0)
            errors.Add("ModerateLoadShedding must be between 0.0 and 1.0");

        if (SevereLoadShedding < 0.0 || SevereLoadShedding > 1.0)
            errors.Add("SevereLoadShedding must be between 0.0 and 1.0");

        return errors;
    }
}