using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory implementation for creating log formatter instances.
    /// Follows the Factory pattern as specified in the AhBearStudios Core Architecture.
    /// Supports all available formatter types and integrates with IProfilerService.
    /// </summary>
    public sealed class LogFormatterFactory : ILogFormatterFactory
    {
        private readonly Dictionary<string, Func<IProfilerService, ILogFormatter>> _formatterFactories;
        private readonly ILoggingService _loggingService;
        
        /// <summary>
        /// Cache for converted settings to avoid repeated conversions.
        /// Uses FormatterConfig equality for cache key matching.
        /// </summary>
        private static readonly ConcurrentDictionary<FormatterConfig, IReadOnlyDictionary<FixedString32Bytes, object>> 
            _settingsCache = new();

        /// <summary>
        /// Initializes a new instance of the LogFormatterFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for internal logging</param>
        public LogFormatterFactory(ILoggingService loggingService = null)
        {
            _loggingService = loggingService;
            _formatterFactories = new Dictionary<string, Func<IProfilerService, ILogFormatter>>(StringComparer.OrdinalIgnoreCase);
            
            RegisterDefaultFormatterTypes();
        }

        /// <inheritdoc />
        public ILogFormatter CreateFormatter(FormatterConfig config, IProfilerService profilerService = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var validationErrors = ValidateFormatterConfig(config);
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Formatter configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            try
            {
                var formatter = CreateFormatter(config.FormatterType, profilerService);
                
                // Configure the formatter with the provided settings if it supports configuration
                if (formatter != null)
                {
                    ValidateConfigBeforeConversion(config);
                    var settings = ConvertFormatterConfigToSettings(config);
                    formatter.Configure(settings);
                    _loggingService?.LogInfo($"Created and configured log formatter '{config.Name}' of type '{config.FormatterType}'");
                }

                return formatter;
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, $"Failed to create log formatter '{config.Name}' of type '{config.FormatterType}'");
                throw new InvalidOperationException($"Failed to create log formatter '{config.Name}' of type '{config.FormatterType}'", ex);
            }
        }

        /// <inheritdoc />
        public ILogFormatter CreateFormatter(string formatterType, IProfilerService profilerService = null)
        {
            if (string.IsNullOrWhiteSpace(formatterType))
                throw new ArgumentException("Formatter type cannot be null or empty", nameof(formatterType));

            var validationErrors = ValidateFormatterType(formatterType);
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Formatter type validation failed:{Environment.NewLine}{errorMessage}");
            }

            if (!_formatterFactories.TryGetValue(formatterType, out var factory))
            {
                _loggingService?.LogError($"Unknown formatter type: {formatterType}");
                throw new InvalidOperationException($"Unknown formatter type: {formatterType}. Available types: {string.Join(", ", _formatterFactories.Keys)}");
            }

            try
            {
                var formatter = factory(profilerService);
                _loggingService?.LogInfo($"Created log formatter of type '{formatterType}'");
                return formatter;
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, $"Failed to create log formatter of type '{formatterType}'");
                throw new InvalidOperationException($"Failed to create log formatter of type '{formatterType}'", ex);
            }
        }

        /// <inheritdoc />
        public T CreateFormatter<T>(IProfilerService profilerService = null) where T : class, ILogFormatter
        {
            var formatterType = GetFormatterTypeName<T>();
            var formatter = CreateFormatter(formatterType, profilerService);
            
            if (formatter is T typedFormatter)
            {
                return typedFormatter;
            }

            throw new InvalidOperationException($"Created formatter is not of type {typeof(T).Name}");
        }

        /// <inheritdoc />
        public IReadOnlyList<ILogFormatter> CreateFormatters(IEnumerable<FormatterConfig> configs, IProfilerService profilerService = null)
        {
            if (configs == null)
                throw new ArgumentNullException(nameof(configs));

            var configList = configs.ToList();
            var formatters = new List<ILogFormatter>(configList.Count);

            foreach (var config in configList)
            {
                try
                {
                    formatters.Add(CreateFormatter(config, profilerService));
                }
                catch (Exception ex)
                {
                    _loggingService?.LogException(ex, $"Failed to create formatter from config: {config?.Name ?? "Unknown"}");
                    
                    // Continue creating other formatters even if one fails
                    // This provides graceful degradation
                }
            }

            return formatters.AsReadOnly();
        }

        /// <inheritdoc />
        public IReadOnlyList<ILogFormatter> CreateFormatters(IEnumerable<string> formatterTypes, IProfilerService profilerService = null)
        {
            if (formatterTypes == null)
                throw new ArgumentNullException(nameof(formatterTypes));

            var typeList = formatterTypes.ToList();
            var formatters = new List<ILogFormatter>(typeList.Count);

            foreach (var formatterType in typeList)
            {
                try
                {
                    formatters.Add(CreateFormatter(formatterType, profilerService));
                }
                catch (Exception ex)
                {
                    _loggingService?.LogException(ex, $"Failed to create formatter of type: {formatterType}");
                    
                    // Continue creating other formatters even if one fails
                    // This provides graceful degradation
                }
            }

            return formatters.AsReadOnly();
        }

        /// <inheritdoc />
        public void RegisterFormatterType(string formatterType, Func<IProfilerService, ILogFormatter> factory)
        {
            if (string.IsNullOrWhiteSpace(formatterType))
                throw new ArgumentException("Formatter type cannot be null or empty", nameof(formatterType));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _formatterFactories[formatterType] = factory;
            _loggingService?.LogInfo($"Registered log formatter type: {formatterType}");
        }

        /// <inheritdoc />
        public void RegisterFormatterType<T>(string formatterType) where T : class, ILogFormatter
        {
            if (string.IsNullOrWhiteSpace(formatterType))
                throw new ArgumentException("Formatter type cannot be null or empty", nameof(formatterType));

            RegisterFormatterType(formatterType, profilerService => 
            {
                // Use reflection to find constructor that accepts IProfilerService
                var type = typeof(T);
                var constructorWithProfiler = type.GetConstructor(new[] { typeof(IProfilerService) });
                
                if (constructorWithProfiler != null)
                {
                    return (T)constructorWithProfiler.Invoke(new object[] { profilerService });
                }
                
                // Fall back to parameterless constructor
                var parameterlessConstructor = type.GetConstructor(Type.EmptyTypes);
                if (parameterlessConstructor != null)
                {
                    return (T)parameterlessConstructor.Invoke(null);
                }
                
                throw new InvalidOperationException($"Type {type.Name} does not have a suitable constructor");
            });
        }

        /// <inheritdoc />
        public bool UnregisterFormatterType(string formatterType)
        {
            if (string.IsNullOrWhiteSpace(formatterType))
                return false;

            var removed = _formatterFactories.Remove(formatterType);
            if (removed)
            {
                _loggingService?.LogInfo($"Unregistered log formatter type: {formatterType}");
            }

            return removed;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetRegisteredFormatterTypes()
        {
            return _formatterFactories.Keys.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public bool IsFormatterTypeRegistered(string formatterType)
        {
            if (string.IsNullOrWhiteSpace(formatterType))
                return false;

            return _formatterFactories.ContainsKey(formatterType);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ValidateFormatterConfig(FormatterConfig config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Formatter configuration cannot be null");
                return errors.AsReadOnly();
            }

            // Use the config's own validation
            var configValidation = config.Validate();
            if (!configValidation.IsValid)
            {
                errors.AddRange(configValidation.Errors.Select(e => e.Message));
            }

            // Additional factory-specific validation
            if (!IsFormatterTypeRegistered(config.FormatterType))
            {
                errors.Add($"Formatter type '{config.FormatterType}' is not registered. Available types: {string.Join(", ", _formatterFactories.Keys)}");
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ValidateFormatterType(string formatterType)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(formatterType))
            {
                errors.Add("Formatter type cannot be null or empty");
                return errors.AsReadOnly();
            }

            // Additional factory-specific validation
            if (!IsFormatterTypeRegistered(formatterType))
            {
                errors.Add($"Formatter type '{formatterType}' is not registered. Available types: {string.Join(", ", _formatterFactories.Keys)}");
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public ILogFormatter CreateDefaultFormatter(IProfilerService profilerService = null)
        {
            return CreateFormatter("PlainText", profilerService);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> GetAvailableFormatterTypes()
        {
            var formatterTypes = _formatterFactories.Keys;
            var descriptions = new Dictionary<string, string>();

            foreach (var formatterType in formatterTypes)
            {
                descriptions[formatterType] = GetFormatterTypeDescription(formatterType);
            }

            return new ReadOnlyDictionary<string, string>(descriptions);
        }

        /// <summary>
        /// Clears the settings cache to free memory and force fresh conversions.
        /// Useful for testing and maintenance purposes.
        /// </summary>
        public static void ClearSettingsCache()
        {
            _settingsCache.Clear();
        }

        /// <summary>
        /// Gets the current size of the settings cache.
        /// Useful for monitoring cache performance and memory usage.
        /// </summary>
        /// <returns>The number of cached settings configurations</returns>
        public static int GetCacheSize()
        {
            return _settingsCache.Count;
        }

        /// <summary>
        /// Registers the default formatter types that are available by default.
        /// </summary>
        private void RegisterDefaultFormatterTypes()
        {
            // Register all available formatters
            RegisterFormatterType("PlainText", profilerService => new PlainTextFormatter(profilerService));
            RegisterFormatterType("Json", profilerService => new JsonFormatter(profilerService));
            RegisterFormatterType("Xml", profilerService => new XmlFormatter(profilerService));
            RegisterFormatterType("Csv", profilerService => new CsvFormatter(profilerService));
            RegisterFormatterType("Syslog", profilerService => new SyslogFormatter(profilerService));
            RegisterFormatterType("KeyValue", profilerService => new KeyValueFormatter(profilerService));
            RegisterFormatterType("Cef", profilerService => new CefFormatter(profilerService));
            RegisterFormatterType("Gelf", profilerService => new GelfFormatter(profilerService));
            RegisterFormatterType("Structured", profilerService => new StructuredFormatter(profilerService));
            RegisterFormatterType("Binary", profilerService => new BinaryFormatter(profilerService));
            RegisterFormatterType("MessagePack", profilerService => new MessagePackFormatter(profilerService));
            RegisterFormatterType("Protobuf", profilerService => new ProtobufFormatter(profilerService));
        }

        /// <summary>
        /// Gets a human-readable description for a formatter type.
        /// </summary>
        /// <param name="formatterType">The formatter type</param>
        /// <returns>A description of the formatter type</returns>
        private static string GetFormatterTypeDescription(string formatterType)
        {
            return formatterType switch
            {
                "PlainText" => "Human-readable plain text format",
                "Json" => "JavaScript Object Notation format",
                "Xml" => "Extensible Markup Language format",
                "Csv" => "Comma-separated values format for data analysis",
                "Syslog" => "Standard syslog format (RFC 5424)",
                "KeyValue" => "Key-value pair format",
                "Cef" => "Common Event Format for security tools",
                "Gelf" => "Graylog Extended Log Format for centralized logging",
                "Structured" => "Flexible structured data format",
                "Binary" => "High-performance binary format",
                "MessagePack" => "Efficient binary serialization format",
                "Protobuf" => "Protocol Buffers for cross-platform serialization",
                _ => $"Custom formatter type: {formatterType}"
            };
        }

        /// <summary>
        /// Gets the formatter type name for a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The formatter type</typeparam>
        /// <returns>The string identifier for the formatter type</returns>
        private static string GetFormatterTypeName<T>() where T : class, ILogFormatter
        {
            var type = typeof(T);
            return type.Name switch
            {
                nameof(PlainTextFormatter) => "PlainText",
                nameof(JsonFormatter) => "Json",
                nameof(XmlFormatter) => "Xml",
                nameof(CsvFormatter) => "Csv",
                nameof(SyslogFormatter) => "Syslog",
                nameof(KeyValueFormatter) => "KeyValue",
                nameof(CefFormatter) => "Cef",
                nameof(GelfFormatter) => "Gelf",
                nameof(StructuredFormatter) => "Structured",
                nameof(BinaryFormatter) => "Binary",
                nameof(MessagePackFormatter) => "MessagePack",
                nameof(ProtobufFormatter) => "Protobuf",
                _ => type.Name.Replace("Formatter", "")
            };
        }

        /// <summary>
        /// Validates configuration before conversion to ensure it's ready for processing.
        /// </summary>
        /// <param name="config">The formatter configuration to validate</param>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
        private static void ValidateConfigBeforeConversion(FormatterConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Formatter configuration cannot be null");

            var validation = config.Validate();
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors.Select(e => e.Message));
                throw new ArgumentException($"Invalid formatter configuration: {errors}", nameof(config));
            }
        }

        /// <summary>
        /// Converts a FormatterConfig to the settings dictionary format expected by formatters.
        /// Uses caching to avoid repeated conversions of the same configuration.
        /// </summary>
        /// <param name="config">The formatter configuration</param>
        /// <returns>A cached settings dictionary</returns>
        private static IReadOnlyDictionary<FixedString32Bytes, object> ConvertFormatterConfigToSettings(FormatterConfig config)
        {
            // Check cache first for performance optimization
            if (_settingsCache.TryGetValue(config, out var cachedSettings))
            {
                return cachedSettings;
            }

            // Convert configuration to settings dictionary
            var settings = new Dictionary<FixedString32Bytes, object>
            {
                ["IncludeTimestamp"] = GetSettingValue(config, nameof(config.IncludeTimestamp), config.IncludeTimestamp),
                ["IncludeLevel"] = GetSettingValue(config, nameof(config.IncludeLogLevel), config.IncludeLogLevel),
                ["IncludeChannel"] = GetSettingValue(config, nameof(config.IncludeChannel), config.IncludeChannel),
                ["IncludeCorrelationId"] = GetSettingValue(config, nameof(config.IncludeCorrelationId), config.IncludeCorrelationId),
                ["IncludeThreadInfo"] = GetSettingValue(config, nameof(config.IncludeThreadInfo), config.IncludeThreadInfo),
                ["IncludeMachineInfo"] = GetSettingValue(config, nameof(config.IncludeMachineInfo), config.IncludeMachineInfo),
                ["IncludeException"] = GetSettingValue(config, nameof(config.IncludeExceptionDetails), config.IncludeExceptionDetails),
                ["IncludeStackTrace"] = GetSettingValue(config, nameof(config.IncludeStackTrace), config.IncludeStackTrace),
                ["IncludePerformanceMetrics"] = GetSettingValue(config, nameof(config.IncludePerformanceMetrics), config.IncludePerformanceMetrics),
                ["UseUtcTimestamp"] = GetSettingValue(config, nameof(config.UseUtcTimestamps), config.UseUtcTimestamps),
                ["CompactOutput"] = GetSettingValue(config, nameof(config.CompactOutput), config.CompactOutput),
                ["PrettyPrint"] = GetSettingValue(config, nameof(config.PrettyPrint), config.PrettyPrint),
                ["TimestampFormat"] = GetSettingValue(config, nameof(config.TimestampFormat), config.TimestampFormat),
                ["FieldSeparator"] = GetSettingValue(config, nameof(config.FieldSeparator), config.FieldSeparator),
                ["LineSeparator"] = GetSettingValue(config, nameof(config.LineSeparator), config.LineSeparator),
                ["NullValue"] = GetSettingValue(config, nameof(config.NullValue), config.NullValue),
                ["MaxMessageLength"] = GetSettingValue(config, nameof(config.MaxMessageLength), config.MaxMessageLength),
                ["MaxExceptionLength"] = GetSettingValue(config, nameof(config.MaxExceptionLength), config.MaxExceptionLength),
                ["EscapeCharacter"] = GetSettingValue(config, nameof(config.EscapeCharacter), config.EscapeCharacter.ToString()),
                ["QuoteCharacter"] = GetSettingValue(config, nameof(config.QuoteCharacter), config.QuoteCharacter.ToString()),
                ["Encoding"] = GetSettingValue(config, nameof(config.Encoding), config.Encoding),
                ["Culture"] = GetSettingValue(config, nameof(config.Culture), config.Culture),
                ["SingleLine"] = GetSettingValue(config, nameof(config.SingleLine), config.SingleLine),
                ["IsEnabled"] = GetSettingValue(config, nameof(config.IsEnabled), config.IsEnabled)
            };

            // Add custom properties from the config with type safety
            if (config.Properties != null)
            {
                foreach (var kvp in config.Properties)
                {
                    // Only add if it doesn't conflict with built-in settings
                    var key = (FixedString32Bytes)kvp.Key;
                    if (!settings.ContainsKey(key))
                    {
                        settings[key] = kvp.Value;
                    }
                }
            }

            // Create read-only dictionary and cache it
            var readOnlySettings = new ReadOnlyDictionary<FixedString32Bytes, object>(settings);
            
            // Cache with size limit to prevent memory bloat
            if (_settingsCache.Count < 100) // Reasonable cache size limit
            {
                _settingsCache.TryAdd(config, readOnlySettings);
            }

            return readOnlySettings;
        }

        /// <summary>
        /// Type-safe helper method for extracting settings values from FormatterConfig.
        /// Provides validation and safe fallback to default values.
        /// Supports extracting values from both strongly-typed properties and custom Properties dictionary.
        /// </summary>
        /// <typeparam name="T">The expected type of the setting value</typeparam>
        /// <param name="config">The formatter configuration</param>
        /// <param name="propertyName">The name of the property being extracted</param>
        /// <param name="defaultValue">The default value to use if extraction fails</param>
        /// <returns>The extracted setting value or the default value</returns>
        private static T GetSettingValue<T>(FormatterConfig config, string propertyName, T defaultValue)
        {
            if (config == null)
                return defaultValue;

            try
            {
                // First, try to get value from custom Properties dictionary
                // This allows for runtime configuration overrides
                if (config.Properties != null && config.Properties.TryGetValue(propertyName, out var customValue))
                {
                    if (customValue is T directValue)
                    {
                        return directValue;
                    }
                    
                    // Attempt type conversion for common scenarios
                    if (typeof(T) == typeof(bool) && customValue is string boolStr)
                    {
                        if (bool.TryParse(boolStr, out var boolResult))
                            return (T)(object)boolResult;
                    }
                    else if (typeof(T) == typeof(int) && customValue is string intStr)
                    {
                        if (int.TryParse(intStr, out var intResult))
                            return (T)(object)intResult;
                    }
                    else if (typeof(T) == typeof(string) && customValue != null)
                    {
                        return (T)(object)customValue.ToString();
                    }
                    
                    // Try general type conversion as last resort
                    try
                    {
                        return (T)Convert.ChangeType(customValue, typeof(T));
                    }
                    catch
                    {
                        // Fall through to use default value
                    }
                }

                // Validate the default value against expected constraints
                if (typeof(T) == typeof(string) && defaultValue is string strValue)
                {
                    // Ensure string values are not null (use empty string instead)
                    if (strValue == null)
                        return (T)(object)string.Empty;
                }
                else if (typeof(T) == typeof(int) && defaultValue is int intValue)
                {
                    // Ensure numeric values are within reasonable bounds
                    if (intValue < 0 && (propertyName.Contains("Max") || propertyName.Contains("Size")))
                        return (T)(object)0; // Non-negative for max/size values
                }

                // Return the provided default value
                return defaultValue;
            }
            catch (Exception)
            {
                // If anything goes wrong, return the default value
                // This ensures robust operation even with malformed configurations
                return defaultValue;
            }
        }
    }
}