using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
    /// Diagnostic information for troubleshooting and system analysis.
    /// Provides comprehensive details about installer state and registered services.
    /// </summary>
    public readonly struct InstallerDiagnostics
    {
        /// <summary>Gets the installer name and identification information.</summary>
        public readonly string InstallerName;
        
        /// <summary>Gets the current installation state and status.</summary>
        public readonly InstallationState State;
        
        /// <summary>Gets information about all registered services.</summary>
        public readonly IReadOnlyList<ServiceDiagnosticInfo> RegisteredServices;
        
        /// <summary>Gets dependency information and resolution status.</summary>
        public readonly IReadOnlyList<DependencyInfo> Dependencies;
        
        /// <summary>Gets configuration information and applied settings.</summary>
        public readonly IReadOnlyDictionary<string, object> ConfigurationValues;
        
        /// <summary>Gets performance metrics and timing information.</summary>
        public readonly InstallationMetrics Metrics;
        
        /// <summary>Gets any error or warning messages from installation.</summary>
        public readonly IReadOnlyList<string> DiagnosticMessages;
        
        /// <summary>
        /// Initializes new installer diagnostics.
        /// </summary>
        public InstallerDiagnostics(string installerName, InstallationState state,
            IReadOnlyList<ServiceDiagnosticInfo> registeredServices,
            IReadOnlyList<DependencyInfo> dependencies,
            IReadOnlyDictionary<string, object> configurationValues,
            InstallationMetrics metrics, IReadOnlyList<string> diagnosticMessages)
        {
            InstallerName = installerName;
            State = state;
            RegisteredServices = registeredServices;
            Dependencies = dependencies;
            ConfigurationValues = configurationValues;
            Metrics = metrics;
            DiagnosticMessages = diagnosticMessages;
        }
    }