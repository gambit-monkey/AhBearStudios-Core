namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Types of validation errors that can occur.
    /// </summary>
    public enum ValidationErrorType
    {
        /// <summary>
        /// Service cannot be constructed due to missing dependencies.
        /// </summary>
        MissingDependency,
        
        /// <summary>
        /// Circular dependency detected in the registration graph.
        /// </summary>
        CircularDependency,
        
        /// <summary>
        /// Abstract type registered as implementation.
        /// </summary>
        AbstractImplementation,
        
        /// <summary>
        /// Generic type definition registered as implementation.
        /// </summary>
        GenericTypeDefinition,
        
        /// <summary>
        /// Interface type registered as implementation.
        /// </summary>
        InterfaceImplementation,
        
        /// <summary>
        /// Type has no public constructors.
        /// </summary>
        NoPublicConstructors,
        
        /// <summary>
        /// Multiple constructors found without clear selection.
        /// </summary>
        AmbiguousConstructors,
        
        /// <summary>
        /// Registration failed for unknown reason.
        /// </summary>
        RegistrationFailure,
        
        /// <summary>
        /// Resolution failed during validation test.
        /// </summary>
        ResolutionFailure
    }
}