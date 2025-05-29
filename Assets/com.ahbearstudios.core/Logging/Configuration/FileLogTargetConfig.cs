using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Targets.Configuration
{
    /// <summary>
    /// Configuration for file-based log targets.
    /// Uses composition over inheritance by implementing ILogTargetConfig directly.
    /// Also implements ILogTargetDirectoryProvider for directory management.
    /// </summary>
    [CreateAssetMenu(fileName = "FileLogTargetConfig", menuName = "AhBearStudios/Logging/File Log Target Config")]
    public class FileLogTargetConfig : ScriptableObject, ILogTargetConfig, ILogTargetDirectoryProvider
    {
        [Header("General Settings")]
        [SerializeField, Tooltip("The unique name of this log target")]
        private string _targetName = "FileTarget";
        
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
        
        [Header("File Settings")]
        [SerializeField, Tooltip("Path to the log file")]
        private string _filePath = "Logs/application.log";
        
        [SerializeField, Tooltip("Whether to append to existing file or overwrite")]
        private bool _appendToFile = true;
        
        [SerializeField, Tooltip("Maximum file size in MB before rotation")]
        private int _maxFileSizeMB = 10;
        
        [SerializeField, Tooltip("Maximum number of backup files to keep")]
        private int _maxBackupFiles = 5;
        
        [SerializeField, Tooltip("Whether to enable file rotation")]
        private bool _enableRotation = true;
        
        [Header("Additional Output")]
        [SerializeField, Tooltip("Optional secondary output path")]
        private string _secondaryOutputPath = "";
        
        #region ILogTargetConfig Implementation
        
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
        
        #endregion

        #region File-Specific Properties
        
        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set => _filePath = value;
        }
        
        /// <summary>
        /// Gets or sets whether to append to existing file.
        /// </summary>
        public bool AppendToFile
        {
            get => _appendToFile;
            set => _appendToFile = value;
        }
        
        /// <summary>
        /// Gets or sets the maximum file size in MB.
        /// </summary>
        public int MaxFileSizeMB
        {
            get => _maxFileSizeMB;
            set => _maxFileSizeMB = value;
        }
        
        /// <summary>
        /// Gets or sets the maximum number of backup files.
        /// </summary>
        public int MaxBackupFiles
        {
            get => _maxBackupFiles;
            set => _maxBackupFiles = value;
        }
        
        /// <summary>
        /// Gets or sets whether file rotation is enabled.
        /// </summary>
        public bool EnableRotation
        {
            get => _enableRotation;
            set => _enableRotation = value;
        }
        
        /// <summary>
        /// Gets or sets the secondary output path.
        /// </summary>
        public string SecondaryOutputPath
        {
            get => _secondaryOutputPath;
            set => _secondaryOutputPath = value;
        }

        #endregion
        
        #region ILogTargetDirectoryProvider Implementation
        
        /// <summary>
        /// Gets whether this log target requires directories to be created.
        /// </summary>
        public bool RequiresDirectories => !string.IsNullOrEmpty(_filePath) || !string.IsNullOrEmpty(_secondaryOutputPath);
        
        /// <summary>
        /// Gets all directory paths that this log target requires to exist.
        /// </summary>
        /// <returns>Collection of directory paths that need to be created.</returns>
        public IEnumerable<string> GetRequiredDirectories()
        {
            var directories = new List<string>();
            
            // Add primary file path
            if (!string.IsNullOrEmpty(_filePath))
            {
                directories.Add(_filePath);
            }
            
            // Add secondary output path if configured
            if (!string.IsNullOrEmpty(_secondaryOutputPath))
            {
                directories.Add(_secondaryOutputPath);
            }
            
            return directories;
        }
        
        #endregion
        
        #region ILogTargetConfig Methods
        
        /// <summary>
        /// Creates a log target based on this configuration.
        /// </summary>
        /// <returns>A configured file log target.</returns>
        public ILogTarget CreateTarget()
        {
            return CreateTarget(null);
        }
        
        /// <summary>
        /// Creates a log target based on this configuration with optional message bus.
        /// </summary>
        /// <param name="messageBus">Optional message bus for publishing log events.</param>
        /// <returns>A configured file log target.</returns>
        public ILogTarget CreateTarget(IMessageBus messageBus)
        {
            // This would create and return your actual file log target implementation
            // For example:
            // var target = new FileLogTarget(this, messageBus);
            // ApplyTagFilters(target);
            // return target;
            
            throw new System.NotImplementedException("FileLogTarget implementation not shown in this example");
        }
        
        /// <summary>
        /// Applies the tag filters to the specified log target.
        /// </summary>
        /// <param name="target">The log target to configure with filters.</param>
        public void ApplyTagFilters(ILogTarget target)
        {
            if (target == null)
                return;

            target.SetTagFilters(_includedTags, _excludedTags, _processUntaggedMessages);
        }
        
        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        public ILogTargetConfig Clone()
        {
            var clone = Instantiate(this);
            clone.name = this.name;
            return clone;
        }
        
        #endregion
        
        /// <summary>
        /// Validates the configuration when values are changed in the inspector.
        /// </summary>
        private void OnValidate()
        {
            // Ensure target name is not empty
            if (string.IsNullOrEmpty(_targetName))
            {
                _targetName = "FileTarget_" + System.Guid.NewGuid().ToString().Substring(0, 8);
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
            
            // Validate file path
            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = "Logs/application.log";
                Debug.LogWarning("File path cannot be empty. Reset to default.");
            }
            
            // Validate file size
            if (_maxFileSizeMB <= 0)
            {
                _maxFileSizeMB = 10;
                Debug.LogWarning("Max file size must be positive. Reset to 10MB.");
            }
            
            // Validate backup file count
            if (_maxBackupFiles < 0)
            {
                _maxBackupFiles = 0;
                Debug.LogWarning("Max backup files cannot be negative. Reset to 0.");
            }
        }
    }
}