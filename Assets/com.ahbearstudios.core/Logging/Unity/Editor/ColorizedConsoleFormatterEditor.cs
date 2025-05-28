using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using System;
using System.Linq;
using AhBearStudios.Core.Logging.Extensions;
using Serilog;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor for ColorizedConsoleFormatter that provides a rich interface
    /// for customizing log formatting with visual previews and color customization options.
    /// </summary>
    [CustomEditor(typeof(ColorizedConsoleFormatter))]
    public class ColorizedConsoleFormatterEditor : UnityEditor.Editor
    {
        // Serialized properties
        private SerializedObject _serializedColorized;

        // Foldout states
        private bool _customizationFoldout = true;
        private bool _levelColorsFoldout = false;
        private bool _tagColorsFoldout = false;
        private bool _previewFoldout = true;

        // Preview message cache to avoid GC
        private LogMessage _debugMessage;
        private LogMessage _infoMessage;
        private LogMessage _warningMessage;
        private LogMessage _errorMessage;
        private LogMessage _criticalMessage;

        // Color dictionary assets 
        private SerializedProperty _levelColorsProp;
        private SerializedProperty _tagColorsProp;

        // Sample tag for previews
        private readonly Tagging.LogTag[] _sampleTags = new[]
        {
            Tagging.LogTag.System,
            Tagging.LogTag.UI,
            Tagging.LogTag.Gameplay,
            Tagging.LogTag.Performance,
            Tagging.LogTag.Network
        };

        // Selected tag for custom preview
        private int _selectedTagIndex = 0;
        private LogLevel _selectedLevelIndex = LogLevel.Info;
        private string _customMessage = "This is a sample log message";

        // Reference for formatter instance
        private ColorizedConsoleFormatter _formatter;

        /// <summary>
        /// Initialize the editor when it's first loaded
        /// </summary>
        private void OnEnable()
        {
            _formatter = (ColorizedConsoleFormatter)target;

            // Initialize preview messages to avoid GC during previews
            InitializePreviewMessages();
        }

        /// <summary>
        /// Initialize log message objects for preview
        /// </summary>
        private void InitializePreviewMessages()
        {
            DateTime now = DateTime.Now;
            long timestamp = now.Ticks;

            // Create sample messages for each log level
            _debugMessage = new LogMessage
            {
                Level = LogLevel.Debug,
                Tag = Tagging.LogTag.System,
                TimestampTicks = timestamp,
                Message = "Debug message example"
            };

            _infoMessage = new LogMessage
            {
                Level = LogLevel.Info,
                Tag = Tagging.LogTag.UI,
                TimestampTicks = timestamp,
                Message = "Information message example"
            };

            _warningMessage = new LogMessage
            {
                Level = LogLevel.Warning,
                Tag = Tagging.LogTag.Performance,
                TimestampTicks = timestamp,
                Message = "Warning message example: Process taking longer than expected"
            };

            _errorMessage = new LogMessage
            {
                Level = LogLevel.Error,
                Tag = Tagging.LogTag.Network,
                TimestampTicks = timestamp,
                Message = "Error message example: Connection failed"
            };

            _criticalMessage = new LogMessage
            {
                Level = LogLevel.Critical,
                Tag = Tagging.LogTag.Exception,
                TimestampTicks = timestamp,
                Message = "Critical error example: System failure"
            };
        }

        /// <summary>
        /// Draw the custom inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            DrawHeader();

            EditorGUILayout.Space();
            DrawColorCustomizationButton();

            EditorGUILayout.Space();
            DrawFormatPreview();

            EditorGUILayout.Space();
            DrawCustomPreview();

            EditorGUILayout.Space();
            DrawTestButtons();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the header section of the editor
        /// </summary>
        private void DrawHeader()
        {
            // Draw background box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField("Colorized Console Formatter", headerStyle);

            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.label);
            descriptionStyle.wordWrap = true;
            descriptionStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField(
                "This formatter enhances Unity Console logs with color coding for improved readability.",
                descriptionStyle);

            EditorGUILayout.LabelField(
                "Color coding is applied to log levels and tag.",
                descriptionStyle);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a preview of formatted logs for each log level
        /// </summary>
        private void DrawFormatPreview()
        {
            _previewFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_previewFoldout, "Formatting Preview");

            if (_previewFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Sample log messages as they will appear in the console:",
                    EditorStyles.boldLabel);

                EditorGUILayout.Space(5);

                // Draw previews for each log level
                DrawLogPreview(_debugMessage, "Debug");
                DrawLogPreview(_infoMessage, "Info");
                DrawLogPreview(_warningMessage, "Warning");
                DrawLogPreview(_errorMessage, "Error");
                DrawLogPreview(_criticalMessage, "Critical");

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Draws a preview of a formatted log message
        /// </summary>
        /// <param name="message">The log message to preview</param>
        /// <param name="label">Label to display above the preview</param>
        private void DrawLogPreview(LogMessage message, string label)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Draw level label
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            // Format the message using the formatter
            string formattedMessage = _formatter.Format(message).ToString();

            // Create style that supports rich text
            GUIStyle richTextStyle = new GUIStyle(EditorStyles.textField);
            richTextStyle.richText = true;
            richTextStyle.wordWrap = true;

            // Draw formatted message
            EditorGUILayout.SelectableLabel(formattedMessage, richTextStyle, GUILayout.Height(40));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // Add this method to the ColorizedConsoleFormatterEditor class
        private void DrawColorCustomizationButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Color Customization", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Use the color editor to customize colors for different log levels and tag.",
                MessageType.Info);

            if (GUILayout.Button("Open Color Editor", GUILayout.Height(30)))
            {
                // Open the color editor window
                ColorizedFormatterColorEditor.OpenWindow(_formatter);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws an interactive preview with customizable message
        /// </summary>
        /// <summary>
        /// Draws an interactive preview with customizable message
        /// </summary>
        private void DrawCustomPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Custom Message Preview", EditorStyles.boldLabel);

            // Log level selection - create array of supported log levels
            LogLevel[] supportedLevels = new[]
            {
                LogLevel.Debug,
                LogLevel.Info,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Critical
            };

            string[] logLevelNames = supportedLevels.Select(l => l.GetName()).ToArray();

            // Find the index of the currently selected level in the supported levels array
            int selectedLevelIndex = Array.IndexOf(supportedLevels, _selectedLevelIndex);
            if (selectedLevelIndex < 0) selectedLevelIndex = 1; // Default to Info if not found

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Log Level:", GUILayout.Width(100));
            int newSelectedIndex = EditorGUILayout.Popup(selectedLevelIndex, logLevelNames);

            // Update the selected level when the popup changes
            if (newSelectedIndex != selectedLevelIndex)
            {
                _selectedLevelIndex = supportedLevels[newSelectedIndex];
            }

            EditorGUILayout.EndHorizontal();

            // Tag selection
            string[] tagNames = new string[_sampleTags.Length];
            for (int i = 0; i < _sampleTags.Length; i++)
            {
                tagNames[i] = _sampleTags[i].ToString();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Log Tag:", GUILayout.Width(100));
            _selectedTagIndex = EditorGUILayout.Popup(_selectedTagIndex, tagNames);
            EditorGUILayout.EndHorizontal();

            // Custom message input
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Message:", GUILayout.Width(100));
            _customMessage = EditorGUILayout.TextField(_customMessage);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Create a custom log message with the proper LogLevel enum value
            LogMessage customMessage = new LogMessage
            {
                Level = _selectedLevelIndex, // Now using the actual LogLevel enum value
                Tag = _sampleTags[_selectedTagIndex],
                TimestampTicks = DateTime.Now.Ticks,
                Message = _customMessage
            };

            // Format and display
            string formattedMessage = _formatter.Format(customMessage).ToString();

            // Create style that supports rich text
            GUIStyle richTextStyle = new GUIStyle(EditorStyles.textField);
            richTextStyle.richText = true;
            richTextStyle.wordWrap = true;

            EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(formattedMessage, richTextStyle, GUILayout.Height(60));

            // Add a send to console button
            if (GUILayout.Button("Send This Message to Console"))
            {
                // Log to console based on selected level
                switch (_selectedLevelIndex)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        Debug.Log(formattedMessage);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(formattedMessage);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        Debug.LogError(formattedMessage);
                        break;
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws test buttons for sending logs to console
        /// </summary>
        private void DrawTestButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Test in Console", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Click the buttons below to send sample messages to the Unity Console to see how they appear.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Log All Examples"))
            {
                // Log all example messages to console
                LogToConsole(LogLevel.Debug, _debugMessage);
                LogToConsole(LogLevel.Info, _infoMessage);
                LogToConsole(LogLevel.Warning, _warningMessage);
                LogToConsole(LogLevel.Error, _errorMessage);
                LogToConsole(LogLevel.Critical, _criticalMessage);
            }

            if (GUILayout.Button("Clear Console"))
            {
                // Clear the console using reflection (Unity doesn't provide a public API for this)
                var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                var clearMethod = logEntries.GetMethod("Clear",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearMethod.Invoke(null, null);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Individual log level buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Log Debug"))
            {
                LogToConsole(LogLevel.Debug, _debugMessage);
            }

            if (GUILayout.Button("Log Info"))
            {
                LogToConsole(LogLevel.Info, _infoMessage);
            }

            if (GUILayout.Button("Log Warning"))
            {
                LogToConsole(LogLevel.Warning, _warningMessage);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Log Error"))
            {
                LogToConsole(LogLevel.Error, _errorMessage);
            }

            if (GUILayout.Button("Log Critical"))
            {
                LogToConsole(LogLevel.Critical, _criticalMessage);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Logs a message to the Unity Console
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        private void LogToConsole(LogLevel level, LogMessage message)
        {
            // Format the message
            string formattedMessage = _formatter.Format(message).ToString();

            // Log to console based on level
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }
    }
}