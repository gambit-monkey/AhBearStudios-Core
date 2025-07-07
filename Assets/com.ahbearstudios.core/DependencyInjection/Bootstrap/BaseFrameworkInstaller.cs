using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Bootstrap
{
    /// <summary>
    /// Base implementation of IFrameworkInstaller providing common functionality.
    /// Framework-specific installers should inherit from this for consistent behavior.
    /// </summary>
    public abstract class BaseFrameworkInstaller : IFrameworkInstaller
    {
        /// <summary>
        /// Gets the framework this installer supports.
        /// </summary>
        public abstract ContainerFramework SupportedFramework { get; }
        
        /// <summary>
        /// Gets the installer name for identification and logging.
        /// </summary>
        public virtual string InstallerName => GetType().Name;
        
        /// <summary>
        /// Gets the priority of this installer. Lower values install first.
        /// </summary>
        public virtual int Priority => 1000;
        
        /// <summary>
        /// Gets whether this installer is enabled and should be processed.
        /// </summary>
        public virtual bool IsEnabled => true;
        
        /// <summary>
        /// Gets the dependencies required by this installer.
        /// </summary>
        public virtual Type[] Dependencies => Array.Empty<Type>();
        
        /// <summary>
        /// Validates that this installer can be properly executed.
        /// </summary>
        public virtual bool ValidateInstaller(IDependencyInjectionConfig config)
        {
            if (config == null)
                return false;
            
            // Check if the framework matches
            if (config.PreferredFramework != SupportedFramework)
            {
                return false;
            }
            
            // Perform custom validation
            return DoValidateInstaller(config);
        }
        
        /// <summary>
        /// Called before the installer is processed to allow for pre-installation setup.
        /// </summary>
        public virtual void PreInstall(IDependencyInjectionConfig config)
        {
            LogIfEnabled(config, $"Pre-installing {InstallerName}");
            DoPreInstall(config);
        }
        
        /// <summary>
        /// Installs services into the container adapter.
        /// </summary>
        public void Install(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            LogIfEnabled(config, $"Installing {InstallerName}");
            
            try
            {
                DoInstall(container, config);
                LogIfEnabled(config, $"Successfully installed {InstallerName}");
            }
            catch (Exception ex)
            {
                LogIfEnabled(config, $"Failed to install {InstallerName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Called after the installer has been processed to allow for post-installation setup.
        /// </summary>
        public virtual void PostInstall(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            LogIfEnabled(config, $"Post-installing {InstallerName}");
            DoPostInstall(container, config);
        }
        
        /// <summary>
        /// Framework-specific validation logic.
        /// </summary>
        protected virtual bool DoValidateInstaller(IDependencyInjectionConfig config)
        {
            return true;
        }
        
        /// <summary>
        /// Framework-specific pre-installation logic.
        /// </summary>
        protected virtual void DoPreInstall(IDependencyInjectionConfig config)
        {
            // Default: no-op
        }
        
        /// <summary>
        /// Framework-specific installation logic.
        /// </summary>
        protected abstract void DoInstall(IContainerAdapter container, IDependencyInjectionConfig config);
        
        /// <summary>
        /// Framework-specific post-installation logic.
        /// </summary>
        protected virtual void DoPostInstall(IContainerAdapter container, IDependencyInjectionConfig config)
        {
            // Default: no-op
        }
        
        /// <summary>
        /// Logs a message if debug logging is enabled.
        /// </summary>
        protected void LogIfEnabled(IDependencyInjectionConfig config, string message)
        {
            if (config.EnableDebugLogging)
            {
                Console.WriteLine($"[{SupportedFramework}Installer] {message}");
            }
        }
    }
}