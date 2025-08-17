using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Content-based alert filter that matches message patterns.
    /// </summary>
    internal sealed class ContentAlertFilter : BaseAlertFilter
    {
        private readonly List<string> _patterns;
        private readonly FixedString64Bytes _name;

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => _name;

        public ContentAlertFilter(IMessageBusService messageBusService, string name = "ContentFilter", IEnumerable<string> patterns = null) : base(messageBusService)
        {
            _name = new FixedString64Bytes(name);
            _patterns = patterns?.AsValueEnumerable().ToList() ?? new List<string>();
        }

        protected override bool CanHandleCore(Alert alert) => alert != null;

        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            var message = alert.Message.ToString();
            
            foreach (var pattern in _patterns)
            {
                if (MatchesPattern(message, pattern))
                {
                    return FilterResult.Suppress($"Content matched pattern: {pattern}");
                }
            }

            return FilterResult.Allow("No content patterns matched");
        }

        private static bool MatchesPattern(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
                return true;

            if (!pattern.Contains('*'))
                return text.Contains(pattern, StringComparison.OrdinalIgnoreCase);

            var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return true;

            var currentIndex = 0;
            foreach (var part in parts)
            {
                var index = text.IndexOf(part, currentIndex, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return false;
                currentIndex = index + part.Length;
            }

            return true;
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            if (configuration == null) return true;

            if (configuration.TryGetValue("Patterns", out var patternsObj) && patternsObj is IEnumerable<string> patterns)
            {
                _patterns.Clear();
                _patterns.AddRange(patterns);
            }

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