using System;
using System.Collections.Generic;
using System.Linq;
using Reflex.Core;
using UnityEngine;

namespace AhBearStudios.Core.Infrastructure.Bootstrap
{
    /// <summary>
    /// Project-wide bootstrap coordinator that manages the complete Reflex DI container lifecycle.
    /// Handles installer discovery, dependency resolution, and coordinated system initialization.
    /// Follows AhBearStudios Core Development Guidelines for Unity Game Development First approach.
    /// </summary>
    [DefaultExecutionOrder(-10000)] // Execute very early in the bootstrap process
    public class BootstrapManager : MonoBehaviour
    {
        [Header("Bootstrap Configuration")]
        [SerializeField] private bool _verboseLogging = false;
        [SerializeField] private bool _validateDependencies = true;
        [SerializeField] private bool _continueOnErrors = false;
        
        [Header("Installer Discovery")]
        [SerializeField] private bool _autoDiscoverInstallers = true;
        [SerializeField] private BootstrapInstaller[] _manualInstallers = Array.Empty<BootstrapInstaller>();
        
        [Header("Performance")]
        [SerializeField] private bool _enablePerformanceMetrics = true;
        [SerializeField] private bool _logInstallationTime = true;

        private Container _container;
        private readonly List<string> _bootstrapErrors = new List<string>();
        private float _bootstrapStartTime;

        #region Unity Lifecycle

        /// <summary>
        /// Unity Awake callback - performs the complete bootstrap process.
        /// </summary>
        private void Awake()
        {
            try
            {
                _bootstrapStartTime = Time.realtimeSinceStartup;
                
                LogInfo("Starting Reflex DI bootstrap process");
                
                // Step 1: Discover and collect all installers
                var installers = CollectInstallers();
                
                // Step 2: Validate dependencies and order installers
                var orderedInstallers = OrderInstallers(installers);
                
                // Step 3: Validate all installers
                if (!ValidateInstallers(orderedInstallers))
                {
                    HandleBootstrapFailure("Installer validation failed");
                    return;
                }
                
                // Step 4: Pre-installation phase
                if (!ExecutePreInstallPhase(orderedInstallers))
                {
                    HandleBootstrapFailure("Pre-installation phase failed");
                    return;
                }
                
                // Step 5: Installation phase - create container
                if (!ExecuteInstallPhase(orderedInstallers))
                {
                    HandleBootstrapFailure("Installation phase failed");
                    return;
                }
                
                // Step 6: Post-installation phase
                if (!ExecutePostInstallPhase(orderedInstallers))
                {
                    HandleBootstrapFailure("Post-installation phase failed");
                    return;
                }
                
                // Step 7: Finalize bootstrap
                FinalizeBootstrap();
                
                LogInfo($"Bootstrap completed successfully in {GetBootstrapTime():F3}s");
            }
            catch (Exception ex)
            {
                HandleBootstrapException(ex);
            }
        }

        /// <summary>
        /// Unity OnDestroy callback - cleanup resources.
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                _container?.Dispose();
                _container = null;
                
