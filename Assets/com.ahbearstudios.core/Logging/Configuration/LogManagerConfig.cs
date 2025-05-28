using UnityEngine;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// ScriptableObject that implements ILoggerConfig for configuring the overall logging system.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LogManagerConfig", 
        menuName = "AhBearStudios/Logging/Log Manager Config", 
        order = 0)]
    public class LogManagerConfig : ScriptableObject, ILoggerConfig
    {
        [Tooltip("The minimum log level that will be processed")]
        [SerializeField] private LogLevel _minimumLevel = LogLevel.Info;
        
        [Tooltip("Maximum number of messages to process per batch")]
        [SerializeField] private int _maxMessagesPerBatch = 200;
        
        [Tooltip("Initial capacity of the log queue")]
        [SerializeField] private int _initialQueueCapacity = 64;
        
        [Tooltip("Enable automatic flushing of logs")]
        [SerializeField] private bool _enableAutoFlush = true;
        
        [Tooltip("Interval in seconds between auto-flush operations")]
        [SerializeField] private float _autoFlushInterval = 0.5f;
        
        [Tooltip("Default tag to use when no tag is specified")]
        [SerializeField] private Tagging.LogTag _defaultTag = Tagging.LogTag.Default;
        
        /// <summary>
        /// The minimum log level that will be processed.
        /// </summary>
        public LogLevel MinimumLevel => _minimumLevel;
        
        /// <summary>
        /// Maximum number of messages to process per batch.
        /// </summary>
        public int MaxMessagesPerBatch => _maxMessagesPerBatch;
        
        /// <summary>
        /// Initial capacity of the log queue.
        /// </summary>
        public int InitialQueueCapacity => _initialQueueCapacity;
        
        /// <summary>
        /// Whether to enable automatic flushing of logs.
        /// </summary>
        public bool EnableAutoFlush => _enableAutoFlush;
        
        /// <summary>
        /// Interval in seconds between auto-flush operations.
        /// </summary>
        public float AutoFlushInterval => _autoFlushInterval;
        
        /// <summary>
        /// Default tag to use when no tag is specified.
        /// </summary>
        public Tagging.LogTag DefaultTag => _defaultTag;
    }
}