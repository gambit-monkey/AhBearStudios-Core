using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Tags;
using System.Collections.Generic;
using System;
using System.Linq;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor window for customizing the colors used in ColorizedConsoleFormatter.
    /// Provides a UI for configuring colors for different log levels and tags.
    /// </summary>
    public class ColorizedFormatterColorEditor : EditorWindow
    {
        // Dependencies
        private ColorizedConsoleFormatter _formatter;
        private IColorManager _colorManager;
        
        // UI state
        private Vector2 _levelColorScroll;
        private Vector2 _tagColorScroll;
        private int _selectedTab = 0;
        private bool[] _categoryFoldouts;
        
        // Log level display configuration
        private readonly LogLevel[] _displayLevels = new[]
        {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical
        };
        
        /// <summary>
        /// Opens the color editor window for a formatter.
        /// </summary>
        /// <param name="formatter">The formatter to edit.</param>
        public static void OpenWindow(ColorizedConsoleFormatter formatter)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
                
            ColorizedFormatterColorEditor window = GetWindow<ColorizedFormatterColorEditor>("Formatter Colors");
            window._formatter = formatter;
            window._colorManager = new ColorManager(formatter);
            window.InitializeState();
            window.Show();
        }
        
        /// <summary>
        /// Initialize the editor window state.
        /// </summary>
        private void InitializeState()
        {
            _colorManager.InitializeColors();
            
            // Initialize category foldouts if needed
            if (_categoryFoldouts == null || _categoryFoldouts.Length == 0)
            {
                _categoryFoldouts = new bool[Enum.GetValues(typeof(Tagging.TagCategory)).Length];
                // Default to having the first category expanded
                if (_categoryFoldouts.Length > 0)
                {
                    _categoryFoldouts[0] = true;
                }
            }
        }
        
        /// <summary>
        /// Draw the editor window.
        /// </summary>
        private void OnGUI()
        {
            if (_formatter == null)
            {
                EditorGUILayout.HelpBox("No formatter selected.", MessageType.Error);
                if (GUILayout.Button("Close Window"))
                {
                    Close();
                }
                return;
            }
            
            EditorGUILayout.BeginVertical();
            
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // Draw tabbed interface
            string[] tabNames = new string[] { "Level Colors", "Tag Colors" };
            _selectedTab = GUILayout.Toolbar(_selectedTab, tabNames);
            
            EditorGUILayout.Space(5);
            
            // Draw the appropriate section based on selected tab
            if (_selectedTab == 0)
            {
                DrawLevelColorSection();
            }
            else
            {
                DrawTagColorSection();
            }
            
            EditorGUILayout.Space(10);
            
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw the header section.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Colorized Formatter Color Editor", headerStyle);
            EditorGUILayout.LabelField("Formatter: " + _formatter.name, EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw the level color section.
        /// </summary>
        private void DrawLevelColorSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Log Level Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize colors for each log level", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            _levelColorScroll = EditorGUILayout.BeginScrollView(_levelColorScroll);
            
            foreach (LogLevel level in _displayLevels)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Display the level name
                EditorGUILayout.LabelField(level.GetName(), GUILayout.Width(100));
                
                // Get current color and display color picker
                Color oldColor = _colorManager.GetLevelColor(level);
                Color newColor = EditorGUILayout.ColorField(oldColor);
                
                // Update color if changed
                if (oldColor != newColor)
                {
                    _colorManager.SetLevelColor(level, newColor);
                    EditorUtility.SetDirty(_formatter);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw the tag color section.
        /// </summary>
        private void DrawTagColorSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Log Tag Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize colors for different log tag categories", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            _tagColorScroll = EditorGUILayout.BeginScrollView(_tagColorScroll, GUILayout.Height(400));
            
            // Draw tag colors grouped by category
            Array categoryValues = Enum.GetValues(typeof(Tagging.TagCategory));
            
            for (int c = 0; c < categoryValues.Length; c++)
            {
                Tagging.TagCategory category = (Tagging.TagCategory)categoryValues.GetValue(c);
                
                // Skip None and All categories
                if (category == Tagging.TagCategory.None || category == Tagging.TagCategory.All)
                    continue;
                
                // Draw category foldout with a more visible style
                GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
                
                _categoryFoldouts[c] = EditorGUILayout.Foldout(_categoryFoldouts[c], category.ToString(), true, foldoutStyle);
                
                if (_categoryFoldouts[c])
                {
                    EditorGUI.indentLevel++;
                    
                    // Get all tags in this category
                    bool hasTagsInCategory = false;
                    var tagsInCategory = _colorManager.GetTagsInCategory(category);
                    
                    foreach (Tagging.LogTag tag in tagsInCategory)
                    {
                        hasTagsInCategory = true;
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        EditorGUILayout.LabelField(tag.ToString(), GUILayout.Width(150));
                        
                        Color oldColor = _colorManager.GetTagColor(tag);
                        Color newColor = EditorGUILayout.ColorField(oldColor);
                        
                        if (oldColor != newColor)
                        {
                            _colorManager.SetTagColor(tag, newColor);
                            EditorUtility.SetDirty(_formatter);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    // If no tag in this category, show a message
                    if (!hasTagsInCategory)
                    {
                        EditorGUILayout.LabelField("No tags in this category", EditorStyles.miniLabel);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw action buttons.
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Colors", 
                    "Are you sure you want to reset all colors to default values?", 
                    "Reset", "Cancel"))
                {
                    _formatter.ResetColorsToDefaults();
                    _colorManager.InitializeColors();
                }
            }
            
            if (GUILayout.Button("Close"))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}