using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Specialized builder implementation for configuring suppression rules with fluent syntax.
    /// Provides advanced suppression rule configuration capabilities and validation.
    /// </summary>
    internal sealed class SuppressionConfigBuilder : ISuppressionConfigBuilder
    {
        private readonly List<SuppressionConfig> _suppressionRules;

        /// <summary>
        /// Initializes a new instance of the SuppressionConfigBuilder.
        /// </summary>
        /// <param name="suppressionRules">The suppression rules list to modify</param>
        public SuppressionConfigBuilder(List<SuppressionConfig> suppressionRules)
        {
            _suppressionRules = suppressionRules ?? throw new ArgumentNullException(nameof(suppressionRules));
        }

        /// <summary>
        /// Adds a suppression rule configuration with fluent syntax.
        /// </summary>
        public ISuppressionConfigBuilder AddRule<TRule>(Action<TRule> configAction) where TRule : SuppressionConfig, new()
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction));

            var rule = new TRule();
            configAction(rule);
            rule.Validate();

            // Remove existing rule with the same name
            _suppressionRules.RemoveAll(r => r.RuleName.Equals(rule.RuleName));
            _suppressionRules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds a suppression rule configuration with fluent syntax.
        /// </summary>
        public ISuppressionConfigBuilder AddRule(SuppressionConfig suppressionConfig)
        {
            if (suppressionConfig == null)
                throw new ArgumentNullException(nameof(suppressionConfig));

            suppressionConfig.Validate();

            // Remove existing rule with the same name
            _suppressionRules.RemoveAll(r => r.RuleName.Equals(suppressionConfig.RuleName));
            _suppressionRules.Add(suppressionConfig);
            return this;
        }

        /// <summary>
        /// Adds a suppression rule configuration with fluent syntax.
        /// </summary>
        public ISuppressionConfigBuilder RemoveRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
                throw new ArgumentException("Rule name cannot be null or whitespace.", nameof(ruleName));

            _suppressionRules.RemoveAll(r => r.RuleName.ToString().Equals(ruleName, StringComparison.OrdinalIgnoreCase));
            return this;
        }

        /// <summary>
        /// Adds a suppression rule configuration with fluent syntax.
        /// </summary>
        public ISuppressionConfigBuilder ClearRules()
        {
            _suppressionRules.Clear();
            return this;
        }
    }
}