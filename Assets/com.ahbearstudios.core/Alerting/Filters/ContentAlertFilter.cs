using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Content-based alert filter that matches message patterns.
    /// </summary>
    internal sealed class ContentAlertFilter : BaseAlertFilter
    {
        private readonly List<string> _patterns;

        public ContentAlertFilter(string name, IEnumerable<string> patterns) : base(name)
        {
            _patterns = patterns?.ZToList() ?? new List<string>();
        }

        public override bool CanHandle(Alert alert) => alert != null;

        public override FilterResult Evaluate(Alert alert, FilterContext context)
        {
            var message = alert.Message.ToString();
            
            foreach (var pattern in _patterns)
            {
                if (MatchesPattern(message, pattern))
                {
                    return FilterResult.Suppress(alert, $"Content matched pattern: {pattern}");
                }
            }

            return FilterResult.Allow(alert, "No content patterns matched");
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
    }
}