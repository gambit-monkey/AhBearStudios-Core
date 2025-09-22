using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.Editor
{
    /// <summary>
    /// Unity Editor Window for runtime log viewing, filtering, and monitoring.
    /// Provides comprehensive logging system management and debugging capabilities.
    /// </summary>
    public class LoggingEditorWindow : EditorWindow
    {
        private const int MAX_LOG_ENTRIES = 1000;
        private const float REFRESH_INTERVAL = 0.1f; // 100ms
        private const string WINDOW_TITLE = "AhBearStudios Logging System";

        // Window state
        private Vector2 _scrollPosition;
        private Vector2 _settingsScrollPosition;
        private double _lastRefreshTime;
        private bool _autoRefresh = true;
        private bool _autoScroll = true;
        private bool _showSettings = false;
        private bool _showStatistics = false;
        private bool _showHealthStatus = false;

        // Filtering options
        private LogLevel _filterLevel = LogLevel.Debug;
        private string _filterChannel = "";
        private string _filterMessage = "";
        private string _filterCorrelationId = "";
        private bool _showOnlyErrors = false;
        private bool _showOnlyWarnings = false;

        // Display options
        private bool _showTimestamp = true;
        private bool _showLevel = true;
        private bool _showChannel = true;
        private bool _showCorrelationId = false;
        private bool _showSourceContext = false;
        private bool _showThreadId = false;
        private bool _colorCodeLevels = true;
        private int _maxDisplayedLogs = 100;

        // Services
        private ILoggingService _loggingService;
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private List<LogEntry> _filteredLogEntries = new List<LogEntry>();

        // GUI Styles
        private GUIStyle _logEntryStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _debugStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _toolbarStyle;
        private bool _stylesInitialized = false;

        // Statistics
        private int _totalLogs = 0;
        private int _errorCount = 0;
        private int _warningCount = 0;
        private int _infoCount = 0;
        private int _debugCount = 0;

        [MenuItem("AhBearStudios/Core/Logging/Logging Monitor")]
        public static void OpenWindow()
        {
            var window = GetWindow<LoggingEditorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeServices();
            InitializeStyles();
            CollectLogEntries();
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            // Clean up subscriptions if any
        }

        private void OnGUI()
        {
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            DrawToolbar();
            
            if (_showSettings)
            {
                DrawSettings();
            }
            else if (_showStatistics)
            {
                DrawStatistics();
            }
            else if (_showHealthStatus)
            {
                DrawHealthStatus();
            }
            else
            {
                DrawLogEntries();
            }

            // Auto-refresh
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshLogEntries();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void InitializeServices()
        {
            // In a real implementation, this would get the service from the DI container
            // For now, we'll simulate it
            _loggingService = FindLoggingService();
        }

        private ILoggingService FindLoggingService()
        {
            // Try to find the logging service in the scene
            var loggingBehaviour = FindObjectOfType<UnityLoggingBehaviour>();
            if (loggingBehaviour != null)
            {
                // In a real implementation, this would access the injected service
                return null; // Placeholder
            }
            return null;
        }

        private void InitializeStyles()
        {
            _logEntryStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true
            };

            _errorStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = Color.red }
            };

            _warningStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = Color.yellow }
            };

            _infoStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = Color.white }
            };

            _debugStyle = new GUIStyle(_logEntryStyle)
            {
                normal = { textColor = Color.gray }
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            _toolbarStyle = new GUIStyle(EditorStyles.toolbar);

            _stylesInitialized = true;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(_toolbarStyle);

            // Refresh controls
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshLogEntries();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(40));
            _autoScroll = GUILayout.Toggle(_autoScroll, "Scroll", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.Space(10);

            // Clear button
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ClearLogEntries();
            }

            GUILayout.Space(10);

            // Filter controls
            GUILayout.Label("Filter:", EditorStyles.miniLabel, GUILayout.Width(40));
            _filterLevel = (LogLevel)EditorGUILayout.EnumPopup(_filterLevel, EditorStyles.toolbarPopup, GUILayout.Width(80));
            
            _filterChannel = EditorGUILayout.TextField(_filterChannel, EditorStyles.toolbarTextField, GUILayout.Width(100));
            if (string.IsNullOrEmpty(_filterChannel))
            {
                GUI.Label(GUILayoutUtility.GetLastRect(), "Channel...", EditorStyles.centeredGreyMiniLabel);
            }

            _filterMessage = EditorGUILayout.TextField(_filterMessage, EditorStyles.toolbarTextField, GUILayout.Width(150));
            if (string.IsNullOrEmpty(_filterMessage))
            {
                GUI.Label(GUILayoutUtility.GetLastRect(), "Message...", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.FlexibleSpace();

            // View toggle buttons
            _showSettings = GUILayout.Toggle(_showSettings, "Settings", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showStatistics = GUILayout.Toggle(_showStatistics, "Stats", EditorStyles.toolbarButton, GUILayout.Width(50));
            _showHealthStatus = GUILayout.Toggle(_showHealthStatus, "Health", EditorStyles.toolbarButton, GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            _settingsScrollPosition = EditorGUILayout.BeginScrollView(_settingsScrollPosition);

            EditorGUILayout.LabelField("Display Settings", _headerStyle);
            EditorGUILayout.BeginVertical("box");

            _showTimestamp = EditorGUILayout.Toggle("Show Timestamp", _showTimestamp);
            _showLevel = EditorGUILayout.Toggle("Show Level", _showLevel);
            _showChannel = EditorGUILayout.Toggle("Show Channel", _showChannel);
            _showCorrelationId = EditorGUILayout.Toggle("Show Correlation ID", _showCorrelationId);
            _showSourceContext = EditorGUILayout.Toggle("Show Source Context", _showSourceContext);
            _showThreadId = EditorGUILayout.Toggle("Show Thread ID", _showThreadId);
            _colorCodeLevels = EditorGUILayout.Toggle("Color Code Levels", _colorCodeLevels);

            EditorGUILayout.Space();
            _maxDisplayedLogs = EditorGUILayout.IntSlider("Max Displayed Logs", _maxDisplayedLogs, 10, 1000);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Filter Settings", _headerStyle);
            EditorGUILayout.BeginVertical("box");

            _showOnlyErrors = EditorGUILayout.Toggle("Show Only Errors", _showOnlyErrors);
            _showOnlyWarnings = EditorGUILayout.Toggle("Show Only Warnings", _showOnlyWarnings);

            EditorGUILayout.Space();
            _filterCorrelationId = EditorGUILayout.TextField("Filter by Correlation ID", _filterCorrelationId);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Performance Settings", _headerStyle);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox("Refresh Interval: " + REFRESH_INTERVAL + "s", MessageType.Info);
            EditorGUILayout.HelpBox("Max Log Entries: " + MAX_LOG_ENTRIES, MessageType.Info);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatistics()
        {
            EditorGUILayout.LabelField("Logging Statistics", _headerStyle);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Total Logs: {_totalLogs}");
            EditorGUILayout.LabelField($"Errors: {_errorCount}");
            EditorGUILayout.LabelField($"Warnings: {_warningCount}");
            EditorGUILayout.LabelField($"Info: {_infoCount}");
            EditorGUILayout.LabelField($"Debug: {_debugCount}");

            EditorGUILayout.Space();

            if (_loggingService != null)
            {
                var stats = _loggingService.GetStatistics();
                EditorGUILayout.LabelField($"Messages Processed: {stats.MessagesProcessed}");
                EditorGUILayout.LabelField($"Errors Encountered: {stats.ErrorCount}");
                EditorGUILayout.LabelField($"Active Targets: {stats.ActiveTargets}");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Performance Metrics", _headerStyle);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Memory Usage: " + (GC.GetTotalMemory(false) / 1024f / 1024f).ToString("F2") + " MB");
            EditorGUILayout.LabelField("Refresh Rate: " + (1f / REFRESH_INTERVAL).ToString("F1") + " Hz");

            EditorGUILayout.EndVertical();
        }

        private void DrawHealthStatus()
        {
            EditorGUILayout.LabelField("Health Status", _headerStyle);
            EditorGUILayout.BeginVertical("box");

            if (_loggingService != null)
            {
                var isHealthy = _loggingService.PerformHealthCheck();
                var healthColor = isHealthy ? Color.green : Color.red;
                var healthText = isHealthy ? "Healthy" : "Unhealthy";

                var originalColor = GUI.color;
                GUI.color = healthColor;
                EditorGUILayout.LabelField($"Service Status: {healthText}");
                GUI.color = originalColor;

                EditorGUILayout.Space();

                var targets = _loggingService.GetTargets();
                EditorGUILayout.LabelField($"Registered Targets: {targets.Count}");

                foreach (var target in targets)
                {
                    var targetHealthy = target.PerformHealthCheck();
                    var targetColor = targetHealthy ? Color.green : Color.red;
                    var targetText = targetHealthy ? "✓" : "✗";

                    GUI.color = targetColor;
                    EditorGUILayout.LabelField($"  {targetText} {target.Name}");
                    GUI.color = originalColor;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Logging service not available", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLogEntries()
        {
            // Status bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Showing {_filteredLogEntries.Count} of {_logEntries.Count} entries", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Last Updated: {DateTime.Now:HH:mm:ss}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Log entries
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var displayedEntries = _filteredLogEntries.Take(_maxDisplayedLogs).ToList();
            foreach (var entry in displayedEntries)
            {
                DrawLogEntry(entry);
            }

            // Auto-scroll to bottom if enabled
            if (_autoScroll && Event.current.type == EventType.Repaint)
            {
                _scrollPosition.y = float.MaxValue;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLogEntry(LogEntry entry)
        {
            var style = GetStyleForLevel(entry.Level);
            var entryText = FormatLogEntry(entry);

            EditorGUILayout.BeginHorizontal();
            
            // Level icon
            var levelIcon = GetIconForLevel(entry.Level);
            if (levelIcon != null)
            {
                GUILayout.Label(levelIcon, GUILayout.Width(16), GUILayout.Height(16));
            }

            // Entry text
            EditorGUILayout.LabelField(entryText, style);

            EditorGUILayout.EndHorizontal();

            // Separator
            if (entry.Level == LogLevel.Error || entry.Level == LogLevel.Critical)
            {
                EditorGUILayout.Space();
            }
        }

        private GUIStyle GetStyleForLevel(LogLevel level)
        {
            if (!_colorCodeLevels) return _logEntryStyle;

            return level switch
            {
                LogLevel.Error or LogLevel.Critical => _errorStyle,
                LogLevel.Warning => _warningStyle,
                LogLevel.Info => _infoStyle,
                LogLevel.Debug => _debugStyle,
                _ => _logEntryStyle
            };
        }

        private Texture2D GetIconForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error or LogLevel.Critical => EditorGUIUtility.IconContent("console.erroricon").image as Texture2D,
                LogLevel.Warning => EditorGUIUtility.IconContent("console.warnicon").image as Texture2D,
                LogLevel.Info => EditorGUIUtility.IconContent("console.infoicon").image as Texture2D,
                _ => null
            };
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var parts = new List<string>();

            if (_showTimestamp)
                parts.Add($"[{entry.Timestamp:HH:mm:ss.fff}]");

            if (_showLevel)
                parts.Add($"[{entry.Level}]");

            if (_showChannel && !string.IsNullOrEmpty(entry.Channel))
                parts.Add($"[{entry.Channel}]");

            if (_showCorrelationId && !string.IsNullOrEmpty(entry.CorrelationId))
                parts.Add($"[{entry.CorrelationId}]");

            if (_showSourceContext && !string.IsNullOrEmpty(entry.SourceContext))
                parts.Add($"[{entry.SourceContext}]");

            if (_showThreadId)
                parts.Add($"[T:{entry.ThreadId}]");

            parts.Add(entry.Message);

            return string.Join(" ", parts);
        }

        private void RefreshLogEntries()
        {
            CollectLogEntries();
            ApplyFilters();
            UpdateStatistics();
            Repaint();
        }

        private void CollectLogEntries()
        {
            // In a real implementation, this would collect from the actual logging targets
            // For now, we'll simulate some log entries
            if (_logEntries.Count == 0)
            {
                GenerateSimulatedLogEntries();
            }
        }

        private void GenerateSimulatedLogEntries()
        {
            var random = new System.Random();
            var channels = new[] { "System", "Gameplay", "UI", "Network", "Audio" };
            var messages = new[]
            {
                "System initialized successfully",
                "Player connected to server",
                "Audio system started",
                "Failed to load texture",
                "Network connection timeout",
                "Game state updated",
                "Performance warning: Frame drop detected"
            };

            for (int i = 0; i < 50; i++)
            {
                var level = (LogLevel)random.Next(0, 5);
                var channel = channels[random.Next(channels.Length)];
                var message = messages[random.Next(messages.Length)];

                _logEntries.Add(new LogEntry
                {
                    Timestamp = DateTime.Now.AddSeconds(-random.Next(0, 3600)),
                    Level = level,
                    Channel = channel,
                    Message = message,
                    CorrelationId = Guid.NewGuid().ToString("N")[..8],
                    SourceContext = "TestSource",
                    ThreadId = random.Next(1, 10)
                });
            }
        }

        private void ApplyFilters()
        {
            _filteredLogEntries = _logEntries.Where(entry =>
            {
                // Level filter
                if (entry.Level < _filterLevel) return false;

                // Channel filter
                if (!string.IsNullOrEmpty(_filterChannel) && 
                    !entry.Channel.Contains(_filterChannel, StringComparison.OrdinalIgnoreCase)) return false;

                // Message filter
                if (!string.IsNullOrEmpty(_filterMessage) && 
                    !entry.Message.Contains(_filterMessage, StringComparison.OrdinalIgnoreCase)) return false;

                // Correlation ID filter
                if (!string.IsNullOrEmpty(_filterCorrelationId) && 
                    !entry.CorrelationId.Contains(_filterCorrelationId, StringComparison.OrdinalIgnoreCase)) return false;

                // Show only errors
                if (_showOnlyErrors && entry.Level != LogLevel.Error && entry.Level != LogLevel.Critical) return false;

                // Show only warnings
                if (_showOnlyWarnings && entry.Level != LogLevel.Warning) return false;

                return true;
            }).ToList();
        }

        private void UpdateStatistics()
        {
            _totalLogs = _logEntries.Count;
            _errorCount = _logEntries.Count(e => e.Level == LogLevel.Error || e.Level == LogLevel.Critical);
            _warningCount = _logEntries.Count(e => e.Level == LogLevel.Warning);
            _infoCount = _logEntries.Count(e => e.Level == LogLevel.Info);
            _debugCount = _logEntries.Count(e => e.Level == LogLevel.Debug);
        }

        private void ClearLogEntries()
        {
            _logEntries.Clear();
            _filteredLogEntries.Clear();
            UpdateStatistics();
            Repaint();
        }
    }

    /// <summary>
    /// Simplified log entry model for the editor window.
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }
        public string SourceContext { get; set; }
        public int ThreadId { get; set; }
        public Exception Exception { get; set; }
    }
}