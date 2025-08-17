using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Filters;
using ValidationResult = AhBearStudios.Core.Common.Models.ValidationResult;
using ValidationError = AhBearStudios.Core.Common.Models.ValidationError;
using ZLinq;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Validates filter configurations according to business rules.
    /// Handles complex validation logic that should not be in factories.
    /// Follows CLAUDE.md principle: Move complex logic to services/validators.
    /// </summary>
    public sealed class FilterConfigurationValidator : IFilterConfigurationValidator
    {
        /// <summary>
        /// Validates a filter configuration before creation.
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <returns>Validation result with any errors found</returns>
        public ValidationResult ValidateFilterConfiguration(FilterConfiguration configuration)
        {
            if (configuration == null)
                return ValidationResult.Failure(new[] { new ValidationError("Configuration cannot be null") }, "FilterConfigurationValidator");

            var errors = new List<ValidationError>();

            // Validate basic properties
            if (string.IsNullOrEmpty(configuration.Type))
                errors.Add(new ValidationError("Filter type cannot be null or empty"));

            if (configuration.Name.IsEmpty)
                errors.Add(new ValidationError("Filter name cannot be empty"));

            if (configuration.Priority < 0)
                errors.Add(new ValidationError("Filter priority cannot be negative"));

            // Validate type-specific settings
            var filterType = DetermineFilterType(configuration);
            switch (filterType)
            {
                case FilterType.Severity:
                    ValidateSeverityFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.Source:
                    ValidateSourceFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.RateLimit:
                    ValidateRateLimitFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.Content:
                    ValidateContentFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.TimeBased:
                    ValidateTimeBasedFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.Composite:
                    ValidateCompositeFilterSettings(configuration.Settings, errors);
                    break;
            }

            return errors.AsValueEnumerable().Any()
                ? ValidationResult.Failure(errors, "FilterConfigurationValidator")
                : ValidationResult.Success("FilterConfigurationValidator");
        }

        private FilterType DetermineFilterType(FilterConfiguration configuration)
        {
            if (Enum.TryParse<FilterType>(configuration.Type, true, out var filterType))
                return filterType;

            // Fallback mapping for string types
            return configuration.Type.ToLowerInvariant() switch
            {
                "severity" => FilterType.Severity,
                "source" => FilterType.Source,
                "ratelimit" or "rate" => FilterType.RateLimit,
                "content" or "message" => FilterType.Content,
                "time" or "timebased" => FilterType.TimeBased,
                "composite" or "combined" => FilterType.Composite,
                "tag" => FilterType.Tag,
                "correlation" => FilterType.Correlation,
                "passthrough" or "allow" => FilterType.PassThrough,
                "block" or "suppress" => FilterType.Block,
                _ => throw new ArgumentException($"Unknown filter type: {configuration.Type}")
            };
        }

        private void ValidateSeverityFilterSettings(IReadOnlyDictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("MinimumSeverity"))
                errors.Add(new ValidationError("Severity filter requires MinimumSeverity setting"));
        }

        private void ValidateSourceFilterSettings(IReadOnlyDictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("AllowedSources"))
                errors.Add(new ValidationError("Source filter requires AllowedSources setting"));
        }

        private void ValidateRateLimitFilterSettings(IReadOnlyDictionary<string, object> settings, List<ValidationError> errors)
        {
            if (settings.TryGetValue("MaxAlertsPerMinute", out var rateValue))
            {
                if (!int.TryParse(rateValue.ToString(), out var rate) || rate <= 0)
                    errors.Add(new ValidationError("RateLimit filter MaxAlertsPerMinute must be a positive integer"));
            }
            else
            {
                errors.Add(new ValidationError("RateLimit filter requires MaxAlertsPerMinute setting"));
            }
        }

        private void ValidateContentFilterSettings(IReadOnlyDictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("Patterns"))
                errors.Add(new ValidationError("Content filter requires Patterns setting"));
        }

        private void ValidateTimeBasedFilterSettings(IReadOnlyDictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("TimeRanges"))
                errors.Add(new ValidationError("TimeBased filter requires TimeRanges setting"));
        }

        private void ValidateCompositeFilterSettings(IReadOnlyDictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("ChildFilters"))
                errors.Add(new ValidationError("Composite filter requires ChildFilters setting"));
            else if (settings["ChildFilters"] is IEnumerable<IAlertFilter> filters && !filters.AsValueEnumerable().Any())
                errors.Add(new ValidationError("Composite filter requires at least one child filter"));
        }
    }
}