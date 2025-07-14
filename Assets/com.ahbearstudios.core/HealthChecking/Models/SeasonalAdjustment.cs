using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Seasonal adjustment configuration
/// </summary>
public sealed record SeasonalAdjustment : IValidatable
{
    public string Name { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double FrequencyMultiplier { get; init; } = 1.0;
    public bool IsYearlyRecurring { get; init; } = true;

    public List<string> Validate()
    {
        var errors = new List<string>();
            
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name cannot be empty");
            
        if (EndDate <= StartDate)
            errors.Add("EndDate must be after StartDate");
            
        if (FrequencyMultiplier <= 0.0)
            errors.Add("FrequencyMultiplier must be greater than zero");
            
        return errors;
    }
}