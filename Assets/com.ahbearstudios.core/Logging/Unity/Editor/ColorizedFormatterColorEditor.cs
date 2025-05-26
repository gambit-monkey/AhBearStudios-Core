using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Tags;
using System.Collections.Generic;
using System;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor window for customizing the colors used in ColorizedConsoleFormatter
    /// </summary>
    public class ColorizedFormatterColorEditor : EditorWindow
    {
        // Reference to the formatter being edited
        private ColorizedConsoleFormatter _formatter;
        
        // Scroll position for level colors
        private Vector2 _levelColorScroll;
        private Vector2 _tagColorScroll;
        
        // Color maps
        private Dictionary<byte, Color> _levelColors = new Dictionary<byte, Color>();
        private Dictionary<Tagging.LogTag, Color> _tagColors = new Dictionary<Tagging.LogTag, Color>();
        
        // Level names for display
        private static readonly string[] LevelNames = new string[]
        {
            "Debug", "Info", "Warning", "Error", "Critical"
        };
        
        // Category foldouts
        private bool[] _categoryFoldouts;
        
        // Selected tab index
        private int _selectedTab = 0;
        
        /// <summary>
        /// Opens the color editor window for a formatter
        /// </summary>
        /// <param name="formatter">The formatter to edit</param>
        public static void OpenWindow(ColorizedConsoleFormatter formatter)
        {
            ColorizedFormatterColorEditor window = GetWindow<ColorizedFormatterColorEditor>("Formatter Colors");
            window._formatter = formatter;
            window.InitializeColors();
            window.Show();
        }
        
        /// <summary>
        /// Initialize colors from formatter
        /// </summary>
        private void InitializeColors()
        {
            if (_formatter == null)
                return;
                
            // Initialize level colors
            _levelColors.Clear();
            for (byte i = 0; i < LevelNames.Length; i++)
            {
                string colorHex = _formatter.GetColorForLevel(i);
                _levelColors[i] = HexToColor(colorHex);
            }
            
            // Initialize tag colors
            _tagColors.Clear();
            
            // Get all tag values
            Array tagValues = Enum.GetValues(typeof(Tagging.LogTag));
            foreach (Tagging.LogTag tag in tagValues)
            {
                string colorHex = _formatter.GetColorForTag(tag);
                _tagColors[tag] = HexToColor(colorHex);
            }
            
            // Initialize category foldouts
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
        /// Draw the editor window
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
            
            // Draw tabbed interface - fixed version
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
        /// Draw the header section
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            
            EditorGUILayout.LabelField("Colorized Formatter Color Editor", headerStyle);
            
            EditorGUILayout.LabelField("Formatter: " + _formatter.name, EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw the level color section
        /// </summary>
        private void DrawLevelColorSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Log Level Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize colors for each log level", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            _levelColorScroll = EditorGUILayout.BeginScrollView(_levelColorScroll);
            
            for (byte i = 0; i < LevelNames.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField(LevelNames[i], GUILayout.Width(100));
                
                Color oldColor = _levelColors[i];
                Color newColor = EditorGUILayout.ColorField(oldColor);
                
                if (oldColor != newColor)
                {
                    _levelColors[i] = newColor;
                    // Save color to formatter
                    _formatter.SetLevelColor(i, "#" + ColorUtility.ToHtmlStringRGB(newColor));
                    EditorUtility.SetDirty(_formatter);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw the tag color section
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
                GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fontStyle = FontStyle.Bold;
                
                _categoryFoldouts[c] = EditorGUILayout.Foldout(_categoryFoldouts[c], category.ToString(), true, foldoutStyle);
                
                if (_categoryFoldouts[c])
                {
                    EditorGUI.indentLevel++;
                    
                    // Get all tag in this category
                    bool hasTagsInCategory = false;
                    Array tagValues = Enum.GetValues(typeof(Tagging.LogTag));
                    
                    foreach (Tagging.LogTag tag in tagValues)
                    {
                        if (Tagging.GetTagCategory(tag) == category)
                        {
                            hasTagsInCategory = true;
                            
                            EditorGUILayout.BeginHorizontal();
                            
                            EditorGUILayout.LabelField(tag.ToString(), GUILayout.Width(150));
                            
                            // Make sure we have this color in our dictionary
                            if (!_tagColors.ContainsKey(tag))
                            {
                                string colorHex = _formatter.GetColorForTag(tag);
                                _tagColors[tag] = HexToColor(colorHex);
                            }
                            
                            Color oldColor = _tagColors[tag];
                            Color newColor = EditorGUILayout.ColorField(oldColor);
                            
                            if (oldColor != newColor)
                            {
                                _tagColors[tag] = newColor;
                                // Save color to formatter
                                _formatter.SetTagColor(tag, "#" + ColorUtility.ToHtmlStringRGB(newColor));
                                EditorUtility.SetDirty(_formatter);
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    // If no tag in this category, show a message
                    if (!hasTagsInCategory)
                    {
                        EditorGUILayout.LabelField("No tag in this category", EditorStyles.miniLabel);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw action buttons
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
                    InitializeColors();
                }
            }
            
            if (GUILayout.Button("Close"))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Convert hex color string to Color
        /// </summary>
        /// <param name="hex">Hex color string</param>
        /// <returns>Color object</returns>
        private Color HexToColor(string hex)
        {
            // Remove # if present
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
                
            // Parse the hex string
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
                return color;
                
            return Color.white;
        }
    }
}