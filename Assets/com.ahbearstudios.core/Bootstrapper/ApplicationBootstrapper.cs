using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using AhBearStudios.Core.Bootstrap.Configuration;
using AhBearStudios.Core.Bootstrap.Installers;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.Bootstrap
{
    /// <summary>
    /// Main application bootstrapper that initializes all core systems using dependency injection.
    /// Provides robust error handling, validation, and platform optimization.
    /// </summary>
    public sealed class ApplicationBootstrapper : LifetimeScope
    {
        [Header("Configuration")]
        [SerializeField]
        private CoreSystemsConfig coreConfig;
        
        [SerializeField]
        private InstallerConfig installerConfig;
        
        [Header("Bootstrap Settings")]
        [SerializeField]
        private bool enableDevelopmentFeatures = true;
        
        [SerializeField]
        private bool enableBootstrapLogging = true;
        
        [SerializeField]
        private bool dontDestroyOnLoad = true;
        
        [SerializeField]
        private bool validateOnStart = true;
        
        [Header("Error Handling")]
        [SerializeField]
        private bool enableGracefulFallbacks = true;
        
        [SerializeField]
        private bool continueOnInstallerFailure = false;
        
        [SerializeField]
        private float bootstrapTimeoutSeconds = 30f;
        
        // Events for bootstrap lifecycle
        public static event Action<string> BootstrapStageChanged;
        public static event Action BootstrapCompleted;
        public static event Action<Exception> BootstrapFailed;
        
        // State tracking
        private bool isBootstrapped = false;
        private readonly List<string> installedSystems = new List<string>();
        private readonly List<Exception> bootstrapErrors = new List<Exception>();
        
        protected override void Awake()
        {
            // Ensure singleton behavior
            if (FindObjectsOfType<ApplicationBootstrapper>().Length > 1)
            {
                if (enableBootstrapLogging)
                    Debug.LogWarning("Multiple ApplicationBootstrapper instances found. Destroying duplicate.");
                    
                Destroy(gameObject);
                return;
            }
            
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
            
            // Start bootstrap process
            _ = BootstrapApplicationAsync();
        }
        
        protected override void Configure(IContainerBuilder builder)
        {
            // This method is called by VContainer's LifetimeScope
            // We'll configure the container in BootstrapApplicationAsync instead
        }
        
        private async Task BootstrapApplicationAsync()
        {
            try
            {
                if (enableBootstrapLogging)
                    Debug.Log("[Bootstrap] Starting application bootstrap process");
                
                BootstrapStageChanged?.Invoke("Initializing");
                
                // Phase 1: Validate configuration
                await ValidateConfigurationAsync();
                
                // Phase 2: Create optimized configuration
                var optimizedConfig = await CreateOptimizedConfigurationAsync();
                
                // Phase 3: Create dependency container
                await CreateDependencyContainerAsync(optimizedConfig);
                
                // Phase 4: Initialize core systems
                await InitializeCoreSystemsAsync();
                
                // Phase 5: Validate system health
                await ValidateSystemHealthAsync();
                
                // Phase 6: Signal completion
                await CompleteBootstrapAsync();
                
                isBootstrapped = true;
                
                if (enableBootstrapLogging)
                    Debug.Log($"[Bootstrap] Application bootstrap completed successfully. Installed systems: {string.Join(", ", installedSystems)}");
                
                BootstrapCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                await HandleBootstrapFailureAsync(ex);
            }
        }
        
        private async Task ValidateConfigurationAsync()
        {
            BootstrapStageChanged?.Invoke("Validating Configuration");
            
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Validating configuration");
            
            // Validate core configuration
            if (coreConfig == null)
                throw new InvalidOperationException("CoreSystemsConfig is required but not assigned");
            
            if (installerConfig == null)
                throw new InvalidOperationException("InstallerConfig is required but not assigned");
            
            // Validate system dependencies
            if (validateOnStart)
            {
                if (!coreConfig.ValidateSystemDependencies(out var coreErrors))
                {
                    var errorMessage = $"Core system dependencies validation failed:\n{string.Join("\n", coreErrors)}";
                    throw new InvalidOperationException(errorMessage);
                }
                
                if (!installerConfig.ValidateSystemDependencies(out var installerErrors))
                {
                    var errorMessage = $"Installer dependencies validation failed:\n{string.Join("\n", installerErrors)}";
                    throw new InvalidOperationException(errorMessage);
                }
            }
            
            // Simulate async validation
            await Task.Yield();
        }
        
        private async Task<(CoreSystemsConfig coreConfig, InstallerConfig installerConfig)> CreateOptimizedConfigurationAsync()
        {
            BootstrapStageChanged?.Invoke("Optimizing Configuration");
            
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Creating platform-optimized configuration");
            
            var optimizedCoreConfig = coreConfig.GetPlatformOptimizedConfig();
            var optimizedInstallerConfig = installerConfig.GetPlatformOptimizedConfig();
            
            // Apply development feature overrides
            if (!enableDevelopmentFeatures)
            {
                if (enableBootstrapLogging)
                    Debug.Log("[Bootstrap] Development features disabled, applying production optimizations");
                // Additional production optimizations could be applied here
            }
            
            await Task.Yield();
            return (optimizedCoreConfig, optimizedInstallerConfig);
        }
        
        private async Task CreateDependencyContainerAsync((CoreSystemsConfig coreConfig, InstallerConfig installerConfig) config)
        {
            BootstrapStageChanged?.Invoke("Creating DI Container");
            
        private async Task CreateDependencyContainerAsync((CoreSystemsConfig coreConfig, InstallerConfig installerConfig) config)
        {
            BootstrapStageChanged?.Invoke("Creating DI Container");
            
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Creating dependency injection container");
            
            // Configure the container using VContainer's LifetimeScope
            var builder = new ContainerBuilder();
            
            try
            {
                // Register configurations first
                RegisterConfigurations(builder, config.coreConfig, config.installerConfig);
                
                // Install core systems in dependency order
                await InstallCoreSystemsAsync(builder, config.installerConfig);
                
                // Register application lifecycle handlers
                RegisterApplicationLifecycle(builder);
                
                // Build the container
                Container = builder.Build();
                
                if (enableBootstrapLogging)
                    Debug.Log("[Bootstrap] Dependency container created successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create dependency container: {ex.Message}", ex);
            }
            
            await Task.Yield();
        }
        
        private void RegisterConfigurations(IContainerBuilder builder, CoreSystemsConfig coreConfig, InstallerConfig installerConfig)
        {
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Registering configurations");
            
            // Register core configurations
            builder.RegisterInstance(coreConfig);
            builder.RegisterInstance(installerConfig);
            
            // Register individual system configurations
            if (coreConfig.LoggingConfig != null)
                builder.RegisterInstance(coreConfig.LoggingConfig);
                
            if (coreConfig.PoolingConfig != null)
                builder.RegisterInstance(coreConfig.PoolingConfig);
                
            if (coreConfig.MessageBusConfig != null)
                builder.RegisterInstance(coreConfig.MessageBusConfig);
                
            if (coreConfig.ProfilingConfig != null)
                builder.RegisterInstance(coreConfig.ProfilingConfig);
        }
        
        private async Task InstallCoreSystemsAsync(IContainerBuilder builder, InstallerConfig installerConfig)
        {
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Installing core systems");
            
            var installers = CreateInstallers(installerConfig);
            var sortedInstallers = SortInstallersByDependencies(installers);
            
            foreach (var installer in sortedInstallers)
            {
                try
                {
                    if (!installer.IsEnabled)
                    {
                        if (enableBootstrapLogging)
                            Debug.Log($"[Bootstrap] Skipping disabled installer: {installer.InstallerName}");
                        continue;
                    }
                    
                    if (enableBootstrapLogging)
                        Debug.Log($"[Bootstrap] Installing: {installer.InstallerName}");
                    
                    BootstrapStageChanged?.Invoke($"Installing {installer.InstallerName}");
                    
                    installer.Install(builder);
                    installedSystems.Add(installer.InstallerName);
                    
                    // Allow other operations to run
                    await Task.Yield();
                }
                catch (Exception ex)
                {
                    var error = new InvalidOperationException($"Failed to install {installer.InstallerName}: {ex.Message}", ex);
                    bootstrapErrors.Add(error);
                    
                    if (enableBootstrapLogging)
                        Debug.LogError($"[Bootstrap] Installation failed for {installer.InstallerName}: {ex.Message}");
                    
                    if (!continueOnInstallerFailure)
                        throw error;
                }
            }
        }
        
        private List<IBootstrapInstaller> CreateInstallers(InstallerConfig config)
        {
            var installers = new List<IBootstrapInstaller>();
            
            // Core system installers (order matters due to dependencies)
            if (config.EnableLogging)
                installers.Add(new LoggingInstaller());
                
            if (config.EnableProfiling)
                installers.Add(new ProfilingInstaller());
                
            if (config.EnablePooling)
                installers.Add(new PoolingInstaller());
                
            if (config.EnableMessageBus)
                installers.Add(new MessageBusInstaller());
            
            // Optional system installers
            if (config.EnableAudio)
                installers.Add(new AudioInstaller());
                
            if (config.EnableInput)
                installers.Add(new InputInstaller());
                
            if (config.EnableSaveSystem)
                installers.Add(new SaveSystemInstaller());
                
            if (config.EnableSceneManagement)
                installers.Add(new SceneManagementInstaller());
                
            if (config.EnableUIFramework)
                installers.Add(new UIFrameworkInstaller());
            
            // Development system installers
            if (enableDevelopmentFeatures)
            {
                if (config.EnableDebugConsole)
                    installers.Add(new DebugConsoleInstaller());
                    
                if (config.EnablePerformanceHUD)
                    installers.Add(new PerformanceHUDInstaller());
                    
                if (config.EnableSystemDiagnostics)
                    installers.Add(new SystemDiagnosticsInstaller());
            }
            
            // Platform-specific installers
            if (config.EnableMobileSpecificSystems)
                installers.Add(new MobileSystemsInstaller());
                
            if (config.EnableConsoleSpecificSystems)
                installers.Add(new ConsoleSystemsInstaller());
                
            if (config.EnablePCSpecificSystems)
                installers.Add(new PCSystemsInstaller());
            
            return installers;
        }
        
        private List<IBootstrapInstaller> SortInstallersByDependencies(List<IBootstrapInstaller> installers)
        {
            // Topological sort based on dependencies and priority
            var sorted = new List<IBootstrapInstaller>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();
            
            void Visit(IBootstrapInstaller installer)
            {
                var installerType = installer.GetType();
                
                if (visiting.Contains(installerType))
                    throw new InvalidOperationException($"Circular dependency detected involving {installer.InstallerName}");
                
                if (visited.Contains(installerType))
                    return;
                
                visiting.Add(installerType);
                
                // Visit dependencies first
                foreach (var dependencyType in installer.Dependencies)
                {
                    var dependency = installers.FirstOrDefault(i => dependencyType.IsAssignableFrom(i.GetType()));
                    if (dependency != null)
                        Visit(dependency);
                }
                
                visiting.Remove(installerType);
                visited.Add(installerType);
                sorted.Add(installer);
            }
            
            // Sort by priority first, then apply dependency sorting
            var prioritySorted = installers.OrderBy(i => i.Priority).ToList();
            
            foreach (var installer in prioritySorted)
            {
                if (!visited.Contains(installer.GetType()))
                    Visit(installer);
            }
            
            return sorted;
        }
        
        private void RegisterApplicationLifecycle(IContainerBuilder builder)
        {
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Registering application lifecycle handlers");
            
            builder.RegisterEntryPoint<ApplicationInitializer>();
        }
        
        private async Task InitializeCoreSystemsAsync()
        {
            BootstrapStageChanged?.Invoke("Initializing Systems");
            
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Initializing core systems");
            
            // VContainer will automatically call IStartable.Start() on registered entry points
            // This happens after container construction
            
            await Task.Yield();
        }
        
        private async Task ValidateSystemHealthAsync()
        {
            BootstrapStageChanged?.Invoke("Validating System Health");
            
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Validating system health");
            
            try
            {
                // Basic health checks - attempt to resolve core systems
                if (coreConfig.EnableLogging)
                {
                    var logger = Container.Resolve<Logging.IBurstLogger>();
                    if (logger == null)
                        throw new InvalidOperationException("Failed to resolve logging system");
                }
                
                if (coreConfig.EnableProfiling)
                {
                    var profiler = Container.Resolve<Profiling.Interfaces.IProfilerService>();
                    if (profiler == null)
                        throw new InvalidOperationException("Failed to resolve profiling system");
                }
                
                if (coreConfig.EnableMessageBus)
                {
                    var messageBus = Container.Resolve<MessageBus.Interfaces.IMessageBusService>();
                    if (messageBus == null)
                        throw new InvalidOperationException("Failed to resolve message bus system");
                }
                
                if (coreConfig.EnablePooling)
                {
                    var poolRegistry = Container.Resolve<Pooling.Interfaces.IPoolRegistry>();
                    if (poolRegistry == null)
                        throw new InvalidOperationException("Failed to resolve pooling system");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"System health validation failed: {ex.Message}", ex);
            }
            
            await Task.Yield();
        }
        
        private async Task CompleteBootstrapAsync()
        {
            BootstrapStageChanged?.Invoke("Completing Bootstrap");
            
            if (enableBootstrapLogging)
            {
                Debug.Log("[Bootstrap] Bootstrap process completing");
                
                if (bootstrapErrors.Count > 0)
                {
                    Debug.LogWarning($"[Bootstrap] Bootstrap completed with {bootstrapErrors.Count} warnings/errors:");
                    foreach (var error in bootstrapErrors)
                    {
                        Debug.LogWarning($"[Bootstrap] - {error.Message}");
                    }
                }
            }
            
            // Final initialization steps
            await Task.Yield();
        }
        
        private async Task HandleBootstrapFailureAsync(Exception ex)
        {
            if (enableBootstrapLogging)
                Debug.LogError($"[Bootstrap] Bootstrap failed: {ex.Message}\n{ex.StackTrace}");
            
            BootstrapFailed?.Invoke(ex);
            
            if (enableGracefulFallbacks)
            {
                if (enableBootstrapLogging)
                    Debug.Log("[Bootstrap] Attempting graceful fallback");
                
                try
                {
                    await AttemptMinimalBootstrapAsync();
                    
                    if (enableBootstrapLogging)
                        Debug.Log("[Bootstrap] Minimal bootstrap succeeded");
                        
                    return;
                }
                catch (Exception fallbackEx)
                {
                    if (enableBootstrapLogging)
                        Debug.LogError($"[Bootstrap] Minimal bootstrap also failed: {fallbackEx.Message}");
                }
            }
            
            // Show critical error to user
            ShowCriticalErrorUI(ex);
        }
        
        private async Task AttemptMinimalBootstrapAsync()
        {
            if (enableBootstrapLogging)
                Debug.Log("[Bootstrap] Attempting minimal system bootstrap");
            
            // Try to bootstrap just the essential systems (logging only)
            var builder = new ContainerBuilder();
            
            // Register minimal configuration
            var minimalConfig = ScriptableObject.CreateInstance<CoreSystemsConfig>();
            builder.RegisterInstance(minimalConfig);
            
            // Install only logging
            var loggingInstaller = new LoggingInstaller();
            loggingInstaller.Install(builder);
            
            Container = builder.Build();
            
            installedSystems.Clear();
            installedSystems.Add("Minimal Logging");
            
            await Task.Yield();
        }
        
        private void ShowCriticalErrorUI(Exception ex)
        {
            // In a real implementation, this would show a user-friendly error dialog
            var errorMessage = $"Application failed to start due to a critical error:\n\n{ex.Message}\n\nPlease restart the application.";
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("Critical Bootstrap Error", errorMessage, "OK");
#else
            Debug.LogError($"[Bootstrap] CRITICAL ERROR: {errorMessage}");
            
            // Could implement platform-specific error dialogs here
            Application.Quit();
#endif
        }
        
        /// <summary>
        /// Gets whether the application has been successfully bootstrapped.
        /// </summary>
        public bool IsBootstrapped => isBootstrapped;
        
        /// <summary>
        /// Gets the list of successfully installed systems.
        /// </summary>
        public IReadOnlyList<string> InstalledSystems => installedSystems.AsReadOnly();
        
        /// <summary>
        /// Gets any errors that occurred during bootstrap.
        /// </summary>
        public IReadOnlyList<Exception> BootstrapErrors => bootstrapErrors.AsReadOnly();
        
        /// <summary>
        /// Forces a re-bootstrap of the application (use with caution).
        /// </summary>
        public async Task ForceRebootstrapAsync()
        {
            if (enableBootstrapLogging)
                Debug.LogWarning("[Bootstrap] Force re-bootstrap requested");
            
            isBootstrapped = false;
            installedSystems.Clear();
            bootstrapErrors.Clear();
            
            await BootstrapApplicationAsync();
        }
    }
    
    // Placeholder installer classes - these would be actual implementations
    internal class PoolingInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Pooling System";
        public override int Priority => 30;
        public override Type[] Dependencies => new[] { typeof(LoggingInstaller), typeof(ProfilingInstaller) };
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class MessageBusInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "MessageBusService System";
        public override int Priority => 40;
        public override Type[] Dependencies => new[] { typeof(LoggingInstaller), typeof(ProfilingInstaller), typeof(PoolingInstaller) };
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class AudioInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Audio System";
        public override int Priority => 100;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class InputInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Input System";
        public override int Priority => 100;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class SaveSystemInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Save System";
        public override int Priority => 100;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class SceneManagementInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Scene Management";
        public override int Priority => 100;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class UIFrameworkInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "UI Framework";
        public override int Priority => 100;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class DebugConsoleInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Debug Console";
        public override int Priority => 200;
        public override Type[] Dependencies => new[] { typeof(UIFrameworkInstaller) };
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class PerformanceHUDInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Performance HUD";
        public override int Priority => 200;
        public override Type[] Dependencies => new[] { typeof(ProfilingInstaller) };
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class SystemDiagnosticsInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "System Diagnostics";
        public override int Priority => 200;
        public override Type[] Dependencies => new[] { typeof(ProfilingInstaller) };
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class MobileSystemsInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Mobile Systems";
        public override int Priority => 300;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class ConsoleSystemsInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "Console Systems";
        public override int Priority => 300;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
    
    internal class PCSystemsInstaller : BootstrapInstallerBase
    {
        public override string InstallerName => "PC Systems";
        public override int Priority => 300;
        protected override void InstallCore(IContainerBuilder builder) { /* Implementation */ }
    }
}