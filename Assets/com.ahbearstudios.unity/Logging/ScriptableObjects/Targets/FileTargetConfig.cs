using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Targets
{
    /// <summary>
    /// ScriptableObject configuration for File log target.
    /// Provides Unity-serializable configuration for file-based logging.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Targets/File Target", 
        fileName = "FileTargetConfig", 
        order = 2)]
    public class FileTargetConfig : LogTargetScriptableObject
    {
        [Header("File Settings")]
        [SerializeField] private string _filePath = "Logs/game.log";
        [SerializeField] private bool _appendToFile = true;
        [SerializeField] private bool _createDirectoryIfMissing = true;
        [SerializeField] private string _fileExtension = ".log";

        [Header("File Rotation")]
        [SerializeField] private bool _enableFileRotation = true;
        [SerializeField] private int _maxFileSizeMB = 10;
        [SerializeField] private int _maxFileCount = 5;
        [SerializeField] private bool _rotateOnStartup = false;
        [SerializeField] private string _rotationNamingPattern = "{0}_{1:yyyy-MM-dd_HH-mm-ss}{2}";

        [Header("File Access")]
        [SerializeField] private bool _sharedFileAccess = false;
        [SerializeField] private bool _lockFile = true;
        [SerializeField] private float _fileAccessTimeoutSeconds = 5.0f;

        [Header("Content Formatting")]
        [SerializeField] private bool _includeTimestamp = true;
        [SerializeField] private bool _includeLogLevel = true;
        [SerializeField] private bool _includeThreadId = false;
        [SerializeField] private bool _includeSourceContext = true;
        [SerializeField] private string _lineEnding = "\n";
        [SerializeField] private string _fieldSeparator = " | ";

        [Header("Encoding Settings")]
        [SerializeField] private string _encoding = "UTF-8";
        [SerializeField] private bool _includeBOM = false;

        [Header("Platform-Specific Settings")]
        [SerializeField] private bool _useUnityPersistentDataPath = true;
        [SerializeField] private bool _useStreamingAssetsPath = false;
        [SerializeField] private string _customBasePath = "";

        /// <summary>
        /// Gets the file path for logging.
        /// </summary>
        public string FilePath => GetResolvedFilePath();

        /// <summary>
        /// Gets whether to append to existing file.
        /// </summary>
        public bool AppendToFile => _appendToFile;

        /// <summary>
        /// Gets whether to create directory if missing.
        /// </summary>
        public bool CreateDirectoryIfMissing => _createDirectoryIfMissing;

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        public string FileExtension => _fileExtension;

        /// <summary>
        /// Gets whether file rotation is enabled.
        /// </summary>
        public bool EnableFileRotation => _enableFileRotation;

        /// <summary>
        /// Gets the maximum file size in bytes.
        /// </summary>
        public long MaxFileSizeBytes => _maxFileSizeMB * 1024L * 1024L;

        /// <summary>
        /// Gets the maximum number of files to keep.
        /// </summary>
        public int MaxFileCount => _maxFileCount;

        /// <summary>
        /// Gets whether to rotate on startup.
        /// </summary>
        public bool RotateOnStartup => _rotateOnStartup;

        /// <summary>
        /// Gets the rotation naming pattern.
        /// </summary>
        public string RotationNamingPattern => _rotationNamingPattern;

        /// <summary>
        /// Gets whether shared file access is enabled.
        /// </summary>
        public bool SharedFileAccess => _sharedFileAccess;

        /// <summary>
        /// Gets whether to lock the file.
        /// </summary>
        public bool LockFile => _lockFile;

        /// <summary>
        /// Gets the file access timeout in seconds.
        /// </summary>
        public float FileAccessTimeoutSeconds => _fileAccessTimeoutSeconds;

        /// <summary>
        /// Gets whether to include timestamp in output.
        /// </summary>
        public bool IncludeTimestamp => _includeTimestamp;

        /// <summary>
        /// Gets whether to include log level in output.
        /// </summary>
        public bool IncludeLogLevel => _includeLogLevel;

        /// <summary>
        /// Gets whether to include thread ID in output.
        /// </summary>
        public bool IncludeThreadId => _includeThreadId;

        /// <summary>
        /// Gets whether to include source context in output.
        /// </summary>
        public bool IncludeSourceContext => _includeSourceContext;

        /// <summary>
        /// Gets the line ending to use.
        /// </summary>
        public string LineEnding => _lineEnding;

        /// <summary>
        /// Gets the field separator to use.
        /// </summary>
        public string FieldSeparator => _fieldSeparator;

        /// <summary>
        /// Gets the encoding to use.
        /// </summary>
        public string Encoding => _encoding;

        /// <summary>
        /// Gets whether to include BOM.
        /// </summary>
        public bool IncludeBOM => _includeBOM;

        /// <summary>
        /// Gets whether to use Unity's persistent data path.
        /// </summary>
        public bool UseUnityPersistentDataPath => _useUnityPersistentDataPath;

        /// <summary>
        /// Gets whether to use streaming assets path.
        /// </summary>
        public bool UseStreamingAssetsPath => _useStreamingAssetsPath;

        /// <summary>
        /// Gets the custom base path.
        /// </summary>
        public string CustomBasePath => _customBasePath;

        /// <summary>
        /// Creates file target specific properties.
        /// </summary>
        /// <returns>Dictionary of file target properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["FilePath"] = GetResolvedFilePath();
            properties["AppendToFile"] = _appendToFile;
            properties["CreateDirectoryIfMissing"] = _createDirectoryIfMissing;
            properties["FileExtension"] = _fileExtension;
            properties["EnableFileRotation"] = _enableFileRotation;
            properties["MaxFileSizeBytes"] = MaxFileSizeBytes;
            properties["MaxFileCount"] = _maxFileCount;
            properties["RotateOnStartup"] = _rotateOnStartup;
            properties["RotationNamingPattern"] = _rotationNamingPattern;
            properties["SharedFileAccess"] = _sharedFileAccess;
            properties["LockFile"] = _lockFile;
            properties["FileAccessTimeoutSeconds"] = _fileAccessTimeoutSeconds;
            properties["IncludeTimestamp"] = _includeTimestamp;
            properties["IncludeLogLevel"] = _includeLogLevel;
            properties["IncludeThreadId"] = _includeThreadId;
            properties["IncludeSourceContext"] = _includeSourceContext;
            properties["LineEnding"] = _lineEnding;
            properties["FieldSeparator"] = _fieldSeparator;
            properties["Encoding"] = _encoding;
            properties["IncludeBOM"] = _includeBOM;
            properties["UseUnityPersistentDataPath"] = _useUnityPersistentDataPath;
            properties["UseStreamingAssetsPath"] = _useStreamingAssetsPath;
            properties["CustomBasePath"] = _customBasePath;
            
            return properties;
        }

        /// <summary>
        /// Validates file target specific configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(_filePath))
            {
                errors.Add("File path cannot be empty");
            }

            if (_maxFileSizeMB <= 0)
            {
                errors.Add("Maximum file size must be greater than zero");
            }

            if (_maxFileCount <= 0)
            {
                errors.Add("Maximum file count must be greater than zero");
            }

            if (_fileAccessTimeoutSeconds <= 0)
            {
                errors.Add("File access timeout must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(_fileExtension))
            {
                errors.Add("File extension cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(_encoding))
            {
                errors.Add("Encoding cannot be empty");
            }

            // Validate file path
            try
            {
                var resolvedPath = GetResolvedFilePath();
                var directory = Path.GetDirectoryName(resolvedPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    errors.Add("Invalid file path: no directory component");
                }
            }
            catch (System.Exception ex)
            {
                errors.Add($"Invalid file path: {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Resets to file target specific defaults.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "File Logger";
            _description = "File-based logging target";
            _targetType = "File";
            _useAsyncWrite = true;
            _bufferSize = 1000;
            _filePath = "Logs/game.log";
            _appendToFile = true;
            _createDirectoryIfMissing = true;
            _fileExtension = ".log";
            _enableFileRotation = true;
            _maxFileSizeMB = 10;
            _maxFileCount = 5;
            _rotateOnStartup = false;
            _rotationNamingPattern = "{0}_{1:yyyy-MM-dd_HH-mm-ss}{2}";
            _sharedFileAccess = false;
            _lockFile = true;
            _fileAccessTimeoutSeconds = 5.0f;
            _includeTimestamp = true;
            _includeLogLevel = true;
            _includeThreadId = false;
            _includeSourceContext = true;
            _lineEnding = "\n";
            _fieldSeparator = " | ";
            _encoding = "UTF-8";
            _includeBOM = false;
            _useUnityPersistentDataPath = true;
            _useStreamingAssetsPath = false;
            _customBasePath = "";
        }

        /// <summary>
        /// Performs file target specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values
            _maxFileSizeMB = Mathf.Max(1, _maxFileSizeMB);
            _maxFileCount = Mathf.Max(1, _maxFileCount);
            _fileAccessTimeoutSeconds = Mathf.Max(0.1f, _fileAccessTimeoutSeconds);

            // Validate strings
            if (string.IsNullOrWhiteSpace(_filePath))
            {
                _filePath = "Logs/game.log";
            }

            if (string.IsNullOrWhiteSpace(_fileExtension))
            {
                _fileExtension = ".log";
            }

            if (string.IsNullOrWhiteSpace(_encoding))
            {
                _encoding = "UTF-8";
            }

            if (string.IsNullOrWhiteSpace(_lineEnding))
            {
                _lineEnding = "\n";
            }

            if (string.IsNullOrWhiteSpace(_fieldSeparator))
            {
                _fieldSeparator = " | ";
            }

            if (string.IsNullOrWhiteSpace(_rotationNamingPattern))
            {
                _rotationNamingPattern = "{0}_{1:yyyy-MM-dd_HH-mm-ss}{2}";
            }

            // Ensure file extension starts with dot
            if (!_fileExtension.StartsWith("."))
            {
                _fileExtension = "." + _fileExtension;
            }

            // File targets should use async write and buffering
            _useAsyncWrite = true;
            _bufferSize = Mathf.Max(100, _bufferSize);
        }

        /// <summary>
        /// Gets the resolved file path based on platform settings.
        /// </summary>
        /// <returns>The resolved file path</returns>
        private string GetResolvedFilePath()
        {
            if (!string.IsNullOrWhiteSpace(_customBasePath))
            {
                return Path.Combine(_customBasePath, _filePath);
            }
            
            if (_useUnityPersistentDataPath)
            {
                return Path.Combine(Application.persistentDataPath, _filePath);
            }
            
            if (_useStreamingAssetsPath)
            {
                return Path.Combine(Application.streamingAssetsPath, _filePath);
            }
            
            return Path.Combine(Application.dataPath, _filePath);
        }

        /// <summary>
        /// Gets the directory path for the log file.
        /// </summary>
        /// <returns>The directory path</returns>
        public string GetDirectoryPath()
        {
            return Path.GetDirectoryName(GetResolvedFilePath());
        }

        /// <summary>
        /// Gets the full file name with extension.
        /// </summary>
        /// <returns>The file name with extension</returns>
        public string GetFileName()
        {
            return Path.GetFileName(GetResolvedFilePath());
        }

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <returns>The file name without extension</returns>
        public string GetFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(GetResolvedFilePath());
        }

        /// <summary>
        /// Creates a rotated file name based on the pattern.
        /// </summary>
        /// <param name="timestamp">The timestamp for the rotation</param>
        /// <param name="index">The rotation index</param>
        /// <returns>The rotated file name</returns>
        public string CreateRotatedFileName(System.DateTime timestamp, int index = 0)
        {
            var baseName = GetFileNameWithoutExtension();
            var extension = Path.GetExtension(GetResolvedFilePath());
            
            if (index > 0)
            {
                return string.Format(_rotationNamingPattern, baseName, timestamp, $"_{index}{extension}");
            }
            
            return string.Format(_rotationNamingPattern, baseName, timestamp, extension);
        }
    }
}