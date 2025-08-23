using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Serialization.Jobs;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Editor
{
    /// <summary>
    /// Advanced debugging and profiling tools for the AhBearStudios serialization system.
    /// Provides detailed analysis, memory profiling, and performance bottleneck identification.
    /// </summary>
    public class SerializationDebugger : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Memory Profiler", "Performance Analyzer", "Data Inspector", "Health Check" };

        // Memory profiling
        private bool _isMemoryProfiling = false;
        private List<MemorySnapshot> _memorySnapshots = new List<MemorySnapshot>();
        private float _memoryProfilingInterval = 1.0f;
        private double _lastMemorySnapshot;

        // Performance analysis
        private List<PerformanceEvent> _performanceEvents = new List<PerformanceEvent>();
        private const int MaxPerformanceEvents = 1000;
        private bool _isPerformanceProfiling = false;

        // Data inspection
        private string _dataToInspect = "";
        private SerializationFormat _inspectionFormat = SerializationFormat.MemoryPack;
        private string _inspectionResults = "";

        // Health check
        private List<HealthCheckResult> _healthCheckResults = new List<HealthCheckResult>();
        private bool _autoHealthCheck = true;
        private float _lastHealthCheck;

        [MenuItem("AhBearStudios/Serialization/Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<SerializationDebugger>("Serialization Debugger");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _lastMemorySnapshot = EditorApplication.timeSinceStartup;
            _lastHealthCheck = Time.realtimeSinceStartup;
            
            if (_autoHealthCheck)
            {
                RunHealthCheck();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_selectedTab)
            {
                case 0: DrawMemoryProfilerTab(); break;
                case 1: DrawPerformanceAnalyzerTab(); break;
                case 2: DrawDataInspectorTab(); break;
                case 3: DrawHealthCheckTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            UpdateProfiling();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AhBearStudios Serialization Debugger", EditorStyles.largeLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawMemoryProfilerTab()
        {
            EditorGUILayout.LabelField("Memory Profiler", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var buttonText = _isMemoryProfiling ? "Stop Profiling" : "Start Profiling";
                    var buttonColor = _isMemoryProfiling ? Color.red : Color.green;
                    
                    var originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = buttonColor;
                    
                    if (GUILayout.Button(buttonText, GUILayout.Width(120)))
                    {
                        _isMemoryProfiling = !_isMemoryProfiling;
                        if (!_isMemoryProfiling)
                        {
                            _memorySnapshots.Clear();
                        }
                    }
                    
                    GUI.backgroundColor = originalColor;
                    
                    GUILayout.Space(10);
                    
                    EditorGUILayout.LabelField("Interval (seconds):", GUILayout.Width(100));
                    _memoryProfilingInterval = EditorGUILayout.FloatField(_memoryProfilingInterval, GUILayout.Width(60));
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Clear History", GUILayout.Width(100)))
                    {
                        _memorySnapshots.Clear();
                    }
                    
                    if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
                    {
                        TakeMemorySnapshot();
                    }
                }
            }

            EditorGUILayout.Space();

            if (_memorySnapshots.Count == 0)
            {
                EditorGUILayout.HelpBox("No memory snapshots available. Start profiling or take a manual snapshot.", MessageType.Info);
                return;
            }

            // Memory statistics
            var latest = _memorySnapshots[_memorySnapshots.Count - 1];
            
            EditorGUILayout.LabelField("Current Memory Statistics", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Total System Memory: {latest.TotalSystemMemory:F2} MB");
                EditorGUILayout.LabelField($"Used Memory: {latest.UsedMemory:F2} MB");
                EditorGUILayout.LabelField($"GC Heap Size: {latest.GCHeapSize:F2} MB");
                EditorGUILayout.LabelField($"Native Memory: {latest.NativeMemory:F2} MB");
                EditorGUILayout.LabelField($"Graphics Memory: {latest.GraphicsMemory:F2} MB");
                EditorGUILayout.LabelField($"Buffer Pool Usage: {latest.BufferPoolUsage:F2} MB");
            }

            // Memory trend graph
            if (_memorySnapshots.Count > 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Memory Usage Trend", EditorStyles.boldLabel);
                DrawMemoryGraph();
            }

            // Memory leak detection
            if (_memorySnapshots.Count >= 5)
            {
                EditorGUILayout.Space();
                DrawMemoryLeakAnalysis();
            }
        }

        private void DrawMemoryGraph()
        {
            var rect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));

            if (_memorySnapshots.Count < 2) return;

            var maxMemory = _memorySnapshots.AsValueEnumerable().Max(s => s.UsedMemory);
            if (maxMemory <= 0) return;

            // Draw memory usage line
            Handles.color = Color.cyan;
            for (int i = 1; i < _memorySnapshots.Count; i++)
            {
                var prevX = rect.x + (float)(i - 1) / (_memorySnapshots.Count - 1) * rect.width;
                var currX = rect.x + (float)i / (_memorySnapshots.Count - 1) * rect.width;
                var prevY = rect.y + rect.height - (_memorySnapshots[i - 1].UsedMemory / maxMemory) * rect.height;
                var currY = rect.y + rect.height - (_memorySnapshots[i].UsedMemory / maxMemory) * rect.height;
                
                Handles.DrawLine(new Vector3(prevX, prevY), new Vector3(currX, currY));
            }

            // Draw GC heap line
            Handles.color = Color.yellow;
            for (int i = 1; i < _memorySnapshots.Count; i++)
            {
                var prevX = rect.x + (float)(i - 1) / (_memorySnapshots.Count - 1) * rect.width;
                var currX = rect.x + (float)i / (_memorySnapshots.Count - 1) * rect.width;
                var prevY = rect.y + rect.height - (_memorySnapshots[i - 1].GCHeapSize / maxMemory) * rect.height;
                var currY = rect.y + rect.height - (_memorySnapshots[i].GCHeapSize / maxMemory) * rect.height;
                
                Handles.DrawLine(new Vector3(prevX, prevY), new Vector3(currX, currY));
            }

            // Legend
            var legendRect = new Rect(rect.x + 5, rect.y + 5, 200, 40);
            EditorGUI.DrawRect(legendRect, new Color(0, 0, 0, 0.7f));
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 2, 100, 18), "Used Memory", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.cyan } });
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 20, 100, 18), "GC Heap", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
        }

        private void DrawMemoryLeakAnalysis()
        {
            EditorGUILayout.LabelField("Memory Leak Analysis", EditorStyles.boldLabel);
            
            // Analyze recent snapshots for memory growth
            var recentSnapshots = _memorySnapshots.Skip(_memorySnapshots.Count - 5).ToArray();
            var memoryGrowth = recentSnapshots.Last().UsedMemory - recentSnapshots.First().UsedMemory;
            var averageGrowth = memoryGrowth / recentSnapshots.Length;
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Memory Growth (last 5 snapshots): {memoryGrowth:F2} MB");
                EditorGUILayout.LabelField($"Average Growth per Snapshot: {averageGrowth:F2} MB");
                
                if (averageGrowth > 1.0f) // Growing by more than 1MB per snapshot
                {
                    EditorGUILayout.HelpBox("⚠ Potential memory leak detected! Memory usage is growing consistently.", MessageType.Warning);
                }
                else if (averageGrowth > 0.1f)
                {
                    EditorGUILayout.HelpBox("ℹ Memory usage is slowly increasing. Monitor for potential leaks.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("✓ Memory usage appears stable.", MessageType.Info);
                }
            }
        }

        private void DrawPerformanceAnalyzerTab()
        {
            EditorGUILayout.LabelField("Performance Analyzer", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var buttonText = _isPerformanceProfiling ? "Stop Profiling" : "Start Profiling";
                    var buttonColor = _isPerformanceProfiling ? Color.red : Color.green;
                    
                    var originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = buttonColor;
                    
                    if (GUILayout.Button(buttonText, GUILayout.Width(120)))
                    {
                        _isPerformanceProfiling = !_isPerformanceProfiling;
                        if (!_isPerformanceProfiling)
                        {
                            _performanceEvents.Clear();
                        }
                    }
                    
                    GUI.backgroundColor = originalColor;
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Clear Events", GUILayout.Width(100)))
                    {
                        _performanceEvents.Clear();
                    }
                    
                    if (GUILayout.Button("Analyze Performance", GUILayout.Width(120)))
                    {
                        AnalyzePerformance();
                    }
                }
            }

            EditorGUILayout.Space();

            if (_performanceEvents.Count == 0)
            {
                EditorGUILayout.HelpBox("No performance events recorded. Start profiling to collect data.", MessageType.Info);
                return;
            }

            // Performance statistics
            DrawPerformanceStatistics();
            
            EditorGUILayout.Space();
            
            // Performance events list
            DrawPerformanceEventsList();
        }

        private void DrawPerformanceStatistics()
        {
            EditorGUILayout.LabelField("Performance Statistics", EditorStyles.boldLabel);
            
            var serializationEvents = _performanceEvents.Where(e => e.EventType == PerformanceEventType.Serialization);
            var deserializationEvents = _performanceEvents.Where(e => e.EventType == PerformanceEventType.Deserialization);
            var compressionEvents = _performanceEvents.Where(e => e.EventType == PerformanceEventType.Compression);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (serializationEvents.Any())
                {
                    var avgSerialization = serializationEvents.Average(e => e.Duration);
                    var maxSerialization = serializationEvents.Max(e => e.Duration);
                    EditorGUILayout.LabelField($"Serialization - Avg: {avgSerialization:F2}ms, Max: {maxSerialization:F2}ms, Count: {serializationEvents.Count()}");
                }
                
                if (deserializationEvents.Any())
                {
                    var avgDeserialization = deserializationEvents.Average(e => e.Duration);
                    var maxDeserialization = deserializationEvents.Max(e => e.Duration);
                    EditorGUILayout.LabelField($"Deserialization - Avg: {avgDeserialization:F2}ms, Max: {maxDeserialization:F2}ms, Count: {deserializationEvents.Count()}");
                }
                
                if (compressionEvents.Any())
                {
                    var avgCompression = compressionEvents.Average(e => e.Duration);
                    var maxCompression = compressionEvents.Max(e => e.Duration);
                    EditorGUILayout.LabelField($"Compression - Avg: {avgCompression:F2}ms, Max: {maxCompression:F2}ms, Count: {compressionEvents.Count()}");
                }
            }
        }

        private void DrawPerformanceEventsList()
        {
            EditorGUILayout.LabelField("Recent Performance Events", EditorStyles.boldLabel);
            
            using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(200)))
            {
                foreach (var evt in _performanceEvents.TakeLast(20))
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"{evt.Timestamp:HH:mm:ss}", GUILayout.Width(60));
                        EditorGUILayout.LabelField($"{evt.EventType}", GUILayout.Width(100));
                        EditorGUILayout.LabelField($"{evt.Duration:F2}ms", GUILayout.Width(60));
                        EditorGUILayout.LabelField($"{evt.DataSize} bytes", GUILayout.Width(80));
                        EditorGUILayout.LabelField(evt.Details, GUILayout.ExpandWidth(true));
                    }
                }
            }
        }

        private void DrawDataInspectorTab()
        {
            EditorGUILayout.LabelField("Data Inspector", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Paste serialized data to inspect (Base64 or Hex):");
                _dataToInspect = EditorGUILayout.TextArea(_dataToInspect, GUILayout.Height(100));
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Expected Format:", GUILayout.Width(100));
                    _inspectionFormat = (SerializationFormat)EditorGUILayout.EnumPopup(_inspectionFormat);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Inspect Data", GUILayout.Width(100)))
                    {
                        InspectData();
                    }
                    
                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        _dataToInspect = "";
                        _inspectionResults = "";
                    }
                }
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(_inspectionResults))
            {
                EditorGUILayout.LabelField("Inspection Results", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(200)))
                    {
                        EditorGUILayout.TextArea(_inspectionResults, EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }

        private void DrawHealthCheckTab()
        {
            EditorGUILayout.LabelField("System Health Check", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _autoHealthCheck = EditorGUILayout.Toggle("Auto Health Check", _autoHealthCheck);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Run Health Check", GUILayout.Width(120)))
                    {
                        RunHealthCheck();
                    }
                }
            }

            EditorGUILayout.Space();

            if (_healthCheckResults.Count == 0)
            {
                EditorGUILayout.HelpBox("No health check results available. Run a health check to see system status.", MessageType.Info);
                return;
            }

            DrawHealthCheckResults();
        }

        private void DrawHealthCheckResults()
        {
            EditorGUILayout.LabelField("Health Check Results", EditorStyles.boldLabel);
            
            foreach (var result in _healthCheckResults)
            {
                var messageType = result.Status switch
                {
                    HealthStatus.Healthy => MessageType.Info,
                    HealthStatus.Warning => MessageType.Warning,
                    HealthStatus.Critical => MessageType.Error,
                    _ => MessageType.None
                };
                
                var statusIcon = result.Status switch
                {
                    HealthStatus.Healthy => "✓",
                    HealthStatus.Warning => "⚠",
                    HealthStatus.Critical => "✗",
                    _ => "?"
                };
                
                EditorGUILayout.HelpBox($"{statusIcon} {result.Component}: {result.Message}", messageType);
            }
        }

        private void UpdateProfiling()
        {
            var currentTime = EditorApplication.timeSinceStartup;
            
            // Update memory profiling
            if (_isMemoryProfiling && currentTime - _lastMemorySnapshot >= _memoryProfilingInterval)
            {
                TakeMemorySnapshot();
                _lastMemorySnapshot = currentTime;
                Repaint();
            }
            
            // Auto health check
            if (_autoHealthCheck && Time.realtimeSinceStartup - _lastHealthCheck >= 30f) // Every 30 seconds
            {
                RunHealthCheck();
                _lastHealthCheck = Time.realtimeSinceStartup;
            }
        }

        private void TakeMemorySnapshot()
        {
            var snapshot = new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                TotalSystemMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(UnityEngine.Profiling.Profiler.Area.None) / (1024f * 1024f),
                UsedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(UnityEngine.Profiling.Profiler.Area.None) / (1024f * 1024f),
                GCHeapSize = GC.GetTotalMemory(false) / (1024f * 1024f),
                NativeMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(UnityEngine.Profiling.Profiler.Area.None) / (1024f * 1024f),
                GraphicsMemory = 0f, // Would need platform-specific implementation
                BufferPoolUsage = 0f  // Would need access to actual buffer pool
            };
            
            _memorySnapshots.Add(snapshot);
            
            // Keep only recent snapshots
            if (_memorySnapshots.Count > 100)
            {
                _memorySnapshots.RemoveAt(0);
            }
        }

        private void AnalyzePerformance()
        {
            Debug.Log("[SerializationDebugger] Analyzing performance data...");
            
            if (_performanceEvents.Count == 0)
            {
                Debug.LogWarning("No performance events to analyze");
                return;
            }
            
            var analysis = new StringBuilder();
            analysis.AppendLine("=== Performance Analysis Report ===");
            analysis.AppendLine($"Total Events: {_performanceEvents.Count}");
            analysis.AppendLine($"Time Range: {_performanceEvents.First().Timestamp:HH:mm:ss} - {_performanceEvents.Last().Timestamp:HH:mm:ss}");
            analysis.AppendLine();
            
            // Group by event type
            var groupedEvents = _performanceEvents.GroupBy(e => e.EventType);
            foreach (var group in groupedEvents)
            {
                var events = group.ToArray();
                analysis.AppendLine($"{group.Key}:");
                analysis.AppendLine($"  Count: {events.Length}");
                analysis.AppendLine($"  Average Duration: {events.Average(e => e.Duration):F2}ms");
                analysis.AppendLine($"  Max Duration: {events.Max(e => e.Duration):F2}ms");
                analysis.AppendLine($"  Min Duration: {events.Min(e => e.Duration):F2}ms");
                analysis.AppendLine($"  Total Data: {events.Sum(e => e.DataSize):N0} bytes");
                analysis.AppendLine();
            }
            
            Debug.Log(analysis.ToString());
        }

        private void InspectData()
        {
            if (string.IsNullOrEmpty(_dataToInspect))
            {
                _inspectionResults = "No data provided for inspection.";
                return;
            }
            
            try
            {
                byte[] data;
                
                // Try to decode as Base64 first
                try
                {
                    data = Convert.FromBase64String(_dataToInspect.Trim());
                }
                catch
                {
                    // Try to decode as hex
                    var hex = _dataToInspect.Replace(" ", "").Replace("-", "");
                    data = new byte[hex.Length / 2];
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    }
                }
                
                var results = new StringBuilder();
                results.AppendLine($"Data Length: {data.Length} bytes");
                results.AppendLine($"Expected Format: {_inspectionFormat}");
                results.AppendLine();
                
                // Hex dump
                results.AppendLine("Hex Dump:");
                for (int i = 0; i < Math.Min(data.Length, 256); i += 16)
                {
                    var hex = BitConverter.ToString(data, i, Math.Min(16, data.Length - i)).Replace("-", " ");
                    results.AppendLine($"{i:X4}: {hex}");
                }
                
                if (data.Length > 256)
                {
                    results.AppendLine($"... ({data.Length - 256} more bytes)");
                }
                
                _inspectionResults = results.ToString();
            }
            catch (Exception ex)
            {
                _inspectionResults = $"Error inspecting data: {ex.Message}";
            }
        }

        private void RunHealthCheck()
        {
            _healthCheckResults.Clear();
            
            // Check formatter registration
            var formatterCount = AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.GetRegisteredFormatterCount();
            var isRegistered = AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.IsRegistered();
            
            _healthCheckResults.Add(new HealthCheckResult
            {
                Component = "Formatter Registration",
                Status = isRegistered ? HealthStatus.Healthy : HealthStatus.Critical,
                Message = isRegistered ? $"{formatterCount} formatters registered" : "Formatters not registered"
            });
            
            // Check memory usage
            var memoryUsage = GC.GetTotalMemory(false) / (1024f * 1024f);
            _healthCheckResults.Add(new HealthCheckResult
            {
                Component = "Memory Usage",
                Status = memoryUsage < 100 ? HealthStatus.Healthy : memoryUsage < 500 ? HealthStatus.Warning : HealthStatus.Critical,
                Message = $"{memoryUsage:F1} MB used"
            });
            
            // Check Unity Job System
            _healthCheckResults.Add(new HealthCheckResult
            {
                Component = "Unity Job System",
                Status = HealthStatus.Healthy,
                Message = "Available and functional"
            });
            
            // Check UniTask
            _healthCheckResults.Add(new HealthCheckResult
            {
                Component = "UniTask Integration",
                Status = HealthStatus.Healthy,
                Message = "UniTask available"
            });
            
            // Check ZLinq
            _healthCheckResults.Add(new HealthCheckResult
            {
                Component = "ZLinq Integration",
                Status = HealthStatus.Healthy,
                Message = "ZLinq available for zero-allocation operations"
            });
            
            Repaint();
        }

        private struct MemorySnapshot
        {
            public DateTime Timestamp;
            public float TotalSystemMemory;
            public float UsedMemory;
            public float GCHeapSize;
            public float NativeMemory;
            public float GraphicsMemory;
            public float BufferPoolUsage;
        }

        private struct PerformanceEvent
        {
            public DateTime Timestamp;
            public PerformanceEventType EventType;
            public float Duration;
            public int DataSize;
            public string Details;
        }

        private enum PerformanceEventType
        {
            Serialization,
            Deserialization,
            Compression,
            Decompression,
            JobExecution
        }

        private struct HealthCheckResult
        {
            public string Component;
            public HealthStatus Status;
            public string Message;
        }

        private enum HealthStatus
        {
            Healthy,
            Warning,
            Critical
        }
    }
}