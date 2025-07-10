using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Scripting backend types for platform requirements.
/// </summary>
public enum ScriptingBackend
{
    /// <summary>Mono scripting backend.</summary>
    Mono,
    /// <summary>IL2CPP scripting backend.</summary>
    IL2CPP,
    /// <summary>Any scripting backend is acceptable.</summary>
    Any
}

/// <summary>
/// Platform requirements for conditional installation.
/// Specifies platform capabilities and constraints for installer execution.
/// </summary>
public readonly struct PlatformRequirements
{
    /// <summary>Gets supported platforms for this installer.</summary>
    public readonly IReadOnlyList<UnityEngine.RuntimePlatform> SupportedPlatforms;
        
    /// <summary>Gets minimum Unity version required.</summary>
    public readonly System.Version MinimumUnityVersion;
        
    /// <summary>Gets required scripting backend.</summary>
    public readonly ScriptingBackend? RequiredScriptingBackend;
        
    /// <summary>Gets whether this installer requires development build capabilities.</summary>
    public readonly bool RequiresDevelopmentBuild;
        
    /// <summary>Gets platform-specific feature requirements.</summary>
    public readonly IReadOnlyDictionary<string, bool> FeatureRequirements;
    
    public readonly bool IsIL2CPP => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains("IL2CPP");
        
    /// <summary>
    /// Initializes platform requirements.
    /// </summary>
    public PlatformRequirements(IReadOnlyList<UnityEngine.RuntimePlatform> supportedPlatforms,
        System.Version minimumUnityVersion, ScriptingBackend? requiredScriptingBackend,
        bool requiresDevelopmentBuild, IReadOnlyDictionary<string, bool> featureRequirements)
    {
        SupportedPlatforms = supportedPlatforms;
        MinimumUnityVersion = minimumUnityVersion;
        RequiredScriptingBackend = requiredScriptingBackend;
        RequiresDevelopmentBuild = requiresDevelopmentBuild;
        FeatureRequirements = featureRequirements;
    }
}