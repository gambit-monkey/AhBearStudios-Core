using System;
using AhBearStudios.Core.Bootstrap.Interfaces;
using UnityEngine;
using VContainer;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    /// <summary>
    /// Base class for bootstrap installers that provides common functionality and lifecycle management.
    /// </summary>
    public abstract class BootstrapInstallerBase : IBootstrapInstaller
    {
        /// <summary>
        /// Gets the name of this installer for identification and logging purposes.
        /// </summary>
        public abstract string InstallerName { get; }
        
        /// <summary>
        /// Gets the priority of this installer. Lower values install first.
        /// Default priority is 100.
        /// </summary>
        public virtual int Priority => 100;
        
        /// <summary>
        /// Gets whether this installer is enabled and should be processed.
        /// Default is true.
        /// </summary>
        public virtual bool IsEnabled => true;
        
        /// <summary>
        /// Gets the dependencies required by this installer.
        /// These installers must be processed before this one.
        /// Default is no dependencies.
        /// </summary>
        public virtual Type[] Dependencies => Array.Empty<Type>();
        
        /// <summary>
        /// Installs dependencies into the specified container builder.
        /// </summary>
        /// <param name="builder">The container builder to install dependencies into.</param>
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
                
            if (!IsEnabled)
            {
                Debug.Log($"[{InstallerName}] Installer is disabled, skipping installation");
                return;
            }
            
            if (!ValidateInstaller())
            {
                Debug.LogError($"[{InstallerName}] Installer validation failed, skipping installation");
                return;
            }
            
            try
            {
                Debug.Log($"[{InstallerName}] Starting installation");
                
                PreInstall();
                InstallCore(builder);
                PostInstall();
                
                Debug.Log($"[{InstallerName}] Installation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{InstallerName}] Installation failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// Validates that this installer can be properly installed.
        /// Default implementation returns true.
        /// </summary>
        /// <returns>True if the installer is valid and can be installed.</returns>
        public virtual bool ValidateInstaller()
        {
            return true;
        }
        
        /// <summary>
        /// Called before the installer is processed to allow for pre-installation setup.
        /// Default implementation does nothing.
        /// </summary>
        public virtual void PreInstall()
        {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Called after the installer has been processed to allow for post-installation setup.
        /// Default implementation does nothing.
        /// </summary>
        public virtual void PostInstall()
        {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Core installation logic that must be implemented by derived classes.
        /// </summary>
        /// <param name="builder">The container builder to install dependencies into.</param>
        protected abstract void InstallCore(IContainerBuilder builder);
        
        /// <summary>
        /// Helper method to register a singleton service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        protected void RegisterSingleton<TInterface, TImplementation>(IContainerBuilder builder)
            where TImplementation : class, TInterface
        {
            builder.Register<TInterface, TImplementation>(Lifetime.Singleton);
        }
        
        /// <summary>
        /// Helper method to register a transient service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        protected void RegisterTransient<TInterface, TImplementation>(IContainerBuilder builder)
            where TImplementation : class, TInterface
        {
            builder.Register<TInterface, TImplementation>(Lifetime.Transient);
        }
        
        /// <summary>
        /// Helper method to register an instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="instance">The instance to register.</param>
        protected void RegisterInstance<T>(IContainerBuilder builder, T instance)
        {
            builder.RegisterInstance(instance);
        }
        
        /// <summary>
        /// Helper method to register a factory function.
        /// </summary>
        /// <typeparam name="T">The type to register.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <param name="factory">The factory function.</param>
        /// <param name="lifetime">The lifetime of the registration.</param>
        protected void RegisterFactory<T>(IContainerBuilder builder, Func<IObjectResolver, T> factory, Lifetime lifetime = Lifetime.Singleton)
        {
            builder.Register(factory, lifetime);
        }
        
        /// <summary>
        /// Helper method to check if a type is already registered.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>True if the type is already registered.</returns>
        protected bool IsRegistered<T>(IContainerBuilder builder)
        {
            // Note: VContainer doesn't provide a direct way to check registration
            // This would need to be implemented based on VContainer's internal API
            // For now, we'll assume it's not registered
            return false;
        }
        
        /// <summary>
        /// Helper method to log installation progress.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void LogInstallation(string message)
        {
            Debug.Log($"[{InstallerName}] {message}");
        }
        
        /// <summary>
        /// Helper method to log installation warnings.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{InstallerName}] {message}");
        }
        
        /// <summary>
        /// Helper method to log installation errors.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[{InstallerName}] {message}");
        }
    }
}