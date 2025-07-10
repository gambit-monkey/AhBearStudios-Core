using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Validation
{
    /// <summary>
    /// Framework-agnostic container validator implementation.
    /// Provides comprehensive validation with minimal allocations and high performance.
    /// </summary>
    public sealed class ContainerValidator : IContainerValidator
    {
        private static readonly HashSet<ContainerFramework> AllFrameworks = new HashSet<ContainerFramework>
        {
            ContainerFramework.VContainer,
            ContainerFramework.Reflex,
            ContainerFramework.Zenject,
            ContainerFramework.Microsoft
        };
        
        private readonly ValidationMetrics _metrics = new ValidationMetrics();
        
        /// <summary>
        /// Gets the frameworks this validator supports.
        /// </summary>
        public IReadOnlySet<ContainerFramework> SupportedFrameworks => AllFrameworks;
        
        /// <summary>
        /// Validates a container adapter.
        /// </summary>
        public IContainerValidationResult Validate(IContainerAdapter adapter, IDependencyInjectionConfig config)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            var registrations = GetRegistrations(adapter);
            
            // Validate individual registrations
            var validCount = 0;
            foreach (var registration in registrations)
            {
                var result = ValidateRegistration(registration);
                if (result.IsValid)
                {
                    validCount++;
                    if (result.Warning != null)
                        warnings.Add(result.Warning);
                }
                else
                {
                    if (result.Error != null)
                        errors.Add(result.Error);
                }
            }
            
            // Check for circular dependencies
            var hasCircularDependencies = HasCircularDependencies(registrations);
            if (hasCircularDependencies)
            {
                errors.Add(new ValidationError(
                    ValidationErrorType.CircularDependency,
                    "Circular dependencies detected in container registrations"));
            }
            
            stopwatch.Stop();
            var isValid = errors.Count == 0 && !hasCircularDependencies;
            
            // Record metrics
            _metrics.RecordValidation(
                stopwatch.Elapsed,
                registrations.Count,
                errors.Count,
                warnings.Count,
                hasCircularDependencies);
            
            return isValid
                ? ContainerValidationResult.Success(registrations.Count, stopwatch.Elapsed, warnings)
                : ContainerValidationResult.Failure(registrations.Count, validCount, errors, stopwatch.Elapsed, warnings, hasCircularDependencies);
        }
        
        /// <summary>
        /// Validates registrations for circular dependencies.
        /// </summary>
        public bool HasCircularDependencies(IEnumerable<ServiceRegistration> registrations)
        {
            if (registrations == null)
                return false;
            
            var dependencyGraph = BuildDependencyGraph(registrations);
            return DetectCycles(dependencyGraph);
        }
        
        /// <summary>
        /// Validates a specific service registration.
        /// </summary>
        public ServiceValidationResult ValidateRegistration(ServiceRegistration registration)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));
            
            // Factory and instance registrations are generally valid
            if (registration.IsFactory || registration.IsInstance)
            {
                return ServiceValidationResult.Success(registration);
            }
            
            var implementationType = registration.ImplementationType;
            if (implementationType == null)
            {
                return ServiceValidationResult.Failure(
                    registration,
                    new ValidationError(
                        ValidationErrorType.MissingDependency,
                        "Implementation type is null for non-factory/non-instance registration",
                        registration.ServiceType));
            }
            
            // Validate implementation type
            var validationError = ValidateImplementationType(implementationType, registration.ServiceType);
            if (validationError != null)
            {
                return ServiceValidationResult.Failure(registration, validationError);
            }
            
            // Check for potential warnings
            var warning = CheckForWarnings(registration);
            if (warning != null)
            {
                return ServiceValidationResult.Warning(registration, warning);
            }
            
            return ServiceValidationResult.Success(registration);
        }
        
        /// <summary>
        /// Gets performance metrics for validation operations.
        /// </summary>
        public ValidationMetrics GetMetrics()
        {
            return _metrics;
        }
        
        /// <summary>
        /// Gets registrations from the container adapter.
        /// This is a simplified version - actual implementation would need adapter-specific logic.
        /// </summary>
        private List<ServiceRegistration> GetRegistrations(IContainerAdapter adapter)
        {
            // This would need to be implemented differently for each framework
            // For now, return empty list as placeholder
            return new List<ServiceRegistration>();
        }
        
        /// <summary>
        /// Validates an implementation type.
        /// </summary>
        private ValidationError ValidateImplementationType(Type implementationType, Type serviceType)
        {
            if (implementationType.IsAbstract)
            {
                return new ValidationError(
                    ValidationErrorType.AbstractImplementation,
                    $"Implementation type '{implementationType.Name}' is abstract",
                    serviceType,
                    implementationType);
            }
            
            if (implementationType.IsInterface)
            {
                return new ValidationError(
                    ValidationErrorType.InterfaceImplementation,
                    $"Implementation type '{implementationType.Name}' is an interface",
                    serviceType,
                    implementationType);
            }
            
            if (implementationType.IsGenericTypeDefinition)
            {
                return new ValidationError(
                    ValidationErrorType.GenericTypeDefinition,
                    $"Implementation type '{implementationType.Name}' is a generic type definition",
                    serviceType,
                    implementationType);
            }
            
            var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                return new ValidationError(
                    ValidationErrorType.NoPublicConstructors,
                    $"Implementation type '{implementationType.Name}' has no public constructors",
                    serviceType,
                    implementationType);
            }
            
            if (!serviceType.IsAssignableFrom(implementationType))
            {
                return new ValidationError(
                    ValidationErrorType.RegistrationFailure,
                    $"Implementation type '{implementationType.Name}' is not assignable to service type '{serviceType.Name}'",
                    serviceType,
                    implementationType);
            }
            
            return null;
        }
        
        /// <summary>
        /// Checks for potential warnings in a registration.
        /// </summary>
        private ValidationWarning CheckForWarnings(ServiceRegistration registration)
        {
            if (registration.Lifetime == ServiceLifetime.Singleton &&
                registration.ImplementationType != null &&
                typeof(IDisposable).IsAssignableFrom(registration.ImplementationType))
            {
                return new ValidationWarning(
                    ValidationWarningType.SingletonDisposable,
                    $"Singleton service '{registration.ImplementationType.Name}' implements IDisposable - consider lifecycle management",
                    registration.ServiceType,
                    registration.ImplementationType);
            }
            
            var constructors = registration.ImplementationType?.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors?.Length > 1)
            {
                return new ValidationWarning(
                    ValidationWarningType.MultipleConstructors,
                    $"Implementation type '{registration.ImplementationType.Name}' has multiple constructors",
                    registration.ServiceType,
                    registration.ImplementationType);
            }
            
            return null;
        }
        
        /// <summary>
        /// Builds a dependency graph from registrations.
        /// </summary>
        private Dictionary<Type, List<Type>> BuildDependencyGraph(IEnumerable<ServiceRegistration> registrations)
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
        private IEnumerable<Type> GetTypeDependencies(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            
            if (primaryConstructor == null)
                return Enumerable.Empty<Type>();
            
            return primaryConstructor.GetParameters().Select(p => p.ParameterType);
        }
        
        /// <summary>
        /// Detects cycles in a dependency graph using depth-first search.
        /// </summary>
        private bool DetectCycles(Dictionary<Type, List<Type>> graph)
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
        private bool DetectCycleDFS(Type node, Dictionary<Type, List<Type>> graph,
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
    }
}