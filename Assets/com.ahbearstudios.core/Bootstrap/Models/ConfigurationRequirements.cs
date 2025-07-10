using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Configuration requirements specification for installer validation.
/// Defines required configuration sections and validation rules.
/// </summary>
public readonly struct ConfigurationRequirements
{
    /// <summary>Gets required configuration section names.</summary>
    public readonly IReadOnlyList<string> RequiredSections;
        
    /// <summary>Gets validation rules for configuration values.</summary>
    public readonly IReadOnlyDictionary<string, ValidationRule> ValidationRules;
        
    /// <summary>Gets platform-specific configuration requirements.</summary>
    public readonly IReadOnlyDictionary<UnityEngine.RuntimePlatform, string[]> PlatformRequirements;
        
    /// <summary>
    /// Initializes configuration requirements.
    /// </summary>
    public ConfigurationRequirements(IReadOnlyList<string> requiredSections,
        IReadOnlyDictionary<string, ValidationRule> validationRules,
        IReadOnlyDictionary<UnityEngine.RuntimePlatform, string[]> platformRequirements)
    {
        RequiredSections = requiredSections;
        ValidationRules = validationRules;
        PlatformRequirements = platformRequirements;
    }
}