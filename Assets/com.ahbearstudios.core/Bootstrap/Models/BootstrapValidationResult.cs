using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
    /// Result of bootstrap installer validation with detailed error and warning information.
    /// Provides comprehensive feedback for troubleshooting and configuration correction.
    /// </summary>
    public readonly record struct BootstrapValidationResult
    {
        /// <summary>Gets whether the validation passed without critical errors.</summary>
        public readonly bool IsValid;
        
        /// <summary>Gets validation error messages for critical issues that prevent installation.</summary>
        public readonly IReadOnlyList<string> Errors;
        
        /// <summary>Gets validation warning messages for non-critical issues.</summary>
        public readonly IReadOnlyList<string> Warnings;
        
        /// <summary>Gets dependency validation results for prerequisite checking.</summary>
        public readonly IReadOnlyList<string> DependencyIssues;
        
        /// <summary>Gets the time taken to perform validation for performance monitoring.</summary>
        public readonly TimeSpan ValidationTime;
        
        /// <summary>
        /// Initializes a new bootstrap validation result.
        /// </summary>
        public BootstrapValidationResult(bool isValid, IReadOnlyList<string> errors = null, 
            IReadOnlyList<string> warnings = null, IReadOnlyList<string> dependencyIssues = null,
            TimeSpan validationTime = default)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<string>();
            Warnings = warnings ?? Array.Empty<string>();
            DependencyIssues = dependencyIssues ?? Array.Empty<string>();
            ValidationTime = validationTime;
        }
        
        /// <summary>Creates a successful validation result.</summary>
        public static BootstrapValidationResult Success(IReadOnlyList<string> warnings = null, TimeSpan validationTime = default) =>
            new(true, null, warnings, null, validationTime);
        
        /// <summary>Creates a failed validation result with error details.</summary>
        public static BootstrapValidationResult Failure(IReadOnlyList<string> errors, 
            IReadOnlyList<string> warnings = null, IReadOnlyList<string> dependencyIssues = null,
            TimeSpan validationTime = default) =>
            new(false, errors, warnings, dependencyIssues, validationTime);
    }