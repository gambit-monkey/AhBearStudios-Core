using UnityEngine;
using AhBearStudios.Core.Logging.LogTargets;
using AhBearStudios.Core.Logging.Formatters;

namespace AhBearStudios.Core.Logging.Config
{
    /// <summary>
    /// Configuration for Unity Console log targets.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UnityConsoleLogConfig", 
        menuName = "AhBearStudios/Logging/Unity Console Log Config", 
        order = 1)]
    public class UnityConsoleLogConfig : LogTargetConfig
    {
        /// <summary>
        /// The formatter to use for console logs. If null, a default formatter will be used.
        /// </summary>
        [SerializeField, Tooltip("The formatter to use for console logs. If none is assigned, a default formatter will be used.")]
        private ColorizedConsoleFormatter _formatter;

        /// <summary>
        /// Determines whether to use colorized output in the console.
        /// </summary>
        [SerializeField, Tooltip("Enable colorized output in the Unity Console")]
        private bool _useColorizedOutput = true;
        
        [SerializeField, Tooltip("Registers our logger with Unity")]
        private bool _registerUnityLogHandler = true;
        
        [SerializeField, Tooltip("When registered with Unity, also send logs to Unity's original handler (may cause duplicate log entries")]
        private bool _duplicateToOriginalHandler = false;

        /// <summary>
        /// Gets or sets the formatter used for console logs.
        /// </summary>
        public ColorizedConsoleFormatter Formatter
        {
            get => _formatter;
            set
            {
                _formatter = value;
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        /// <summary>
        /// Gets or sets whether to use colorized output.
        /// </summary>
        public bool UseColorizedOutput
        {
            get => _useColorizedOutput;
            set
            {
                _useColorizedOutput = value;
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
        
        public bool RegisterUnityLogHandler => _registerUnityLogHandler;
        public bool DuplicateToOriginalHandler => _duplicateToOriginalHandler;

        /// <summary>
        /// Creates a Unity Console log target based on this configuration.
        /// </summary>
        /// <returns>A configured UnityConsoleTarget.</returns>
        public override ILogTarget CreateTarget()
        {
            var target = new UnityConsoleTarget(TargetName, MinimumLevel);
            target.IsEnabled = Enabled;
            
            // Assign the formatter if colorized output is enabled and a formatter is available
            if (_useColorizedOutput && _formatter != null)
            {
                target.SetFormatter(_formatter);
            }
            
            ApplyTagFilters(target);
            return target;
        }
        
        /// <summary>
        /// Validate the configuration to ensure all required components are present.
        /// </summary>
        private void OnValidate()
        {
            // If colorized output is enabled but no formatter is assigned, log a warning in the editor
            #if UNITY_EDITOR
            if (_useColorizedOutput && _formatter == null)
            {
                Debug.LogWarning($"[{name}] Colorized output is enabled but no formatter is assigned. A default formatter will be used.");
            }
            #endif
        }
    }
}