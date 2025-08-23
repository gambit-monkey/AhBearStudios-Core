using System.Collections.Generic;
using UnityEngine;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Targets
{
    /// <summary>
    /// ScriptableObject configuration for Memory log target.
    /// Provides Unity-serializable configuration for in-memory logging.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Targets/Memory Target", 
        fileName = "MemoryTargetConfig", 
        order = 3)]
    public class MemoryTargetConfig : LogTargetScriptableObject
    {
        [Header("Memory Settings")]
        [SerializeField] private int _maxEntries = 1000;
        [SerializeField] private bool _useCircularBuffer = true;
        [SerializeField] private bool _autoTrim = true;
        [SerializeField] private int _trimThreshold = 1500;
        [SerializeField] private int _trimTarget = 1000;

        [Header("Memory Management")]
        [SerializeField] private bool _enableMemoryPressureMonitoring = true;
        [SerializeField] private float _memoryPressureThresholdMB = 50.0f;
        [SerializeField] private bool _clearOnMemoryPressure = true;
        [SerializeField] private int _emergencyTrimSize = 100;

        [Header("Search and Filtering")]
        [SerializeField] private bool _enableSearching = true;
        [SerializeField] private bool _enableFiltering = true;
        [SerializeField] private bool _maintainIndex = true;
        [SerializeField] private bool _enableRegexSearch = false;

        [Header("Export Settings")]
        [SerializeField] private bool _enableExport = true;
        [SerializeField] private string _exportFormat = "JSON";
        [SerializeField] private bool _includeTimestamps = true;
        [SerializeField] private bool _includeMetadata = true;

        [Header("Unity Inspector Integration")]
        [SerializeField] private bool _showInInspector = true;
        [SerializeField] private int _maxInspectorEntries = 100;
        [SerializeField] private bool _autoRefreshInspector = true;
        [SerializeField] private float _inspectorRefreshInterval = 1.0f;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableCompression = false;
        [SerializeField] private bool _enableStringPooling = true;
        [SerializeField] private int _stringPoolSize = 1000;
        [SerializeField] private bool _enableStatistics = true;

        /// <summary>
        /// Gets the maximum number of entries to keep in memory.
        /// </summary>
        public int MaxEntries => _maxEntries;

        /// <summary>
        /// Gets whether to use circular buffer behavior.
        /// </summary>
        public bool UseCircularBuffer => _useCircularBuffer;

        /// <summary>
        /// Gets whether auto-trimming is enabled.
        /// </summary>
        public bool AutoTrim => _autoTrim;

        /// <summary>
        /// Gets the threshold for triggering auto-trim.
        /// </summary>
        public int TrimThreshold => _trimThreshold;

        /// <summary>
        /// Gets the target size after trimming.
        /// </summary>
        public int TrimTarget => _trimTarget;

        /// <summary>
        /// Gets whether memory pressure monitoring is enabled.
        /// </summary>
        public bool EnableMemoryPressureMonitoring => _enableMemoryPressureMonitoring;

        /// <summary>
        /// Gets the memory pressure threshold in MB.
        /// </summary>
        public float MemoryPressureThresholdMB => _memoryPressureThresholdMB;

        /// <summary>
        /// Gets whether to clear on memory pressure.
        /// </summary>
        public bool ClearOnMemoryPressure => _clearOnMemoryPressure;

        /// <summary>
        /// Gets the emergency trim size.
        /// </summary>
        public int EmergencyTrimSize => _emergencyTrimSize;

        /// <summary>
        /// Gets whether searching is enabled.
        /// </summary>
        public bool EnableSearching => _enableSearching;

        /// <summary>
        /// Gets whether filtering is enabled.
        /// </summary>
        public bool EnableFiltering => _enableFiltering;

        /// <summary>
        /// Gets whether to maintain search index.
        /// </summary>
        public bool MaintainIndex => _maintainIndex;

        /// <summary>
        /// Gets whether regex search is enabled.
        /// </summary>
        public bool EnableRegexSearch => _enableRegexSearch;

        /// <summary>
        /// Gets whether export functionality is enabled.
        /// </summary>
        public bool EnableExport => _enableExport;

        /// <summary>
        /// Gets the export format.
        /// </summary>
        public string ExportFormat => _exportFormat;

        /// <summary>
        /// Gets whether to include timestamps in export.
        /// </summary>
        public bool IncludeTimestamps => _includeTimestamps;

        /// <summary>
        /// Gets whether to include metadata in export.
        /// </summary>
        public bool IncludeMetadata => _includeMetadata;

        /// <summary>
        /// Gets whether to show logs in Unity Inspector.
        /// </summary>
        public bool ShowInInspector => _showInInspector;

        /// <summary>
        /// Gets the maximum number of entries to show in inspector.
        /// </summary>
        public int MaxInspectorEntries => _maxInspectorEntries;

        /// <summary>
        /// Gets whether to auto-refresh the inspector.
        /// </summary>
        public bool AutoRefreshInspector => _autoRefreshInspector;

        /// <summary>
        /// Gets the inspector refresh interval.
        /// </summary>
        public float InspectorRefreshInterval => _inspectorRefreshInterval;

        /// <summary>
        /// Gets whether compression is enabled.
        /// </summary>
        public bool EnableCompression => _enableCompression;

        /// <summary>
        /// Gets whether string pooling is enabled.
        /// </summary>
        public bool EnableStringPooling => _enableStringPooling;

        /// <summary>
        /// Gets the string pool size.
        /// </summary>
        public int StringPoolSize => _stringPoolSize;

        /// <summary>
        /// Gets whether statistics collection is enabled.
        /// </summary>
        public bool EnableStatistics => _enableStatistics;

        /// <summary>
        /// Creates memory target specific properties.
        /// </summary>
        /// <returns>Dictionary of memory target properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["MaxEntries"] = _maxEntries;
            properties["UseCircularBuffer"] = _useCircularBuffer;
            properties["AutoTrim"] = _autoTrim;
            properties["TrimThreshold"] = _trimThreshold;
            properties["TrimTarget"] = _trimTarget;
            properties["EnableMemoryPressureMonitoring"] = _enableMemoryPressureMonitoring;
            properties["MemoryPressureThresholdMB"] = _memoryPressureThresholdMB;
            properties["ClearOnMemoryPressure"] = _clearOnMemoryPressure;
            properties["EmergencyTrimSize"] = _emergencyTrimSize;
            properties["EnableSearching"] = _enableSearching;
            properties["EnableFiltering"] = _enableFiltering;
            properties["MaintainIndex"] = _maintainIndex;
            properties["EnableRegexSearch"] = _enableRegexSearch;
            properties["EnableExport"] = _enableExport;
            properties["ExportFormat"] = _exportFormat;
            properties["IncludeTimestamps"] = _includeTimestamps;
            properties["IncludeMetadata"] = _includeMetadata;
            properties["ShowInInspector"] = _showInInspector;
            properties["MaxInspectorEntries"] = _maxInspectorEntries;
            properties["AutoRefreshInspector"] = _autoRefreshInspector;
            properties["InspectorRefreshInterval"] = _inspectorRefreshInterval;
            properties["EnableCompression"] = _enableCompression;
            properties["EnableStringPooling"] = _enableStringPooling;
            properties["StringPoolSize"] = _stringPoolSize;
            properties["EnableStatistics"] = _enableStatistics;
            
            return properties;
        }

        /// <summary>
        /// Validates memory target specific configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (_maxEntries <= 0)
            {
                errors.Add("Maximum entries must be greater than zero");
            }

            if (_trimThreshold <= 0)
            {
                errors.Add("Trim threshold must be greater than zero");
            }

            if (_trimTarget <= 0)
            {
                errors.Add("Trim target must be greater than zero");
            }

            if (_trimTarget >= _trimThreshold)
            {
                errors.Add("Trim target must be less than trim threshold");
            }

            if (_memoryPressureThresholdMB <= 0)
            {
                errors.Add("Memory pressure threshold must be greater than zero");
            }

            if (_emergencyTrimSize <= 0)
            {
                errors.Add("Emergency trim size must be greater than zero");
            }

            if (_maxInspectorEntries <= 0)
            {
                errors.Add("Maximum inspector entries must be greater than zero");
            }

            if (_inspectorRefreshInterval <= 0)
            {
                errors.Add("Inspector refresh interval must be greater than zero");
            }

            if (_stringPoolSize <= 0)
            {
                errors.Add("String pool size must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(_exportFormat))
            {
                errors.Add("Export format cannot be empty");
            }

            return errors;
        }

        /// <summary>
        /// Resets to memory target specific defaults.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "Memory Logger";
            _description = "In-memory logging target";
            _targetType = "Memory";
            _useAsyncWrite = false;
            _bufferSize = 1;
            _maxEntries = 1000;
            _useCircularBuffer = true;
            _autoTrim = true;
            _trimThreshold = 1500;
            _trimTarget = 1000;
            _enableMemoryPressureMonitoring = true;
            _memoryPressureThresholdMB = 50.0f;
            _clearOnMemoryPressure = true;
            _emergencyTrimSize = 100;
            _enableSearching = true;
            _enableFiltering = true;
            _maintainIndex = true;
            _enableRegexSearch = false;
            _enableExport = true;
            _exportFormat = "JSON";
            _includeTimestamps = true;
            _includeMetadata = true;
            _showInInspector = true;
            _maxInspectorEntries = 100;
            _autoRefreshInspector = true;
            _inspectorRefreshInterval = 1.0f;
            _enableCompression = false;
            _enableStringPooling = true;
            _stringPoolSize = 1000;
            _enableStatistics = true;
        }

        /// <summary>
        /// Performs memory target specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values
            _maxEntries = Mathf.Max(1, _maxEntries);
            _trimThreshold = Mathf.Max(1, _trimThreshold);
            _trimTarget = Mathf.Max(1, _trimTarget);
            _memoryPressureThresholdMB = Mathf.Max(0.1f, _memoryPressureThresholdMB);
            _emergencyTrimSize = Mathf.Max(1, _emergencyTrimSize);
            _maxInspectorEntries = Mathf.Max(1, _maxInspectorEntries);
            _inspectorRefreshInterval = Mathf.Max(0.1f, _inspectorRefreshInterval);
            _stringPoolSize = Mathf.Max(1, _stringPoolSize);

            // Ensure trim target is less than threshold
            if (_trimTarget >= _trimThreshold)
            {
                _trimTarget = Mathf.Max(1, _trimThreshold - 1);
            }

            // Validate strings
            if (string.IsNullOrWhiteSpace(_exportFormat))
            {
                _exportFormat = "JSON";
            }

            // Memory targets don't need async write or buffering
            _useAsyncWrite = false;
            _bufferSize = 1;

            // Limit inspector entries to reasonable maximum
            _maxInspectorEntries = Mathf.Min(_maxInspectorEntries, _maxEntries);
        }

        /// <summary>
        /// Estimates memory usage based on current configuration.
        /// </summary>
        /// <returns>Estimated memory usage in MB</returns>
        public float EstimateMemoryUsageMB()
        {
            // Rough estimate: assume average log entry is 200 bytes
            const float averageEntrySize = 200f;
            const float bytesToMB = 1024f * 1024f;
            
            float baseMemory = (_maxEntries * averageEntrySize) / bytesToMB;
            
            if (_enableStringPooling)
            {
                baseMemory += (_stringPoolSize * 50f) / bytesToMB; // Assume 50 bytes per pooled string
            }
            
            if (_maintainIndex)
            {
                baseMemory += (_maxEntries * 20f) / bytesToMB; // Index overhead
            }
            
            return baseMemory;
        }

        /// <summary>
        /// Gets recommended settings for the current platform.
        /// </summary>
        /// <returns>Dictionary of recommended settings</returns>
        public Dictionary<string, object> GetRecommendedSettings()
        {
            var settings = new Dictionary<string, object>();
            
#if UNITY_ANDROID || UNITY_IOS
            // Mobile optimizations
            settings["MaxEntries"] = 500;
            settings["EnableCompression"] = true;
            settings["EnableStringPooling"] = true;
            settings["EnableRegexSearch"] = false;
            settings["MemoryPressureThresholdMB"] = 25.0f;
#elif UNITY_WEBGL
            // WebGL optimizations
            settings["MaxEntries"] = 200;
            settings["EnableCompression"] = true;
            settings["EnableStringPooling"] = true;
            settings["EnableRegexSearch"] = false;
            settings["MemoryPressureThresholdMB"] = 15.0f;
#else
            // Desktop/Console optimizations
            settings["MaxEntries"] = 2000;
            settings["EnableCompression"] = false;
            settings["EnableStringPooling"] = true;
            settings["EnableRegexSearch"] = true;
            settings["MemoryPressureThresholdMB"] = 100.0f;
#endif
            
            return settings;
        }

        /// <summary>
        /// Applies recommended settings for the current platform.
        /// </summary>
        [ContextMenu("Apply Recommended Settings")]
        public void ApplyRecommendedSettings()
        {
            var recommended = GetRecommendedSettings();
            
            foreach (var setting in recommended)
            {
                switch (setting.Key)
                {
                    case "MaxEntries":
                        _maxEntries = (int)setting.Value;
                        break;
                    case "EnableCompression":
                        _enableCompression = (bool)setting.Value;
                        break;
                    case "EnableStringPooling":
                        _enableStringPooling = (bool)setting.Value;
                        break;
                    case "EnableRegexSearch":
                        _enableRegexSearch = (bool)setting.Value;
                        break;
                    case "MemoryPressureThresholdMB":
                        _memoryPressureThresholdMB = (float)setting.Value;
                        break;
                }
            }
            
            ValidateInEditor();
        }
    }
}