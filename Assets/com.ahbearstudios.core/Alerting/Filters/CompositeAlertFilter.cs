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
    /// Composite alert filter that combines multiple child filters.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    internal sealed class CompositeAlertFilter : BaseAlertFilter
    {
        private readonly FixedString64Bytes _name;
        private readonly List<IAlertFilter> _childFilters;
        private readonly LogicalOperator _logicalOperator;

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => _name;

        /// <summary>
        /// Initializes a new instance of the CompositeAlertFilter class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing filter events</param>
        /// <param name="name">The name of this filter</param>
        /// <param name="childFilters">Child filters to combine</param>
        /// <param name="logicalOperator">Logical operator for combining results</param>
        public CompositeAlertFilter(IMessageBusService messageBusService, string name = "CompositeFilter", IEnumerable<IAlertFilter> childFilters = null, LogicalOperator logicalOperator = LogicalOperator.And) : base(messageBusService)
        {
            _name = name;
            _childFilters = childFilters?.AsValueEnumerable().ToList() ?? new List<IAlertFilter>();
            _logicalOperator = logicalOperator;
        }

        /// <summary>
        /// Core implementation to determine if filter can handle an alert.
        /// </summary>
        /// <param name="alert">Alert to check</param>
        /// <returns>True if alert is not null and there are child filters</returns>
        protected override bool CanHandleCore(Alert alert) => alert != null && _childFilters.AsValueEnumerable().Any();

        /// <summary>
        /// Core implementation of alert evaluation.
        /// </summary>
        /// <param name="alert">Alert to evaluate</param>
        /// <param name="context">Filtering context</param>
        /// <returns>Combined result from child filters</returns>
        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
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
                return FilterResult.Allow("No applicable child filters");

            return _logicalOperator switch
            {
                LogicalOperator.And => EvaluateAnd(alert, results),
                LogicalOperator.Or => EvaluateOr(alert, results),
                LogicalOperator.Xor => EvaluateXor(alert, results),
                LogicalOperator.Not => EvaluateNot(alert, results),
                _ => FilterResult.Allow("Unknown logical operator")
            };
        }

        private FilterResult EvaluateAnd(Alert alert, List<FilterResult> results)
        {
            return results.AsValueEnumerable().All(r => r.Decision == FilterDecision.Allow)
                ? FilterResult.Allow("All child filters allowed")
                : FilterResult.Suppress("One or more child filters suppressed");
        }

        private FilterResult EvaluateOr(Alert alert, List<FilterResult> results)
        {
            return results.AsValueEnumerable().Any(r => r.Decision == FilterDecision.Allow)
                ? FilterResult.Allow("At least one child filter allowed")
                : FilterResult.Suppress("All child filters suppressed");
        }

        private FilterResult EvaluateXor(Alert alert, List<FilterResult> results)
        {
            var allowCount = results.AsValueEnumerable().Count(r => r.Decision == FilterDecision.Allow);
            return allowCount == 1
                ? FilterResult.Allow("Exactly one child filter allowed")
                : FilterResult.Suppress($"{allowCount} child filters allowed (expected 1)");
        }

        private FilterResult EvaluateNot(Alert alert, List<FilterResult> results)
        {
            return results.AsValueEnumerable().All(r => r.Decision != FilterDecision.Allow)
                ? FilterResult.Allow("All child filters were suppressed (NOT)")
                : FilterResult.Suppress("One or more child filters allowed (NOT)");
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// Applies configuration to all child filters.
        /// </summary>
        /// <param name="configuration">Configuration to apply</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>True if all child filters configured successfully</returns>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            var allSuccess = true;
            foreach (var childFilter in _childFilters)
            {
                if (!childFilter.Configure(configuration, correlationId))
                {
                    allSuccess = false;
                }
            }
            return allSuccess;
        }

        /// <summary>
        /// Core implementation of configuration validation.
        /// Validates configuration against all child filters.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Combined validation result</returns>
        protected override FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration)
        {
            var allErrors = new List<string>();
            var allWarnings = new List<string>();
            
            foreach (var childFilter in _childFilters)
            {
                var result = childFilter.ValidateConfiguration(configuration);
                if (!result.IsValid)
                {
                    allErrors.AddRange(result.Errors);
                    allWarnings.AddRange(result.Warnings);
                }
            }
            
            return allErrors.Count > 0 
                ? FilterValidationResult.Invalid(allErrors, allWarnings)
                : FilterValidationResult.Valid();
        }
    }
}