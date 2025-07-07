namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Represents a validation warning found during container validation.
    /// </summary>
    public readonly record struct ValidationWarning
    {
        /// <summary>
        /// Gets the warning type.
        /// </summary>
        public ValidationWarningType WarningType { get; }

        /// <summary>
        /// Gets the warning message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the service type that caused the warning.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Gets the implementation type that caused the warning.
        /// </summary>
        public Type ImplementationType { get; }

        /// <summary>
        /// Initializes a new validation warning.
        /// </summary>
        public ValidationWarning(
            ValidationWarningType warningType,
            string message,
            Type serviceType = null,
            Type implementationType = null)
        {
            WarningType = warningType;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ServiceType = serviceType;
            ImplementationType = implementationType;
        }

        /// <summary>
        /// Returns a string representation of this warning.
        /// </summary>
        public override string ToString()
        {
            var result = $"[{WarningType}] {Message}";

            if (ServiceType != null)
                result += $" (Service: {ServiceType.Name})";

            if (ImplementationType != null)
                result += $" (Implementation: {ImplementationType.Name})";

            return result;
        }
    }
}