using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Editor window that visualizes log messages with filtering and level controls.
    /// Uses message bus for communication with logging system.
    /// </summary>
    public class LogVisualizerWindow : EditorWindow, IDisposable
    {
        // Dependencies
        private IMessageBusService _messageBusService;
        private List<IDisposable> _subscriptions = new List<IDisposable>();
        private ILogVisualizationProfileManager _profileManager;

        // UI state
        private bool _initialized;
        private Vector2 _scrollPosition;
        private string _filterText = "";
        private Dictionary<string, bool> _tagFilters = new Dictionary<string, bool>();
        private LogVisualizationProfile _currentProfile;
        private string _newProfileName = "New Profile";
        private string _currentProfileName = "Default";
        private LogLevel _selectedLogLevel = LogLevel.Info;
        private List<LogMessage> _filteredMessages = new List<LogMessage>();
        private List<LogMessage> _allMessages = new List<LogMessage>();
        private bool _autoScroll = true;
        private double _lastUpdateTime;
        private const double UpdateInterval = 0.1; // seconds

        // Statistics
        private int _totalMessageCount;
        private Dictionary<LogLevel, int> _messageCountByLevel = new Dictionary<LogLevel, int>();

        // UI styles
        private GUIStyleCollection _styles;

        // Static reference to the currently active message bus for this window
        private static IMessageBusService _globalMessageBusService;

        /// <summary>
        /// Sets the global message bus instance that log visualizer windows will use.
        /// This should be called during application initialization.
        /// </summary>
        /// <param name="messageBusService">The message bus instance to use for log visualization.</param>
        public static void SetGlobalMessageBus(IMessageBusService messageBusService)
        {
            _globalMessageBusService = messageBusService;
        }

        /// <summary>
        /// Opens or focuses the log visualizer window.
        /// </summary>
        [MenuItem("Window/AhBearStudios/Log Visualizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<LogVisualizerWindow>();
            window.titleContent = new GUIContent("Log Visualizer");
            window.Show();
        }

        /// <summary>
        /// Opens or focuses the log visualizer window with a specific message bus.
        /// </summary>
        /// <param name="messageBusService">The message bus to use for this window instance.</param>
        public static void ShowWindow(IMessageBusService messageBusService)
        {
            var window = GetWindow<LogVisualizerWindow>();
            window.titleContent = new GUIContent("Log Visualizer");
            window._messageBusService = messageBusService; // Set the message bus before initialization
            window.Show();
        }

        /// <summary>
        /// Called when the window is enabled. Initializes resources and subscriptions.
        /// </summary>
        private void OnEnable()
        {
            InitializeIfNeeded();
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Called when the window is disabled. Cleans up resources.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// Called when the window is destroyed. Ensures proper cleanup.
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Initializes the window if it hasn't been initialized yet.
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            // Initialize message bus if not already set
            if (_messageBusService == null)
            {
                // Try to use the global message bus first
                _messageBusService = _globalMessageBusService;

                // If no global message bus is set, create a null implementation
                if (_messageBusService == null)
                {
                    _messageBusService = CreateNullMessageBus();
                    Debug.LogWarning("LogVisualizerWindow: No message bus configured. " +
                                     "Use LogVisualizerWindow.SetGlobalMessageBus() or ShowWindow(IMessageBusService) to provide a message bus instance. " +
                                     "Log visualization will be limited without a proper message bus.");
                }
            }

            // Initialize profile manager
            if (_profileManager == null)
            {
                _profileManager = new LogVisualizationProfileManager();
                _currentProfile = _profileManager.GetOrCreateProfile(_currentProfileName);
            }

            // Set up subscriptions to message types
            SubscribeToMessages();

            // Initialize UI styles
            _styles = new GUIStyleCollection();
            _styles.Initialize();

            // Initialize level count dictionary
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (level != LogLevel.None)
                    _messageCountByLevel[level] = 0;
            }

            _initialized = true;
        }

        /// <summary>
        /// Creates a no-op message bus implementation when no real message bus is available.
        /// </summary>
        /// <returns>A no-op message bus instance.</returns>
        private static IMessageBusService CreateNullMessageBus()
        {
            return new NullMessageBusService();
        }

        /// <summary>
        /// Sets up subscriptions to the message types we care about.
        /// </summary>
        private void SubscribeToMessages()
        {
            // Clear any existing subscriptions first
            UnsubscribeFromMessages();

            try
            {
                // Subscribe to log entry written messages
                _subscriptions.Add(_messageBusService.SubscribeToMessage<LogEntryWrittenMessage>(OnLogEntryWritten));

                // Subscribe to log level changed messages
                _subscriptions.Add(_messageBusService.SubscribeToMessage<LogLevelChangedMessage>(OnLogLevelChanged));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LogVisualizerWindow: Failed to subscribe to log messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Unsubscribes from all message types.
        /// </summary>
        private void UnsubscribeFromMessages()
        {
            foreach (var subscription in _subscriptions)
            {
                try
                {
                    subscription?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"LogVisualizerWindow: Error disposing subscription: {ex.Message}");
                }
            }

            _subscriptions.Clear();
        }

        /// <summary>
        /// Handler for log entry written messages from the message bus.
        /// </summary>
        private void OnLogEntryWritten(LogEntryWrittenMessage message)
        {
            // Process the log message
            ProcessLogMessage(message.LogMessage);
        }

        /// <summary>
        /// Handler for log level changed messages from the message bus.
        /// </summary>
        private void OnLogLevelChanged(LogLevelChangedMessage message)
        {
            // Update the selected log level if needed
            _selectedLogLevel = message.NewLevel;
            FilterMessages();
            Repaint();
        }

        /// <summary>
        /// Called periodically by the editor to update the window.
        /// </summary>
        private void OnEditorUpdate()
        {
            var time = EditorApplication.timeSinceStartup;
            if (time - _lastUpdateTime >= UpdateInterval)
            {
                _lastUpdateTime = time;
                Repaint();
            }
        }

        /// <summary>
        /// Processes a log message to update statistics and filtered lists.
        /// </summary>
        private void ProcessLogMessage(LogMessage message)
        {
            _allMessages.Add(message);
            _totalMessageCount++;

            var level = (LogLevel)message.Level;
            if (_messageCountByLevel.ContainsKey(level))
            {
                _messageCountByLevel[level]++;
            }

            // Add the tag to our filter list if it doesn't exist
            var tagString = message.Tag.ToString();
            if (!_tagFilters.ContainsKey(tagString))
            {
                _tagFilters[tagString] = true;
            }

            FilterMessages();

            if (_autoScroll)
            {
                _scrollPosition = new Vector2(0, float.MaxValue);
            }

            Repaint();
        }

        /// <summary>
        /// Draws the window GUI.
        /// </summary>
        private void OnGUI()
        {
            if (!_initialized)
            {
                InitializeIfNeeded();
            }

            EditorGUILayout.BeginVertical();

            // Top toolbar
            DrawToolbar();

            // Filters section
            DrawFilters();

            // Statistics
            DrawStatistics();

            // Log level control
            DrawLevelControl();

            // Message list
            DrawMessageList();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the top toolbar with control buttons.
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", _styles.ToolbarButtonStyle))
            {
                ClearMessages();
            }

            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", _styles.ToolbarButtonStyle);

            GUILayout.FlexibleSpace();

            // Show message bus status
            var messageBusStatus = _messageBusService is NullMessageBusService ? "No Message Bus" : "Connected";
            var statusColor = _messageBusService is NullMessageBusService ? Color.yellow : Color.green;

            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label($"Status: {messageBusStatus}", EditorStyles.miniLabel);
            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the filters section for tag and text filtering.
        /// </summary>
        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Filters", _styles.HeaderStyle);

            // Text filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(50));
            string newFilter = EditorGUILayout.TextField(_filterText);
            if (newFilter != _filterText)
            {
                _filterText = newFilter;
                FilterMessages();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _filterText = "";
                FilterMessages();
            }

            EditorGUILayout.EndHorizontal();

            // Tag filters
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tags:", GUILayout.Width(50));

            if (GUILayout.Button("All", GUILayout.Width(60)))
            {
                SelectAllTags(true);
            }

            if (GUILayout.Button("None", GUILayout.Width(60)))
            {
                SelectAllTags(false);
            }

            EditorGUILayout.EndHorizontal();

            // Display tag toggles in a scrollable area
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(50); // Indent to align with "Tags:" label

            EditorGUILayout.BeginVertical();
            int tagsPerRow = Mathf.Max(1, (int)(EditorGUIUtility.currentViewWidth - 100) / 200);

            int tagCount = _tagFilters.Count;
            string[] tags = _tagFilters.Keys.ToArray();

            for (int i = 0; i < tagCount; i += tagsPerRow)
            {
                EditorGUILayout.BeginHorizontal();

                for (int j = 0; j < tagsPerRow && i + j < tagCount; j++)
                {
                    string tag = tags[i + j];
                    bool included = _tagFilters[tag];
                    bool newIncluded = EditorGUILayout.ToggleLeft(tag, included, GUILayout.Width(200));

                    if (newIncluded != included)
                    {
                        SetTagIncluded(new FixedString32Bytes(tag), newIncluded);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Updates the visibility of a specific log level.
        /// </summary>
        private void UpdateLogLevelVisibility(LogLevel level, bool isVisible)
        {
            if (_currentProfile != null)
            {
                _currentProfile.SetLevelVisibility(level, isVisible);
                FilterMessages();
            }
        }

        /// <summary>
        /// Draws the statistics section showing message counts by level.
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Statistics", _styles.HeaderStyle);
            EditorGUILayout.LabelField($"Total Messages: {_totalMessageCount}");

            EditorGUILayout.BeginHorizontal();

            // Display counts for each log level
            DrawLevelBar("Debug", LogLevel.Debug, _styles.DebugStyle);
            DrawLevelBar("Info", LogLevel.Info, _styles.InfoStyle);
            DrawLevelBar("Warning", LogLevel.Warning, _styles.WarningStyle);
            DrawLevelBar("Error", LogLevel.Error, _styles.ErrorStyle);
            DrawLevelBar("Critical", LogLevel.Critical, _styles.CriticalStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a bar representing the count of messages at a specific log level.
        /// </summary>
        private void DrawLevelBar(string label, LogLevel level, GUIStyle style)
        {
            int count = _messageCountByLevel.ContainsKey(level) ? _messageCountByLevel[level] : 0;
            bool isVisible = _currentProfile.IsLevelVisible(level);

            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            // Label with count
            EditorGUILayout.BeginHorizontal();
            bool newIsVisible = EditorGUILayout.ToggleLeft(label, isVisible, GUILayout.Width(80));
            EditorGUILayout.LabelField(count.ToString(), style, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            if (newIsVisible != isVisible)
            {
                UpdateLogLevelVisibility(level, newIsVisible);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the log level control section.
        /// </summary>
        private void DrawLevelControl()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Log Level Profiles", _styles.HeaderStyle);

            EditorGUILayout.BeginHorizontal();

            // Profile selection dropdown
            string[] profileNames = _profileManager.GetProfileNames();
            int currentIndex = Array.IndexOf(profileNames, _currentProfileName);
            int newIndex = EditorGUILayout.Popup("Profile:", currentIndex, profileNames);

            if (newIndex != currentIndex && newIndex >= 0 && newIndex < profileNames.Length)
            {
                _currentProfileName = profileNames[newIndex];
                _currentProfile = _profileManager.GetOrCreateProfile(_currentProfileName);
                FilterMessages();
            }

            EditorGUILayout.EndHorizontal();

            // Create new profile
            EditorGUILayout.BeginHorizontal();

            _newProfileName = EditorGUILayout.TextField("New Profile:", _newProfileName);

            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateNewProfile();
            }

            EditorGUILayout.EndHorizontal();

            // Minimum log level selection
            EditorGUILayout.BeginHorizontal();

            LogLevel newSelectedLevel = (LogLevel)EditorGUILayout.EnumPopup("Minimum Level:", _selectedLogLevel);
            if (newSelectedLevel != _selectedLogLevel)
            {
                _selectedLogLevel = newSelectedLevel;
                FilterMessages();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Creates a new log visualization profile.
        /// </summary>
        private void CreateNewProfile()
        {
            if (string.IsNullOrEmpty(_newProfileName) || _profileManager.ProfileExists(_newProfileName))
            {
                return;
            }

            _currentProfile = _profileManager.CreateProfile(_newProfileName);
            _currentProfileName = _newProfileName;
            _newProfileName = "New Profile";

            FilterMessages();
        }

        /// <summary>
        /// Draws the message list with filtering applied.
        /// </summary>
        private void DrawMessageList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            EditorGUILayout.LabelField($"Messages ({_filteredMessages.Count})", _styles.HeaderStyle);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            float messageHeight = 40; // Approximate height for each message
            int totalMessages = _filteredMessages.Count;

            // Calculate the visible range
            Rect scrollViewRect = GUILayoutUtility.GetLastRect();
            float scrollViewHeight = scrollViewRect.height;

            int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(_scrollPosition.y / messageHeight));
            int visibleCount = Mathf.CeilToInt(scrollViewHeight / messageHeight) + 1;
            int lastVisibleIndex = Mathf.Min(firstVisibleIndex + visibleCount, totalMessages - 1);

            // Provide height for the content
            GUILayout.Space(totalMessages * messageHeight);

            // Draw only the visible messages
            for (int i = firstVisibleIndex; i <= lastVisibleIndex && i < totalMessages; i++)
            {
                Rect messageRect = new Rect(0, i * messageHeight, EditorGUIUtility.currentViewWidth - 20,
                    messageHeight);

                if (messageRect.yMin < scrollViewRect.yMax && messageRect.yMax > scrollViewRect.yMin)
                {
                    DrawMessage(messageRect, _filteredMessages[i]);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a single message in the message list.
        /// </summary>
        private void DrawMessage(Rect rect, LogMessage message)
        {
            LogLevel level = (LogLevel)message.Level;
            GUIStyle style = GetStyleForLevel(level);

            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.2f));

            // Draw timestamp
            string timestamp = new DateTime(message.TimestampTicks).ToString("HH:mm:ss.fff");
            Rect timestampRect = new Rect(rect.x + 5, rect.y + 2, 100, 16);
            EditorGUI.LabelField(timestampRect, timestamp, style);

            // Draw tag
            Rect tagRect = new Rect(rect.x + 110, rect.y + 2, 150, 16);
            EditorGUI.LabelField(tagRect, $"[{message.Tag}]", style);

            // Draw level
            Rect levelRect = new Rect(rect.x + 270, rect.y + 2, 80, 16);
            EditorGUI.LabelField(levelRect, GetLogLevelName(level), style);

            // Draw message content
            Rect contentRect = new Rect(rect.x + 5, rect.y + 20, rect.width - 10, rect.height - 22);
            EditorGUI.LabelField(contentRect, message.Message.ToString(), _styles.MessageStyle);

            // Draw line at the bottom
            Rect lineRect = new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        /// <summary>
        /// Filters messages based on the current filter criteria.
        /// </summary>
        private void FilterMessages()
        {
            _filteredMessages.Clear();

            foreach (var message in _allMessages)
            {
                LogLevel level = (LogLevel)message.Level;

                // Skip if level is not visible
                if (!IsLevelVisible(level))
                    continue;

                // Skip if level is below selected minimum
                if (level < _selectedLogLevel)
                    continue;

                // Skip if tag is not included
                // Convert LogTag to FixedString32Bytes before passing to IsTagIncluded
                FixedString32Bytes tagString = new FixedString32Bytes(message.Tag.ToString());
                if (!IsTagIncluded(tagString))
                    continue;

                // Skip if doesn't match text filter
                if (!string.IsNullOrEmpty(_filterText) && !MessageMatchesFilter(message, _filterText))
                    continue;

                _filteredMessages.Add(message);
            }
        }

        /// <summary>
        /// Checks if a log level is visible according to the current profile.
        /// </summary>
        private bool IsLevelVisible(LogLevel level)
        {
            return _currentProfile != null && _currentProfile.IsLevelVisible(level);
        }

        /// <summary>
        /// Checks if a message matches the text filter.
        /// </summary>
        private bool MessageMatchesFilter(LogMessage message, string filterText)
        {
            if (string.IsNullOrEmpty(filterText))
                return true;

            // Check message content
            if (message.Message.ToString().IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Check tag
            if (message.Tag.ToString().IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Check properties
            if (PropertiesContainText(message.Properties, filterText))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if properties contain the filter text.
        /// </summary>
        private bool PropertiesContainText(LogProperties properties, string searchText)
        {
            if (!properties.IsCreated)
                return false;

            // Create a buffer for properties to search in
            var buffer = new System.Text.StringBuilder();

            // Iterate through properties and add them to the buffer
            foreach (var property in properties)
            {
                buffer.Append(property.Key);
                buffer.Append('=');
                buffer.Append(property.Value);
                buffer.Append(' ');
            }

            // Check if the buffer contains the search text
            return buffer.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Gets the appropriate GUI style for a log level.
        /// </summary>
        private GUIStyle GetStyleForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => _styles.DebugStyle,
                LogLevel.Info => _styles.InfoStyle,
                LogLevel.Warning => _styles.WarningStyle,
                LogLevel.Error => _styles.ErrorStyle,
                LogLevel.Critical => _styles.CriticalStyle,
                _ => EditorStyles.label
            };
        }

        /// <summary>
        /// Gets a display name for a log level.
        /// </summary>
        private string GetLogLevelName(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Info => "Info",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Checks if a tag is included in the current filter.
        /// </summary>
        private bool IsTagIncluded(FixedString32Bytes tag)
        {
            string tagString = tag.ToString();
            return _tagFilters.ContainsKey(tagString) && _tagFilters[tagString];
        }

        /// <summary>
        /// Sets whether a tag is included in the filter.
        /// </summary>
        private void SetTagIncluded(FixedString32Bytes tag, bool included)
        {
            string tagString = tag.ToString();
            _tagFilters[tagString] = included;
            FilterMessages();
        }

        /// <summary>
        /// Selects all tags to be included or excluded.
        /// </summary>
        private void SelectAllTags(bool included)
        {
            foreach (var tag in _tagFilters.Keys.ToArray())
            {
                _tagFilters[tag] = included;
            }

            FilterMessages();
        }

        /// <summary>
        /// Clears all messages and resets statistics.
        /// </summary>
        private void ClearMessages()
        {
            _allMessages.Clear();
            _filteredMessages.Clear();
            _totalMessageCount = 0;

            foreach (var level in _messageCountByLevel.Keys.ToArray())
            {
                _messageCountByLevel[level] = 0;
            }

            Repaint();
        }

        /// <summary>
        /// Disposes of any resources that need cleanup.
        /// </summary>
        private void DisposeResources()
        {
            UnsubscribeFromMessages();

            _profileManager?.Dispose();
            _profileManager = null;

            _currentProfile = null;
            _allMessages.Clear();
            _filteredMessages.Clear();
        }

        /// <summary>
        /// Disposes of all resources used by the window.
        /// </summary>
        public void Dispose()
        {
            DisposeResources();
            _initialized = false;
        }

        /// <summary>
        /// No-op implementation of IMessageBusService for scenarios where messaging is not needed.
        /// This prevents the log visualizer from failing when no message bus is configured.
        /// </summary>
        private class NullMessageBusService : IMessageBusService
        {
            public IMessagePublisher<TMessage> GetPublisher<TMessage>() => new NullPublisher<TMessage>();
            public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() => new NullSubscriber<TMessage>();

            public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>() =>
                new NullKeyedPublisher<TKey, TMessage>();

            public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>() =>
                new NullKeyedSubscriber<TKey, TMessage>();

            public void ClearCaches()
            {
            }

            public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
            {
            }

            public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage =>
                new NullDisposable();

            public IDisposable SubscribeToAllMessages(Action<IMessage> handler) => new NullDisposable();
            public IMessageRegistry GetMessageRegistry() => new NullMessageRegistry();

            private class NullPublisher<TMessage> : IMessagePublisher<TMessage>
            {
                public void Publish(TMessage message)
                {
                }

                public IDisposable PublishAsync(TMessage message) => new NullDisposable();
            }

            private class NullSubscriber<TMessage> : IMessageSubscriber<TMessage>
            {
                public IDisposable Subscribe(Action<TMessage> handler) => new NullDisposable();

                public IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter) =>
                    new NullDisposable();
            }

            private class NullKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>
            {
                public void Publish(TKey key, TMessage message)
                {
                }

                public IDisposable PublishAsync(TKey key, TMessage message) => new NullDisposable();
            }

            private class NullKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>
            {
                public IDisposable Subscribe(TKey key, Action<TMessage> handler) => new NullDisposable();
                public IDisposable Subscribe(Action<TKey, TMessage> handler) => new NullDisposable();

                public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter) =>
                    new NullDisposable();
            }

            private class NullMessageRegistry : IMessageRegistry
            {
                public void DiscoverMessages()
                {
                }

                public void RegisterMessageType(Type messageType)
                {
                }

                public void RegisterMessageType(Type messageType, ushort typeCode)
                {
                }

                public IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes() =>
                    new Dictionary<Type, IMessageInfo>();

                public IReadOnlyList<string> GetCategories() => new List<string>();
                public IReadOnlyList<Type> GetMessageTypesByCategory(string category) => new List<Type>();
                public IMessageInfo GetMessageInfo(Type messageType) => null;
                public IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage => null;
                public bool IsRegistered(Type messageType) => false;
                public bool IsRegistered<TMessage>() where TMessage : IMessage => false;
                public ushort GetTypeCode(Type messageType) => 0;
                public ushort GetTypeCode<TMessage>() where TMessage : IMessage => 0;
                public Type GetMessageType(ushort typeCode) => null;
                public IReadOnlyDictionary<ushort, Type> GetAllTypeCodes() => new Dictionary<ushort, Type>();

                public void Clear()
                {
                }
            }

            private class NullDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}