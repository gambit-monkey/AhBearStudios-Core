using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Blackout period configuration
/// </summary>
public sealed record BlackoutPeriod : IValidatable
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsRecurring { get; init; } = false;
    public TimeSpan RecurrenceInterval { get; init; } = TimeSpan.FromDays(1);

    public bool Contains(DateTime time)
    {
        return time >= Start && time <= End;
    }

    public List<string> Validate()
    {
        var errors = new List<string>();
            
        if (End <= Start)
            errors.Add("End time must be after start time");
            
        if (IsRecurring && RecurrenceInterval <= TimeSpan.Zero)
            errors.Add("RecurrenceInterval must be greater than zero for recurring blackouts");
            
        return errors;
    }
}