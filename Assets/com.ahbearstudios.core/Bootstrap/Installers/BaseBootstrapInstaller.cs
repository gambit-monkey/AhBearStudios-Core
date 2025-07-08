using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerts;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Alerts.Interfaces;
using AhBearStudios.Core.Bootstrap.Messages;
using AhBearStudios.Core.Bootstrap.Models;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;
using VContainer;

namespace AhBearStudios.Core.Bootstrap.Implementation
{
    /// <summary>
    /// Production-ready base implementation of IBootstrapInstaller providing comprehensive functionality
    /// for system installation with integrated logging, health monitoring, profiling, and error handling.
    /// 
    /// Follows development guidelines with complete error handling, resource management,
    /// thread safety, and integration with all core systems including logging, messaging,
    /// profiling, health checks, and alerting.
    /// 
    /// Provides robust production capabilities including:
    /// - Comprehensive validation and dependency checking
    /// - Performance monitoring and metrics collection
    /// - Health check registration and monitoring
    /// - Alert configuration and threshold management
    /// - Recovery scenarios and graceful degradation
    /// - Hot-reload support for development scenarios
    /// - Thread-safe operations and resource management
    /// </summary>
    public abstract class BaseBootstrapInstaller : IBootstrapInstaller, IDisposable
    {
        #region Constants

        private const int DEFAULT_INSTALL_TIMEOUT_MS = 30000;
        private const int DEFAULT_VALIDATION_TIMEOUT_MS = 5000;
        private const double DEFAULT_INSTALL_TIME_WARNING_THRESHOLD_MS = 1000.0;
        private const double DEFAULT_INSTALL_TIME_CRITICAL_THRESHOLD_MS = 5000.0;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the logger for this installer.
        /// </summary>
        protected ILoggingService Logger { get; private set; }

        /// <summary>
        /// Gets the message bus service for this installer.
        /// </summary>
        protected IMessageBusService MessageBusService { get; private set; }

        /// <summary>
        /// Gets the profiler service for performance monitoring.
        /// </summary>
        protected IProfilerService ProfilerService { get; private set; }

        /// <summary>
        /// Gets the health check service for system monitoring.
        /// </summary>
        protected IHealthCheckService HealthCheckService { get; private set; }

        /// <summary>
        /// Gets the alert service for critical notifications.
        /// </summary>
        protected IAlertService AlertService { get; private set; }

        /// <summary>
        /// Gets the container for dependency resolution during installation.
        /// </summary>
        protected IObjectResolver Container { get; private set; }

        /// <summary>
        /// Gets whether this installer has been successfully installed.
        /// </summary>
        protected bool IsInstalled { get; private set; }

        /// <summary>
        /// Gets whether this installer has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets whether hot-reload is enabled for this installer.
        /// </summary>
        protected bool IsHotReloadEnabled { get; private set; }

        /// <summary>
        /// Gets the validation errors encountered during installation.
        /// </summary>
        protected IReadOnlyList<string> ValidationErrors => _validationErrors.ToList().AsReadOnly();

        /// <summary>
        /// Gets the warnings encountered during installation.
        /// </summary>
        protected IReadOnlyList<string> Warnings => _warnings.ToList().AsReadOnly();

        /// <summary>
        /// Gets the installation metrics for this installer.
        /// </summary>
        protected InstallationMetrics Metrics => _metrics;

        #endregion

        #region Private Fields

        private readonly ConcurrentBag<string> _validationErrors = new ConcurrentBag<string>();
        private readonly ConcurrentBag<string> _warnings = new ConcurrentBag<string>();
        private readonly ConcurrentDictionary<string, object> _contextData = new ConcurrentDictionary<string, object>();
        private readonly SemaphoreSlim _installationSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _lockObject = new object();
        
        private InstallationMetrics _metrics = new InstallationMetrics();
        private CancellationTokenSource _cancellationTokenSource;
        private IDisposable _profilerSession;
        private IHealthCheckRegistration _healthCheckRegistration;
        private bool _isRecoveryMode;
        private int _recoveryAttempts;

        #endregion

        #region IBootstrapInstaller Implementation

