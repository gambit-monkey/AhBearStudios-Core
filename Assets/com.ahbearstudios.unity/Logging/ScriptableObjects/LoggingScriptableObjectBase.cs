using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// Base class for all logging-related ScriptableObjects.
    /// Provides common functionality for validation, serialization, and Unity integration.
    /// </summary>
    public abstract class LoggingScriptableObjectBase : ScriptableObject
    {
        [Header("Common Settings")]
        [SerializeField] protected string _name = string.Empty;
        [SerializeField] protected string _description = string.Empty;
        [SerializeField] protected bool _isEnabled = true;
        [SerializeField] protected LogLevel _minimumLevel = LogLevel.Debug;

        [Header("Editor Settings")]
        [SerializeField] protected bool _enableEditorValidation = true;
        [SerializeField] protected bool _showAdvancedOptions = false;

        /// <summary>
        /// Gets or sets the name of this logging component.
        /// </summary>
        public string Name
        {
            get => string.IsNullOrEmpty(_name) ? name : _name;
            set => _name = value;
        }

        /// <summary>
        /// Gets or sets the description of this logging component.
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        /// Gets or sets whether this logging component is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Gets or sets the minimum log level for this component.
        /// </summary>
        public LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        /// <summary>
        /// Gets whether editor validation is enabled.
        /// </summary>
        public bool EnableEditorValidation => _enableEditorValidation;

        /// <summary>
        /// Gets whether advanced options should be shown in the editor.
        /// </summary>
        public bool ShowAdvancedOptions => _showAdvancedOptions;

        /// <summary>
        /// Validates the configuration and returns any validation errors.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public virtual List<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_name))
            {
                errors.Add("Name cannot be empty");
            }

            return errors;
        }

        /// <summary>
        /// Resets the configuration to default values.
        /// Override in derived classes to provide specific defaults.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public virtual void ResetToDefaults()
        {
            _name = GetType().Name.Replace("ConfigSo", "").Replace("ScriptableObject", "");
            _description = $"Default {_name} configuration";
            _isEnabled = true;
            _minimumLevel = LogLevel.Debug;
            _enableEditorValidation = true;
            _showAdvancedOptions = false;
        }

        /// <summary>
        /// Validates the configuration in the Unity Editor.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (!_enableEditorValidation) return;

            ValidateInEditor();
        }

        /// <summary>
        /// Performs editor-specific validation.
        /// Override in derived classes to provide specific validation logic.
        /// </summary>
        protected virtual void ValidateInEditor()
        {
            // Ensure name is not empty
            if (string.IsNullOrWhiteSpace(_name))
            {
                _name = name;
            }

            // Ensure description is not empty
            if (string.IsNullOrWhiteSpace(_description))
            {
                _description = $"Configuration for {_name}";
            }
        }

        /// <summary>
        /// Logs validation results to the Unity Console.
        /// </summary>
        /// <param name="errors">List of validation errors</param>
        protected void LogValidationResults(List<string> errors)
        {
            if (errors.Count == 0)
            {
                Debug.Log($"[{GetType().Name}] Configuration validation passed successfully.", this);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Configuration validation failed with {errors.Count} errors:\n{string.Join("\n", errors)}", this);
            }
        }

        /// <summary>
        /// Gets a property value from a dictionary with a default fallback.
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="properties">The properties dictionary</param>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">The default value</param>
        /// <returns>The property value or default if not found</returns>
        protected T GetProperty<T>(Dictionary<string, object> properties, string key, T defaultValue)
        {
            if (properties != null && properties.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Creates a properties dictionary from the current configuration.
        /// Override in derived classes to provide specific properties.
        /// </summary>
        /// <returns>Dictionary of configuration properties</returns>
        public virtual Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["Name"] = _name,
                ["Description"] = _description,
                ["IsEnabled"] = _isEnabled,
                ["MinimumLevel"] = _minimumLevel.ToString()
            };
        }

        /// <summary>
        /// Creates a display name for the asset menu and inspector.
        /// </summary>
        /// <returns>A formatted display name</returns>
        protected string CreateDisplayName()
        {
            return string.IsNullOrEmpty(_name) ? name : _name;
        }

        /// <summary>
        /// Provides a string representation of this logging component.
        /// </summary>
        /// <returns>A descriptive string</returns>
        public override string ToString()
        {
            return $"{GetType().Name}(Name={Name}, Enabled={IsEnabled}, Level={MinimumLevel})";
        }
    }
}