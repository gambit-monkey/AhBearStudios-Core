using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Composite alert filter that combines multiple child filters.
    /// </summary>
    internal sealed class CompositeAlertFilter : BaseAlertFilter
    {
        private readonly List<IAlertFilter> _childFilters;
        private readonly LogicalOperator _logicalOperator;

        public CompositeAlertFilter(string name, IEnumerable<IAlertFilter> childFilters, LogicalOperator logicalOperator) : base(name)
        {
            _childFilters = childFilters?.AsValueEnumerable().ToList() ?? new List<IAlertFilter>();
            _logicalOperator = logicalOperator;
        }

        public override bool CanHandle(Alert alert) => alert != null && _childFilters.AsValueEnumerable().Any();

        public override FilterResult Evaluate(Alert alert, FilterContext context)
        {
            var results = new List<FilterResult>();
            
            foreach (var childFilter in _childFilters)
            {
                if (!childFilter.IsEnabled || !childFilter.CanHandle(alert))
                    continue;

                var result = childFilter.Evaluate(alert, context);
                results.Add(result);
            }

            if (results.Count == 0)
                return FilterResult.Allow(alert, "No applicable child filters");

            return _logicalOperator switch
            {
                LogicalOperator.And => EvaluateAnd(alert, results),
                LogicalOperator.Or => EvaluateOr(alert, results),
                LogicalOperator.Xor => EvaluateXor(alert, results),
                LogicalOperator.Not => EvaluateNot(alert, results),
                _ => FilterResult.Allow(alert, "Unknown logical operator")
            };
        }

        private FilterResult EvaluateAnd(Alert alert, List<FilterResult> results)
        {
            return results.AsValueEnumerable().All(r => r.Decision == FilterDecision.Allow)
                ? FilterResult.Allow(alert, "All child filters allowed")
                : FilterResult.Suppress(alert, "One or more child filters suppressed");
        }

        private FilterResult EvaluateOr(Alert alert, List<FilterResult> results)
        {
            return results.AsValueEnumerable().Any(r => r.Decision == FilterDecision.Allow)
                ? FilterResult.Allow(alert, "At least one child filter allowed")
                : FilterResult.Suppress(alert, "All child filters suppressed");
        }

        private FilterResult EvaluateXor(Alert alert, List<FilterResult> results)
        {
            var allowCount = results.AsValueEnumerable().Count(r => r.Decision == FilterDecision.Allow);
            return allowCount == 1
                ? FilterResult.Allow(alert, "Exactly one child filter allowed")
                : FilterResult.Suppress(alert, $"{allowCount} child filters allowed (expected 1)");
        }

        private FilterResult EvaluateNot(Alert alert, List<FilterResult> results)
        {
            return results.AsValueEnumerable().All(r => r.Decision != FilterDecision.Allow)
                ? FilterResult.Allow(alert, "All child filters were suppressed (NOT)")
                : FilterResult.Suppress(alert, "One or more child filters allowed (NOT)");
        }
    }
}