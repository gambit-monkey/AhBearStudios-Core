using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Time window for execution
/// </summary>
public sealed record TimeWindow : IValidatable
{
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public DayOfWeek? DayOfWeek { get; init; }
    public bool IsRecurring { get; init; } = true;

    public bool Contains(DateTime time)
    {
        var timeOfDay = time.TimeOfDay;
            
        if (DayOfWeek.HasValue && time.DayOfWeek != DayOfWeek.Value)
            return false;

        if (Start <= End)
            return timeOfDay >= Start && timeOfDay <= End;
        else
            return timeOfDay >= Start || timeOfDay <= End; // Spans midnight
    }

    public List<string> Validate()
    {
        var errors = new List<string>();
            
        if (Start < TimeSpan.Zero || Start >= TimeSpan.FromDays(1))
            errors.Add("Start time must be between 00:00:00 and 23:59:59");
            
        if (End < TimeSpan.Zero || End >= TimeSpan.FromDays(1))
            errors.Add("End time must be between 00:00:00 and 23:59:59");
            
        return errors;
    }
}