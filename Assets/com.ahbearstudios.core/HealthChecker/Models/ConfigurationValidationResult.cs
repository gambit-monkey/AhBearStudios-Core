using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the result of configuration validation.
/// </summary>
public readonly record struct ConfigurationValidationResult(
    /// <summary>
    /// Whether the configuration is valid.
    /// </summary>
    bool IsValid,

    /// <summary>
    /// Collection of validation errors if any.
    /// </summary>
    IReadOnlyList<string> Errors,

    /// <summary>
    /// Collection of validation warnings if any.
    /// </summary>
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Gets a successful validation result with no errors or warnings.
    /// </summary>
    public static ConfigurationValidationResult Success => new(true, Array.Empty<string>(), Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">Optional validation warnings.</param>
    /// <returns>A failed validation result.</returns>
    public static ConfigurationValidationResult Failure(IReadOnlyList<string> errors, IReadOnlyList<string> warnings = null)
    {
        return new ConfigurationValidationResult(false, errors, warnings ?? Array.Empty<string>());
    }

    /// <summary>
    /// Creates a validation result with warnings but no errors.
    /// </summary>
    /// <param name="warnings">The validation warnings.</param>
    /// <returns>A successful validation result with warnings.</returns>
    public static ConfigurationValidationResult WithWarnings(IReadOnlyList<string> warnings)
    {
        return new ConfigurationValidationResult(true, Array.Empty<string>(), warnings);
    }

    /// <summary>
    /// Gets whether there are any errors or warnings.
    /// </summary>
    public bool HasIssues => (Errors?.Count > 0) || (Warnings?.Count > 0);

    /// <summary>
    /// Gets the total number of issues (errors + warnings).
    /// </summary>
    public int TotalIssueCount => (Errors?.Count ?? 0) + (Warnings?.Count ?? 0);
}