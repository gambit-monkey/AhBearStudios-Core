using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Recovery options for failed installation scenarios.
/// Provides graceful degradation and fallback capabilities.
/// </summary>
public readonly struct RecoveryOptions
{
    /// <summary>Gets available recovery options.</summary>
    public readonly IReadOnlyList<RecoveryOption> AvailableOptions;
        
    /// <summary>Gets whether graceful degradation is supported.</summary>
    public readonly bool SupportsGracefulDegradation;
        
    /// <summary>Gets the recommended recovery option for typical failures.</summary>
    public readonly RecoveryOption RecommendedOption;
        
    /// <summary>
    /// Initializes recovery options.
    /// </summary>
    public RecoveryOptions(IReadOnlyList<RecoveryOption> availableOptions,
        bool supportsGracefulDegradation, RecoveryOption recommendedOption)
    {
        AvailableOptions = availableOptions;
        SupportsGracefulDegradation = supportsGracefulDegradation;
        RecommendedOption = recommendedOption;
    }
}