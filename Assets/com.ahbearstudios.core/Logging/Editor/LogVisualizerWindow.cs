using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Events;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom Unity editor window that provides visualization and filtering of log messages.
    /// Uses Unity Collections v2 for efficient memory management and supports advanced filtering options.
    /// </summary>
    public class LogVisualizerWindow : EditorWindow, IDisposable
    {
        #region Configuration and Constants

        /// <summary>
        /// Maximum number of messages to cache by default.
        /// </summary>
        private const int DEFAULT_MAX_CACHED_MESSAGES = 1000;

        /// <summary>
        /// Refresh interval for the window in seconds when in play mode.
        /// </summary>
        private const float REFRESH_INTERVAL = 1.0f;

        /// <summary>
        /// Height of each log message in the list view.
        /// </summary>
        private const float MESSAGE_HEIGHT = 40f;

        #endregion

        #region Internal Data Structures

        /// <summary>
        /// Represents a visualization profile for log filtering and display settings.
        /// </summary>
        private struct LogVisualizationProfile
        {
            /// <summary>
            /// Unique name for this profile.
            /// </summary>
            public string Name;

            /// <summary>
            /// Filter text applied to message content.
            /// </summary>
            public string FilterText;

            /// <summary>
            /// Collection of tags to include in the filter.
            /// </summary>
            public NativeList<FixedString32Bytes> IncludedTags;

            /// <summary>
            /// Bitfield of log levels to display (1 bit per level).
            /// </summary>
            public int VisibleLevels;

            /// <summary>
            /// Creates a new profile with the specified name.
            /// </summary>
            /// <param name="name">The name for this profile.</param>
            public LogVisualizationProfile(string name)
            {
                Name = name;
                FilterText = string.Empty;
                IncludedTags = new NativeList<FixedString32Bytes>(16, Allocator.Persistent);
                VisibleLevels = 0x1F; // Show all levels by default (bits 0-4 set)
            }

            /// <summary>
            /// Checks if a given log level is visible in this profile.
            /// </summary>
            /// <param name="level">The log level to check.</param>
            /// <returns>True if the level should be visible.</returns>
            public bool IsLevelVisible(byte level)
            {
                return (VisibleLevels & (1 << level)) != 0;
            }

            /// <summary>
            /// Sets the visibility of a specific log level.
            /// </summary>
            /// <param name="level">The log level to set.</param>
            /// <param name="isVisible">Whether the level should be visible.</param>
            public void SetLevelVisibility(byte level, bool isVisible)
            {
                if (isVisible)
                    VisibleLevels |= (1 << level);
                else
                    VisibleLevels &= ~(1 << level);
            }

            /// <summary>
            /// Disposes native collections used by this profile.
            /// </summary>
            public void Dispose()
            {
                if (IncludedTags.IsCreated)
                    IncludedTags.Dispose();
            }
        }

        #endregion

        #region Private Fields

        // Native Collections for efficient memory management
        private NativeList<LogMessage> _messageBuffer;
        private NativeParallelHashMap<FixedString32Bytes, int> _tagCounts;
        private NativeParallelHashMap<byte, int> _levelCounts;

        // Profile management
        private List<LogVisualizationProfile> _profiles;
        private int _currentProfileIndex;
        private string _newProfileName = "New Profile";

        // Configuration
        private int _maxCachedMessages = DEFAULT_MAX_CACHED_MESSAGES;
        private float _lastUpdateTime;

        // UI State
        private Vector2 _scrollPosition;
        private bool _autoScroll = true;
        private bool _showFilters = true;
        private bool _showStatistics = true;
        private bool _showLevelControl = false;
        private string _filterText = string.Empty;
        private int _filteredMessageCount;

        // Cached filtered results to avoid filtering on every frame
        private NativeList<int> _filteredMessageIndices;
        private bool _needsRefiltering = true;

        // Synchronization object
        private readonly object _syncLock = new object();

        // Styles
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

        // State tracking
        private bool _initialized;
        private bool _isDisposed;

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Opens or focuses the Log Visualizer window.
        /// </summary>
        [MenuItem("Window/AhBearStudios/Log Visualizer")]
        private static void ShowWindow()
        {
            var window = GetWindow<LogVisualizerWindow>();
            window.titleContent = new GUIContent("Log Visualizer");
            window.Show();
        }

        /// <summary>
        /// Initializes the window when it's enabled.
        /// </summary>
        private void OnEnable()
        {
            try
            {
                InitializeIfNeeded();

                // Subscribe to logging events
                LogEvents.OnMessageWritten += OnMessageWritten;
                LogEvents.OnLogLevelChanged += OnLogLevelChanged;
                EditorApplication.update += OnEditorUpdate;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing Log Visualizer: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up resources when the window is disabled.
        /// </summary>
        private void OnDisable()
        {
            // Unsubscribe from events
            LogEvents.OnMessageWritten -= OnMessageWritten;
            LogEvents.OnLogLevelChanged -= OnLogLevelChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// Cleans up resources when the window is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization and Resource Management

        /// <summary>
        /// Initializes the window's resources if they haven't been initialized yet.
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            try
            {
                // Initialize native collections
                _messageBuffer = new NativeList<LogMessage>(DEFAULT_MAX_CACHED_MESSAGES, Allocator.Persistent);
                _tagCounts = new NativeParallelHashMap<FixedString32Bytes, int>(32, Allocator.Persistent);
                _levelCounts = new NativeParallelHashMap<byte, int>(8, Allocator.Persistent);
                _filteredMessageIndices = new NativeList<int>(DEFAULT_MAX_CACHED_MESSAGES, Allocator.Persistent);

                // Initialize profiles
                _profiles = new List<LogVisualizationProfile>();
                _profiles.Add(new LogVisualizationProfile("Default"));
                _currentProfileIndex = 0;

                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Log Visualizer resources: {ex.Message}");
                DisposeResources();
            }
        }

        /// <summary>
        /// Disposes all native resources used by this window.
        /// </summary>
        private void DisposeResources()
        {
            if (_messageBuffer.IsCreated)
                _messageBuffer.Dispose();

            if (_tagCounts.IsCreated)
                _tagCounts.Dispose();

            if (_levelCounts.IsCreated)
                _levelCounts.Dispose();

            if (_filteredMessageIndices.IsCreated)
                _filteredMessageIndices.Dispose();

            // Dispose profiles
            foreach (var profile in _profiles)
            {
                profile.Dispose();
            }

            _profiles?.Clear();
            _initialized = false;
        }

        /// <summary>
        /// Implements IDisposable to ensure proper cleanup of unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            DisposeResources();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called periodically by Unity's editor update.
        /// Forces window repaint when in play mode.
        /// </summary>
        private void OnEditorUpdate()
        {
            // Force repaint periodically when playing to ensure UI stays up to date
            if (EditorApplication.isPlaying && (Time.realtimeSinceStartup - _lastUpdateTime > REFRESH_INTERVAL))
            {
                _lastUpdateTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        /// <summary>
        /// Handles logging system message written events.
        /// Stores log messages and updates statistics.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments containing the log message.</param>
        private void OnMessageWritten(object sender, LogMessageWrittenEventArgs e)
        {
            try
            {
                lock (_syncLock)
                {
                    if (!_initialized || _isDisposed)
                        return;

                    // Add message to buffer
                    _messageBuffer.Add(e.Message);

                    // Update tag counts
                    var tagString = e.Message.GetTagString();
                    _tagCounts.TryGetValue(tagString, out int tagCount);
                    _tagCounts[tagString] = tagCount + 1;

                    // Update level counts
                    _levelCounts.TryGetValue(e.Message.Level, out int levelCount);
                    _levelCounts[e.Message.Level] = levelCount + 1;

                    // Trim buffer if needed
                    while (_messageBuffer.Length > _maxCachedMessages)
                    {
                        _messageBuffer.RemoveAt(0);
                    }

                    // Mark that filtering needs to be redone
                    _needsRefiltering = true;

                    // Request repaint
                    Repaint();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing log message: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles log level changed events.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnLogLevelChanged(object sender, LogLevelChangedEventArgs e)
        {
            // Just request a repaint to update UI
            Repaint();
        }

        #endregion

        #region UI Rendering

        /// <summary>
        /// Initialize GUI styles used in the window.
        /// </summary>
        private void InitializeStyles()
        {
            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
            }

            if (_infoStyle == null)
            {
                _infoStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 1.0f) }
                };
            }

            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1.0f, 0.9f, 0.4f) }
                };
            }

            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1.0f, 0.5f, 0.5f) }
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
                _tagStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }

            if (_timestampStyle == null)
            {
                _timestampStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }

            if (_messageStyle == null)
            {
                _messageStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    richText = true
                };
            }

            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    margin = new RectOffset(4, 4, 8, 4)
                };
            }

            if (_propertyStyle == null)
            {
                _propertyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.7f, 0.9f, 0.7f) }
                };
            }
        }

        /// <summary>
        /// Main GUI rendering method.
        /// </summary>
        private void OnGUI()
        {
            if (!_initialized)
            {
                InitializeIfNeeded();
                if (!_initialized)
                {
                    EditorGUILayout.HelpBox("Failed to initialize Log Visualizer. Check console for errors.",
                        MessageType.Error);
                    return;
                }
            }

            InitializeStyles();

            EditorGUILayout.BeginVertical();

            DrawToolbar();

            if (_showFilters)
                DrawFilters();

            if (_showStatistics)
                DrawStatistics();

            if (_showLevelControl)
                DrawLevelControl();

            DrawMessageList();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the toolbar with action buttons and toggles.
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ClearMessages();
            }

            _showFilters = GUILayout.Toggle(_showFilters, "Filters", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showStatistics =
                GUILayout.Toggle(_showStatistics, "Stats", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showLevelControl = GUILayout.Toggle(_showLevelControl, "Log Levels", EditorStyles.toolbarButton,
                GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            // Profile selection
            int newProfileIndex = EditorGUILayout.Popup(_currentProfileIndex,
                _profiles.ConvertAll(p => p.Name).ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(120));
            if (newProfileIndex != _currentProfileIndex)
            {
                _currentProfileIndex = newProfileIndex;
                _needsRefiltering = true;
            }

            // Profile management buttons
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(22)))
            {
                CreateNewProfile();
            }

            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto-Scroll", EditorStyles.toolbarButton, GUILayout.Width(80));

            // Display message count
            int totalMessages = _messageBuffer.Length;
            GUILayout.Label($"Messages: {_filteredMessageCount}/{totalMessages}", EditorStyles.toolbarButton,
                GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws filter controls for filtering log messages.
        /// </summary>
        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Filters", _headerStyle);

            EditorGUILayout.BeginHorizontal();

            // Filter text field
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            string newFilterText = EditorGUILayout.TextField(_filterText);
            if (newFilterText != _filterText)
            {
                _filterText = newFilterText;
                _needsRefiltering = true;
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _filterText = string.Empty;
                _needsRefiltering = true;
            }

            EditorGUILayout.EndHorizontal();

            // Log level filters
            EditorGUILayout.BeginHorizontal();

            bool newShowDebug = GUILayout.Toggle(_profiles[_currentProfileIndex].IsLevelVisible(LogLevel.Debug),
                "Debug",
                GUILayout.Width(60));
            UpdateLogLevelVisibility(LogLevel.Debug, newShowDebug);

            bool newShowInfo = GUILayout.Toggle(_profiles[_currentProfileIndex].IsLevelVisible(LogLevel.Info), "Info",
                GUILayout.Width(60));
            UpdateLogLevelVisibility(LogLevel.Info, newShowInfo);

            bool newShowWarning = GUILayout.Toggle(_profiles[_currentProfileIndex].IsLevelVisible(LogLevel.Warning),
                "Warning",
                GUILayout.Width(70));
            UpdateLogLevelVisibility(LogLevel.Warning, newShowWarning);

            bool newShowError = GUILayout.Toggle(_profiles[_currentProfileIndex].IsLevelVisible(LogLevel.Error),
                "Error",
                GUILayout.Width(60));
            UpdateLogLevelVisibility(LogLevel.Error, newShowError);

            bool newShowCritical = GUILayout.Toggle(_profiles[_currentProfileIndex].IsLevelVisible(LogLevel.Critical),
                "Critical",
                GUILayout.Width(70));
            UpdateLogLevelVisibility(LogLevel.Critical, newShowCritical);

            EditorGUILayout.EndHorizontal();

            // Tag filters
            EditorGUILayout.LabelField("Tags:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                // Implement Select All tags
                SelectAllTags(true);
                _needsRefiltering = true;
            }

            if (GUILayout.Button("Select None", GUILayout.Width(80)))
            {
                // Implement Select None tags
                SelectAllTags(false);
                _needsRefiltering = true;
            }

            EditorGUILayout.EndHorizontal();

            // Tag grid (in scroll view if many tags)
            float height = Mathf.Min(100, (_tagCounts.Count() / 4 + 1) * 20);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(height));

            EditorGUILayout.BeginHorizontal();
            int column = 0;
            using var tagEnumerator = _tagCounts.GetEnumerator();

            while (tagEnumerator.MoveNext())
            {
                var tagString = tagEnumerator.Current.Key;
                bool isIncluded = IsTagIncluded(tagString);

                bool newValue = GUILayout.Toggle(isIncluded, $"{tagString} ({tagEnumerator.Current.Value})",
                    GUILayout.Width(120));
                if (newValue != isIncluded)
                {
                    SetTagIncluded(tagString, newValue);
                    _needsRefiltering = true;
                }

                column++;
                if (column >= 4)
                {
                    column = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Updates the visibility of a specific log level and triggers refiltering if needed.
        /// </summary>
        /// <param name="level">The log level to update.</param>
        /// <param name="isVisible">Whether the log level should be visible.</param>
        private void UpdateLogLevelVisibility(byte level, bool isVisible)
        {
            if (isVisible != _profiles[_currentProfileIndex].IsLevelVisible(level))
            {
                _profiles[_currentProfileIndex].SetLevelVisibility(level, isVisible);
                _needsRefiltering = true;
            }
        }

        /// <summary>
        /// Draws statistics about log message counts.
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Statistics", _headerStyle);

            // Messages by level
            EditorGUILayout.LabelField("Messages by Level:", EditorStyles.boldLabel);

            using (var levelEnumerator = _levelCounts.GetEnumerator())
            {
                while (levelEnumerator.MoveNext())
                {
                    var level = levelEnumerator.Current.Key;
                    var count = levelEnumerator.Current.Value;
                    DrawLevelBar(GetLogLevelName(level), level, GetStyleForLevel(level));
                }
            }

            EditorGUILayout.Space();

            // Top tags
            EditorGUILayout.LabelField("Top Tags:", EditorStyles.boldLabel);

            // Sort tags by count and display top 5
            using (var tagEnumerator = _tagCounts.GetEnumerator())
            {
                var topTags = new List<KeyValuePair<FixedString32Bytes, int>>();

                while (tagEnumerator.MoveNext())
                {
                    topTags.Add(new KeyValuePair<FixedString32Bytes, int>(
                        tagEnumerator.Current.Key, tagEnumerator.Current.Value));
                }

                topTags.Sort((a, b) => b.Value.CompareTo(a.Value));

                for (int i = 0; i < Math.Min(5, topTags.Count); i++)
                {
                    EditorGUILayout.LabelField($"{topTags[i].Key}: {topTags[i].Value}", _tagStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a bar visualization for a log level with message count.
        /// </summary>
        /// <param name="label">The level name.</param>
        /// <param name="level">The level value.</param>
        /// <param name="style">The GUI style to use.</param>
        private void DrawLevelBar(string label, byte level, GUIStyle style)
        {
            _levelCounts.TryGetValue(level, out int count);
            int totalMessages = _messageBuffer.Length;
            float percentage = totalMessages > 0 ? (float)count / totalMessages : 0;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, style, GUILayout.Width(70));
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Height(18), GUILayout.ExpandWidth(true)),
                percentage, $"{count} ({percentage:P1})");

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws global log level controls if available.
        /// </summary>
        private void DrawLevelControl()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Log Level Management", _headerStyle);

            // Profile creation field
            EditorGUILayout.BeginHorizontal();

            _newProfileName = EditorGUILayout.TextField("New Profile Name:", _newProfileName);

            if (GUILayout.Button("Create", GUILayout.Width(80)))
            {
                CreateNewProfile();
            }

            EditorGUILayout.EndHorizontal();

            // Here you would add controls to modify the current logging profile
            // by interacting with your logging system

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Creates a new visualization profile with the current name.
        /// </summary>
        private void CreateNewProfile()
        {
            if (string.IsNullOrWhiteSpace(_newProfileName))
                return;

            _profiles.Add(new LogVisualizationProfile(_newProfileName));
            _currentProfileIndex = _profiles.Count - 1;
            _newProfileName = $"Profile {_profiles.Count + 1}";
            _needsRefiltering = true;
        }

        /// <summary>
        /// Draws the list of filtered log messages.
        /// </summary>
        private void DrawMessageList()
        {
            if (_needsRefiltering)
            {
                FilterMessages();
                _needsRefiltering = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            var contentHeight = _filteredMessageIndices.Length * MESSAGE_HEIGHT;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            // Only draw visible messages based on scroll position
            Rect scrollViewRect = GUILayoutUtility.GetLastRect();
            float scrollY = _scrollPosition.y;
            float scrollHeight = scrollViewRect.height;

            int startIndex = Mathf.Max(0, Mathf.FloorToInt(scrollY / MESSAGE_HEIGHT));
            int visibleCount = Mathf.CeilToInt(scrollHeight / MESSAGE_HEIGHT) + 1;
            int endIndex = Mathf.Min(startIndex + visibleCount, _filteredMessageIndices.Length);

            // Create a tall rect to accommodate all messages
            GUILayoutUtility.GetRect(scrollViewRect.width, contentHeight);

            // Draw only visible messages
            for (int i = startIndex; i < endIndex; i++)
            {
                int messageIndex = _filteredMessageIndices[i];
                var message = _messageBuffer[messageIndex];

                Rect messageRect = new Rect(0, i * MESSAGE_HEIGHT, scrollViewRect.width, MESSAGE_HEIGHT);

                if (messageRect.yMax < scrollY || messageRect.y > scrollY + scrollHeight)
                    continue;

                DrawMessage(messageRect, message);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Auto-scroll to bottom if enabled
            if (_autoScroll && EditorApplication.isPlaying && _filteredMessageIndices.Length > 0)
            {
                _scrollPosition = new Vector2(_scrollPosition.x, contentHeight);
                Repaint();
            }
        }

        /// <summary>
        /// Draws a single log message in the provided rect.
        /// </summary>
        /// <param name="rect">The rectangle to draw the message in.</param>
        /// <param name="message">The message to draw.</param>
        private void DrawMessage(Rect rect, LogMessage message)
        {
            // Background with alternating colors
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.1f));

            // Get the appropriate style based on message level
            GUIStyle levelStyle = GetStyleForLevel(message.Level);

            // Draw tag and timestamp
            Rect tagRect = new Rect(rect.x + 5, rect.y + 2, 120, 16);
            EditorGUI.LabelField(tagRect, message.GetTagString().ToString(), _tagStyle);

            // Format timestamp
            DateTime dt = new DateTime(message.TimestampTicks);
            string timestamp = $"{dt:HH:mm:ss.fff}";

            Rect timeRect = new Rect(rect.x + 130, rect.y + 2, 100, 16);
            EditorGUI.LabelField(timeRect, timestamp, _timestampStyle);

            // Level indicator
            Rect levelRect = new Rect(rect.x + 240, rect.y + 2, 70, 16);
            EditorGUI.LabelField(levelRect, GetLogLevelName(message.Level), levelStyle);

            // Message content
            Rect messageRect = new Rect(rect.x + 5, rect.y + 18, rect.width - 10, 22);
            EditorGUI.LabelField(messageRect, message.Message.ToString(), _messageStyle);

            // Draw properties if available
            if (message.Properties.IsCreated)
            {
                string propertiesText = FormatProperties(message.Properties);
                Rect propertiesRect = new Rect(rect.x + 320, rect.y + 2, rect.width - 325, 16);
                EditorGUI.LabelField(propertiesRect, propertiesText, _propertyStyle);
            }
        }

        #endregion

        /// <summary>
/// Filters messages based on current filter settings.
/// </summary>
private void FilterMessages()
{
    lock (_syncLock)
    {
        if (!_initialized || _isDisposed)
            return;

        _filteredMessageIndices.Clear();

        if (_messageBuffer.Length == 0)
        {
            _filteredMessageCount = 0;
            return;
        }

        bool hasFilterText = !string.IsNullOrEmpty(_filterText);

        // Loop through all messages and apply filters
        for (int i = 0; i < _messageBuffer.Length; i++)
        {
            var message = _messageBuffer[i];

            // Apply level filter
            if (!IsLevelVisible(message.Level))
                continue;

            // Apply tag filter
            if (!IsTagIncluded(message.GetTagString()))
                continue;

            // Apply text filter
            if (hasFilterText && !MessageMatchesFilter(message, _filterText))
                continue;

            // Message passed all filters
            _filteredMessageIndices.Add(i);
        }

        _filteredMessageCount = _filteredMessageIndices.Length;
    }
}

/// <summary>
/// Checks if a log level is visible in the current profile.
/// </summary>
/// <param name="level">The log level to check.</param>
/// <returns>True if the log level should be visible.</returns>
private bool IsLevelVisible(byte level)
{
    return _profiles[_currentProfileIndex].IsLevelVisible(level);
}

/// <summary>
/// Checks if a message matches the filter text.
/// </summary>
/// <param name="message">The message to check.</param>
/// <param name="filterText">The filter text to match against.</param>
/// <returns>True if the message matches the filter.</returns>
private bool MessageMatchesFilter(LogMessage message, string filterText)
{
    // Check message content
    if (message.Message.ToString().IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
        return true;
            
    // Check properties if message content didn't match
    if (message.Properties.IsCreated)
        return PropertiesContainText(message.Properties, filterText);
            
    return false;
}

        /// <summary>
        /// Checks if properties contain the specified text.
        /// </summary>
        /// <param name="properties">The log properties to search.</param>
        /// <param name="searchText">The text to search for.</param>
        /// <returns>True if any property contains the search text.</returns>
        private bool PropertiesContainText(LogProperties properties, string searchText)
        {
            if (!properties.IsCreated)
                return false;

            foreach (var kvp in properties)
            {
                if (kvp.Key.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Value.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Formats properties for display in the UI.
        /// </summary>
        /// <param name="properties">The properties to format.</param>
        /// <returns>A formatted string of the properties.</returns>
        private string FormatProperties(LogProperties properties)
        {
            if (!properties.IsCreated)
                return string.Empty;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('{');

            bool first = true;
            foreach (var kvp in properties)
            {
                if (!first)
                    sb.Append(", ");

                sb.Append(kvp.Key.ToString());
                sb.Append('=');
                sb.Append(kvp.Value.ToString());
                first = false;

                // Limit the length to avoid excessive text
                if (sb.Length > 100)
                {
                    sb.Append("...");
                    break;
                }
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Gets the GUI style for a specific log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The appropriate GUI style.</returns>
        private GUIStyle GetStyleForLevel(byte level)
        {
            switch (level)
            {
                case LogLevel.Debug: return _debugStyle;
                case LogLevel.Info: return _infoStyle;
                case LogLevel.Warning: return _warningStyle;
                case LogLevel.Error: return _errorStyle;
                case LogLevel.Critical: return _criticalStyle;
                default: return EditorStyles.label;
            }
        }

        /// <summary>
        /// Gets a human-readable name for a log level.
        /// </summary>
        /// <param name="level">The log level byte value.</param>
        /// <returns>A human-readable name.</returns>
        private string GetLogLevelName(byte level)
        {
            switch (level)
            {
                case LogLevel.Debug: return "Debug";
                case LogLevel.Info: return "Info";
                case LogLevel.Warning: return "Warning";
                case LogLevel.Error: return "Error";
                case LogLevel.Critical: return "Critical";
                default: return $"Level {level}";
            }
        }

        /// <summary>
        /// Checks if a tag is included in the current filter profile.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag is included in the filter.</returns>
        private bool IsTagIncluded(FixedString32Bytes tag)
        {
            // If no tags are explicitly included, show all tags
            if (_profiles[_currentProfileIndex].IncludedTags.Length == 0)
                return true;

            // Otherwise check if this specific tag is included
            for (int i = 0; i < _profiles[_currentProfileIndex].IncludedTags.Length; i++)
            {
                if (_profiles[_currentProfileIndex].IncludedTags[i].Equals(tag))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets whether a tag is included in the current filter profile.
        /// </summary>
        /// <param name="tag">The tag to set.</param>
        /// <param name="included">Whether the tag should be included.</param>
        private void SetTagIncluded(FixedString32Bytes tag, bool included)
        {
            bool isCurrentlyIncluded = IsTagIncluded(tag);
    
            if (included && !isCurrentlyIncluded)
            {
                // Add the tag to the included list
                _profiles[_currentProfileIndex].IncludedTags.Add(tag);
            }
            else if (!included && isCurrentlyIncluded)
            {
                // Get a direct reference to the tags in the current profile
                var includedTags = _profiles[_currentProfileIndex].IncludedTags;
        
                // Remove the tag from the included list
                for (int i = 0; i < includedTags.Length; i++)
                {
                    if (includedTags[i].Equals(tag))
                    {
                        _profiles[_currentProfileIndex].IncludedTags.RemoveAtSwapBack(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Sets all tags to be included or excluded.
        /// </summary>
        /// <param name="included">Whether all tags should be included.</param>
        private void SelectAllTags(bool included)
        {
            // Clear the current included tags
            _profiles[_currentProfileIndex].IncludedTags.Clear();

            if (included)
            {
                // Add all known tags to the included list
                using var tagEnumerator = _tagCounts.GetEnumerator();
                while (tagEnumerator.MoveNext())
                {
                    _profiles[_currentProfileIndex].IncludedTags.Add(tagEnumerator.Current.Key);
                }
            }
        }

        /// <summary>
        /// Clears all cached messages and statistics.
        /// </summary>
        private void ClearMessages()
        {
            lock (_syncLock)
            {
                _messageBuffer.Clear();
                _tagCounts.Clear();
                _levelCounts.Clear();
                _filteredMessageIndices.Clear();
                _filteredMessageCount = 0;
                _needsRefiltering = false;
                Repaint();
            }
        }
    }
}