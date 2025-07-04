using UnityEngine;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Base configuration for log targets.
    /// Implements common functionality shared across different log target types.
    /// </summary>
    public abstract class LogTargetConfig_Old : ScriptableObject, ILogTargetConfig
    {
        [Header("General Settings")]
        [SerializeField, Tooltip("The unique name of this log target")]
        private string _targetName = "DefaultTarget";
        
        [SerializeField, Tooltip("Whether this log target is enabled")]
        private bool _enabled = true;
        
        [SerializeField, Tooltip("The minimum log level that this target will process")]
        private LogLevel _minimumLevel = LogLevel.Debug;
        
        [Header("Tag Filtering")]
        [SerializeField, Tooltip("Tags that should be included by this log target (leave empty to include all)")]
        private string[] _includedTags = new string[0];
        
        [SerializeField, Tooltip("Tags that should be excluded by this log target")]
        private string[] _excludedTags = new string[0];
        
        [SerializeField, Tooltip("Whether to process untagged log messages")]
        private bool _processUntaggedMessages = true;
        
        [Header("Unity Integration")]
        [SerializeField, Tooltip("Whether this target will forward Unity's internal logs")]
        private bool _captureUnityLogs = true;
        
        [Header("Message Formatting")]
        [SerializeField, Tooltip("Whether stack traces should be included in log output")]
        private bool _includeStackTraces = true;
        
        [SerializeField, Tooltip("Whether to include timestamps in log messages")]
        private bool _includeTimestamps = true;
        
        [SerializeField, Tooltip("Format string for timestamps")]
        private string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        
        [SerializeField, Tooltip("Whether to include source context information in log messages")]
        private bool _includeSourceContext = true;
        
        [SerializeField, Tooltip("Whether to include thread ID in log messages")]
        private bool _includeThreadId = false;
        
        [SerializeField, Tooltip("Whether structured logging is enabled")]
        private bool _enableStructuredLogging = false;
        
        [Header("Performance")]
        [SerializeField, Tooltip("Whether this target should flush immediately on each log")]
        private bool _autoFlush = true;
        
        [SerializeField, Tooltip("Buffer size for batched logging (0 = disabled)")]
        private int _bufferSize = 0;
        
        [SerializeField, Tooltip("Flush interval in seconds (0 = disabled)")]
        private float _flushIntervalSeconds = 0;
        
        [SerializeField, Tooltip("Whether to limit message length")]
        private bool _limitMessageLength = false;
        
        [SerializeField, Tooltip("Maximum message length when limiting is enabled")]
        private int _maxMessageLength = 8192;
        
        // ILogTargetConfig implementation
        public string TargetName 
        { 
            get => _targetName; 
            set => _targetName = value; 
        }
        
        public bool Enabled 
        { 
            get => _enabled; 
            set => _enabled = value; 
        }
        
        public LogLevel MinimumLevel 
        { 
            get => _minimumLevel; 
            set => _minimumLevel = value; 
        }
        
        public string[] IncludedTags 
        { 
            get => _includedTags; 
            set => _includedTags = value; 
        }
        
        public string[] ExcludedTags 
        { 
            get => _excludedTags; 
            set => _excludedTags = value; 
        }
        
        public bool ProcessUntaggedMessages 
        { 
            get => _processUntaggedMessages; 
            set => _processUntaggedMessages = value; 
        }
        
        public bool CaptureUnityLogs 
        { 
            get => _captureUnityLogs; 
            set => _captureUnityLogs = value; 
        }
        
        public bool IncludeStackTraces 
        { 
            get => _includeStackTraces; 
            set => _includeStackTraces = value; 
        }
        
        public bool IncludeTimestamps 
        { 
            get => _includeTimestamps; 
            set => _includeTimestamps = value; 
        }
        
        public string TimestampFormat 
        { 
            get => _timestampFormat; 
            set => _timestampFormat = value; 
        }
        
        public bool IncludeSourceContext 
        { 
            get => _includeSourceContext; 
            set => _includeSourceContext = value; 
        }
        
        public bool IncludeThreadId 
        { 
            get => _includeThreadId; 
            set => _includeThreadId = value; 
        }
        
        public bool EnableStructuredLogging 
        { 
            get => _enableStructuredLogging; 
            set => _enableStructuredLogging = value; 
        }
        
        public bool AutoFlush 
        { 
            get => _autoFlush; 
            set => _autoFlush = value; 
        }
        
        public int BufferSize 
        { 
            get => _bufferSize; 
            set => _bufferSize = value; 
        }
        
        public float FlushIntervalSeconds 
        { 
            get => _flushIntervalSeconds; 
            set => _flushIntervalSeconds = value; 
        }
        
        public bool LimitMessageLength 
        { 
            get => _limitMessageLength; 
            set => _limitMessageLength = value; 
        }
        
        public int MaxMessageLength 
        { 
            get => _maxMessageLength; 
            set => _maxMessageLength = value; 
        }
        
        /// <summary>
        /// Creates a log target based on this configuration.
        /// </summary>
        /// <returns>A configured log target.</returns>
        public abstract ILogTarget CreateTarget();
        
        /// <summary>
        /// Creates a log target based on this configuration with optional message bus.
        /// </summary>
        /// <param name="messageBusService">Optional message bus for publishing log events.</param>
        /// <returns>A configured log target.</returns>
        public virtual ILogTarget CreateTarget(IMessageBusService messageBusService)
        {
            return CreateTarget();
        }
        
        /// <summary>
        /// Applies the tag filters to the specified log target.
        /// </summary>
        /// <param name="target">The log target to configure with filters.</param>
        public virtual void ApplyTagFilters(ILogTarget target)
        {
            if (target == null)
                return;

            target.SetTagFilters(_includedTags, _excludedTags, _processUntaggedMessages);
        }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        public virtual ILogTargetConfig Clone()
        {
            var clone = Instantiate(this);
            clone.name = this.name;
            return clone;
        }
        
        /// <summary>
        /// Validates the configuration when values are changed in the inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            // Ensure target name is not empty
            if (string.IsNullOrEmpty(_targetName))
            {
                _targetName = "Target_" + System.Guid.NewGuid().ToString().Substring(0, 8);
                Debug.LogWarning($"Target name cannot be empty. Generated a new name: {_targetName}");
            }
            
            // Ensure buffer size is valid
            if (_bufferSize < 0)
            {
                _bufferSize = 0;
                Debug.LogWarning("Buffer size cannot be negative. Reset to 0.");
            }
            
            // Ensure max message length is valid
            if (_limitMessageLength && _maxMessageLength <= 0)
            {
                _maxMessageLength = 8192;
                Debug.LogWarning("Max message length must be positive when limiting is enabled. Reset to 8192.");
            }
            
            // Ensure flush interval is valid
            if (_flushIntervalSeconds < 0)
            {
                _flushIntervalSeconds = 0;
                Debug.LogWarning("Flush interval cannot be negative. Reset to 0.");
            }
        }
    }
}