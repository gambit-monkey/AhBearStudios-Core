using System;
using System.Reflection;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Utilities
{
    /// <summary>
    /// Utility class for applying builder data to ScriptableObject configs using reflection.
    /// This approach keeps the existing configs unchanged.
    /// </summary>
    internal static class ConfigReflectionUtility
    {
        private static readonly BindingFlags PrivateInstanceFields = 
            BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Applies Serilog builder data to a SerilogFileTargetConfig using reflection
        /// </summary>
        public static void ApplySerilogData(SerilogFileConfigBuilder.ConfigData data, SerilogFileTargetConfig targetConfig)
        {
            // Apply base LogTargetConfig properties
            ApplyBaseData(data, targetConfig);
            
            // Apply SerilogFileTargetConfig-specific properties
            SetPrivateField(targetConfig, "_logFilePath", data.LogFilePath);
            SetPrivateField(targetConfig, "_useJsonFormat", data.UseJsonFormat);
            SetPrivateField(targetConfig, "_logToConsole", data.LogToConsole);
            SetPrivateField(targetConfig, "_retainedDays", data.RetainedDays);
        }

        /// <summary>
        /// Applies Unity console builder data to a UnityConsoleTargetConfig using reflection
        /// </summary>
        public static void ApplyUnityConsoleData(UnityConsoleConfigBuilder.ConfigData data, UnityConsoleTargetConfig config)
        {
            // Apply base LogTargetConfig properties
            ApplyBaseData(data, config);
            
            // Apply UnityConsoleTargetConfig-specific properties
            SetPrivateField(config, "_useColorizedOutput", data.UseColorizedOutput);
            SetPrivateField(config, "_registerUnityLogHandler", data.RegisterUnityLogHandler);
            SetPrivateField(config, "_duplicateToOriginalHandler", data.DuplicateToOriginalHandler);
        }

        /// <summary>
        /// Applies common LogTargetConfig properties using reflection - Serilog version
        /// </summary>
        private static void ApplyBaseData(SerilogFileConfigBuilder.ConfigData data, ILogTargetConfig config)
        {
            SetPrivateField(config, "_targetName", data.TargetName);
            SetPrivateField(config, "_enabled", data.Enabled);
            SetPrivateField(config, "_minimumLevel", data.MinimumLevel);
            SetPrivateField(config, "_includedTags", data.IncludedTags);
            SetPrivateField(config, "_excludedTags", data.ExcludedTags);
            SetPrivateField(config, "_processUntaggedMessages", data.ProcessUntaggedMessages);
            SetPrivateField(config, "_captureUnityLogs", data.CaptureUnityLogs);
            SetPrivateField(config, "_includeStackTraces", data.IncludeStackTraces);
            SetPrivateField(config, "_includeTimestamps", data.IncludeTimestamps);
            SetPrivateField(config, "_timestampFormat", data.TimestampFormat);
            SetPrivateField(config, "_includeSourceContext", data.IncludeSourceContext);
            SetPrivateField(config, "_includeThreadId", data.IncludeThreadId);
            SetPrivateField(config, "_enableStructuredLogging", data.EnableStructuredLogging);
            SetPrivateField(config, "_autoFlush", data.AutoFlush);
            SetPrivateField(config, "_bufferSize", data.BufferSize);
            SetPrivateField(config, "_flushIntervalSeconds", data.FlushIntervalSeconds);
            SetPrivateField(config, "_limitMessageLength", data.LimitMessageLength);
            SetPrivateField(config, "_maxMessageLength", data.MaxMessageLength);
        }

        /// <summary>
        /// Applies common LogTargetConfig properties using reflection - Unity Console version
        /// </summary>
        private static void ApplyBaseData(UnityConsoleConfigBuilder.ConfigData data, ILogTargetConfig config)
        {
            SetPrivateField(config, "_targetName", data.TargetName);
            SetPrivateField(config, "_enabled", data.Enabled);
            SetPrivateField(config, "_minimumLevel", data.MinimumLevel);
            SetPrivateField(config, "_includedTags", data.IncludedTags);
            SetPrivateField(config, "_excludedTags", data.ExcludedTags);
            SetPrivateField(config, "_processUntaggedMessages", data.ProcessUntaggedMessages);
            SetPrivateField(config, "_captureUnityLogs", data.CaptureUnityLogs);
            SetPrivateField(config, "_includeStackTraces", data.IncludeStackTraces);
            SetPrivateField(config, "_includeTimestamps", data.IncludeTimestamps);
            SetPrivateField(config, "_timestampFormat", data.TimestampFormat);
            SetPrivateField(config, "_includeSourceContext", data.IncludeSourceContext);
            SetPrivateField(config, "_includeThreadId", data.IncludeThreadId);
            SetPrivateField(config, "_enableStructuredLogging", data.EnableStructuredLogging);
            SetPrivateField(config, "_autoFlush", data.AutoFlush);
            SetPrivateField(config, "_bufferSize", data.BufferSize);
            SetPrivateField(config, "_flushIntervalSeconds", data.FlushIntervalSeconds);
            SetPrivateField(config, "_limitMessageLength", data.LimitMessageLength);
            SetPrivateField(config, "_maxMessageLength", data.MaxMessageLength);
        }

        /// <summary>
        /// Sets a private field value using reflection
        /// </summary>
        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            try
            {
                var field = target.GetType().GetField(fieldName, PrivateInstanceFields);
                if (field != null && field.FieldType.IsAssignableFrom(typeof(T)))
                {
                    field.SetValue(target, value);
                }
                else if (field == null)
                {
                    // Try parent classes
                    var currentType = target.GetType().BaseType;
                    while (currentType != null && field == null)
                    {
                        field = currentType.GetField(fieldName, PrivateInstanceFields);
                        if (field != null && field.FieldType.IsAssignableFrom(typeof(T)))
                        {
                            field.SetValue(target, value);
                            break;
                        }
                        currentType = currentType.BaseType;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to set field {fieldName} on {target.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a private field value using reflection
        /// </summary>
        public static T GetPrivateField<T>(object target, string fieldName)
        {
            try
            {
                var field = target.GetType().GetField(fieldName, PrivateInstanceFields);
                if (field != null)
                {
                    return (T)field.GetValue(target);
                }
                
                // Try parent classes
                var currentType = target.GetType().BaseType;
                while (currentType != null && field == null)
                {
                    field = currentType.GetField(fieldName, PrivateInstanceFields);
                    if (field != null)
                    {
                        return (T)field.GetValue(target);
                    }
                    currentType = currentType.BaseType;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to get field {fieldName} from {target.GetType().Name}: {ex.Message}");
            }
            
            return default(T);
        }
    }
}