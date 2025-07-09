using System;
using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Bootstrap.Models;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Configuration Implementation

        /// <inheritdoc />
        public virtual ConfigurationRequirements GetConfigurationRequirements()
        {
            return new ConfigurationRequirements
            {
                InstallerName = InstallerName,
                RequiredSections = GetRequiredConfigurationSections(),
                OptionalSections = GetOptionalConfigurationSections(),
                ValidationRules = GetConfigurationValidationRules()
            };
        }

        /// <inheritdoc />
        public abstract ServiceRegistrationInfo[] GetServiceRegistrations();

        /// <inheritdoc />
        public virtual PlatformRequirements GetPlatformRequirements()
        {
            return new PlatformRequirements
            {
                InstallerName = InstallerName,
                SupportedPlatforms = GetSupportedPlatforms(),
                RequiredFeatures = GetRequiredPlatformFeatures(),
                MinimumMemoryMB = EstimatedMemoryOverheadBytes / (1024 * 1024),
                RequiredUnityVersion = GetMinimumUnityVersion()
            };
        }

        #endregion

        #region Protected Configuration Methods

        /// <summary>
        /// Gets the required configuration sections for this installer.
        /// Override in derived classes to specify configuration requirements.
        /// </summary>
        protected virtual string[] GetRequiredConfigurationSections()
        {
            return Array.Empty<string>();
        }

        /// <summary>
        /// Gets the optional configuration sections for this installer.
        /// Override in derived classes to specify optional configuration.
        /// </summary>
        protected virtual string[] GetOptionalConfigurationSections()
        {
            return Array.Empty<string>();
        }

        /// <summary>
        /// Gets the configuration validation rules for this installer.
        /// Override in derived classes to specify validation logic.
        /// </summary>
        protected virtual ConfigurationValidationRule[] GetConfigurationValidationRules()
        {
            return Array.Empty<ConfigurationValidationRule>();
        }

        /// <summary>
        /// Gets the platforms supported by this installer.
        /// Override in derived classes to specify platform constraints.
        /// </summary>
        protected virtual UnityEngine.RuntimePlatform[] GetSupportedPlatforms()
        {
            return Enum.GetValues<UnityEngine.RuntimePlatform>();
        }

        /// <summary>
        /// Gets the platform features required by this installer.
        /// Override in derived classes to specify feature requirements.
        /// </summary>
        protected virtual string[] GetRequiredPlatformFeatures()
        {
            return Array.Empty<string>();
        }

        /// <summary>
        /// Gets the minimum Unity version required by this installer.
        /// Override in derived classes to specify version constraints.
        /// </summary>
        protected virtual string GetMinimumUnityVersion()
        {
            return "2022.3.0f1";
        }

        #endregion

        #region Private Configuration Methods

        private bool ValidateConfigurationRequirements(IBootstrapConfig config, 
            ConfigurationRequirements requirements, BootstrapValidationResult result)
        {
            if (config == null || requirements == null || result == null)
                return false;

            var isValid = true;

            if (requirements.RequiredSections != null)
            {
                foreach (var section in requirements.RequiredSections)
                {
                    if (!HasConfigurationSection(config, section))
                    {
                        result.Errors.Add($"Required configuration section '{section}' is missing");
                        isValid = false;
                    }
                }
            }

            if (requirements.ValidationRules != null)
            {
                foreach (var rule in requirements.ValidationRules)
                {
                    if (!ValidateConfigurationRule(config, rule))
                    {
                        result.Errors.Add($"Configuration validation failed for rule: {rule.RuleName}");
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        private bool ValidatePlatformRequirements(PlatformRequirements requirements, 
            BootstrapValidationResult result)
        {
            if (requirements?.SupportedPlatforms == null || result == null)
                return false;

            var currentPlatform = UnityEngine.Application.platform;
            var isSupported = Array.Exists(requirements.SupportedPlatforms, p => p == currentPlatform);

            if (!isSupported)
            {
                result.Warnings.Add($"Current platform {currentPlatform} is not in the supported platforms list");
                return false;
            }

            return true;
        }

        private bool HasConfigurationSection(IBootstrapConfig config, string section)
        {
            if (config == null || string.IsNullOrEmpty(section))
                return false;

            try
            {
                // Check if the configuration has the required section
                // This would need to be implemented based on the actual IBootstrapConfig structure
                return config.HasSection(section);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to check configuration section '{section}': {ex.Message}");
                return false;
            }
        }

        private bool ValidateConfigurationRule(IBootstrapConfig config, ConfigurationValidationRule rule)
        {
            if (config == null || rule?.ValidationFunction == null)
                return false;

            try
            {
                return rule.ValidationFunction(config);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Configuration validation rule '{rule.RuleName}' failed: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}