using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Result of container validation with detailed information.
    /// </summary>
    public interface IContainerValidationResult
    {
        /// <summary>
        /// Gets whether the validation passed.
        /// </summary>
        bool IsValid { get; }
        
        /// <summary>
        /// Gets the total number of registrations validated.
        /// </summary>
        int TotalRegistrations { get; }
        
        /// <summary>
        /// Gets the number of valid registrations.
        /// </summary>
        int ValidRegistrations { get; }
        
        /// <summary>
        /// Gets the validation errors found.
        /// </summary>
        IReadOnlyList<ValidationError> Errors { get; }
        
        /// <summary>
        /// Gets the validation warnings found.
        /// </summary>
        IReadOnlyList<ValidationWarning> Warnings { get; }
        
        /// <summary>
        /// Gets whether circular dependencies were detected.
        /// </summary>
        bool HasCircularDependencies { get; }
        
        /// <summary>
        /// Gets the time taken to perform validation.
        /// </summary>
        TimeSpan ValidationTime { get; }
        
        /// <summary>
        /// Gets a summary message of the validation results.
        /// </summary>
        string Summary { get; }
    }
}