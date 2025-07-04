﻿using System;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Builder for Serilog file configurations that can work with both runtime creation 
    /// and ScriptableObject configuration
    /// </summary>
    public sealed class SerilogFileConfigBuilder : ILogTargetConfigBuilder<SerilogFileTargetConfig, SerilogFileConfigBuilder>
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
            public string[] IncludedTags { get; set; } = Array.Empty<string>();
            public string[] ExcludedTags { get; set; } = Array.Empty<string>();
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
            _data.IncludedTags = includedTags ?? Array.Empty<string>();
            _data.ExcludedTags = excludedTags ?? Array.Empty<string>();
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
        /// Initializes builder from existing ScriptableObject targetConfig
        /// </summary>
        public SerilogFileConfigBuilder FromExisting(SerilogFileTargetConfig targetConfig)
        {
            if (targetConfig == null)
                throw new ArgumentNullException(nameof(targetConfig));

            // Use public properties where available
            _data.TargetName = targetConfig.TargetName;
            _data.Enabled = targetConfig.Enabled;
            _data.MinimumLevel = targetConfig.MinimumLevel;
            _data.LogFilePath = targetConfig.LogFilePath;
            _data.UseJsonFormat = targetConfig.UseJsonFormat;
            _data.LogToConsole = targetConfig.LogToConsole;
            _data.RetainedDays = targetConfig.RetainedDays;
            _data.IncludedTags = (string[])targetConfig.IncludedTags?.Clone() ?? Array.Empty<string>();
            _data.ExcludedTags = (string[])targetConfig.ExcludedTags?.Clone() ?? Array.Empty<string>();
            _data.ProcessUntaggedMessages = targetConfig.ProcessUntaggedMessages;
            _data.CaptureUnityLogs = targetConfig.CaptureUnityLogs;
            _data.IncludeStackTraces = targetConfig.IncludeStackTraces;
            _data.IncludeTimestamps = targetConfig.IncludeTimestamps;
            _data.TimestampFormat = targetConfig.TimestampFormat;
            _data.IncludeSourceContext = targetConfig.IncludeSourceContext;
            _data.IncludeThreadId = targetConfig.IncludeThreadId;
            _data.EnableStructuredLogging = targetConfig.EnableStructuredLogging;
            _data.AutoFlush = targetConfig.AutoFlush;
            _data.BufferSize = targetConfig.BufferSize;
            _data.FlushIntervalSeconds = targetConfig.FlushIntervalSeconds;
            _data.LimitMessageLength = targetConfig.LimitMessageLength;
            _data.MaxMessageLength = targetConfig.MaxMessageLength;
    
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
        /// Builds a new instance with the configured settings
        /// </summary>
        public SerilogFileTargetConfig Build()
        {
            var config = new SerilogFileTargetConfig();
            ApplyDataToConfig(_data, config);
            return config;
        }
        
        public void ApplyTo(SerilogFileTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
        
            ApplyDataToConfig(_data, config);
        }

        private static void ApplyDataToConfig(ConfigData data, SerilogFileTargetConfig config)
        {
            // Apply all the configured settings to the POCO
            config.TargetName = data.TargetName;
            config.Enabled = data.Enabled;
            config.MinimumLevel = data.MinimumLevel;
            config.LogFilePath = data.LogFilePath;
            config.UseJsonFormat = data.UseJsonFormat;
            config.LogToConsole = data.LogToConsole;
            config.RetainedDays = data.RetainedDays;
            config.IncludedTags = data.IncludedTags ?? Array.Empty<string>();
            config.ExcludedTags = data.ExcludedTags ?? Array.Empty<string>();
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