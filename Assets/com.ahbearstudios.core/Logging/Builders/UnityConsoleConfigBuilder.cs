using System;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Builder for Unity console configurations
    /// </summary>
    public sealed class UnityConsoleConfigBuilder : ILogTargetConfigBuilder<UnityConsoleLogConfig, UnityConsoleConfigBuilder>
    {
        public class ConfigData
        {
            public string TargetName { get; set; } = "UnityConsole";
            public bool Enabled { get; set; } = true;
            public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
            public bool UseColorizedOutput { get; set; } = true;
            public bool RegisterUnityLogHandler { get; set; } = true;
            public bool DuplicateToOriginalHandler { get; set; } = false;
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

        public UnityConsoleConfigBuilder()
        {
            _data = new ConfigData();
        }

        // Implement all the interface methods similar to SerilogFileConfigBuilder...
        public UnityConsoleConfigBuilder WithTargetName(string name)
        {
            _data.TargetName = name;
            return this;
        }

        public UnityConsoleConfigBuilder WithEnabled(bool enabled)
        {
            _data.Enabled = enabled;
            return this;
        }

        public UnityConsoleConfigBuilder WithMinimumLevel(LogLevel level)
        {
            _data.MinimumLevel = level;
            return this;
        }

        public UnityConsoleConfigBuilder WithColorizedOutput(bool useColorized)
        {
            _data.UseColorizedOutput = useColorized;
            return this;
        }

        public UnityConsoleConfigBuilder WithUnityLogHandlerRegistration(bool register, bool duplicateToOriginal = false)
        {
            _data.RegisterUnityLogHandler = register;
            _data.DuplicateToOriginalHandler = duplicateToOriginal;
            return this;
        }

        public UnityConsoleConfigBuilder WithTagFilters(string[] includedTags, string[] excludedTags, bool processUntagged = true)
        {
            _data.IncludedTags = includedTags ?? new string[0];
            _data.ExcludedTags = excludedTags ?? new string[0];
            _data.ProcessUntaggedMessages = processUntagged;
            return this;
        }

        public UnityConsoleConfigBuilder WithTimestamps(bool include, string format = "yyyy-MM-dd HH:mm:ss.fff")
        {
            _data.IncludeTimestamps = include;
            _data.TimestampFormat = format;
            return this;
        }

        public UnityConsoleConfigBuilder WithPerformance(bool autoFlush = true, int bufferSize = 0, float flushInterval = 0f)
        {
            _data.AutoFlush = autoFlush;
            _data.BufferSize = bufferSize;
            _data.FlushIntervalSeconds = flushInterval;
            return this;
        }

        public UnityConsoleConfigBuilder WithUnityIntegration(bool captureUnityLogs = true)
        {
            _data.CaptureUnityLogs = captureUnityLogs;
            return this;
        }

        public UnityConsoleConfigBuilder WithMessageFormatting(bool includeStackTraces = true, bool includeSourceContext = true, bool includeThreadId = false)
        {
            _data.IncludeStackTraces = includeStackTraces;
            _data.IncludeSourceContext = includeSourceContext;
            _data.IncludeThreadId = includeThreadId;
            return this;
        }

        public UnityConsoleConfigBuilder WithStructuredLogging(bool enabled = false)
        {
            _data.EnableStructuredLogging = enabled;
            return this;
        }

        public UnityConsoleConfigBuilder WithMessageLengthLimit(bool limitLength = false, int maxLength = 8192)
        {
            _data.LimitMessageLength = limitLength;
            _data.MaxMessageLength = maxLength;
            return this;
        }

        /// <summary>
        /// Initializes builder from existing ScriptableObject config
        /// </summary>
        public UnityConsoleConfigBuilder FromExisting(UnityConsoleLogConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Copy all base LogTargetConfig properties
            _data.TargetName = config.TargetName;
            _data.Enabled = config.Enabled;
            _data.MinimumLevel = config.MinimumLevel;
            _data.IncludedTags = config.IncludedTags ?? new string[0];
            _data.ExcludedTags = config.ExcludedTags ?? new string[0];
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
    
            // Copy UnityConsoleLogConfig-specific properties
            _data.UseColorizedOutput = config.UseColorizedOutput;
            _data.RegisterUnityLogHandler = config.RegisterUnityLogHandler;
            _data.DuplicateToOriginalHandler = config.DuplicateToOriginalHandler;
    
            return this;
        }

        /// <summary>
        /// Configures for development with colorized output and detailed logging
        /// </summary>
        public UnityConsoleConfigBuilder AsDevelopment()
        {
            _data.MinimumLevel = LogLevel.Debug;
            _data.UseColorizedOutput = true;
            _data.IncludeTimestamps = true;
            _data.IncludeSourceContext = true;
            _data.RegisterUnityLogHandler = true;
            _data.DuplicateToOriginalHandler = false;
            return this;
        }

        /// <summary>
        /// Configures for production with minimal overhead
        /// </summary>
        public UnityConsoleConfigBuilder AsProduction()
        {
            _data.MinimumLevel = LogLevel.Warning;
            _data.UseColorizedOutput = false;
            _data.IncludeTimestamps = false;
            _data.IncludeSourceContext = false;
            _data.IncludeStackTraces = false;
            _data.RegisterUnityLogHandler = false;
            return this;
        }

        internal ConfigData GetData() => _data;

        public UnityConsoleLogConfig Build()
        {
            var config = ScriptableObject.CreateInstance<UnityConsoleLogConfig>();
            ApplyDataToScriptableObject(_data, config);
            return config;
        }

        public void ApplyTo(UnityConsoleLogConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            ApplyDataToScriptableObject(_data, config);
        }

        private static void ApplyDataToScriptableObject(ConfigData data, UnityConsoleLogConfig config)
        {
            // Apply base LogTargetConfig properties
            config.TargetName = data.TargetName;
            config.Enabled = data.Enabled;
            config.MinimumLevel = data.MinimumLevel;
            config.IncludedTags = data.IncludedTags ?? new string[0];
            config.ExcludedTags = data.ExcludedTags ?? new string[0];
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
    
            // Apply UnityConsoleLogConfig-specific properties
            config.UseColorizedOutput = data.UseColorizedOutput;
            config.RegisterUnityLogHandler = data.RegisterUnityLogHandler;
            config.DuplicateToOriginalHandler = data.DuplicateToOriginalHandler;
        }
    }
}