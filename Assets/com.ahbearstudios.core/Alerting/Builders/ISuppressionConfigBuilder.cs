using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Builders;

/// <summary>
/// Specialized builder interface for configuring suppression rules with fluent syntax.
/// Provides advanced suppression rule configuration capabilities beyond basic rule addition.
/// </summary>
public interface ISuppressionConfigBuilder
{
    /// <summary>
    /// Adds a suppression rule with advanced configuration options.
    /// </summary>
    /// <typeparam name="TRule">The rule type to add</typeparam>
    /// <param name="configAction">Action to configure the rule</param>
    /// <returns>The suppression builder instance for method chaining</returns>
    ISuppressionConfigBuilder AddRule<TRule>(Action<TRule> configAction) where TRule : SuppressionConfig, new();

    /// <summary>
    /// Adds a suppression rule with fluent configuration.
    /// </summary>
    /// <param name="suppressionConfig">The suppression rule configuration</param>
    /// <returns>The suppression builder instance for method chaining</returns>
    ISuppressionConfigBuilder AddRule(SuppressionConfig suppressionConfig);

    /// <summary>
    /// Removes a suppression rule by name.
    /// </summary>
    /// <param name="ruleName">The name of the rule to remove</param>
    /// <returns>The suppression builder instance for method chaining</returns>
    ISuppressionConfigBuilder RemoveRule(string ruleName);

    /// <summary>
    /// Clears all configured suppression rules.
    /// </summary>
    /// <returns>The suppression builder instance for method chaining</returns>
    ISuppressionConfigBuilder ClearRules();
}