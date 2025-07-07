using System.Collections.Generic;
using System.Text;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Validation
{
    /// <summary>
    /// Implementation of container validation result with comprehensive information.
    /// Optimized for minimal allocations and fast access to validation data.
    /// </summary>
    public sealed class ContainerValidationResult : IContainerValidationResult
    {
        private static readonly IReadOnlyList<ValidationError> EmptyErrors = new List<ValidationError>(0);
        private static readonly IReadOnlyList<ValidationWarning> EmptyWarnings = new List<ValidationWarning>(0);
        
        /// <summary>
        /// Gets whether the validation passed.
        /// </summary>
        public bool IsValid { get; }
        
        /// <summary>
        /// Gets the total number of registrations validated.
        /// </summary>
        public int TotalRegistrations { get; }
        
        /// <summary>
        /// Gets the number of valid registrations.
        /// </summary>
        public int ValidRegistrations { get; }
        
        /// <summary>
        /// Gets the validation errors found.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }
        
        /// <summary>
        /// Gets the validation warnings found.
        /// </summary>
        public IReadOnlyList<ValidationWarning> Warnings { get; }
        
        /// <summary>
        /// Gets whether circular dependencies were detected.
        /// </summary>
        public bool HasCircularDependencies { get; }
        
        /// <summary>
        /// Gets the time taken to perform validation.
        /// </summary>
        public TimeSpan ValidationTime { get; }
        
        /// <summary>
        /// Gets a summary message of the validation results.
        /// </summary>
        public string Summary { get; }
        
        /// <summary>
        /// Initializes a new validation result.
        /// </summary>
        public ContainerValidationResult(
            bool isValid,
            int totalRegistrations,
            int validRegistrations,
            IReadOnlyList<ValidationError> errors = null,
            IReadOnlyList<ValidationWarning> warnings = null,
            bool hasCircularDependencies = false,
            TimeSpan validationTime = default)
        {
            IsValid = isValid;
            TotalRegistrations = totalRegistrations;
            ValidRegistrations = validRegistrations;
            Errors = errors ?? EmptyErrors;
            Warnings = warnings ?? EmptyWarnings;
            HasCircularDependencies = hasCircularDependencies;
            ValidationTime = validationTime;
            Summary = GenerateSummary();
        }
        
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ContainerValidationResult Success(
            int totalRegistrations,
            TimeSpan validationTime = default,
            IReadOnlyList<ValidationWarning> warnings = null)
        {
            return new ContainerValidationResult(
                true,
                totalRegistrations,
                totalRegistrations,
                EmptyErrors,
                warnings,
                false,
                validationTime);
        }
        
        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        public static ContainerValidationResult Failure(
            int totalRegistrations,
            int validRegistrations,
            IReadOnlyList<ValidationError> errors,
            TimeSpan validationTime = default,
            IReadOnlyList<ValidationWarning> warnings = null,
            bool hasCircularDependencies = false)
        {
            return new ContainerValidationResult(
                false,
                totalRegistrations,
                validRegistrations,
                errors,
                warnings,
                hasCircularDependencies,
                validationTime);
        }
        
        /// <summary>
        /// Generates a summary message for the validation results.
        /// </summary>
        private string GenerateSummary()
        {
            if (IsValid)
            {
                var warningText = Warnings.Count > 0 ? $" with {Warnings.Count} warnings" : "";
                return $"Validation passed for {TotalRegistrations} registrations{warningText} " +
                       $"in {ValidationTime.TotalMilliseconds:F1}ms";
            }
            
            var sb = new StringBuilder();
            sb.Append($"Validation failed: {Errors.Count} errors, ");
            sb.Append($"{ValidRegistrations}/{TotalRegistrations} valid registrations");
            
            if (Warnings.Count > 0)
                sb.Append($", {Warnings.Count} warnings");
            
            if (HasCircularDependencies)
                sb.Append(", circular dependencies detected");
            
            sb.Append($" (validated in {ValidationTime.TotalMilliseconds:F1}ms)");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Returns a detailed string representation of the validation results.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Container Validation Results ===");
            sb.AppendLine($"Status: {(IsValid ? "VALID" : "INVALID")}");
            sb.AppendLine($"Registrations: {ValidRegistrations}/{TotalRegistrations} valid");
            sb.AppendLine($"Validation Time: {ValidationTime.TotalMilliseconds:F1}ms");
            sb.AppendLine($"Circular Dependencies: {(HasCircularDependencies ? "YES" : "NO")}");
            
            if (Errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Errors ({Errors.Count}):");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }
            
            if (Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Warnings ({Warnings.Count}):");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }
            
            return sb.ToString();
        }
    }
}