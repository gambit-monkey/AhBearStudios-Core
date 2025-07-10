using AhBearStudios.Core.Bootstrap.Models;
using Reflex.Core;
using BootstrapValidationResult = AhBearStudios.Core.DependencyInjection.Bootstrap.BootstrapValidationResult;

namespace AhBearStudios.Core.Bootstrap.Interfaces
{
    /// <summary>
    /// Interface for bootstrap installers that register services with Reflex containers.
    /// Provides comprehensive system installation capabilities with dependency management,
    /// validation, health monitoring, and performance tracking.
    /// 
    /// Integrates with core systems: logging, message bus, health checks, alerts, and profiling.
    /// All installers must implement proper error handling, resource cleanup, and monitoring.
    /// </summary>
    public interface IBootstrapInstaller
    {
        #region Core Properties
        
        /// <summary>
        /// Gets the unique name of this installer for identification and logging purposes.
        /// Used for dependency resolution, error reporting, and system monitoring.
        /// </summary>
        string InstallerName { get; }
        
        /// <summary>
        /// Gets the priority of this installer for dependency ordering.
        /// Lower values install first. Core systems (0-99), Optional systems (100-199),
        /// Development systems (200-299), Platform-specific systems (300+).
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Gets whether this installer is enabled and should be processed during bootstrap.
        /// Allows runtime configuration of which systems to install.
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Gets the installer types that must be processed before this one.
        /// Used for dependency ordering and validation. Dependencies must be satisfied
        /// or installation will fail with detailed error information.
        /// </summary>
        Type[] Dependencies { get; }
        
        /// <summary>
        /// Gets the estimated memory overhead in bytes that this installer will add.
        /// Used for capacity planning and memory monitoring during bootstrap.
        /// </summary>
        long EstimatedMemoryOverheadBytes { get; }
        
        /// <summary>
        /// Gets whether this installer supports hot-reload scenarios.
        /// Affects cleanup and re-initialization behavior during development.
        /// </summary>
        bool SupportsHotReload { get; }
        
        /// <summary>
        /// Gets the system category this installer belongs to for organizational purposes.
        /// Used for filtering, grouping, and conditional installation based on build configuration.
        /// </summary>
        SystemCategory Category { get; }
        
        #endregion
        
        #region Lifecycle Methods
        
        /// <summary>
        /// Validates that this installer can be properly executed with the given configuration.
        /// Performs dependency checking, configuration validation, and prerequisite verification.
        /// 
        /// Called during bootstrap validation phase before any installation begins.
        /// Must not throw exceptions - return validation result with detailed error information.
        /// </summary>
        /// <param name="config">The bootstrap configuration to validate against.</param>
        /// <returns>Validation result with success status and detailed error/warning information.</returns>
        BootstrapValidationResult ValidateInstaller(IBootstrapConfig config);
        
        /// <summary>
        /// Called before the installer is processed to allow for pre-installation setup.
        /// Use for resource preparation, dependency verification, and initialization tasks
        /// that must occur before service registration.
        /// 
        /// Integrates with ILoggingService for activity tracking and error reporting.
        /// Must handle failures gracefully and provide detailed error context.
        /// </summary>
        /// <param name="config">The bootstrap configuration being used.</param>
        /// <param name="context">Bootstrap context with logging, profiling, and messaging services.</param>
        void PreInstall(IBootstrapConfig config, IBootstrapContext context);
        
        /// <summary>
        /// Installs services into the Reflex container.
        /// Register all services, implementations, and configurations required by this system.
        /// 
        /// Must use proper error handling and provide detailed failure information.
        /// Should register health checks for all critical services.
        /// Performance-critical paths should be profiled using the provided context.
        /// </summary>
        /// <param name="container">The Reflex container to register services with.</param>
        /// <param name="config">The bootstrap configuration being used.</param>
        /// <param name="context">Bootstrap context with logging, profiling, and messaging services.</param>
        void Install(Container container, IBootstrapConfig config, IBootstrapContext context);
        
        /// <summary>
        /// Called after the installer has been processed to allow for post-installation setup.
        /// Use for service validation, health check registration, metric setup, and
        /// final initialization tasks that require the full container to be available.
        /// 
        /// Should register health checks with IHealthCheckService and configure
        /// alerting thresholds with IAlertService for critical failures.
        /// </summary>
        /// <param name="container">The configured Reflex container.</param>
        /// <param name="config">The bootstrap configuration being used.</param>
        /// <param name="context">Bootstrap context with logging, profiling, and messaging services.</param>
        void PostInstall(Container container, IBootstrapConfig config, IBootstrapContext context);
        
