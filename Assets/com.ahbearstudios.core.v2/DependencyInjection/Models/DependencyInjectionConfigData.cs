using System.Runtime.Serialization;

namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Data contract for JSON/XML serialization of DI configuration.
    /// Uses DataContract attributes for cross-platform compatibility.
    /// </summary>
    [DataContract]
    public sealed class DependencyInjectionConfigData
    {
        [DataMember(Name = "preferredFramework")]
        public string PreferredFramework { get; set; } = "VContainer";
        
        [DataMember(Name = "enableValidation")]
        public bool EnableValidation { get; set; } = true;
        
        [DataMember(Name = "enableDebugLogging")]
        public bool EnableDebugLogging { get; set; } = false;
        
        [DataMember(Name = "enablePerformanceMetrics")]
        public bool EnablePerformanceMetrics { get; set; } = false;
        
        [DataMember(Name = "throwOnValidationFailure")]
        public bool ThrowOnValidationFailure { get; set; } = false;
        
        [DataMember(Name = "maxBuildTimeWarningMs")]
        public int MaxBuildTimeWarningMs { get; set; } = 100;
        
        [DataMember(Name = "enableScoping")]
        public bool EnableScoping { get; set; } = true;
        
        [DataMember(Name = "enableNamedServices")]
        public bool EnableNamedServices { get; set; } = false;
        
        [DataMember(Name = "vcontainerOptions")]
        public VContainerConfigData VContainerOptions { get; set; }
        
        [DataMember(Name = "reflexOptions")]
        public ReflexConfigData ReflexOptions { get; set; }
    }
    
    [DataContract]
    public sealed class VContainerConfigData
    {
        [DataMember(Name = "enableCodeGeneration")]
        public bool EnableCodeGeneration { get; set; } = false;
        
        [DataMember(Name = "enableDiagnostics")]
        public bool EnableDiagnostics { get; set; } = false;
        
        [DataMember(Name = "validateDependencies")]
        public bool ValidateDependencies { get; set; } = true;
    }
    
    [DataContract]
    public sealed class ReflexConfigData
    {
        [DataMember(Name = "enableProfiler")]
        public bool EnableProfiler { get; set; } = false;
        
        [DataMember(Name = "logRegistrations")]
        public bool LogRegistrations { get; set; } = false;
        
        [DataMember(Name = "enableResolverLogging")]
        public bool EnableResolverLogging { get; set; } = false;
    }
}