using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Platform requirements for conditional installation.
/// Specifies platform capabilities and constraints for installer execution.
/// </summary>
public readonly struct PlatformRequirements
{
    /// <summary>Gets supported platforms for this installer.</summary>
    public readonly IReadOnlyList<UnityEngine.RuntimePlatform> SupportedPlatforms;
        
    /// <summary>Gets minimum Unity version required.</summary>
    public readonly Version MinimumUnityVersion;
        
    /// <summary>Gets required scripting backend.</summary>
    public readonly UnityEngine.ScriptingImplementation? RequiredScriptingBackend;
        
    /// <summary>Gets whether this installer requires development build capabilities.</summary>
    public readonly bool RequiresDevelopmentBuild;
        
    /// <summary>Gets platform-specific feature requirements.</summary>
    public readonly IReadOnlyDictionary<string, bool> FeatureRequirements;
        
    /// <summary>
    /// Initializes platform requirements.
    /// </summary>
    public PlatformRequirements(IReadOnlyList<UnityEngine.RuntimePlatform> supportedPlatforms,
        Version minimumUnityVersion, UnityEngine.ScriptingImplementation? requiredScriptingBackend,
        bool requiresDevelopmentBuild, IReadOnlyDictionary<string, bool> featureRequirements)
    {
        SupportedPlatforms = supportedPlatforms;
        MinimumUnityVersion = minimumUnityVersion;
        RequiredScriptingBackend = requiredScriptingBackend;
        RequiresDevelopmentBuild = requiresDevelopmentBuild;
        FeatureRequirements = featureRequirements;
    }
}