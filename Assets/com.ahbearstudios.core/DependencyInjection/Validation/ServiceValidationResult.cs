using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Validation
{
    /// <summary>
    /// Result of validating a specific service registration.
    /// </summary>
    public sealed class ServiceValidationResult
    {
        /// <summary>
        /// Gets the service registration that was validated.
        /// </summary>
        public ServiceRegistration Registration { get; }
        
        /// <summary>
        /// Gets whether the service registration is valid.
        /// </summary>
        public bool IsValid { get; }
        
        /// <summary>
        /// Gets the validation error if the registration is invalid.
        /// </summary>
        public ValidationError Error { get; }
        
        /// <summary>
        /// Gets the validation warning if applicable.
        /// </summary>
        public ValidationWarning Warning { get; }
        
        /// <summary>
        /// Gets additional validation details.
        /// </summary>
        public string Details { get; }
        
        /// <summary>
        /// Initializes a new service validation result.
        /// </summary>
        public ServiceValidationResult(
            ServiceRegistration registration,
            bool isValid,
            ValidationError error = null,
            ValidationWarning warning = null,
            string details = null)
        {
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
            IsValid = isValid;
            Error = error;
            Warning = warning;
            Details = details;
        }
        
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ServiceValidationResult Success(ServiceRegistration registration, string details = null)
        {
            return new ServiceValidationResult(registration, true, null, null, details);
        }
        
        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        public static ServiceValidationResult Failure(
            ServiceRegistration registration,
            ValidationError error,
            string details = null)
        {
            return new ServiceValidationResult(registration, false, error, null, details);
        }
        
        /// <summary>
        /// Creates a validation result with warning.
        /// </summary>
        public static ServiceValidationResult Warning(
            ServiceRegistration registration,
            ValidationWarning warning,
            string details = null)
        {
            return new ServiceValidationResult(registration, true, null, warning, details);
        }
    }
}