using System;
using UnityEngine;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Unity.Attributes
{
    /// <summary>
    /// Attribute that can be applied to classes or methods to automatically generate logging code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WithLogAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the minimum log level.
        /// </summary>
        public byte MinimumLevel { get; }
        
        /// <summary>
        /// Gets or sets the tag for log messages.
        /// </summary>
        public Tagging.LogTag Tag { get; }
        
        /// <summary>
        /// Gets or sets the custom tag string for log messages.
        /// </summary>
        public string CustomTag { get; }
        
        /// <summary>
        /// Gets or sets whether to log method entry.
        /// </summary>
        public bool LogEntry { get; }
        
        /// <summary>
        /// Gets or sets whether to log method exit.
        /// </summary>
        public bool LogExit { get; }
        
        /// <summary>
        /// Gets or sets whether to log method parameters.
        /// </summary>
        public bool LogParameters { get; }
        
        /// <summary>
        /// Gets or sets whether to log method return value.
        /// </summary>
        public bool LogReturnValue { get; }
        
        /// <summary>
        /// Gets or sets whether to track method execution time.
        /// </summary>
        public bool TrackTime { get; }
        
        /// <summary>
        /// Creates a new WithLogAttribute with the specified parameters.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <param name="tag">The tag for log messages.</param>
        /// <param name="logEntry">Whether to log method entry.</param>
        /// <param name="logExit">Whether to log method exit.</param>
        /// <param name="logParameters">Whether to log method parameters.</param>
        /// <param name="logReturnValue">Whether to log method return value.</param>
        /// <param name="trackTime">Whether to track method execution time.</param>
        public WithLogAttribute(
            byte minimumLevel = LogLevel.Debug,
            Tagging.LogTag tag = Tagging.LogTag.Info,
            bool logEntry = true,
            bool logExit = true,
            bool logParameters = false,
            bool logReturnValue = false,
            bool trackTime = false)
        {
            MinimumLevel = minimumLevel;
            Tag = tag;
            CustomTag = null;
            LogEntry = logEntry;
            LogExit = logExit;
            LogParameters = logParameters;
            LogReturnValue = logReturnValue;
            TrackTime = trackTime;
        }
        
        /// <summary>
        /// Creates a new WithLogAttribute with a custom tag.
        /// </summary>
        /// <param name="customTag">The custom tag string.</param>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <param name="logEntry">Whether to log method entry.</param>
        /// <param name="logExit">Whether to log method exit.</param>
        /// <param name="logParameters">Whether to log method parameters.</param>
        /// <param name="logReturnValue">Whether to log method return value.</param>
        /// <param name="trackTime">Whether to track method execution time.</param>
        public WithLogAttribute(
            string customTag,
            byte minimumLevel = LogLevel.Debug,
            bool logEntry = true,
            bool logExit = true,
            bool logParameters = false,
            bool logReturnValue = false,
            bool trackTime = false)
        {
            MinimumLevel = minimumLevel;
            Tag = Tagging.LogTag.Undefined;
            CustomTag = customTag;
            LogEntry = logEntry;
            LogExit = logExit;
            LogParameters = logParameters;
            LogReturnValue = logReturnValue;
            TrackTime = trackTime;
        }
    }
}