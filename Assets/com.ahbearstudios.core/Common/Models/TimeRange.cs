using System;
using System.Linq;
using ZLinq;

namespace AhBearStudios.Core.Common.Models
{
    /// <summary>
    /// Represents a time range for filtering and scheduling operations.
    /// Supports both DateTime-based ranges and TimeSpan-based recurring patterns.
    /// </summary>
    public readonly record struct TimeRange
    {
        /// <summary>
        /// Gets the start time of the range.
        /// </summary>
        public DateTime StartTime { get; init; }

        /// <summary>
        /// Gets the end time of the range.
        /// </summary>
        public DateTime EndTime { get; init; }

        /// <summary>
        /// Gets the recurring start time (time of day as TimeSpan).
        /// </summary>
        public TimeSpan? RecurringStartTime { get; init; }

        /// <summary>
        /// Gets the recurring end time (time of day as TimeSpan).
        /// </summary>
        public TimeSpan? RecurringEndTime { get; init; }

        /// <summary>
        /// Gets the days of the week this range applies to (for recurring ranges).
        /// </summary>
        public DayOfWeek[] DaysOfWeek { get; init; }

        /// <summary>
        /// Gets whether the time range is valid.
        /// </summary>
        public bool IsValid => StartTime <= EndTime;

        /// <summary>
        /// Gets the duration of the time range.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Gets whether this is a recurring time range.
        /// </summary>
        public bool IsRecurring => RecurringStartTime.HasValue && RecurringEndTime.HasValue;

        /// <summary>
        /// Gets whether this recurring range spans midnight.
        /// </summary>
        public bool SpansMidnight => IsRecurring && RecurringEndTime < RecurringStartTime;

        /// <summary>
        /// Initializes a new instance of the TimeRange struct for a specific date/time range.
        /// </summary>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        public TimeRange(DateTime startTime, DateTime endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
            RecurringStartTime = null;
            RecurringEndTime = null;
            DaysOfWeek = new DayOfWeek[0];
        }

        /// <summary>
        /// Initializes a new instance of the TimeRange struct for recurring time patterns.
        /// </summary>
        /// <param name="startTime">Start time of day as TimeSpan</param>
        /// <param name="endTime">End time of day as TimeSpan</param>
        /// <param name="daysOfWeek">Applicable days of week</param>
        public TimeRange(TimeSpan startTime, TimeSpan endTime, params DayOfWeek[] daysOfWeek)
        {
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MaxValue;
            RecurringStartTime = startTime;
            RecurringEndTime = endTime;
            DaysOfWeek = daysOfWeek ?? new DayOfWeek[0];
        }

        /// <summary>
        /// Determines if the given time is within this range.
        /// </summary>
        /// <param name="time">The time to check</param>
        /// <returns>True if the time is within the range</returns>
        public bool Contains(DateTime time)
        {
            if (IsRecurring)
            {
                return ContainsRecurring(time);
            }
            
            return time >= StartTime && time <= EndTime;
        }

        /// <summary>
        /// Checks if a DateTime falls within the recurring time range.
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>True if within recurring range, false otherwise</returns>
        private bool ContainsRecurring(DateTime dateTime)
        {
            if (!RecurringStartTime.HasValue || !RecurringEndTime.HasValue)
                return false;

            var timeOfDay = dateTime.TimeOfDay;
            var dayOfWeek = dateTime.DayOfWeek;

            // Check if day matches
            if (DaysOfWeek.Length > 0 && !DaysOfWeek.AsValueEnumerable().Contains(dayOfWeek))
                return false;

            // Check if time matches
            if (SpansMidnight)
            {
                return timeOfDay >= RecurringStartTime.Value || timeOfDay <= RecurringEndTime.Value;
            }
            else
            {
                return timeOfDay >= RecurringStartTime.Value && timeOfDay <= RecurringEndTime.Value;
            }
        }

        /// <summary>
        /// Creates a 24/7 time range (always active).
        /// </summary>
        /// <returns>Always-active time range</returns>
        public static TimeRange Always()
        {
            return new TimeRange(
                TimeSpan.Zero,
                new TimeSpan(23, 59, 59),
                (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)));
        }

        /// <summary>
        /// Creates a business hours time range (9 AM to 5 PM, Monday to Friday).
        /// </summary>
        /// <returns>Business hours time range</returns>
        public static TimeRange BusinessHours()
        {
            return new TimeRange(
                new TimeSpan(9, 0, 0),
                new TimeSpan(17, 0, 0),
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday);
        }

        /// <summary>
        /// Creates a weekend time range.
        /// </summary>
        /// <returns>Weekend time range</returns>
        public static TimeRange Weekend()
        {
            return new TimeRange(
                TimeSpan.Zero,
                new TimeSpan(23, 59, 59),
                DayOfWeek.Saturday,
                DayOfWeek.Sunday);
        }

        /// <summary>
        /// Creates a time range for today.
        /// </summary>
        /// <returns>Time range for today</returns>
        public static TimeRange Today()
        {
            var now = DateTime.Now;
            var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Local);
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
            return new TimeRange(startOfDay, endOfDay);
        }

        /// <summary>
        /// Creates a time range for the last N hours.
        /// </summary>
        /// <param name="hours">Number of hours</param>
        /// <returns>Time range for the last N hours</returns>
        public static TimeRange LastHours(int hours)
        {
            var now = DateTime.Now;
            return new TimeRange(now.AddHours(-hours), now);
        }

        /// <summary>
        /// Creates a time range for the last N days.
        /// </summary>
        /// <param name="days">Number of days</param>
        /// <returns>Time range for the last N days</returns>
        public static TimeRange LastDays(int days)
        {
            var now = DateTime.Now;
            return new TimeRange(now.AddDays(-days), now);
        }

        /// <summary>
        /// Creates a recurring time range from hour and minute values.
        /// </summary>
        /// <param name="startHour">Start hour (0-23)</param>
        /// <param name="startMinute">Start minute (0-59)</param>
        /// <param name="endHour">End hour (0-23)</param>
        /// <param name="endMinute">End minute (0-59)</param>
        /// <param name="daysOfWeek">Applicable days of week</param>
        /// <returns>Recurring time range</returns>
        public static TimeRange FromHours(int startHour, int startMinute, int endHour, int endMinute, params DayOfWeek[] daysOfWeek)
        {
            return new TimeRange(
                new TimeSpan(startHour, startMinute, 0),
                new TimeSpan(endHour, endMinute, 0),
                daysOfWeek);
        }
    }
}