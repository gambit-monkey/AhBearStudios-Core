using System;
using Reflex.Core;
using UnityEngine;

namespace AhBearStudios.Core.Infrastructure.Bootstrap
{
    /// <summary>
    /// Abstract base class for Reflex DI installers that require MonoBehaviour functionality.
    /// Provides Unity serialization support for configuration while maintaining proper Reflex patterns.
    /// Follows AhBearStudios Core Development Guidelines for Unity Game Development First approach.
    /// </summary>
    public abstract class BootstrapInstaller : MonoBehaviour, IBootstrapInstaller
    {
        [Header("Bootstrap Settings")]
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private bool _verboseLogging = false;
        
        #region IBootstrapInstaller Implementation

        /// <inheritdoc />
        public abstract string InstallerName { get; }

        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <inheritdoc />
        public virtual bool IsEnabled => _isEnabled && enabled;

        /// <inheritdoc />
        public virtual Type[] Dependencies => Array.Empty<Type>();

        /// <inheritdoc />
        public virtual bool ValidateInstaller()
        {
            try
            {
                if (_verboseLogging)
                {
                    Debug.Log($"[{InstallerName}] Starting validation");
                }

                // Perform installer-specific validation
                var isValid = PerformValidation();

                if (_verboseLogging)
                {
                    Debug.Log($"[{InstallerName}] Validation {(isValid ? "succeeded" : "failed")}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{InstallerName}] Validation failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public virtual void PreInstall()
        {
            try
            {
                if (_verboseLogging)
                {
                    Debug.Log($"[{InstallerName}] Starting pre-installation");
                }

                // Perform installer-specific pre-installation setup
                PerformPreInstall();

                if (_verboseLogging)
                {
                    Debug.Log($"[{InstallerName}] Pre-installation completed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{InstallerName}] Pre-installation failed: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public abstract void InstallBindings(ContainerBuilder builder);

        /// <inheritdoc />
        public virtual void PostInstall(Container container)
        {
            try
            {
                if (_verboseLogging)
                {
                    Debug.Log($"[{InstallerName}] Starting post-installation");
                }

                // Perform installer-specific post-installation setup
                PerformPostInstall(container);

                if (_verboseLogging)
                {
                    Debug.Log($"[{InstallerName}] Post-installation completed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{InstallerName}] Post-installation failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// Performs installer-specific validation logic.
        /// Override this method to implement custom validation.
        /// Default implementation returns true.
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        protected virtual bool PerformValidation()
        {
            return true;
        }

        /// <summary>
        /// Performs installer-specific pre-installation setup.
        /// Override this method to implement custom pre-installation logic.
        /// Default implementation does nothing.
        /// </summary>
        protected virtual void PerformPreInstall()
        {
            // Default implementation - do nothing
        }

        /// <summary>
        /// Performs installer-specific post-installation setup.
        /// Override this method to implement custom post-installation logic.
        /// Default implementation does nothing.
        /// </summary>
        /// <param name="container">The built Reflex container for service resolution</param>
        protected virtual void PerformPostInstall(Container container)
        {
            // Default implementation - do nothing
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unity validation callback for Inspector value changes.
        /// Ensures configuration values are valid in the editor.
        /// </summary>
        protected virtual void OnValidate()
        {
            // Default implementation - can be overridden by derived classes
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Logs a debug message if verbose logging is enabled.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void LogDebug(string message)
        {
            if (_verboseLogging)
            {
                Debug.Log($"[{InstallerName}] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{InstallerName}] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[{InstallerName}] {message}");
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">Additional context message</param>
        protected void LogException(Exception exception, string message = null)
        {
            var fullMessage = string.IsNullOrEmpty(message) 
                ? $"[{InstallerName}] Exception: {exception.Message}" 
                : $"[{InstallerName}] {message}: {exception.Message}";
            
            Debug.LogError(fullMessage);
            Debug.LogException(exception);
        }

        #endregion
    }
}