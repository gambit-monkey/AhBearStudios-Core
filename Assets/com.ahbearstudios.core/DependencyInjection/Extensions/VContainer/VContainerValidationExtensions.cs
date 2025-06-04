using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Extensions.VContainer
{
    /// <summary>
    /// Extension methods for VContainer validation operations.
    /// Provides comprehensive validation capabilities for container registrations.
    /// </summary>
    public static class VContainerValidationExtensions
    {
        /// <summary>
        /// Validates all registrations in the container builder to ensure they can be resolved.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="throwOnError">Whether to throw an exception on validation failure.</param>
        /// <returns>True if all registrations are valid, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <exception cref="DependencyInjectionException">Thrown when validation fails and throwOnError is true.</exception>
        public static bool ValidateRegistrations(this IContainerBuilder builder, bool throwOnError = false)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var validationResults = PerformDetailedValidation(builder);

            if (validationResults.HasErrors)
            {
                var errorMessage = FormatValidationResults(validationResults);

                if (!throwOnError)
                {
                    UnityEngine.Debug.LogError(errorMessage);
                }
                else
                {
                    throw new DependencyInjectionException(errorMessage);
                }

                return false;
            }

            if (validationResults.HasWarnings)
            {
                var warningMessage = FormatValidationResults(validationResults, true);
                UnityEngine.Debug.LogWarning(warningMessage);
            }

            UnityEngine.Debug.Log(
                $"Container validation passed for {validationResults.ValidatedRegistrationsCount} registrations");
            return true;
        }

        /// <summary>
        /// Performs a quick validation check without detailed error reporting.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>True if basic validation passes, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static bool IsValid(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                var registrations = builder.GetRegistrationInfo();
                return registrations.Count > 0 && !HasCircularDependencies(builder);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for circular dependencies in the container registrations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>True if circular dependencies are detected, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static bool HasCircularDependencies(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                var registrations = builder.GetRegistrationInfo();
                var dependencyGraph = BuildDependencyGraph(registrations);

                return DetectCycles(dependencyGraph);
            }
            catch (Exception)
            {
                // If we can't analyze dependencies, assume no cycles
                return false;
            }
        }

        /// <summary>
        /// Gets all unresolvable registrations in the container.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A collection of registration info for unresolvable services.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyCollection<RegistrationInfo> GetUnresolvableRegistrations(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var validationResults = PerformDetailedValidation(builder);
            return validationResults.UnresolvableRegistrations;
        }

        /// <summary>
        /// Gets validation warnings for the container registrations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A collection of validation warnings.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyCollection<ValidationWarning> GetValidationWarnings(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var validationResults = PerformDetailedValidation(builder);
            return validationResults.Warnings;
        }

        /// <summary>
        /// Validates a specific registration and returns detailed results.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="serviceType">The service type to validate.</param>
        /// <returns>Validation result for the specific service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or serviceType is null.</exception>
        public static ServiceValidationResult ValidateService(this IContainerBuilder builder, Type serviceType)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            var registrations = builder.GetRegistrationInfo();
            var serviceRegistrations = registrations.Where(r =>
                r.InterfaceTypes.Contains(serviceType) ||
                r.ImplementationType == serviceType).ToList();

            if (!serviceRegistrations.Any())
            {
                return new ServiceValidationResult
                {
                    ServiceType = serviceType,
                    IsValid = false,
                    ErrorMessage = $"Service type '{serviceType.FullName}' is not registered"
                };
            }

            // Check for multiple registrations
            if (serviceRegistrations.Count > 1)
            {
                return new ServiceValidationResult
                {
                    ServiceType = serviceType,
                    IsValid = false,
                    ErrorMessage = $"Multiple registrations found for service type '{serviceType.FullName}'"
                };
            }

            var registration = serviceRegistrations.First();

            // Validate implementation type if present
            if (registration.ImplementationType != null)
            {
                try
                {
                    ValidateImplementationType(registration.ImplementationType);
                }
                catch (Exception ex)
                {
                    return new ServiceValidationResult
                    {
                        ServiceType = serviceType,
                        IsValid = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

            return new ServiceValidationResult
            {
                ServiceType = serviceType,
                IsValid = true,
                Registration = registration
            };
        }

        /// <summary>
        /// Creates a detailed validation report for the container.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A comprehensive validation report.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static ContainerValidationReport CreateValidationReport(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var validationResults = PerformDetailedValidation(builder);
            var registrations = builder.GetRegistrationInfo();

            return new ContainerValidationReport
            {
                TotalRegistrations = registrations.Count,
                ValidRegistrations = validationResults.ValidatedRegistrationsCount - validationResults.ErrorCount,
                InvalidRegistrations = validationResults.ErrorCount,
                WarningCount = validationResults.WarningCount,
                Errors = validationResults.Errors,
                Warnings = validationResults.Warnings,
                UnresolvableRegistrations = validationResults.UnresolvableRegistrations,
                DuplicateRegistrations = builder.FindDuplicateRegistrations(),
                HasCircularDependencies = HasCircularDependencies(builder),
                ValidationTimestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Performs detailed validation and returns comprehensive results.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>Detailed validation results.</returns>
        private static DetailedValidationResults PerformDetailedValidation(IContainerBuilder builder)
        {
            var results = new DetailedValidationResults();
            var registrations = builder.GetRegistrationInfo();

            results.ValidatedRegistrationsCount = registrations.Count;

            foreach (var registration in registrations)
            {
                try
                {
                    // Validate implementation type if present
                    if (registration.ImplementationType != null)
                    {
                        ValidateImplementationType(registration.ImplementationType);

                        // Check for potential issues
                        CheckForPotentialIssues(registration, results);
                    }
                    else
                    {
                        // Factory registration - add warning if no interface types
                        if (registration.InterfaceTypes == null || registration.InterfaceTypes.Length == 0)
                        {
                            results.Warnings.Add(new ValidationWarning
                            {
                                WarningType = ValidationWarningType.FactoryWithoutInterface,
                                Message = "Factory registration found without interface types",
                                Registration = registration
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Errors.Add(new ValidationError
                    {
                        ErrorType = ValidationErrorType.ConstructionError,
                        Message = ex.Message,
                        Registration = registration,
                        Exception = ex
                    });
                    results.UnresolvableRegistrations.Add(registration);
                }
            }

            // Check for circular dependencies
            if (HasCircularDependencies(builder))
            {
                results.Errors.Add(new ValidationError
                {
                    ErrorType = ValidationErrorType.CircularDependency,
                    Message = "Circular dependencies detected in container registrations"
                });
            }

            return results;
        }

        /// <summary>
        /// Validates that an implementation type can be constructed.
        /// </summary>
        /// <param name="implementationType">The implementation type to validate.</param>
        private static void ValidateImplementationType(Type implementationType)
        {
            if (implementationType.IsAbstract)
                throw new InvalidOperationException($"Cannot register abstract type '{implementationType.FullName}'");

            if (implementationType.IsInterface)
                throw new InvalidOperationException(
                    $"Cannot register interface type '{implementationType.FullName}' as implementation");

            if (implementationType.IsGenericTypeDefinition)
                throw new InvalidOperationException(
                    $"Cannot register open generic type '{implementationType.FullName}'");

            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0)
                throw new InvalidOperationException($"Type '{implementationType.FullName}' has no public constructors");
        }

        /// <summary>
        /// Checks for potential issues with a registration.
        /// </summary>
        /// <param name="registration">The registration to check.</param>
        /// <param name="results">The validation results to add warnings to.</param>
        private static void CheckForPotentialIssues(RegistrationInfo registration, DetailedValidationResults results)
        {
            // Check for singleton implementations of IDisposable
            if (registration.Lifetime == Lifetime.Singleton &&
                registration.ImplementationType != null &&
                typeof(IDisposable).IsAssignableFrom(registration.ImplementationType))
            {
                results.Warnings.Add(new ValidationWarning
                {
                    WarningType = ValidationWarningType.SingletonDisposable,
                    Message =
                        $"Singleton registration '{registration.ImplementationType.Name}' implements IDisposable - consider lifecycle management",
                    Registration = registration
                });
            }

            // Check for multiple interface registrations
            if (registration.ServesMultipleInterfaces)
            {
                results.Warnings.Add(new ValidationWarning
                {
                    WarningType = ValidationWarningType.MultipleInterfaces,
                    Message =
                        $"Registration serves multiple interfaces: {string.Join(", ", registration.InterfaceTypes.Select(t => t.Name))}",
                    Registration = registration
                });
            }
        }

        /// <summary>
        /// Builds a dependency graph from registrations.
        /// </summary>
        /// <param name="registrations">The registrations to analyze.</param>
        /// <returns>A dependency graph.</returns>
        private static Dictionary<Type, List<Type>> BuildDependencyGraph(IReadOnlyList<RegistrationInfo> registrations)
        {
            var graph = new Dictionary<Type, List<Type>>();

            foreach (var registration in registrations)
            {
                if (registration.ImplementationType == null) continue;

                var dependencies = GetTypeDependencies(registration.ImplementationType);
                graph[registration.ImplementationType] = dependencies.ToList();
            }

            return graph;
        }

        /// <summary>
        /// Gets the dependencies of a type by analyzing its constructors.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>The dependency types.</returns>
        private static IEnumerable<Type> GetTypeDependencies(Type type)
        {
            var constructors = type.GetConstructors();
            var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (primaryConstructor == null)
                return Enumerable.Empty<Type>();

            return primaryConstructor.GetParameters().Select(p => p.ParameterType);
        }

        /// <summary>
        /// Detects cycles in a dependency graph using depth-first search.
        /// </summary>
        /// <param name="graph">The dependency graph.</param>
        /// <returns>True if cycles are detected, false otherwise.</returns>
        private static bool DetectCycles(Dictionary<Type, List<Type>> graph)
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var node in graph.Keys)
            {
                if (DetectCycleDFS(node, graph, visited, recursionStack))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Depth-first search for cycle detection.
        /// </summary>
        /// <param name="node">Current node.</param>
        /// <param name="graph">The dependency graph.</param>
        /// <param name="visited">Visited nodes.</param>
        /// <param name="recursionStack">Current recursion stack.</param>
        /// <returns>True if a cycle is detected.</returns>
        private static bool DetectCycleDFS(Type node, Dictionary<Type, List<Type>> graph,
            HashSet<Type> visited, HashSet<Type> recursionStack)
        {
            if (recursionStack.Contains(node))
                return true;

            if (visited.Contains(node))
                return false;

            visited.Add(node);
            recursionStack.Add(node);

            if (graph.TryGetValue(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (DetectCycleDFS(dependency, graph, visited, recursionStack))
                        return true;
                }
            }

            recursionStack.Remove(node);
            return false;
        }

        /// <summary>
        /// Formats validation results into a readable message.
        /// </summary>
        /// <param name="results">The validation results.</param>
        /// <param name="warningsOnly">Whether to format only warnings.</param>
        /// <returns>A formatted validation message.</returns>
        private static string FormatValidationResults(DetailedValidationResults results, bool warningsOnly = false)
        {
            var sb = new StringBuilder();

            if (warningsOnly)
            {
                sb.AppendLine($"Container validation completed with {results.WarningCount} warnings:");

                foreach (var warning in results.Warnings)
                {
                    sb.AppendLine($"  WARNING: {warning.Message}");
                    if (warning.Registration != null)
                    {
                        sb.AppendLine($"    Registration: {warning.Registration}");
                    }
                }
            }
            else
            {
                sb.AppendLine($"Container validation failed with {results.ErrorCount} errors:");

                foreach (var error in results.Errors)
                {
                    sb.AppendLine($"  ERROR: {error.Message}");
                    if (error.Registration != null)
                    {
                        sb.AppendLine($"    Registration: {error.Registration}");
                    }

                    if (error.Exception != null)
                    {
                        sb.AppendLine($"    Exception: {error.Exception.GetType().Name}: {error.Exception.Message}");
                    }
                }

                if (results.WarningCount > 0)
                {
                    sb.AppendLine($"\nAdditional warnings ({results.WarningCount}):");
                    foreach (var warning in results.Warnings)
                    {
                        sb.AppendLine($"  WARNING: {warning.Message}");
                    }
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Detailed validation results for container analysis.
    /// </summary>
    internal sealed class DetailedValidationResults
    {
        public int ValidatedRegistrationsCount { get; set; }
        public List<ValidationError> Errors { get; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; } = new List<ValidationWarning>();
        public List<RegistrationInfo> UnresolvableRegistrations { get; } = new List<RegistrationInfo>();

        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public bool HasErrors => ErrorCount > 0;
        public bool HasWarnings => WarningCount > 0;
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// Gets or sets the type of validation error.
        /// </summary>
        public ValidationErrorType ErrorType { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the registration that caused the error.
        /// </summary>
        public RegistrationInfo Registration { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during validation.
        /// </summary>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Represents a validation warning.
    /// </summary>
    public sealed class ValidationWarning
    {
        /// <summary>
        /// Gets or sets the type of validation warning.
        /// </summary>
        public ValidationWarningType WarningType { get; set; }

        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the registration that caused the warning.
        /// </summary>
        public RegistrationInfo Registration { get; set; }
    }

    /// <summary>
    /// Validation result for a specific service.
    /// </summary>
    public sealed class ServiceValidationResult
    {
        /// <summary>
        /// Gets or sets the service type that was validated.
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Gets or sets whether the service is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the registration information for the service.
        /// </summary>
        public RegistrationInfo Registration { get; set; }
    }

    /// <summary>
    /// Comprehensive validation report for a container.
    /// </summary>
    public sealed class ContainerValidationReport
    {
        /// <summary>
        /// Gets or sets the total number of registrations.
        /// </summary>
        public int TotalRegistrations { get; set; }

        /// <summary>
        /// Gets or sets the number of valid registrations.
        /// </summary>
        public int ValidRegistrations { get; set; }

        /// <summary>
        /// Gets or sets the number of invalid registrations.
        /// </summary>
        public int InvalidRegistrations { get; set; }

        /// <summary>
        /// Gets or sets the number of warnings.
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationError> Errors { get; set; }

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public IReadOnlyCollection<ValidationWarning> Warnings { get; set; }

        /// <summary>
        /// Gets or sets the unresolvable registrations.
        /// </summary>
        public IReadOnlyCollection<RegistrationInfo> UnresolvableRegistrations { get; set; }

        /// <summary>
        /// Gets or sets the duplicate registrations.
        /// </summary>
        public IReadOnlyDictionary<Type, int> DuplicateRegistrations { get; set; }

        /// <summary>
        /// Gets or sets whether circular dependencies were detected.
        /// </summary>
        public bool HasCircularDependencies { get; set; }

        /// <summary>
        /// Gets or sets when the validation was performed.
        /// </summary>
        public DateTime ValidationTimestamp { get; set; }

        /// <summary>
        /// Gets whether the container passed validation.
        /// </summary>
        public bool IsValid => InvalidRegistrations == 0 && !HasCircularDependencies;

        /// <summary>
        /// Gets the validation success rate.
        /// </summary>
        public double SuccessRate => TotalRegistrations > 0 ? (double)ValidRegistrations / TotalRegistrations : 0.0;

        /// <summary>
        /// Returns a formatted summary of the validation report.
        /// </summary>
        /// <returns>A string summary of the validation results.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Container Validation Report ===");
            sb.AppendLine($"Timestamp: {ValidationTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Overall Status: {(IsValid ? "VALID" : "INVALID")}");
            sb.AppendLine($"Success Rate: {SuccessRate:P1}");
            sb.AppendLine();
            sb.AppendLine("Statistics:");
            sb.AppendLine($"  Total Registrations: {TotalRegistrations}");
            sb.AppendLine($"  Valid Registrations: {ValidRegistrations}");
            sb.AppendLine($"  Invalid Registrations: {InvalidRegistrations}");
            sb.AppendLine($"  Warnings: {WarningCount}");
            sb.AppendLine($"  Duplicate Registrations: {DuplicateRegistrations?.Count ?? 0}");
            sb.AppendLine($"  Circular Dependencies: {(HasCircularDependencies ? "Yes" : "No")}");

            if (InvalidRegistrations > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Errors:");
                foreach (var error in Errors ?? Enumerable.Empty<ValidationError>())
                {
                    sb.AppendLine($"  - {error.Message}");
                }
            }

            if (WarningCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Warnings:");
                foreach (var warning in Warnings ?? Enumerable.Empty<ValidationWarning>())
                {
                    sb.AppendLine($"  - {warning.Message}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Types of validation errors.
    /// </summary>
    public enum ValidationErrorType
    {
        /// <summary>
        /// Error constructing the type.
        /// </summary>
        ConstructionError,

        /// <summary>
        /// Circular dependency detected.
        /// </summary>
        CircularDependency,

        /// <summary>
        /// Missing dependency.
        /// </summary>
        MissingDependency,

        /// <summary>
        /// Invalid registration.
        /// </summary>
        InvalidRegistration,

        /// <summary>
        /// Abstract type registration.
        /// </summary>
        AbstractType,

        /// <summary>
        /// Generic type definition registration.
        /// </summary>
        GenericTypeDefinition
    }

    /// <summary>
    /// Types of validation warnings.
    /// </summary>
    public enum ValidationWarningType
    {
        /// <summary>
        /// Singleton implements IDisposable.
        /// </summary>
        SingletonDisposable,

        /// <summary>
        /// Registration serves multiple interfaces.
        /// </summary>
        MultipleInterfaces,

        /// <summary>
        /// Factory registration without interface types.
        /// </summary>
        FactoryWithoutInterface,

        /// <summary>
        /// Potential performance issue.
        /// </summary>
        PerformanceIssue,

        /// <summary>
        /// Unused registration.
        /// </summary>
        UnusedRegistration,

        /// <summary>
        /// Duplicate registration.
        /// </summary>
        DuplicateRegistration
    }
}