using UnityEngine;
using AhBearStudios.Core.Logging.LogTargets;
using System.IO;

namespace AhBearStudios.Core.Logging.Config
{
    /// <summary>
    /// Configuration for Serilog file log targets.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SerilogFileConfig", 
        menuName = "AhBearStudios/Logging/Serilog File Config", 
        order = 2)]
    public class SerilogFileConfig : LogTargetConfig
    {
        [Tooltip("The file path where logs will be written (relative to application data path)")]
        [SerializeField] private string _logFilePath = "Logs/app.log";
        
        [Tooltip("Whether to use JSON formatting for logs")]
        [SerializeField] private bool _useJsonFormat = false;
        
        [Tooltip("Whether to log to both file and console")]
        [SerializeField] private bool _logToConsole = false;
        
        [Tooltip("Number of days to retain log files")]
        [SerializeField] private int _retainedDays = 7;
        
        /// <summary>
        /// Gets the resolved log file path.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_logFilePath))
                    return "Logs/app.log";
                    
                // Resolve relative to persistentDataPath if not absolute
                if (!Path.IsPathRooted(_logFilePath))
                {
                    return Path.Combine(Application.persistentDataPath, _logFilePath);
                }
                
                return _logFilePath;
            }
        }
        
        /// <summary>
        /// Gets whether to use JSON formatting.
        /// </summary>
        public bool UseJsonFormat => _useJsonFormat;
        
        /// <summary>
        /// Gets whether to log to both file and console.
        /// </summary>
        public bool LogToConsole => _logToConsole;
        
        /// <summary>
        /// Gets the number of days to retain log files.
        /// </summary>
        public int RetainedDays => _retainedDays <= 0 ? 7 : _retainedDays;
        
        /// <summary>
        /// Creates a Serilog log target based on this configuration.
        /// </summary>
        /// <returns>A configured SerilogTarget.</returns>
        public override ILogTarget CreateTarget()
        {
            EnsureDirectoryExists(LogFilePath);
            
            ILogTarget target;
            
            if (_logToConsole)
            {
                // Create a combined file and console target
                target = SerilogTarget.CreateFileAndConsoleTarget(
                    TargetName,
                    LogFilePath,
                    MinimumLevel
                );
            }
            else
            {
                // Create a file-only target
                target = SerilogTarget.CreateFileTarget(
                    TargetName,
                    LogFilePath,
                    MinimumLevel,
                    UseJsonFormat
                );
            }
            
            target.IsEnabled = Enabled;
            ApplyTagFilters(target);
            
            return target;
        }
        
        /// <summary>
        /// Ensures the directory for the log file exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        private void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}