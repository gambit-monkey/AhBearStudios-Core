using System;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Builder for Serilog file configurations that can work with both runtime creation 
    /// and ScriptableObject configuration
    /// </summary>
    public sealed class SerilogFileConfigBuilder : ILogTargetConfigBuilder<SerilogFileConfig, SerilogFileConfigBuilder>
    {
        /// <summary>
        /// Internal config data structure
        /// </summary>
        public class ConfigData
        {
            public string TargetName { get; set; } = "SerilogFile";
            public bool Enabled { get; set; } = true;
            public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
            public string LogFilePath { get; set; } = "Logs/app.log";
            public bool UseJsonFormat { get; set; } = false;
            public bool LogToConsole { get; set; } = false;
            public int RetainedDays { get; set; } = 7;
            public string[] IncludedTags { get; set; } = new string[0];
            public string[] ExcludedTags { get; set; } = new string[0];
            public bool ProcessUntaggedMessages { get; set; } = true;
            public bool CaptureUnityLogs { get; set; } = true;
            public bool IncludeStackTraces { get; set; } = true;
            public bool IncludeTimestamps { get; set; } = true;
            public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
            public bool IncludeSourceContext { get; set; } = true;
            public bool IncludeThreadId { get; set; } = false;
            public bool EnableStructuredLogging { get; set; } = false;
            public bool AutoFlush { get; set; } = true;
            public int BufferSize { get; set; } = 0;
            public float FlushIntervalSeconds { get; set; } = 0;
            public bool LimitMessageLength { get; set; } = false;
            public int MaxMessageLength { get; set; } = 8192;
        }

        private readonly ConfigData _data;

        public string ConfigId
        {
            get => _data.TargetName;
            set => _data.TargetName = value;
        }

        public SerilogFileConfigBuilder()
        {
            _data = new ConfigData();
        }

        public SerilogFileConfigBuilder WithTargetName(string name)
        {
            _data.TargetName = name;
            return this;
        }

        public SerilogFileConfigBuilder WithEnabled(bool enabled)
        {
            _data.Enabled = enabled;
            return this;
        }

        public SerilogFileConfigBuilder WithMinimumLevel(LogLevel level)
        {
            _data.MinimumLevel = level;
            return this;
        }

        public SerilogFileConfigBuilder WithLogFilePath(string path)
        {
            _data.LogFilePath = path;
            return this;
        }

        public SerilogFileConfigBuilder WithJsonFormat(bool useJson)
        {
            _data.UseJsonFormat = useJson;
            return this;
        }

        public SerilogFileConfigBuilder WithConsoleOutput(bool logToConsole)
        {
            _data.LogToConsole = logToConsole;
            return this;
        }

        public SerilogFileConfigBuilder WithRetention(int days)
        {
            _data.RetainedDays = days;
            return this;
        }

        public SerilogFileConfigBuilder WithTagFilters(string[] includedTags, string[] excludedTags, bool processUntagged = true)
        {
            _data.IncludedTags = includedTags ?? new string[0];
            _data.ExcludedTags = excludedTags ?? new string[0];
            _data.ProcessUntaggedMessages = processUntagged;
            return this;
        }

        public SerilogFileConfigBuilder WithTimestamps(bool include, string format = "yyyy-MM-dd HH:mm:ss.fff")
        {
            _data.IncludeTimestamps = include;
            _data.TimestampFormat = format;
            return this;
        }

        public SerilogFileConfigBuilder WithPerformance(bool autoFlush = true, int bufferSize = 0, float flushInterval = 0f)
        {
            _data.AutoFlush = autoFlush;
            _data.BufferSize = bufferSize;
            _data.FlushIntervalSeconds = flushInterval;
            return this;
        }

        public SerilogFileConfigBuilder WithUnityIntegration(bool captureUnityLogs = true)
        {
            _data.CaptureUnityLogs = captureUnityLogs;
            return this;
        }

        public SerilogFileConfigBuilder WithMessageFormatting(bool includeStackTraces = true, bool includeSourceContext = true, bool includeThreadId = false)
        {
            _data.IncludeStackTraces = includeStackTraces;
            _data.IncludeSourceContext = includeSourceContext;
            _data.IncludeThreadId = includeThreadId;
            return this;
        }

        public SerilogFileConfigBuilder WithStructuredLogging(bool enabled = false)
        {
            _data.EnableStructuredLogging = enabled;
            return this;
        }

        public SerilogFileConfigBuilder WithMessageLengthLimit(bool limitLength = false, int maxLength = 8192)
        {
            _data.LimitMessageLength = limitLength;
            _data.MaxMessageLength = maxLength;
            return this;
        }

        /// <summary>
        /// Initializes builder from existing ScriptableObject config
        /// </summary>
        public SerilogFileConfigBuilder FromExisting(SerilogFileConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Use public properties where available
            _data.TargetName = config.TargetName;
            _data.Enabled = config.Enabled;
            _data.MinimumLevel = config.MinimumLevel;
            _data.LogFilePath = config.LogFilePath;
            _data.UseJsonFormat = config.UseJsonFormat;
            _data.LogToConsole = config.LogToConsole;
            _data.RetainedDays = config.RetainedDays;
            _data.IncludedTags = (string[])config.IncludedTags?.Clone() ?? new string[0];
            _data.ExcludedTags = (string[])config.ExcludedTags?.Clone() ?? new string[0];
            _data.ProcessUntaggedMessages = config.ProcessUntaggedMessages;
            _data.CaptureUnityLogs = config.CaptureUnityLogs;
            _data.IncludeStackTraces = config.IncludeStackTraces;
            _data.IncludeTimestamps = config.IncludeTimestamps;
            _data.TimestampFormat = config.TimestampFormat;
            _data.IncludeSourceContext = config.IncludeSourceContext;
            _data.IncludeThreadId = config.IncludeThreadId;
            _data.EnableStructuredLogging = config.EnableStructuredLogging;
            _data.AutoFlush = config.AutoFlush;
            _data.BufferSize = config.BufferSize;
            _data.FlushIntervalSeconds = config.FlushIntervalSeconds;
            _data.LimitMessageLength = config.LimitMessageLength;
            _data.MaxMessageLength = config.MaxMessageLength;
    
            return this;
        }

        /// <summary>
        /// Configures for high-performance logging with minimal overhead
        /// </summary>
        public SerilogFileConfigBuilder AsHighPerformance()
        {
            _data.MinimumLevel = LogLevel.Warning;
            _data.IncludeStackTraces = false;
            _data.IncludeSourceContext = false;
            _data.IncludeThreadId = false;
            _data.IncludeTimestamps = false;
            _data.EnableStructuredLogging = false;
            _data.AutoFlush = false;
            _data.BufferSize = 1024;
            _data.FlushIntervalSeconds = 2.0f;
            _data.UseJsonFormat = false;
            return this;
        }

        /// <summary>
        /// Configures for debug logging with detailed information
        /// </summary>
        public SerilogFileConfigBuilder AsDebug()
        {
            _data.MinimumLevel = LogLevel.Trace;
            _data.IncludeStackTraces = true;
            _data.IncludeSourceContext = true;
            _data.IncludeThreadId = true;
            _data.IncludeTimestamps = true;
            _data.EnableStructuredLogging = true;
            _data.UseJsonFormat = true;
            _data.LogToConsole = true;
            _data.AutoFlush = true;
            return this;
        }

        /// <summary>
        /// Gets the internal config data (useful for extensions and copying)
        /// </summary>
        internal ConfigData GetData() => _data;

        /// <summary>
        /// Builds a new ScriptableObject instance with the configured settings
        /// </summary>
        public SerilogFileConfig Build()
        {
            var config = ScriptableObject.CreateInstance<SerilogFileConfig>();
            
            // Apply all the configured settings to the ScriptableObject
            // Note: This requires either reflection or exposing internal setters
            ApplyDataToScriptableObject(_data, config);
            
            return config;
        }

        /// <summary>
        /// Applies the builder data to an existing ScriptableObject config
        /// </summary>
        public void ApplyTo(SerilogFileConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            ApplyDataToScriptableObject(_data, config);
        }

        private static void ApplyDataToScriptableObject(ConfigData data, SerilogFileConfig config)
        {
            config.TargetName = data.TargetName;
            config.Enabled = data.Enabled;
            config.MinimumLevel = data.MinimumLevel;
            config.IncludedTags = data.IncludedTags;
            config.ExcludedTags = data.ExcludedTags;
            config.ProcessUntaggedMessages = data.ProcessUntaggedMessages;
            config.CaptureUnityLogs = data.CaptureUnityLogs;
            config.IncludeStackTraces = data.IncludeStackTraces;
            config.IncludeTimestamps = data.IncludeTimestamps;
            config.TimestampFormat = data.TimestampFormat;
            config.IncludeSourceContext = data.IncludeSourceContext;
            config.IncludeThreadId = data.IncludeThreadId;
            config.EnableStructuredLogging = data.EnableStructuredLogging;
            config.AutoFlush = data.AutoFlush;
            config.BufferSize = data.BufferSize;
            config.FlushIntervalSeconds = data.FlushIntervalSeconds;
            config.LimitMessageLength = data.LimitMessageLength;
            config.MaxMessageLength = data.MaxMessageLength;
        }
    }
}