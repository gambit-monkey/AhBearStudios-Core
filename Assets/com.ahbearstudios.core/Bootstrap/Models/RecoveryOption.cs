namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Recovery option for failed installation scenarios.
/// </summary>
public readonly struct RecoveryOption
{
    /// <summary>Gets the unique identifier for this recovery option.</summary>
    public readonly string OptionId;
        
    /// <summary>Gets the human-readable name for this recovery option.</summary>
    public readonly string Name;
        
    /// <summary>Gets the description of what this recovery option does.</summary>
    public readonly string Description;
        
    /// <summary>Gets the recovery strategy type.</summary>
    public readonly RecoveryStrategy Strategy;
        
    /// <summary>Gets whether this option provides full functionality or reduced capabilities.</summary>
    public readonly bool IsFullFunctionality;
        
    /// <summary>Gets the estimated success probability for this recovery option.</summary>
    public readonly float SuccessProbability;
        
    /// <summary>
    /// Initializes a new recovery option.
    /// </summary>
    public RecoveryOption(string optionId, string name, string description, 
        RecoveryStrategy strategy, bool isFullFunctionality, float successProbability)
    {
        OptionId = optionId;
        Name = name;
        Description = description;
        Strategy = strategy;
        IsFullFunctionality = isFullFunctionality;
        SuccessProbability = successProbability;
    }
}