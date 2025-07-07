using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for framework-agnostic container validation.
    /// Provides comprehensive validation capabilities across different DI frameworks.
    /// </summary>
    public interface IContainerValidator
    {
        /// <summary>
        /// Gets the frameworks this validator supports.
        /// </summary>
        IReadOnlySet<ContainerFramework> SupportedFrameworks { get; }
        
        /// <summary>
        /// Validates a container adapter.
        /// </summary>
        /// <param name="adapter">The container adapter to validate.</param>
        /// <param name="config">The configuration to use for validation.</param>
        /// <returns>Validation result with detailed information.</returns>
        IContainerValidationResult Validate(IContainerAdapter adapter, IDependencyInjectionConfig config);
        
        /// <summary>
        /// Validates registrations for circular dependencies.
        /// </summary>
        /// <param name="registrations">The registrations to validate.</param>
        /// <returns>True if circular dependencies are detected, false otherwise.</returns>
        bool HasCircularDependencies(IEnumerable<ServiceRegistration> registrations);
        
        /// <summary>
        /// Validates a specific service registration.
        /// </summary>
        /// <param name="registration">The registration to validate.</param>
        /// <returns>Validation result for the specific registration.</returns>
        ServiceValidationResult ValidateRegistration(ServiceRegistration registration);
        
        /// <summary>
        /// Gets performance metrics for validation operations.
        /// </summary>
        /// <returns>Validation performance metrics.</returns>
        ValidationMetrics GetMetrics();
    }
}