        /// <summary>
        /// Called when the installer needs to be uninstalled during hot-reload or shutdown.
        /// Properly dispose resources, unregister services, and clean up any persistent state.
        /// 
        /// Only called if SupportsHotReload is true. Must handle partial installation states
        /// and provide robust cleanup even if installation was incomplete.
        /// </summary>
        /// <param name="container">The container to clean up from.</param>
        /// <param name="context">Bootstrap context for logging cleanup activities.</param>
        void Uninstall(Container container, IBootstrapContext context);
        
        #endregion
        
        #region Monitoring and Diagnostics
        
        /// <summary>
        /// Gets installation metrics for performance monitoring and optimization.
        /// Includes timing data, memory usage, service count, and error information.
        /// Used for bootstrap performance analysis and capacity planning.
        /// </summary>
        /// <returns>Installation metrics with comprehensive performance data.</returns>
        InstallationMetrics GetInstallationMetrics();
        
        /// <summary>
        /// Gets the health status of services installed by this installer.
        /// Used for runtime monitoring and system diagnostics.
        /// Should reflect the current operational state of all registered services.
        /// </summary>
        /// <returns>Health status with detailed service information.</returns>
        SystemHealthStatus GetHealthStatus();
        
        /// <summary>
        /// Gets diagnostic information about the installer and its registered services.
        /// Used for debugging, troubleshooting, and system analysis.
        /// Should include service registration details, dependency information, and current state.
        /// </summary>
        /// <returns>Diagnostic information for troubleshooting and analysis.</returns>
        InstallerDiagnostics GetDiagnostics();
        
        /// <summary>
        /// Resets installation metrics and performance counters.
        /// Used for performance testing and metric collection scenarios.
        /// Should reset all collected timing, memory, and operational data.
        /// </summary>
        void ResetMetrics();
        
        #endregion
        
        #region Configuration and Customization
        
        /// <summary>
        /// Gets configuration requirements that this installer needs to function properly.
        /// Used for validation and configuration UI generation.
        /// Should specify all required configuration sections and their constraints.
        /// </summary>
        /// <returns>Configuration requirements with validation rules and descriptions.</returns>
        ConfigurationRequirements GetConfigurationRequirements();
        
        /// <summary>
        /// Gets the services that this installer will register in the container.
        /// Used for dependency analysis, conflict detection, and documentation generation.
        /// Should include service types, implementation types, and lifecycle information.
        /// </summary>
        /// <returns>Service registration information for dependency analysis.</returns>
        ServiceRegistrationInfo[] GetServiceRegistrations();
        
        /// <summary>
        /// Gets platform-specific installation requirements and constraints.
        /// Used for conditional installation based on target platform, build configuration,
        /// and runtime environment capabilities.
        /// </summary>
        /// <returns>Platform requirements and constraints for conditional installation.</returns>
        PlatformRequirements GetPlatformRequirements();
        
        #endregion
        
        #region Error Handling and Recovery
        
        /// <summary>
        /// Called when installation fails to allow for cleanup and error reporting.
        /// Should clean up any partially created resources and provide detailed error context
        /// for troubleshooting and recovery scenarios.
        /// 
        /// Must not throw exceptions - should handle all cleanup failures gracefully
        /// and log appropriate error information for diagnostic purposes.
        /// </summary>
        /// <param name="exception">The exception that caused the installation failure.</param>
        /// <param name="context">Bootstrap context for error logging and alerting.</param>
        void HandleInstallationFailure(Exception exception, IBootstrapContext context);
        
        /// <summary>
        /// Gets recovery options available when this installer fails.
        /// Used for graceful degradation and fallback installation scenarios.
        /// Should specify alternative configurations or partial installation options.
        /// </summary>
        /// <returns>Available recovery options for failed installations.</returns>
        RecoveryOptions GetRecoveryOptions();
        
        /// <summary>
        /// Attempts to recover from a failed installation using the specified recovery option.
        /// Used for graceful degradation and fallback scenarios when primary installation fails.
        /// 
        /// Should implement robust error handling and provide detailed status information
        /// about the recovery attempt and its success or failure.
        /// </summary>
        /// <param name="option">The recovery option to attempt.</param>
        /// <param name="container">The container to install fallback services into.</param>
        /// <param name="config">The bootstrap configuration.</param>
        /// <param name="context">Bootstrap context for logging and monitoring.</param>
        /// <returns>Recovery result with success status and detailed information.</returns>
        RecoveryResult AttemptRecovery(RecoveryOption option, Container container, 
            IBootstrapConfig config, IBootstrapContext context);
        
        #endregion
    }
}