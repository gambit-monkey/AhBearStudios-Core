using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Bootstrap
{
    /// <summary>
    /// Result of bootstrap validation with detailed information.
    /// </summary>
    public sealed class BootstrapValidationResult
    {
        /// <summary>
        /// Gets whether the validation passed.
        /// </summary>
        public bool IsValid { get; }
        
        /// <summary>
        /// Gets the number of installers validated.
        /// </summary>
        public int ValidatedInstallers { get; }
        
        /// <summary>
        /// Gets validation errors found.
        /// </summary>
        public IReadOnlyList<BootstrapError> Errors { get; }
        
        /// <summary>
        /// Gets validation warnings found.
        /// </summary>
        public IReadOnlyList<BootstrapWarning> Warnings { get; }
        
        /// <summary>
        /// Gets whether circular dependencies were detected in installers.
        /// </summary>
        public bool HasCircularDependencies { get; }
        
        /// <summary>
        /// Gets the ordered list of installers if validation passed.
        /// </summary>
        public IReadOnlyList<string> OrderedInstallers { get; }
        
        /// <summary>
        /// Initializes a new bootstrap validation result.
        /// </summary>
        public BootstrapValidationResult(
            bool isValid,
            int validatedInstallers,
            IReadOnlyList<BootstrapError> errors = null,
            IReadOnlyList<BootstrapWarning> warnings = null,
            bool hasCircularDependencies = false,
            IReadOnlyList<string> orderedInstallers = null)
        {
            IsValid = isValid;
            ValidatedInstallers = validatedInstallers;
            Errors = errors ?? Array.Empty<BootstrapError>();
            Warnings = warnings ?? Array.Empty<BootstrapWarning>();
            HasCircularDependencies = hasCircularDependencies;
            OrderedInstallers = orderedInstallers ?? Array.Empty<string>();
        }
        
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static BootstrapValidationResult Success(
            int validatedInstallers,
            IReadOnlyList<string> orderedInstallers,
            IReadOnlyList<BootstrapWarning> warnings = null)
        {
            return new BootstrapValidationResult(
                true,
                validatedInstallers,
                null,
                warnings,
                false,
                orderedInstallers);
        }
        
        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        public static BootstrapValidationResult Failure(
            int validatedInstallers,
            IReadOnlyList<BootstrapError> errors,
            IReadOnlyList<BootstrapWarning> warnings = null,
            bool hasCircularDependencies = false)
        {
            return new BootstrapValidationResult(
                false,
                validatedInstallers,
                errors,
                warnings,
                hasCircularDependencies);
        }
    }
}