        /// <summary>
        /// Gets the name of this installer for identification and logging purposes.
        /// </summary>
        public abstract string InstallerName { get; }

        /// <summary>
        /// Gets the priority of this installer. Lower values install first.
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// Gets whether this installer is enabled and should be processed.
        /// Default implementation returns true, override to provide conditional logic.
        /// </summary>
        public virtual bool IsEnabled => true;

        /// <summary>
        /// Gets the dependencies required by this installer.
        /// These installers must be processed before this one.
        /// </summary>
        public virtual Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// Gets the maximum installation timeout in milliseconds.
        /// Override to provide installer-specific timeout.
        /// </summary>
        public virtual int InstallTimeoutMs => DEFAULT_INSTALL_TIMEOUT_MS;

        /// <summary>
        /// Gets the installation time warning threshold in milliseconds.
        /// Override to provide installer-specific threshold.
        /// </summary>
        public virtual double InstallTimeWarningThresholdMs => DEFAULT_INSTALL_TIME_WARNING_THRESHOLD_MS;

        /// <summary>
        /// Gets the installation time critical threshold in milliseconds.
        /// Override to provide installer-specific threshold.
        /// </summary>
        public virtual double InstallTimeCriticalThresholdMs => DEFAULT_INSTALL_TIME_CRITICAL_THRESHOLD_MS;

        /// <summary>
        /// Gets whether this installer supports hot-reload.
        /// Override to enable hot-reload capabilities.
        /// </summary>
        public virtual bool SupportsHotReload => false;

        /// <summary>
        /// Gets the maximum number of recovery attempts.
        /// Override to provide installer-specific recovery limits.
        /// </summary>
        public virtual int MaxRecoveryAttempts => 3;

