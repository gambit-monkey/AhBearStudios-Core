using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Targets.Configuration
{
    /// <summary>
    /// Configuration for file-based log targets.
    /// Example implementation showing how to implement ILogTargetDirectoryProvider.
    /// </summary>
    [CreateAssetMenu(fileName = "FileLogTargetConfig", menuName = "AhBearStudios/Logging/File Log Target Config")]
    public class FileLogTargetConfig : LogTargetConfig, ILogTargetDirectoryProvider
    {
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
        
        #region LogTargetConfig Overrides
        
        /// <summary>
        /// Creates a log target based on this configuration.
        /// </summary>
        /// <returns>A configured file log target.</returns>
        public override ILogTarget CreateTarget()
        {
            // This would create and return your actual file log target implementation
            // For example:
            // return new FileLogTarget(this);
            
            throw new System.NotImplementedException("FileLogTarget implementation not shown in this example");
        }
        
        /// <summary>
        /// Validates the configuration when values are changed in the inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
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
        
        #endregion
    }
}