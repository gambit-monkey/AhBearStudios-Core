using UnityEditor;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Collection of GUI styles used in the log visualizer window.
    /// </summary>
    public class GUIStyleCollection
    {
        /// <summary>
        /// Style for debug level messages.
        /// </summary>
        public GUIStyle DebugStyle { get; private set; }
        
        /// <summary>
        /// Style for info level messages.
        /// </summary>
        public GUIStyle InfoStyle { get; private set; }
        
        /// <summary>
        /// Style for warning level messages.
        /// </summary>
        public GUIStyle WarningStyle { get; private set; }
        
        /// <summary>
        /// Style for error level messages.
        /// </summary>
        public GUIStyle ErrorStyle { get; private set; }
        
        /// <summary>
        /// Style for critical level messages.
        /// </summary>
        public GUIStyle CriticalStyle { get; private set; }
        
        /// <summary>
        /// Style for message content.
        /// </summary>
        public GUIStyle MessageStyle { get; private set; }
        
        /// <summary>
        /// Style for selected items.
        /// </summary>
        public GUIStyle SelectedStyle { get; private set; }
        
        /// <summary>
        /// Style for section headers.
        /// </summary>
        public GUIStyle HeaderStyle { get; private set; }
        
        /// <summary>
        /// Style for toolbar buttons.
        /// </summary>
        public GUIStyle ToolbarButtonStyle { get; private set; }
        
        /// <summary>
        /// Style for level buttons.
        /// </summary>
        public GUIStyle LevelButtonStyle { get; private set; }
        
        /// <summary>
        /// Style for selected level buttons.
        /// </summary>
        public GUIStyle LevelButtonSelectedStyle { get; private set; }
        
        /// <summary>
        /// Initializes all styles.
        /// </summary>
        public void Initialize()
        {
            DebugStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            
            InfoStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.0f, 0.7f, 0.0f) }
            };
            
            WarningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.9f, 0.0f) }
            };
            
            ErrorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.0f, 0.0f) }
            };
            
            CriticalStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1.0f, 0.0f, 0.0f) },
                fontStyle = FontStyle.Bold
            };
            
            MessageStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(5, 5, 2, 2)
            };
            
            SelectedStyle = new GUIStyle(EditorStyles.selectionRect)
            {
                normal = { background = EditorGUIUtility.whiteTexture },
                padding = new RectOffset(5, 5, 2, 2)
            };
            
            HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 2, 2)
            };
            
            ToolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                padding = new RectOffset(10, 10, 2, 2),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            LevelButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(8, 8, 2, 2),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            LevelButtonSelectedStyle = new GUIStyle(LevelButtonStyle)
            {
                normal = { background = EditorGUIUtility.whiteTexture, textColor = Color.black }
            };
        }
    }
}