        /// <summary>
        /// Validates that this installer can be properly installed.
        /// </summary>
        /// <returns>True if the installer is valid and can be installed.</returns>
        public virtual bool ValidateInstaller()
        {
            using var validationSession = ProfilerService?.BeginScope("Validation", InstallerName);
            
            try
            {
                _metrics.ValidationStartTime = DateTime.UtcNow;
                ClearValidationResults();

                // Thread-safe validation
                lock (_lockObject)
                {
                    if (IsDisposed)
                    {
                        AddValidationError("Installer has been disposed");
                        return false;
                    }

                    if (IsInstalled && !SupportsHotReload)
                    {
                        AddValidationWarning("Installer has already been installed and does not support hot-reload");
                    }
                }

                // Validate with timeout
                using var cts = new CancellationTokenSource(DEFAULT_VALIDATION_TIMEOUT_MS);
                var validationTask = Task.Run(() => PerformValidation(), cts.Token);

                try
                {
                    var isValid = validationTask.GetAwaiter().GetResult();
                    _metrics.ValidationEndTime = DateTime.UtcNow;
                    _metrics.ValidationDuration = _metrics.ValidationEndTime - _metrics.ValidationStartTime;
                    
                    LogValidationResults();
                    RecordValidationMetrics(isValid);
                    
                    return isValid;
                }
                catch (OperationCanceledException)
                {
                    AddValidationError("Validation timed out");
                    TriggerAlert("Validation timeout", AlertSeverity.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddValidationError($"Validation failed with exception: {ex.Message}");
                Logger?.LogException(ex, $"Validation failed for installer {InstallerName}");
                TriggerAlert($"Validation exception: {ex.Message}", AlertSeverity.Critical);
                return false;
            }
        }

        /// <summary>
        /// Called before the installer is processed to allow for pre-installation setup.
        /// </summary>
        public virtual void PreInstall()
        {
            using var preInstallSession = ProfilerService?.BeginScope("PreInstall", InstallerName);
            
            try
            {
                Logger?.LogInfo($"Starting pre-installation for {InstallerName}");
                _metrics.PreInstallStartTime = DateTime.UtcNow;

                // Initialize cancellation token
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource(InstallTimeoutMs);

                // Perform pre-installation tasks
                OnPreInstall();

                _metrics.PreInstallEndTime = DateTime.UtcNow;
                _metrics.PreInstallDuration = _metrics.PreInstallEndTime - _metrics.PreInstallStartTime;
                
                Logger?.LogInfo($"Pre-installation completed for {InstallerName} in {_metrics.PreInstallDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Pre-installation failed for {InstallerName}");
                TriggerAlert($"Pre-installation failed: {ex.Message}", AlertSeverity.Critical);
                
                if (ShouldAttemptRecovery())
                {
                    AttemptRecovery(nameof(PreInstall), ex);
                }
                else
                {
                    throw new InvalidOperationException($"Pre-installation failed for {InstallerName}", ex);
                }
            }
        }

        /// <summary>
        /// Installs services into the container.
        /// </summary>
        /// <param name="builder">The container builder to configure.</param>
        public async void Install(IContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            await _installationSemaphore.WaitAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
            
            try
            {
                using var installSession = ProfilerService?.BeginScope("Install", InstallerName);
                
                Logger?.LogInfo($"Installing services for {InstallerName}");
                _metrics.InstallStartTime = DateTime.UtcNow;

                // Perform core installation
                await PerformInstallationAsync(builder);

                _metrics.InstallEndTime = DateTime.UtcNow;
                _metrics.InstallDuration = _metrics.InstallEndTime - _metrics.InstallStartTime;
                
                // Check installation performance
                CheckInstallationPerformance();
                
                Logger?.LogInfo($"Service installation completed for {InstallerName} in {_metrics.InstallDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Service installation failed for {InstallerName}");
                TriggerAlert($"Installation failed: {ex.Message}", AlertSeverity.Critical);
                
                if (ShouldAttemptRecovery())
                {
                    await AttemptRecoveryAsync(nameof(Install), ex, () => OnInstall(builder));
                }
                else
                {
                    throw new InvalidOperationException($"Service installation failed for {InstallerName}", ex);
                }
            }
            finally
            {
                _installationSemaphore.Release();
            }
        }

        /// <summary>
        /// Called after the installer has been processed to allow for post-installation setup.
        /// </summary>
        public virtual void PostInstall()
        {
            using var postInstallSession = ProfilerService?.BeginScope("PostInstall", InstallerName);
            
            try
            {
                Logger?.LogInfo($"Starting post-installation for {InstallerName}");
                _metrics.PostInstallStartTime = DateTime.UtcNow;

                // Resolve core services
                ResolveCoreServices();

                // Perform post-installation tasks
                OnPostInstall();

                // Register health checks
                RegisterHealthChecks();

                // Configure alerts
                ConfigureAlerts();

                // Enable hot-reload if supported
                ConfigureHotReload();

                // Mark as successfully installed
                lock (_lockObject)
                {
                    IsInstalled = true;
                }

                _metrics.PostInstallEndTime = DateTime.UtcNow;
                _metrics.PostInstallDuration = _metrics.PostInstallEndTime - _metrics.PostInstallStartTime;
                _metrics.TotalInstallDuration = _metrics.PostInstallEndTime - _metrics.PreInstallStartTime;

                Logger?.LogInfo($"Post-installation completed for {InstallerName} in {_metrics.PostInstallDuration.TotalMilliseconds:F2}ms (Total: {_metrics.TotalInstallDuration.TotalMilliseconds:F2}ms)");

                // Publish installation completed event
                PublishInstallationEvent();

                // Record final metrics
                RecordInstallationMetrics();
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Post-installation failed for {InstallerName}");
                TriggerAlert($"Post-installation failed: {ex.Message}", AlertSeverity.Critical);
                
                if (ShouldAttemptRecovery())
                {
                    AttemptRecovery(nameof(PostInstall), ex);
                }
                else
                {
                    throw new InvalidOperationException($"Post-installation failed for {InstallerName}", ex);
                }
            }
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Performs the actual service registration. Must be implemented by derived classes.
        /// </summary>
        /// <param name="builder">The container builder to configure.</param>
        protected abstract void OnInstall(IContainerBuilder builder);

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Validates dependencies required by this installer.
        /// Override to provide custom dependency validation logic.
        /// </summary>
        /// <returns>True if dependencies are valid.</returns>
        protected virtual bool ValidateDependencies()
        {
            if (Dependencies == null || Dependencies.Length == 0)
                return true;

            foreach (var dependencyType in Dependencies)
            {
                if (dependencyType == null)
                {
                    AddValidationError("Null dependency type found");
                    continue;
                }

                if (!typeof(IBootstrapInstaller).IsAssignableFrom(dependencyType))
                {
                    AddValidationError($"Dependency {dependencyType.Name} does not implement IBootstrapInstaller");
                }
            }

            return !_validationErrors.Any();
        }

        /// <summary>
        /// Validates configuration required by this installer.
        /// Override to provide custom configuration validation logic.
        /// </summary>
        /// <returns>True if configuration is valid.</returns>
        protected virtual bool ValidateConfiguration()
        {
            // Base implementation assumes configuration is valid
            // Override in derived classes to add specific validation
            return true;
        }

        /// <summary>
        /// Validates custom requirements specific to the installer.
        /// Override to provide installer-specific validation logic.
        /// </summary>
        /// <returns>True if custom requirements are met.</returns>
        protected virtual bool ValidateCustomRequirements()
        {
            // Base implementation assumes requirements are met
            // Override in derived classes to add specific validation
            return true;
        }

        /// <summary>
        /// Validates system resources and dependencies.
        /// Override to provide resource-specific validation logic.
        /// </summary>
        /// <returns>True if system resources are available.</returns>
        protected virtual bool ValidateSystemResources()
        {
            // Check available memory, disk space, etc.
            // Base implementation assumes resources are available
            return true;
        }

        /// <summary>
        /// Called during PreInstall to perform installer-specific setup.
        /// Override to provide custom pre-installation logic.
        /// </summary>
        protected virtual void OnPreInstall()
        {
            // Default implementation does nothing
            // Override in derived classes to add specific logic
        }

        /// <summary>
        /// Called during PostInstall to perform installer-specific finalization.
        /// Override to provide custom post-installation logic.
        /// </summary>
        protected virtual void OnPostInstall()
        {
            // Default implementation does nothing
            // Override in derived classes to add specific logic
        }

        /// <summary>
        /// Creates health checks for this installer.
        /// Override to provide installer-specific health checks.
        /// </summary>
        /// <returns>Collection of health checks to register.</returns>
        protected virtual IEnumerable<IHealthCheck> CreateHealthChecks()
        {
            // Return default installer health check
            yield return new InstallerHealthCheck(this);
        }

        /// <summary>
        /// Configures alert thresholds for this installer.
        /// Override to provide installer-specific alert configuration.
        /// </summary>
        protected virtual void ConfigureInstallerAlerts()
        {
            // Default implementation configures basic alerts
            // Override in derived classes to add specific alert configuration
        }

        /// <summary>
        /// Attempts recovery from installation failure.
        /// Override to provide installer-specific recovery logic.
        /// </summary>
        /// <param name="phase">The installation phase that failed.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <returns>True if recovery was successful.</returns>
        protected virtual bool OnRecoveryAttempt(string phase, Exception exception)
        {
            // Default implementation performs basic recovery
            Logger?.LogWarning($"Attempting recovery for {InstallerName} in phase {phase}: {exception.Message}");
            
            // Clear any partial state
            ClearValidationResults();
            
            // Reset installation state if needed
            if (phase == nameof(Install))
            {
                lock (_lockObject)
                {
                    IsInstalled = false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Configures hot-reload capabilities for this installer.
        /// Override to provide installer-specific hot-reload logic.
        /// </summary>
        protected virtual void OnConfigureHotReload()
        {
            if (SupportsHotReload)
            {
                Logger?.LogInfo($"Hot-reload enabled for {InstallerName}");
                IsHotReloadEnabled = true;
                
                // Subscribe to configuration change events if message bus is available
                if (MessageBusService != null)
                {
                    MessageBusService.SubscribeToMessage<ConfigurationChangedMessage>(OnConfigurationChanged);
                }
            }
        }

        /// <summary>
        /// Resolves core services after container is built.
        /// Override to resolve additional services specific to the installer.
        /// </summary>
        protected virtual void ResolveCoreServices()
        {
            try
            {
                // Try to resolve core services if available
                if (Container != null)
                {
                    Logger = ResolveService<ILoggingService>();
                    MessageBusService = ResolveService<IMessageBusService>();
                    ProfilerService = ResolveService<IProfilerService>();
                    HealthCheckService = ResolveService<IHealthCheckService>();
                    AlertService = ResolveService<IAlertService>();
                }
            }
            catch (Exception ex)
            {
                // Log if logger is available, otherwise just capture in warnings
                if (Logger != null)
                {
                    Logger.LogException(ex, $"Failed to resolve core services for {InstallerName}");
                }
                else
                {
                    AddValidationWarning($"Failed to resolve core services: {ex.Message}");
                }
            }
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Sets the container for dependency resolution.
        /// </summary>
        /// <param name="container">The container to use for resolution.</param>
        protected void SetContainer(IObjectResolver container)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Adds a validation error to the error collection in a thread-safe manner.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        protected void AddValidationError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                _validationErrors.Add($"[{InstallerName}] {error}");
            }
        }

        /// <summary>
        /// Adds a validation warning to the warning collection in a thread-safe manner.
        /// </summary>
        /// <param name="warning">The warning message to add.</param>
        protected void AddValidationWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                _warnings.Add($"[{InstallerName}] {warning}");
            }
        }

        /// <summary>
        /// Stores context data for use during installation in a thread-safe manner.
        /// </summary>
        /// <param name="key">The key for the context data.</param>
        /// <param name="value">The value to store.</param>
        protected void SetContextData(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _contextData.AddOrUpdate(key, value, (k, v) => value);
            }
        }

        /// <summary>
        /// Retrieves context data stored during installation in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">The type of the context data.</typeparam>
        /// <param name="key">The key for the context data.</param>
        /// <returns>The context data if found and of the correct type, otherwise default.</returns>
        protected T GetContextData<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !_contextData.TryGetValue(key, out var value))
                return default;

            return value is T typedValue ? typedValue : default;
        }

