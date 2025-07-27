using System;
using System.Collections.Generic;
using ZLinq;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents the result of a validation operation with success status and error details.
    /// Used throughout the logging system for configuration validation and system health checks.
    /// </summary>
    public sealed record ValidationResult
    {
        /// <summary>
        /// Gets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the list of validation errors, if any.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }

        /// <summary>
        /// Gets the list of validation warnings, if any.
        /// </summary>
        public IReadOnlyList<ValidationWarning> Warnings { get; }

        /// <summary>
        /// Gets the validation timestamp.
        /// </summary>
        public DateTime ValidatedAt { get; }

        /// <summary>
        /// Gets the component that was validated.
        /// </summary>
        public string Component { get; }

        /// <summary>
        /// Gets additional context information about the validation.
        /// </summary>
        public IReadOnlyDictionary<string, object> Context { get; }

        /// <summary>
        /// Gets whether the validation has any errors.
        /// </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Gets whether the validation has any warnings.
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Gets the total number of issues (errors + warnings).
        /// </summary>
        public int TotalIssues => Errors.Count + Warnings.Count;

        /// <summary>
        /// Gets a summary of the validation result.
        /// </summary>
        public string Summary
        {
            get
            {
                if (IsValid && !HasWarnings)
                    return "Validation successful";
                if (IsValid && HasWarnings)
                    return $"Validation successful with {Warnings.Count} warning(s)";
                return $"Validation failed with {Errors.Count} error(s)" + 
                       (HasWarnings ? $" and {Warnings.Count} warning(s)" : "");
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidationResult record.
        /// </summary>
        /// <param name="isValid">Whether the validation was successful</param>
        /// <param name="errors">The list of validation errors</param>
        /// <param name="warnings">The list of validation warnings</param>
        /// <param name="component">The component that was validated</param>
        /// <param name="context">Additional context information</param>
        public ValidationResult(
            bool isValid,
            IReadOnlyList<ValidationError> errors = null,
            IReadOnlyList<ValidationWarning> warnings = null,
            string component = null,
            IReadOnlyDictionary<string, object> context = null)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<ValidationError>();
            Warnings = warnings ?? Array.Empty<ValidationWarning>();
            Component = component ?? "Unknown";
            Context = context ?? new Dictionary<string, object>();
            ValidatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <param name="component">The component that was validated</param>
        /// <param name="warnings">Optional warnings</param>
        /// <param name="context">Additional context information</param>
        /// <returns>A successful ValidationResult</returns>
        public static ValidationResult Success(
            string component = null,
            IReadOnlyList<ValidationWarning> warnings = null,
            IReadOnlyDictionary<string, object> context = null)
        {
            return new ValidationResult(
                isValid: true,
                errors: null,
                warnings: warnings,
                component: component,
                context: context);
        }

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        /// <param name="errors">The validation errors</param>
        /// <param name="component">The component that was validated</param>
        /// <param name="warnings">Optional warnings</param>
        /// <param name="context">Additional context information</param>
        /// <returns>A failed ValidationResult</returns>
        public static ValidationResult Failure(
            IReadOnlyList<ValidationError> errors,
            string component = null,
            IReadOnlyList<ValidationWarning> warnings = null,
            IReadOnlyDictionary<string, object> context = null)
        {
            return new ValidationResult(
                isValid: false,
                errors: errors,
                warnings: warnings,
                component: component,
                context: context);
        }

        /// <summary>
        /// Creates a failed validation result with a single error.
        /// </summary>
        /// <param name="error">The validation error</param>
        /// <param name="component">The component that was validated</param>
        /// <param name="warnings">Optional warnings</param>
        /// <param name="context">Additional context information</param>
        /// <returns>A failed ValidationResult</returns>
        public static ValidationResult Failure(
            ValidationError error,
            string component = null,
            IReadOnlyList<ValidationWarning> warnings = null,
            IReadOnlyDictionary<string, object> context = null)
        {
            return Failure(new[] { error }, component, warnings, context);
        }

        /// <summary>
        /// Creates a failed validation result with a single error message.
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="component">The component that was validated</param>
        /// <param name="propertyName">The property name associated with the error</param>
        /// <param name="context">Additional context information</param>
        /// <returns>A failed ValidationResult</returns>
        public static ValidationResult Failure(
            string errorMessage,
            string component = null,
            string propertyName = null,
            IReadOnlyDictionary<string, object> context = null)
        {
            var error = new ValidationError(errorMessage, propertyName);
            return Failure(error, component, null, context);
        }

        /// <summary>
        /// Combines multiple validation results into a single result.
        /// </summary>
        /// <param name="results">The results to combine</param>
        /// <param name="component">The component name for the combined result</param>
        /// <returns>A combined ValidationResult</returns>
        public static ValidationResult Combine(
            IEnumerable<ValidationResult> results,
            string component = null)
        {
            var resultList = results.AsValueEnumerable().ToList();
            if (resultList.Count == 0)
                return Success(component);

            var allErrors = resultList.AsValueEnumerable().SelectMany(r => r.Errors).ToList();
            var allWarnings = resultList.AsValueEnumerable().SelectMany(r => r.Warnings).ToList();
            var isValid = resultList.AsValueEnumerable().All(r => r.IsValid);

            var combinedContext = new Dictionary<string, object>();
            for (int i = 0; i < resultList.Count; i++)
            {
                var result = resultList[i];
                combinedContext[$"Result{i}.Component"] = result.Component;
                combinedContext[$"Result{i}.ValidatedAt"] = result.ValidatedAt;
                combinedContext[$"Result{i}.IsValid"] = result.IsValid;
                
                foreach (var kvp in result.Context)
                {
                    combinedContext[$"Result{i}.{kvp.Key}"] = kvp.Value;
                }
            }

            return new ValidationResult(
                isValid: isValid,
                errors: allErrors,
                warnings: allWarnings,
                component: component ?? "Combined",
                context: combinedContext);
        }

        /// <summary>
        /// Creates a new ValidationResult with additional warnings.
        /// </summary>
        /// <param name="additionalWarnings">The warnings to add</param>
        /// <returns>A new ValidationResult with the additional warnings</returns>
        public ValidationResult WithWarnings(IEnumerable<ValidationWarning> additionalWarnings)
        {
            var newWarnings = Warnings.AsValueEnumerable().Concat(additionalWarnings).ToList();
            return new ValidationResult(
                isValid: IsValid,
                errors: Errors,
                warnings: newWarnings,
                component: Component,
                context: Context);
        }

        /// <summary>
        /// Creates a new ValidationResult with additional context.
        /// </summary>
        /// <param name="additionalContext">The context to add</param>
        /// <returns>A new ValidationResult with the additional context</returns>
        public ValidationResult WithContext(IReadOnlyDictionary<string, object> additionalContext)
        {
            var newContext = new Dictionary<string, object>(Context);
            foreach (var kvp in additionalContext)
            {
                newContext[kvp.Key] = kvp.Value;
            }

            return new ValidationResult(
                isValid: IsValid,
                errors: Errors,
                warnings: Warnings,
                component: Component,
                context: newContext);
        }

        /// <summary>
        /// Gets all issues (errors and warnings) as a single collection.
        /// </summary>
        /// <returns>A collection of all validation issues</returns>
        public IEnumerable<ValidationIssue> GetAllIssues()
        {
            return Errors.AsValueEnumerable().Cast<ValidationIssue>().Concat(Warnings.AsValueEnumerable().Cast<ValidationIssue>()).ToList();
        }

        /// <summary>
        /// Gets a detailed string representation of the validation result.
        /// </summary>
        /// <returns>A detailed string representation</returns>
        public string GetDetailedString()
        {
            var lines = new List<string>
            {
                $"Validation Result for {Component}:",
                $"  Status: {(IsValid ? "Valid" : "Invalid")}",
                $"  Validated At: {ValidatedAt:yyyy-MM-dd HH:mm:ss}",
                $"  Errors: {Errors.Count}",
                $"  Warnings: {Warnings.Count}"
            };

            if (HasErrors)
            {
                lines.Add("  Errors:");
                foreach (var error in Errors)
                {
                    lines.Add($"    - {error}");
                }
            }

            if (HasWarnings)
            {
                lines.Add("  Warnings:");
                foreach (var warning in Warnings)
                {
                    lines.Add($"    - {warning}");
                }
            }

            if (Context.Count > 0)
            {
                lines.Add("  Context:");
                foreach (var kvp in Context)
                {
                    lines.Add($"    {kvp.Key}: {kvp.Value}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Throws an exception if the validation failed.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when validation failed</exception>
        public void ThrowIfInvalid()
        {
            if (!IsValid)
            {
                throw new ValidationException($"Validation failed for {Component}", this);
            }
        }
    }

    /// <summary>
    /// Base class for validation issues.
    /// </summary>
    public abstract record ValidationIssue
    {
        /// <summary>
        /// Gets the issue message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the property name associated with the issue, if any.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the severity of the issue.
        /// </summary>
        public abstract ValidationSeverity Severity { get; }

        /// <summary>
        /// Gets the timestamp when the issue was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationIssue record.
        /// </summary>
        /// <param name="message">The issue message</param>
        /// <param name="propertyName">The property name associated with the issue</param>
        protected ValidationIssue(string message, string propertyName = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            PropertyName = propertyName;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the validation issue.
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            var prefix = string.IsNullOrEmpty(PropertyName) ? "" : $"{PropertyName}: ";
            return $"{prefix}{Message}";
        }
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public sealed record ValidationError : ValidationIssue
    {
        /// <summary>
        /// Gets the severity of the error.
        /// </summary>
        public override ValidationSeverity Severity => ValidationSeverity.Error;

        /// <summary>
        /// Initializes a new instance of the ValidationError record.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="propertyName">The property name associated with the error</param>
        public ValidationError(string message, string propertyName = null) : base(message, propertyName)
        {
        }
    }

    /// <summary>
    /// Represents a validation warning.
    /// </summary>
    public sealed record ValidationWarning : ValidationIssue
    {
        /// <summary>
        /// Gets the severity of the warning.
        /// </summary>
        public override ValidationSeverity Severity => ValidationSeverity.Warning;

        /// <summary>
        /// Initializes a new instance of the ValidationWarning record.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="propertyName">The property name associated with the warning</param>
        public ValidationWarning(string message, string propertyName = null) : base(message, propertyName)
        {
        }
    }

    /// <summary>
    /// Defines the severity levels for validation issues.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Warning level - indicates a potential issue that doesn't prevent operation.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level - indicates a serious issue that prevents proper operation.
        /// </summary>
        Error
    }

    /// <summary>
    /// Exception thrown when validation fails.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the validation result that caused the exception.
        /// </summary>
        public ValidationResult ValidationResult { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="validationResult">The validation result that caused the exception</param>
        public ValidationException(string message, ValidationResult validationResult) : base(message)
        {
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="validationResult">The validation result that caused the exception</param>
        /// <param name="innerException">The inner exception</param>
        public ValidationException(string message, ValidationResult validationResult, Exception innerException) 
            : base(message, innerException)
        {
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }
    }
}