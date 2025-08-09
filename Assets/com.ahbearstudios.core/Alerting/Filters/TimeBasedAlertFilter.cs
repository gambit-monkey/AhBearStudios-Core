using System;
using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Time-based alert filter that allows alerts during specific time ranges.
    /// </summary>
    internal sealed class TimeBasedAlertFilter : BaseAlertFilter
    {
        private readonly List<TimeRange> _allowedTimeRanges;
        private readonly TimeZoneInfo _timezone;

        public TimeBasedAlertFilter(string name, IEnumerable<TimeRange> timeRanges, TimeZoneInfo timezone = null) : base(name)
        {
            _allowedTimeRanges = timeRanges?.ZToList() ?? new List<TimeRange> { TimeRange.Always() };
            _timezone = timezone ?? TimeZoneInfo.Utc;
        }

        public override bool CanHandle(Alert alert) => alert != null;

        public override FilterResult Evaluate(Alert alert, FilterContext context)
        {
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timezone);
            
            foreach (var timeRange in _allowedTimeRanges)
            {
                if (timeRange.Contains(currentTime))
                {
                    return FilterResult.Allow(alert, $"Within allowed time range");
                }
            }

            return FilterResult.Suppress(alert, "Outside allowed time ranges");
        }
    }
}