        /// <summary>
        /// Checks if a service type is already registered in the container.
        /// </summary>
        /// <typeparam name="T">The service type to check.</typeparam>
        /// <returns>True if the service is registered, false otherwise.</returns>
        protected bool IsServiceRegistered<T>()
        {
            return Container?.TryResolve<T>(out _) ?? false;
        }

        /// <summary>
        /// Safely resolves a service from the container.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service or default if not available.</returns>
        protected T ResolveService<T>()
        {
            return Container != null && Container.TryResolve<T>(out var service) ? service : default;
        }

        /// <summary>
        /// Triggers an alert using the alert service if available.
        /// </summary>
        /// <param name="message">The alert message.</param>
        /// <param name="severity">The alert severity.</param>
        protected void TriggerAlert(string message, AlertSeverity severity)
        {
            try
            {
                if (AlertService != null && !string.IsNullOrWhiteSpace(message))
                {
                    var alertMessage = new FixedString128Bytes(message);
                    var source = new FixedString64Bytes(InstallerName);
                    var tag = new FixedString64Bytes("Bootstrap");
                    
                    AlertService.RaiseAlert(alertMessage, severity, source, tag);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Failed to trigger alert for {InstallerName}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Clears validation results in a thread-safe manner.
        /// </summary>
        private void ClearValidationResults()
        {
            while (_validationErrors.TryTake(out _)) { }
            while (_warnings.TryTake(out _)) { }
        }

        /// <summary>
        /// Performs comprehensive validation.
        /// </summary>
        /// <returns>True if validation passes.</returns>
        private bool PerformValidation()
        {
            return ValidateDependencies() &&
                   ValidateConfiguration() &&
                   ValidateCustomRequirements() &&
                   ValidateSystemResources();
        }

        /// <summary>
        /// Performs installation with timeout and error handling.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private async Task PerformInstallationAsync(IContainerBuilder builder)
        {
            var installTask = Task.Run(() => OnInstall(builder), _cancellationTokenSource.Token);
            
            try
            {
                await installTask;
            }
            catch (OperationCanceledException)
            {
                TriggerAlert("Installation timed out", AlertSeverity.Critical);
                throw new TimeoutException($"Installation timed out for {InstallerName}");
            }
        }

        /// <summary>
        /// Checks installation performance against thresholds.
        /// </summary>
        private void CheckInstallationPerformance()
        {
            var installTime = _metrics.InstallDuration.TotalMilliseconds;
            
            if (installTime > InstallTimeCriticalThresholdMs)
            {
                TriggerAlert($"Installation took {installTime:F2}ms (critical threshold: {InstallTimeCriticalThresholdMs}ms)", AlertSeverity.Critical);
            }
            else if (installTime > InstallTimeWarningThresholdMs)
            {
                TriggerAlert($"Installation took {installTime:F2}ms (warning threshold: {InstallTimeWarningThresholdMs}ms)", AlertSeverity.Warning);
            }
        }

        /// <summary>
        /// Registers health checks for this installer.
        /// </summary>
        private void RegisterHealthChecks()
        {
            try
            {
                if (HealthCheckService != null)
                {
                    var healthChecks = CreateHealthChecks();
                    foreach (var healthCheck in healthChecks)
                    {
                        _healthCheckRegistration = HealthCheckService.RegisterHealthCheck(healthCheck);
                    }
                    
                    Logger?.LogInfo($"Registered health checks for {InstallerName}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Failed to register health checks for {InstallerName}");
                AddValidationWarning($"Health check registration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures alerts for this installer.
        /// </summary>
        private void ConfigureAlerts()
        {
            try
            {
                if (AlertService != null)
                {
                    // Set minimum severity for this installer
                    AlertService.SetMinimumSeverity(AlertSeverity.Warning);
                    
                    // Configure installer-specific alerts
                    ConfigureInstallerAlerts();
                    
                    Logger?.LogInfo($"Configured alerts for {InstallerName}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Failed to configure alerts for {InstallerName}");
                AddValidationWarning($"Alert configuration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures hot-reload if supported.
        /// </summary>
        private void ConfigureHotReload()
        {
            try
            {
                if (SupportsHotReload)
                {
                    OnConfigureHotReload();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Failed to configure hot-reload for {InstallerName}");
                AddValidationWarning($"Hot-reload configuration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles configuration change events for hot-reload.
        /// </summary>
        /// <param name="message">The configuration change message.</param>
        private void OnConfigurationChanged(ConfigurationChangedMessage message)
        {
            try
            {
                if (IsHotReloadEnabled && message.AffectedInstaller == InstallerName)
                {
                    Logger?.LogInfo($"Hot-reloading configuration for {InstallerName}");
                    
                    // Trigger re-validation and potential re-installation
                    if (ValidateInstaller())
                    {
                        // Perform hot-reload specific logic
                        TriggerAlert("Configuration reloaded successfully", AlertSeverity.Info);
                    }
                    else
                    {
                        TriggerAlert("Configuration reload validation failed", AlertSeverity.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Hot-reload failed for {InstallerName}");
                TriggerAlert($"Hot-reload failed: {ex.Message}", AlertSeverity.Warning);
            }
        }

        /// <summary>
        /// Determines if recovery should be attempted.
        /// </summary>
        /// <returns>True if recovery should be attempted.</returns>
        private bool ShouldAttemptRecovery()
        {
            return !_isRecoveryMode && _recoveryAttempts < MaxRecoveryAttempts;
        }

        /// <summary>
        /// Attempts recovery from failure.
        /// </summary>
        /// <param name="phase">The phase that failed.</param>
        /// <param name="exception">The exception that occurred.</param>
        private void AttemptRecovery(string phase, Exception exception)
        {
            try
            {
                _isRecoveryMode = true;
                _recoveryAttempts++;
                
                Logger?.LogWarning($"Attempting recovery {_recoveryAttempts}/{MaxRecoveryAttempts} for {InstallerName} in phase {phase}");
                
                if (OnRecoveryAttempt(phase, exception))
                {
                    Logger?.LogInfo($"Recovery successful for {InstallerName}");
                    TriggerAlert($"Recovery successful after {_recoveryAttempts} attempts", AlertSeverity.Info);
                }
                else
                {
                    Logger?.LogError($"Recovery failed for {InstallerName}");
                    TriggerAlert($"Recovery failed after {_recoveryAttempts} attempts", AlertSeverity.Critical);
                    throw new InvalidOperationException($"Recovery failed for {InstallerName} in phase {phase}", exception);
                }
            }
            finally
            {
                _isRecoveryMode = false;
            }
        }

        /// <summary>
        /// Attempts async recovery from failure.
        /// </summary>
        /// <param name="phase">The phase that failed.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="retryAction">The action to retry.</param>
        private async Task AttemptRecoveryAsync(string phase, Exception exception, Action retryAction)
        {
            try
            {
                _isRecoveryMode = true;
                _recoveryAttempts++;
                
                Logger?.LogWarning($"Attempting async recovery {_recoveryAttempts}/{MaxRecoveryAttempts} for {InstallerName} in phase {phase}");
                
                if (OnRecoveryAttempt(phase, exception))
                {
                    // Retry the failed operation
                    await Task.Run(retryAction, _cancellationTokenSource?.Token ?? CancellationToken.None);
                    
                    Logger?.LogInfo($"Async recovery successful for {InstallerName}");
                    TriggerAlert($"Async recovery successful after {_recoveryAttempts} attempts", AlertSeverity.Info);
                }
                else
                {
                    Logger?.LogError($"Async recovery failed for {InstallerName}");
                    TriggerAlert($"Async recovery failed after {_recoveryAttempts} attempts", AlertSeverity.Critical);
                    throw new InvalidOperationException($"Async recovery failed for {InstallerName} in phase {phase}", exception);
                }
            }
            finally
            {
                _isRecoveryMode = false;
            }
        }

        /// <summary>
        /// Logs the validation results.
        /// </summary>
        private void LogValidationResults()
        {
            var errors = _validationErrors.ToList();
            var warnings = _warnings.ToList();
            
            if (errors.Count > 0)
            {
                var errorMessage = $"Validation failed for {InstallerName}: {string.Join("; ", errors)}";
                Logger?.LogError(errorMessage);
            }

            if (warnings.Count > 0)
            {
                var warningMessage = $"Validation warnings for {InstallerName}: {string.Join("; ", warnings)}";
                Logger?.LogWarning(warningMessage);
            }

            if (errors.Count == 0 && warnings.Count == 0)
            {
                Logger?.LogInfo($"Validation passed for {InstallerName}");
            }
        }

        /// <summary>
        /// Records validation metrics.
        /// </summary>
        /// <param name="isValid">Whether validation was successful.</param>
        private void RecordValidationMetrics(bool isValid)
        {
            _metrics.IsValidationSuccessful = isValid;
            _metrics.ValidationErrorCount = _validationErrors.Count;
            _metrics.ValidationWarningCount = _warnings.Count;
            
            // Record metrics with profiler if available
            if (ProfilerService != null)
            {
                ProfilerService.GetMetrics(new ProfilerTag($"{InstallerName}_Validation"));
            }
        }

        /// <summary>
        /// Records final installation metrics.
        /// </summary>
        private void RecordInstallationMetrics()
        {
            _metrics.IsInstallationSuccessful = IsInstalled;
            _metrics.RecoveryAttempts = _recoveryAttempts;
            
            // Log performance metrics
            Logger?.LogInfo($"Installation metrics for {InstallerName}: " +
                           $"Total: {_metrics.TotalInstallDuration.TotalMilliseconds:F2}ms, " +
                           $"Validation: {_metrics.ValidationDuration.TotalMilliseconds:F2}ms, " +
                           $"Install: {_metrics.InstallDuration.TotalMilliseconds:F2}ms, " +
                           $"PostInstall: {_metrics.PostInstallDuration.TotalMilliseconds:F2}ms, " +
                           $"Errors: {_metrics.ValidationErrorCount}, " +
                           $"Warnings: {_metrics.ValidationWarningCount}, " +
                           $"Recovery Attempts: {_metrics.RecoveryAttempts}");
        }

        /// <summary>
        /// Publishes an installation completed event if message bus is available.
        /// </summary>
        private void PublishInstallationEvent()
        {
            try
            {
                if (MessageBusService != null)
                {
                    var installationEvent = new InstallerCompletedMessage
                    {
                        Id = Guid.NewGuid(),
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        TypeCode = 1001,
                        InstallerName = InstallerName,
                        Priority = Priority,
                        InstallDuration = _metrics.TotalInstallDuration,
                        ErrorCount = _metrics.ValidationErrorCount,
                        WarningCount = _metrics.ValidationWarningCount,
                        RecoveryAttempts = _metrics.RecoveryAttempts,
                        IsSuccessful = IsInstalled,
                        SupportsHotReload = SupportsHotReload,
                        IsHotReloadEnabled = IsHotReloadEnabled
                    };

                    MessageBusService.PublishMessage(installationEvent);
                    Logger?.LogInfo($"Published installation completed event for {InstallerName}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex, $"Failed to publish installation event for {InstallerName}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
            {
                try
                {
                    Logger?.LogInfo($"Disposing installer {InstallerName}");

                    // Perform cleanup
                    OnDispose();

                    // Dispose resources
                    _cancellationTokenSource?.Dispose();
                    _profilerSession?.Dispose();
                    _healthCheckRegistration?.Dispose();
                    _installationSemaphore?.Dispose();

                    // Clear collections
                    ClearValidationResults();
                    _contextData.Clear();

                    // Clear references
                    Logger = null;
                    MessageBusService = null;
                    ProfilerService = null;
                    HealthCheckService = null;
                    AlertService = null;
                    Container = null;

                    lock (_lockObject)
                    {
                        IsDisposed = true;
                    }

                    Logger?.LogInfo($"Installer {InstallerName} disposed successfully");
                }
                catch (Exception ex)
                {
                    // Log disposal error if logger is still available
                    Logger?.LogException(ex, $"Error disposing installer {InstallerName}");
                }
            }
        }

        /// <summary>
        /// Called during disposal to perform installer-specific cleanup.
        /// Override to provide custom disposal logic.
        /// </summary>
        protected virtual void OnDispose()
        {
            // Default implementation does nothing
            // Override in derived classes to add specific logic
        }

        #endregion

        #region Nested Types

        

        /// <summary>
        /// Default health check implementation for installers.
        /// </summary>
        protected class InstallerHealthCheck : IHealthCheck
        {
            private readonly BaseBootstrapInstaller _installer;
            private readonly FixedString64Bytes _name;

            public FixedString64Bytes Name => _name;

            public InstallerHealthCheck(BaseBootstrapInstaller installer)
            {
                _installer = installer ?? throw new ArgumentNullException(nameof(installer));
                _name = new FixedString64Bytes($"{installer.InstallerName}_Health");
            }

            public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
            {
                try
                {
                    if (_installer.IsDisposed)
                    {
                        return HealthCheckResult.Unhealthy($"Installer {_installer.InstallerName} has been disposed");
                    }

                    if (!_installer.IsInstalled)
                    {
                        return HealthCheckResult.Unhealthy($"Installer {_installer.InstallerName} is not installed");
                    }

                    // Check if there are any validation errors
                    if (_installer.ValidationErrors.Any())
                    {
                        return HealthCheckResult.Degraded($"Installer {_installer.InstallerName} has validation errors: {string.Join(", ", _installer.ValidationErrors)}");
                    }

                    // Check if recovery attempts were made
                    if (_installer._recoveryAttempts > 0)
                    {
                        return HealthCheckResult.Degraded($"Installer {_installer.InstallerName} required {_installer._recoveryAttempts} recovery attempts");
                    }

                    // All checks passed
                    return HealthCheckResult.Healthy($"Installer {_installer.InstallerName} is healthy");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"Health check failed for installer {_installer.InstallerName}: {ex.Message}", ex);
                }
            }
        }

        #endregion
    }
}