// Assets/com.ahbearstudios.core/Logging/Scripts/Unity/Editor/LogVisualizerWindow.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Events;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom Unity editor window that provides visualization and filtering of log messages.
    /// </summary>
    public class LogVisualizerWindow : EditorWindow
    {
        // Message storage
        private readonly List<LogMessage> _cachedMessages = new List<LogMessage>();
        private readonly Dictionary<string, int> _tagCounts = new Dictionary<string, int>();
        private readonly Dictionary<byte, int> _levelCounts = new Dictionary<byte, int>();
        private int _maxCachedMessages = 1000;
        
        // Filtering
        private string _filterText = "";
        private bool _showDebug = true;
        private bool _showInfo = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _showCritical = true;
        private HashSet<string> _selectedTags = new HashSet<string>();
        
        // UI State
        private Vector2 _scrollPosition;
        private bool _autoScroll = true;
        private bool _showFilters = true;
        private bool _showStatistics = true;
        private bool _showLevelControl = false;
        private LoggingProfile _selectedProfile;
        private string _newProfileName = "New Profile";
        
        // Tracking
        private float _lastUpdateTime;
        
        // Styling
        private GUIStyle _debugStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _criticalStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _timestampStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _propertyStyle;
        
        [MenuItem("Window/AhBearStudios/Log Visualizer")]
        private static void ShowWindow()
        {
            var window = GetWindow<LogVisualizerWindow>();
            window.titleContent = new GUIContent("Log Visualizer");
            window.Show();
        }
        
        private void OnEnable()
        {
            // Subscribe to logging events
            LogEvents.OnMessageWritten += OnMessageWritten;
            LogEvents.OnLogLevelChanged += OnLogLevelChanged;
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from logging events
            LogEvents.OnMessageWritten -= OnMessageWritten;
            LogEvents.OnLogLevelChanged -= OnLogLevelChanged;
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            // Force repaint every second when playing
            if (EditorApplication.isPlaying && (Time.realtimeSinceStartup - _lastUpdateTime > 1.0f))
            {
                _lastUpdateTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }
        
        private void OnMessageWritten(object sender, LogMessageWrittenEventArgs e)
        {
            // Add message to cache
            _cachedMessages.Add(e.Message);
            
            // Update tag counts
            string tagString = e.Message.GetTagString();
            if (!_tagCounts.ContainsKey(tagString))
            {
                _tagCounts[tagString] = 0;
                _selectedTags.Add(tagString); // Auto-select new tags
            }
            _tagCounts[tagString]++;
            
            // Update level counts
            if (!_levelCounts.ContainsKey(e.Message.Level))
            {
                _levelCounts[e.Message.Level] = 0;
            }
            _levelCounts[e.Message.Level]++;
            
            // Trim cache if needed
            if (_cachedMessages.Count > _maxCachedMessages)
            {
                _cachedMessages.RemoveAt(0);
            }
            
            // Repaint the window
            Repaint();
        }
        
        private void OnLogLevelChanged(object sender, LogLevelChangedEventArgs e)
        {
            // Just repaint
            Repaint();
        }
        
        private void InitializeStyles()
        {
            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    fontStyle = FontStyle.Normal
                };
            }
            
            if (_infoStyle == null)
            {
                _infoStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.7f, 0.9f, 1.0f) },
                    fontStyle = FontStyle.Normal
                };
            }
            
            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1.0f, 0.9f, 0.4f) },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1.0f, 0.5f, 0.5f) },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_criticalStyle == null)
            {
                _criticalStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1.0f, 0.3f, 0.3f) },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_tagStyle == null)
            {
                _tagStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.5f, 0.8f, 0.5f) },
                    fontStyle = FontStyle.Normal,
                    fixedWidth = 100
                };
            }
            
            if (_timestampStyle == null)
            {
                _timestampStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    fontStyle = FontStyle.Normal,
                    fixedWidth = 150
                };
            }
            
            if (_messageStyle == null)
            {
                _messageStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.white },
                    wordWrap = true
                };
            }
            
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
            
            if (_propertyStyle == null)
            {
                _propertyStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                    padding = new RectOffset(5, 5, 5, 5),
                    margin = new RectOffset(10, 10, 2, 2)
                };
            }
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            EditorGUILayout.BeginVertical();
            
            DrawToolbar();
            
            if (_showFilters)
            {
                DrawFilters();
            }
            
            if (_showStatistics)
            {
                DrawStatistics();
            }
            
            if (_showLevelControl)
            {
                DrawLevelControl();
            }
            
            DrawMessageList();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ClearMessages();
            }
            
            _showFilters = GUILayout.Toggle(_showFilters, "Filters", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showStatistics = GUILayout.Toggle(_showStatistics, "Stats", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showLevelControl = GUILayout.Toggle(_showLevelControl, "Log Levels", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            GUILayout.FlexibleSpace();
            
            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto-Scroll", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            GUILayout.Label($"Messages: {_cachedMessages.Count}", EditorStyles.toolbarButton, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Filters", _headerStyle);
            
            // Search filter
            _filterText = EditorGUILayout.TextField("Filter Text:", _filterText);
            
            EditorGUILayout.BeginHorizontal();
            
            // Level filters
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.LabelField("Level Filters:", EditorStyles.boldLabel);
            
            _showDebug = EditorGUILayout.ToggleLeft("Debug", _showDebug, _debugStyle);
            _showInfo = EditorGUILayout.ToggleLeft("Info", _showInfo, _infoStyle);
            _showWarning = EditorGUILayout.ToggleLeft("Warning", _showWarning, _warningStyle);
            _showError = EditorGUILayout.ToggleLeft("Error", _showError, _errorStyle);
            _showCritical = EditorGUILayout.ToggleLeft("Critical", _showCritical, _criticalStyle);
            
            EditorGUILayout.EndVertical();
            
            // Tag filters
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Tag Filters:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(100)))
            {
                foreach (var tag in _tagCounts.Keys)
                {
                    _selectedTags.Add(tag);
                }
            }
            
            if (GUILayout.Button("Select None", GUILayout.Width(100)))
            {
                _selectedTags.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));
            
            foreach (var tagEntry in _tagCounts.OrderBy(kvp => kvp.Key))
            {
                bool isSelected = _selectedTags.Contains(tagEntry.Key);
                bool newSelection = EditorGUILayout.ToggleLeft($"{tagEntry.Key} ({tagEntry.Value})", isSelected);
                
                if (newSelection != isSelected)
                {
                    if (newSelection)
                    {
                        _selectedTags.Add(tagEntry.Key);
                    }
                    else
                    {
                        _selectedTags.Remove(tagEntry.Key);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Log Statistics", _headerStyle);
            
            // Get runtime stats if available
            int queuedCount = 0;
            int targetCount = 0;
            byte globalMinLevel = 0;
            
            var logManagerComponent = FindObjectOfType<LogManagerComponent>();
            if (logManagerComponent != null && EditorApplication.isPlaying)
            {
                var loggerManager = logManagerComponent.LoggerManager;
                if (loggerManager != null)
                {
                    queuedCount = loggerManager.QueuedMessageCount;
                    targetCount = loggerManager.TargetCount;
                    globalMinLevel = loggerManager.GlobalMinimumLevel;
                }
            }
            
            // Draw stats
            EditorGUILayout.BeginHorizontal();
            
            // Level counts in a bar chart
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.LabelField("Message Levels:", EditorStyles.boldLabel);
            
            int totalMessages = _cachedMessages.Count;
            if (totalMessages > 0)
            {
                DrawLevelBar("Debug", LogLevel.Debug, _debugStyle);
                DrawLevelBar("Info", LogLevel.Info, _infoStyle);
                DrawLevelBar("Warning", LogLevel.Warning, _warningStyle);
                DrawLevelBar("Error", LogLevel.Error, _errorStyle);
                DrawLevelBar("Critical", LogLevel.Critical, _criticalStyle);
            }
            else
            {
                EditorGUILayout.LabelField("No messages collected");
            }
            
            EditorGUILayout.EndVertical();
            
            // Runtime info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Runtime Info:", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Queued Messages:", queuedCount.ToString());
            EditorGUILayout.LabelField("Active Targets:", targetCount.ToString());
            EditorGUILayout.LabelField("Global Min Level:", GetLogLevelName(globalMinLevel));
            
            // Runtime actions
            if (EditorApplication.isPlaying && logManagerComponent != null)
            {
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Flush Logs"))
                {
                    int processed = logManagerComponent.Flush();
                    EditorUtility.DisplayDialog("Log Flush Result", $"Processed {processed} log messages.", "OK");
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLevelBar(string label, byte level, GUIStyle style)
        {
            int count = _levelCounts.ContainsKey(level) ? _levelCounts[level] : 0;
            float percentage = (float)count / _cachedMessages.Count;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, style, GUILayout.Width(70));
            
            Rect barRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * percentage, barRect.height), 
                style.normal.textColor);
            
            EditorGUI.LabelField(barRect, $"{count} ({percentage:P1})", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLevelControl()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Log Level Control", _headerStyle);
            
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Log level control is only available in play mode.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            
            var logManagerComponent = FindObjectOfType<LogManagerComponent>();
            if (logManagerComponent == null || logManagerComponent.LoggerManager == null)
            {
                EditorGUILayout.HelpBox("Log manager not found in scene.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }
            
            var loggerManager = logManagerComponent.LoggerManager;
            
            // Global level control
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Global Minimum Level:", GUILayout.Width(150));
            
            byte currentLevel = loggerManager.GlobalMinimumLevel;
            string[] levelNames = new[] { "Debug", "Info", "Warning", "Error", "Critical" };
            
            int selectedIndex = 0;
            if (currentLevel == LogLevel.Debug) selectedIndex = 0;
            else if (currentLevel == LogLevel.Info) selectedIndex = 1;
            else if (currentLevel == LogLevel.Warning) selectedIndex = 2;
            else if (currentLevel == LogLevel.Error) selectedIndex = 3;
            else if (currentLevel == LogLevel.Critical) selectedIndex = 4;
            
            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, levelNames);
            
            if (newSelectedIndex != selectedIndex)
            {
                byte newLevel = LogLevel.Info;
                switch (newSelectedIndex)
                {
                    case 0: newLevel = LogLevel.Debug; break;
                    case 1: newLevel = LogLevel.Info; break;
                    case 2: newLevel = LogLevel.Warning; break;
                    case 3: newLevel = LogLevel.Error; break;
                    case 4: newLevel = LogLevel.Critical; break;
                }
                
                loggerManager.GlobalMinimumLevel = newLevel;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Profile selection
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Logging Profiles:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _selectedProfile = EditorGUILayout.ObjectField("Profile:", _selectedProfile, typeof(LoggingProfile), false) as LoggingProfile;
            
            if (_selectedProfile != null)
            {
                if (GUILayout.Button("Apply", GUILayout.Width(80)))
                {
                    loggerManager.ApplyProfile(_selectedProfile);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Create new profile
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create New Profile:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _newProfileName = EditorGUILayout.TextField("Name:", _newProfileName);
            
            if (GUILayout.Button("Create", GUILayout.Width(80)))
            {
                CreateNewProfile();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void CreateNewProfile()
        {
            // Create save dialog
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Logging Profile",
                _newProfileName,
                "asset",
                "Choose a location to save the logging profile."
            );
            
            if (string.IsNullOrEmpty(path))
                return;
                
            var profile = CreateInstance<LoggingProfile>();
            
            // Set current levels
            var logManagerComponent = FindObjectOfType<LogManagerComponent>();
            if (logManagerComponent != null && logManagerComponent.LoggerManager != null)
            {
                profile.SetGlobalLevel(logManagerComponent.LoggerManager.GlobalMinimumLevel);
                
                // Could capture tag overrides here if needed
            }
            
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _selectedProfile = profile;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
        }
        
        private void DrawMessageList()
        {
            // Get filtered messages
            var filteredMessages = FilterMessages();
            
            // Calculate height
            float messageHeight = EditorGUIUtility.singleLineHeight * 1.5f;
            float totalHeight = filteredMessages.Count * messageHeight;
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, 
                GUILayout.ExpandHeight(true));
            
            // Reserve space for messages
            Rect contentRect = EditorGUILayout.GetControlRect(GUILayout.Height(totalHeight));
            
            // Draw messages
            float y = contentRect.y;
            foreach (var message in filteredMessages)
            {
                Rect messageRect = new Rect(contentRect.x, y, contentRect.width, messageHeight);
                DrawMessage(messageRect, message);
                y += messageHeight;
            }
            
            EditorGUILayout.EndScrollView();
            
            // Auto-scroll to bottom if enabled
            if (_autoScroll && Event.current.type == EventType.Repaint && filteredMessages.Count > 0)
            {
                _scrollPosition = new Vector2(0, float.MaxValue);
            }
        }
        
        private void DrawMessage(Rect rect, LogMessage message)
        {
            // Draw message background based on level
            Color bgColor = Color.clear;
            GUIStyle style = _infoStyle;
            
            if (message.Level == LogLevel.Debug)
            {
                bgColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);
                style = _debugStyle;
            }
            else if (message.Level == LogLevel.Info)
            {
                bgColor = new Color(0.2f, 0.2f, 0.3f, 0.1f);
                style = _infoStyle;
            }
            else if (message.Level == LogLevel.Warning)
            {
                bgColor = new Color(0.3f, 0.3f, 0.1f, 0.2f);
                style = _warningStyle;
            }
            else if (message.Level == LogLevel.Error)
            {
                bgColor = new Color(0.3f, 0.1f, 0.1f, 0.2f);
                style = _errorStyle;
            }
            else if (message.Level == LogLevel.Critical)
            {
                bgColor = new Color(0.4f, 0.1f, 0.1f, 0.3f);
                style = _criticalStyle;
            }
            
            EditorGUI.DrawRect(rect, bgColor);
            
            // Draw message components
            float x = rect.x + 5;
            
            // Level
            EditorGUI.LabelField(new Rect(x, rect.y, 60, rect.height), 
                GetLogLevelName(message.Level), style);
            x += 60;
            
            // Tag
            EditorGUI.LabelField(new Rect(x, rect.y, 100, rect.height), 
                message.GetTagString(), _tagStyle);
            x += 100;
            
            // Message content (remainder of the width)
            EditorGUI.LabelField(new Rect(x, rect.y, rect.width - x - 5, rect.height), 
                message.Message.ToString(), _messageStyle);
            
            // Draw a separator line
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), 
                new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }
        
        private List<LogMessage> FilterMessages()
        {
            return _cachedMessages.Where(msg => 
            {
                // Filter by level
                if (msg.Level == LogLevel.Debug && !_showDebug) return false;
                if (msg.Level == LogLevel.Info && !_showInfo) return false;
                if (msg.Level == LogLevel.Warning && !_showWarning) return false;
                if (msg.Level == LogLevel.Error && !_showError) return false;
                if (msg.Level == LogLevel.Critical && !_showCritical) return false;
                
                // Filter by tag
                string tagString = msg.GetTagString();
                if (!_selectedTags.Contains(tagString)) return false;
                
                // Filter by search text
                if (!string.IsNullOrEmpty(_filterText))
                {
                    string messageText = msg.Message.ToString();
                    return messageText.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                
                return true;
            }).ToList();
        }
        
        private string GetLogLevelName(byte level)
        {
            if (level == LogLevel.Debug) return "Debug";
            if (level == LogLevel.Info) return "Info";
            if (level == LogLevel.Warning) return "Warning";
            if (level == LogLevel.Error) return "Error";
            if (level == LogLevel.Critical) return "Critical";
            return $"Level {level}";
        }
        
        private void ClearMessages()
        {
            _cachedMessages.Clear();
            _tagCounts.Clear();
            _levelCounts.Clear();
            Repaint();
        }
    }
}