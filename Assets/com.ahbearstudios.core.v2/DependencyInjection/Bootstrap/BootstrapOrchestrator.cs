using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Bootstrap
{
    /// <summary>
    /// Implementation of bootstrap orchestrator that manages installer execution.
    /// Handles dependency ordering, validation, and error recovery with performance optimization.
    /// </summary>
    public sealed class BootstrapOrchestrator : IBootstrapOrchestrator
    {
        private readonly List<IFrameworkInstaller> _installers;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Initializes a new bootstrap orchestrator.
        /// </summary>
        public BootstrapOrchestrator()
        {
            _installers = new List<IFrameworkInstaller>();
        }
        
        /// <summary>
        /// Registers a framework installer.
        /// </summary>
        public void RegisterInstaller(IFrameworkInstaller installer)
        {
            if (installer == null)
                throw new ArgumentNullException(nameof(installer));
            
            lock (_lock)
            {
                // Prevent duplicate registrations
                if (_installers.Any(i => i.GetType() == installer.GetType()))
                {
                    throw new InvalidOperationException(
                        $"Installer of type '{installer.GetType().Name}' is already registered");
                }
                
                _installers.Add(installer);
            }
        }
        
        /// <summary>
        /// Registers multiple framework installers.
        /// </summary>
        public void RegisterInstallers(params IFrameworkInstaller[] installers)
        {
            if (installers == null)
                throw new ArgumentNullException(nameof(installers));
            
            foreach (var installer in installers)
            {
                RegisterInstaller(installer);
            }
        }
        
        /// <summary>
        /// Executes the bootstrap process for a container.
        /// </summary>
        public BootstrapResult Execute(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BootstrapError>();
            var warnings = new List<BootstrapWarning>();
            var successfulInstallers = new List<string>();
            var failedInstallers = new List<string>();
            
            try
            {
                // Phase 1: Validate
                var validationResult = Validate(config);
                if (!validationResult.IsValid)
                {
                    errors.AddRange(validationResult.Errors);
                    warnings.AddRange(validationResult.Warnings);
                    
                    if (config.ThrowOnValidationFailure)
                    {
                        return BootstrapResult.Failure(
                            container.Framework,
                            0,
                            stopwatch.Elapsed,
                            errors,
                            successfulInstallers,
                            failedInstallers,
                            warnings);
                    }
                }
                
                // Phase 2: Get ordered installers
                var orderedInstallers = GetOrderedInstallers(container.Framework);
                var enabledInstallers = orderedInstallers.Where(i => i.IsEnabled).ToList();
                
                if (config.EnableDebugLogging)
                {
                    Console.WriteLine($"[BootstrapOrchestrator] Executing {enabledInstallers.Count} installers for {container.Framework}");
                }
                
                // Phase 3: Execute installers
                foreach (var installer in enabledInstallers)
                {
                    try
                    {
                        // Pre-install
                        ExecutePhase(() => installer.PreInstall(config), 
                                   installer.InstallerName, BootstrapPhase.PreInstall, 
                                   errors, warnings, config);
                        
                        // Install
                        ExecutePhase(() => installer.Install(container, config),
                                   installer.InstallerName, BootstrapPhase.Install,
                                   errors, warnings, config);
                        
                        // Post-install
                        ExecutePhase(() => installer.PostInstall(container, config),
                                   installer.InstallerName, BootstrapPhase.PostInstall,
                                   errors, warnings, config);
                        
                        successfulInstallers.Add(installer.InstallerName);
                        
                        if (config.EnableDebugLogging)
                        {
                            Console.WriteLine($"[BootstrapOrchestrator] Successfully executed {installer.InstallerName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BootstrapError(
                            installer.InstallerName,
                            $"Installer execution failed: {ex.Message}",
                            BootstrapPhase.Install,
                            ex));
                        
                        failedInstallers.Add(installer.InstallerName);
                        
                        if (config.EnableDebugLogging)
                        {
                            Console.WriteLine($"[BootstrapOrchestrator] Failed to execute {installer.InstallerName}: {ex.Message}");
                        }
                        
                        // Continue with other installers unless it's a critical failure
                        if (config.ThrowOnValidationFailure)
                        {
                            break;
                        }
                    }
                }
                
                stopwatch.Stop();
                
                var isSuccess = errors.Count == 0 || !config.ThrowOnValidationFailure;
                return isSuccess
                    ? BootstrapResult.Success(container.Framework, successfulInstallers.Count, stopwatch.Elapsed, successfulInstallers, warnings)
                    : BootstrapResult.Failure(container.Framework, successfulInstallers.Count, stopwatch.Elapsed, errors, successfulInstallers, failedInstallers, warnings);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                errors.Add(new BootstrapError(
                    "BootstrapOrchestrator",
                    $"Bootstrap process failed: {ex.Message}",
                    BootstrapPhase.Install,
                    ex));
                
                return BootstrapResult.Failure(
                    container.Framework,
                    successfulInstallers.Count,
                    stopwatch.Elapsed,
                    errors,
                    successfulInstallers,
                    failedInstallers,
                    warnings);
            }
        }
        
        /// <summary>
        /// Validates all registered installers for the given configuration.
        /// </summary>
        public BootstrapValidationResult Validate(IDependencyInjectionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            var errors = new List<BootstrapError>();
            var warnings = new List<BootstrapWarning>();
            
            lock (_lock)
            {
                var frameworkInstallers = _installers
                    .Where(i => i.SupportedFramework == config.PreferredFramework)
                    .ToList();
                
                // Validate individual installers
                foreach (var installer in frameworkInstallers)
                {
                    try
                    {
                        if (!installer.ValidateInstaller(config))
                        {
                            errors.Add(new BootstrapError(
                                installer.InstallerName,
                                "Installer validation failed",
                                BootstrapPhase.Validation));
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BootstrapError(
                            installer.InstallerName,
                            $"Installer validation threw exception: {ex.Message}",
                            BootstrapPhase.Validation,
                            ex));
                    }
                }
                
                // Check for circular dependencies
                var hasCircularDependencies = HasCircularDependencies(frameworkInstallers);
                if (hasCircularDependencies)
                {
                    errors.Add(new BootstrapError(
                        "DependencyAnalyzer",
                        "Circular dependencies detected in installer dependency graph",
                        BootstrapPhase.DependencyOrdering));
                }
                
                // Get ordered installers if validation passed
                var orderedInstallers = new List<string>();
                if (errors.Count == 0)
                {
                    try
                    {
                        var ordered = TopologicalSort(frameworkInstallers);
                        orderedInstallers.AddRange(ordered.Select(i => i.InstallerName));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new BootstrapError(
                            "DependencyAnalyzer",
                            $"Failed to order installers: {ex.Message}",
                            BootstrapPhase.DependencyOrdering,
                            ex));
                    }
                }
                
                var isValid = errors.Count == 0;
                return isValid
                    ? BootstrapValidationResult.Success(frameworkInstallers.Count, orderedInstallers, warnings)
                    : BootstrapValidationResult.Failure(frameworkInstallers.Count, errors, warnings, hasCircularDependencies);
            }
        }
        
        /// <summary>
        /// Gets all registered installers ordered by priority and dependencies.
        /// </summary>
        public IReadOnlyList<IFrameworkInstaller> GetOrderedInstallers(ContainerFramework? framework = null)
        {
            lock (_lock)
            {
                var installers = framework.HasValue
                    ? _installers.Where(i => i.SupportedFramework == framework.Value).ToList()
                    : _installers.ToList();
                
                return TopologicalSort(installers);
            }
        }
        
        /// <summary>
        /// Clears all registered installers.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _installers.Clear();
            }
        }
        
        /// <summary>
        /// Executes a bootstrap phase with error handling.
        /// </summary>
        private void ExecutePhase(
            Action action,
            string installerName,
            BootstrapPhase phase,
            List<BootstrapError> errors,
            List<BootstrapWarning> warnings,
            IDependencyInjectionConfig config)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                errors.Add(new BootstrapError(
                    installerName,
                    $"{phase} failed: {ex.Message}",
                    phase,
                    ex));
                throw;
            }
        }
        
        /// <summary>
        /// Performs topological sort on installers based on dependencies.
        /// </summary>
        private IReadOnlyList<IFrameworkInstaller> TopologicalSort(List<IFrameworkInstaller> installers)
        {
            var graph = BuildDependencyGraph(installers);
            var visited = new HashSet<IFrameworkInstaller>();
            var result = new List<IFrameworkInstaller>();
            
            foreach (var installer in installers.OrderBy(i => i.Priority))
            {
                if (!visited.Contains(installer))
                {
                    TopologicalSortDFS(installer, graph, visited, result);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Depth-first search for topological sorting.
        /// </summary>
        private void TopologicalSortDFS(
            IFrameworkInstaller installer,
            Dictionary<IFrameworkInstaller, List<IFrameworkInstaller>> graph,
            HashSet<IFrameworkInstaller> visited,
            List<IFrameworkInstaller> result)
        {
            visited.Add(installer);
            
            if (graph.TryGetValue(installer, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        TopologicalSortDFS(dependency, graph, visited, result);
                    }
                }
            }
            
            result.Add(installer);
        }
        
        /// <summary>
        /// Builds a dependency graph for installers.
        /// </summary>
        private Dictionary<IFrameworkInstaller, List<IFrameworkInstaller>> BuildDependencyGraph(
            List<IFrameworkInstaller> installers)
        {
            var graph = new Dictionary<IFrameworkInstaller, List<IFrameworkInstaller>>();
            var installerLookup = installers.ToDictionary(i => i.GetType(), i => i);
            
            foreach (var installer in installers)
            {
                var dependencies = new List<IFrameworkInstaller>();
                
                foreach (var dependencyType in installer.Dependencies)
                {
                    if (installerLookup.TryGetValue(dependencyType, out var dependency))
                    {
                        dependencies.Add(dependency);
                    }
                }
                
                graph[installer] = dependencies;
            }
            
            return graph;
        }
        
        /// <summary>
        /// Checks for circular dependencies in installers.
        /// </summary>
        private bool HasCircularDependencies(List<IFrameworkInstaller> installers)
        {
            try
            {
                TopologicalSort(installers);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}