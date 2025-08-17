using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Time-based alert filter that allows alerts during specific time ranges.
    /// </summary>
    internal sealed class TimeBasedAlertFilter : BaseAlertFilter
    {
        private readonly List<TimeRange> _allowedTimeRanges;
        private readonly TimeZoneInfo _timezone;
        private readonly FixedString64Bytes _name;

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => _name;

        public TimeBasedAlertFilter(IMessageBusService messageBusService, string name = "TimeBasedFilter", IEnumerable<TimeRange> timeRanges = null, TimeZoneInfo timezone = null) : base(messageBusService)
        {
            _name = new FixedString64Bytes(name);
            _allowedTimeRanges = timeRanges?.AsValueEnumerable().ToList() ?? new List<TimeRange> { TimeRange.Always() };
            _timezone = timezone ?? TimeZoneInfo.Utc;
        }

        protected override bool CanHandleCore(Alert alert) => alert != null;

        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timezone);
            
            foreach (var timeRange in _allowedTimeRanges)
            {
                if (timeRange.Contains(currentTime))
                {
                    return FilterResult.Allow($"Within allowed time range");
                }
            }

            return FilterResult.Suppress("Outside allowed time ranges");
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            if (configuration == null) return true;

            // Configuration could include time range updates, timezone changes, etc.
            // For now, basic implementation that accepts any configuration
            return true;
        }

        /// <summary>
        /// Core implementation of configuration validation.
        /// </summary>
        protected override FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration)
        {
            return FilterValidationResult.Valid();
        }
    }
}