                LogInfo("Bootstrap container disposed");
            }
            catch (Exception ex)
            {
                LogError($"Error during bootstrap cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Installer Management

        /// <summary>
        /// Collects all installers from the scene and manual assignments.
        /// </summary>
        private List<IBootstrapInstaller> CollectInstallers()
        {
            var installers = new List<IBootstrapInstaller>();

            // Add manual installers
            foreach (var installer in _manualInstallers)
            {
                if (installer != null)
                {
                    installers.Add(installer);
                }
            }

            // Auto-discover installers if enabled
            if (_autoDiscoverInstallers)
            {
                var discoveredInstallers = FindObjectsOfType<MonoBehaviour>()
                    .OfType<IBootstrapInstaller>()
                    .Where(i => !installers.Contains(i))
                    .ToList();

                installers.AddRange(discoveredInstallers);
            }

            LogInfo($"Collected {installers.Count} installers");
            return installers;
        }

        /// <summary>
        /// Orders installers by priority and validates dependencies.
        /// </summary>
        private List<IBootstrapInstaller> OrderInstallers(List<IBootstrapInstaller> installers)
        {
            // Filter enabled installers
            var enabledInstallers = installers.Where(i => i.IsEnabled).ToList();
            
            if (_validateDependencies)
            {
                ValidateDependencyChain(enabledInstallers);
            }

            // Sort by priority (lower values first)
            var orderedInstallers = enabledInstallers
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.InstallerName)
                .ToList();

            LogInfo($"Ordered {orderedInstallers.Count} enabled installers by priority");
            
            if (_verboseLogging)
            {
                foreach (var installer in orderedInstallers)
                {
                    LogInfo($"  - {installer.InstallerName} (Priority: {installer.Priority})");
                }
            }

            return orderedInstallers;
        }

        /// <summary>
        /// Validates that all installer dependencies are satisfied.
        /// </summary>
        private void ValidateDependencyChain(List<IBootstrapInstaller> installers)
        {
            var installerTypes = installers.Select(i => i.GetType()).ToHashSet();
            
            foreach (var installer in installers)
            {
                foreach (var dependency in installer.Dependencies)
                {
                    if (!installerTypes.Contains(dependency))
                    {
                        var error = $"Installer {installer.InstallerName} depends on {dependency.Name} which is not available";
                        _bootstrapErrors.Add(error);
                        LogError(error);
                    }
                }
            }
        }

        #endregion

        #region Bootstrap Phases

        /// <summary>
        /// Executes the validation phase for all installers.
        /// </summary>
        private bool ValidateInstallers(List<IBootstrapInstaller> installers)
        {
            LogInfo("Executing installer validation phase");
            
            var hasErrors = false;
            
            foreach (var installer in installers)
            {
                try
                {
                    if (!installer.ValidateInstaller())
                    {
                        var error = $"Installer {installer.InstallerName} failed validation";
                        _bootstrapErrors.Add(error);
                        LogError(error);
                        hasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Installer {installer.InstallerName} validation threw exception: {ex.Message}";
                    _bootstrapErrors.Add(error);
                    LogError(error);
                    hasErrors = true;
                }
            }
            
            return !hasErrors || _continueOnErrors;
        }

        /// <summary>
        /// Executes the pre-installation phase for all installers.
        /// </summary>
        private bool ExecutePreInstallPhase(List<IBootstrapInstaller> installers)
        {
            LogInfo("Executing pre-installation phase");
            
            var hasErrors = false;
            
            foreach (var installer in installers)
            {
                try
                {
                    installer.PreInstall();
                }
                catch (Exception ex)
                {
                    var error = $"Installer {installer.InstallerName} pre-installation failed: {ex.Message}";
                    _bootstrapErrors.Add(error);
                    LogError(error);
                    hasErrors = true;
                }
            }
            
            return !hasErrors || _continueOnErrors;
        }

        /// <summary>
        /// Executes the installation phase - creates the container.
        /// </summary>
        private bool ExecuteInstallPhase(List<IBootstrapInstaller> installers)
        {
            LogInfo("Executing installation phase");
            
            try
            {
                var builder = new ContainerBuilder();
                var hasErrors = false;
                
                foreach (var installer in installers)
                {
                    try
                    {
                        installer.InstallBindings(builder);
                        LogInfo($"Installed {installer.InstallerName}");
                    }
                    catch (Exception ex)
                    {
                        var error = $"Installer {installer.InstallerName} installation failed: {ex.Message}";
                        _bootstrapErrors.Add(error);
                        LogError(error);
                        hasErrors = true;
                    }
                }
                
                if (hasErrors && !_continueOnErrors)
                {
                    return false;
                }
                
                _container = builder.Build();
                LogInfo("Container built successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Container build failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the post-installation phase for all installers.
        /// </summary>
        private bool ExecutePostInstallPhase(List<IBootstrapInstaller> installers)
        {
            LogInfo("Executing post-installation phase");
            
            var hasErrors = false;
            
            foreach (var installer in installers)
            {
                try
                {
                    installer.PostInstall(_container);
                }
                catch (Exception ex)
                {
                    var error = $"Installer {installer.InstallerName} post-installation failed: {ex.Message}";
                    _bootstrapErrors.Add(error);
                    LogError(error);
                    hasErrors = true;
                }
            }
            
            return !hasErrors || _continueOnErrors;
        }

        /// <summary>
        /// Finalizes the bootstrap process.
        /// </summary>
        private void FinalizeBootstrap()
        {
            // Make container globally accessible (if needed)
            // This could be done through a static accessor if required
            
            // Log performance metrics if enabled
            if (_enablePerformanceMetrics)
            {
                LogPerformanceMetrics();
            }
            
            // Log any warnings or errors
            if (_bootstrapErrors.Count > 0)
            {
                LogWarning($"Bootstrap completed with {_bootstrapErrors.Count} errors/warnings");
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Handles bootstrap failure.
        /// </summary>
        private void HandleBootstrapFailure(string reason)
        {
            LogError($"Bootstrap failed: {reason}");
            
            // Log all collected errors
            foreach (var error in _bootstrapErrors)
            {
                LogError($"  - {error}");
            }
            
            // Optionally disable the component or GameObject
            enabled = false;
        }

        /// <summary>
        /// Handles bootstrap exception.
        /// </summary>
        private void HandleBootstrapException(Exception ex)
        {
            LogError($"Bootstrap failed with exception: {ex.Message}");
            Debug.LogException(ex);
            
            enabled = false;
        }

        #endregion

        #region Logging and Metrics

        /// <summary>
        /// Logs performance metrics.
        /// </summary>
        private void LogPerformanceMetrics()
        {
            var bootstrapTime = GetBootstrapTime();
            LogInfo($"Bootstrap performance: {bootstrapTime:F3}s");
            
            if (_container != null)
            {
                // Log container statistics if available
                // This would depend on Reflex's container implementation
            }
        }

        /// <summary>
        /// Gets the bootstrap time in seconds.
        /// </summary>
        private float GetBootstrapTime()
        {
            return Time.realtimeSinceStartup - _bootstrapStartTime;
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        private void LogInfo(string message)
        {
            if (_verboseLogging)
            {
                Debug.Log($"[Bootstrap] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[Bootstrap] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[Bootstrap] {message}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the built container. Available after successful bootstrap.
        /// </summary>
        public Container Container => _container;

        /// <summary>
        /// Gets whether the bootstrap process completed successfully.
        /// </summary>
        public bool IsBootstrapped => _container != null;

        /// <summary>
        /// Gets the list of bootstrap errors that occurred.
        /// </summary>
        public IReadOnlyList<string> BootstrapErrors => _bootstrapErrors.AsReadOnly();

        #endregion
    }
}