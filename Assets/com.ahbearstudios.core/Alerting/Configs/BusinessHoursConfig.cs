using System;
using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration for business hours-based suppression logic.
    /// Defines when business hours occur and different suppression rules for business vs. after hours.
    /// </summary>
    public sealed record BusinessHoursConfig
    {
        /// <summary>
        /// Gets the time zone used for business hours calculations.
        /// </summary>
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Local;

        /// <summary>
        /// Gets the start time for business hours as TimeSpan from midnight (e.g., 9:00 AM = 9 hours).
        /// </summary>
        public TimeSpan StartTime { get; init; } = TimeSpan.FromHours(9); // 9:00 AM

        /// <summary>
        /// Gets the end time for business hours as TimeSpan from midnight (e.g., 5:00 PM = 17 hours).
        /// </summary>
        public TimeSpan EndTime { get; init; } = TimeSpan.FromHours(17); // 5:00 PM

        /// <summary>
        /// Gets the days of the week considered business days.
        /// </summary>
        public IReadOnlyList<DayOfWeek> WorkDays { get; init; } = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        };

        /// <summary>
        /// Gets the minimum severity level for alerts during business hours.
        /// Alerts below this level are suppressed during business hours.
        /// </summary>
        public AlertSeverity BusinessHoursMinimumSeverity { get; init; } = AlertSeverity.Warning;

        /// <summary>
        /// Gets the minimum severity level for alerts after business hours.
        /// Typically set higher than business hours to reduce after-hours noise.
        /// </summary>
        public AlertSeverity AfterHoursMinimumSeverity { get; init; } = AlertSeverity.Critical;

        /// <summary>
        /// Gets the collection of holidays when business hours rules don't apply.
        /// Holidays are treated as non-business days regardless of day of week.
        /// </summary>
        public IReadOnlyList<DateTime> Holidays { get; init; } = Array.Empty<DateTime>();

        /// <summary>
        /// Determines whether the specified timestamp falls within business hours.
        /// </summary>
        /// <param name="timestamp">The timestamp to evaluate.</param>
        /// <returns>True if the timestamp is during business hours; otherwise, false.</returns>
        public bool IsBusinessHours(DateTime timestamp)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(timestamp, TimeZone);
            var dateOnly = localTime.Date;
            var timeOfDay = localTime.TimeOfDay;

            // Check if it's a holiday
            if (Holidays.AsValueEnumerable().Any(holiday => holiday.Date == dateOnly))
                return false;

            // Check if it's a work day
            if (!WorkDays.AsValueEnumerable().Contains(localTime.DayOfWeek))
                return false;

            // Check if it's within business hours
            return timeOfDay >= StartTime && timeOfDay <= EndTime;
        }

        /// <summary>
        /// Gets the appropriate minimum severity for the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp to evaluate.</param>
        /// <returns>The minimum severity level that should be applied.</returns>
        public AlertSeverity GetMinimumSeverity(DateTime timestamp)
        {
            return IsBusinessHours(timestamp) ? BusinessHoursMinimumSeverity : AfterHoursMinimumSeverity;
        }

        /// <summary>
        /// Validates the business hours configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (TimeZone == null)
                throw new InvalidOperationException("Time zone cannot be null.");

            if (EndTime <= StartTime)
                throw new InvalidOperationException("End time must be after start time.");

            if (WorkDays.Count == 0)
                throw new InvalidOperationException("At least one work day must be specified.");

            // Check for duplicates by comparing count with distinct count
            var distinctCount = 0;
            var seenDays = new bool[7]; // 7 days of week
            foreach (var day in WorkDays)
            {
                var dayIndex = (int)day;
                if (seenDays[dayIndex])
                    throw new InvalidOperationException("Work days cannot contain duplicates.");
                seenDays[dayIndex] = true;
                distinctCount++;
            }
            
            if (distinctCount != WorkDays.Count)
                throw new InvalidOperationException("Work days cannot contain duplicates.");
        }

        /// <summary>
        /// Gets the default business hours configuration (9 AM - 5 PM, Monday-Friday, local time).
        /// </summary>
        public static BusinessHoursConfig Default => new()
        {
            TimeZone = TimeZoneInfo.Local,
            StartTime = TimeSpan.FromHours(9), // 9:00 AM
            EndTime = TimeSpan.FromHours(17), // 5:00 PM
            WorkDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            BusinessHoursMinimumSeverity = AlertSeverity.Warning,
            AfterHoursMinimumSeverity = AlertSeverity.Critical,
            Holidays = Array.Empty<DateTime>()
        };
    }
}