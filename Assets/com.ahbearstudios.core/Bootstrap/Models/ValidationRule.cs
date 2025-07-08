namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Validation rule for configuration values.
/// </summary>
public readonly struct ValidationRule
{
    /// <summary>Gets the configuration key this rule applies to.</summary>
    public readonly string ConfigurationKey;
        
    /// <summary>Gets the expected value type.</summary>
    public readonly Type ExpectedType;
        
    /// <summary>Gets whether this configuration value is required.</summary>
    public readonly bool IsRequired;
        
    /// <summary>Gets the validation function to apply.</summary>
    public readonly Func<object, bool> ValidationFunction;
        
    /// <summary>Gets the error message to display if validation fails.</summary>
    public readonly string ErrorMessage;
        
    /// <summary>
    /// Initializes a new validation rule.
    /// </summary>
    public ValidationRule(string configurationKey, Type expectedType, bool isRequired,
        Func<object, bool> validationFunction, string errorMessage)
    {
        ConfigurationKey = configurationKey;
        ExpectedType = expectedType;
        IsRequired = isRequired;
        ValidationFunction = validationFunction;
        ErrorMessage = errorMessage;
    }
}