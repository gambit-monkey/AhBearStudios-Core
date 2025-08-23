using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Formatters
{
    /// <summary>
    /// ScriptableObject configuration for Plain Text log formatter.
    /// Provides Unity-serializable configuration for plain text output formatting.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Formatters/Plain Text Formatter", 
        fileName = "PlainTextFormatterConfig", 
        order = 2)]
    public class PlainTextFormatterConfig : LogFormatterScriptableObject
    {
        [Header("Text Settings")]
        [SerializeField] private bool _useColors = false;
        [SerializeField] private bool _includeBrackets = true;
        [SerializeField] private string _fieldSeparator = " ";
        [SerializeField] private string _lineEnding = "\n";
        [SerializeField] private bool _padLevels = true;
        [SerializeField] private int _levelPadding = 5;

        [Header("Formatting Options")]
        [SerializeField] private bool _useUpperCaseLevels = false;
        [SerializeField] private bool _abbreviateLevels = false;
        [SerializeField] private bool _includeMilliseconds = true;
        [SerializeField] private bool _use24HourFormat = true;
        [SerializeField] private bool _includeDate = true;

        [Header("Content Settings")]
        [SerializeField] private bool _trimWhitespace = true;
        [SerializeField] private bool _escapeNewlines = false;
        [SerializeField] private bool _limitMessageLength = false;
        [SerializeField] private int _maxMessageLength = 1000;
        [SerializeField] private string _truncationIndicator = "...";

        [Header("Exception Formatting")]
        [SerializeField] private bool _formatExceptionOnNewLine = true;
        [SerializeField] private bool _indentExceptionDetails = true;
        [SerializeField] private string _exceptionIndent = "  ";
        [SerializeField] private bool _includeExceptionSeparator = true;
        [SerializeField] private string _exceptionSeparator = "---";

        /// <summary>
        /// Gets whether to use colors in output.
        /// </summary>
        public bool UseColors => _useColors;

        /// <summary>
        /// Gets whether to include brackets around field values.
        /// </summary>
        public bool IncludeBrackets => _includeBrackets;

        /// <summary>
        /// Gets the field separator.
        /// </summary>
        public string FieldSeparator => _fieldSeparator;

        /// <summary>
        /// Gets the line ending.
        /// </summary>
        public string LineEnding => _lineEnding;

        /// <summary>
        /// Gets whether to pad log levels.
        /// </summary>
        public bool PadLevels => _padLevels;

        /// <summary>
        /// Gets the level padding length.
        /// </summary>
        public int LevelPadding => _levelPadding;

        /// <summary>
        /// Gets whether to use uppercase levels.
        /// </summary>
        public bool UseUpperCaseLevels => _useUpperCaseLevels;

        /// <summary>
        /// Gets whether to abbreviate levels.
        /// </summary>
        public bool AbbreviateLevels => _abbreviateLevels;

        /// <summary>
        /// Gets whether to include milliseconds in timestamps.
        /// </summary>
        public bool IncludeMilliseconds => _includeMilliseconds;

        /// <summary>
        /// Gets whether to use 24-hour format.
        /// </summary>
        public bool Use24HourFormat => _use24HourFormat;

        /// <summary>
        /// Gets whether to include date in timestamps.
        /// </summary>
        public bool IncludeDate => _includeDate;

        /// <summary>
        /// Gets whether to trim whitespace.
        /// </summary>
        public bool TrimWhitespace => _trimWhitespace;

        /// <summary>
        /// Gets whether to escape newlines.
        /// </summary>
        public bool EscapeNewlines => _escapeNewlines;

        /// <summary>
        /// Gets whether to limit message length.
        /// </summary>
        public bool LimitMessageLength => _limitMessageLength;

        /// <summary>
        /// Gets the maximum message length.
        /// </summary>
        public int MaxMessageLength => _maxMessageLength;

        /// <summary>
        /// Gets the truncation indicator.
        /// </summary>
        public string TruncationIndicator => _truncationIndicator;

        /// <summary>
        /// Gets whether to format exceptions on new lines.
        /// </summary>
        public bool FormatExceptionOnNewLine => _formatExceptionOnNewLine;

        /// <summary>
        /// Gets whether to indent exception details.
        /// </summary>
        public bool IndentExceptionDetails => _indentExceptionDetails;

        /// <summary>
        /// Gets the exception indent string.
        /// </summary>
        public string ExceptionIndent => _exceptionIndent;

        /// <summary>
        /// Gets whether to include exception separator.
        /// </summary>
        public bool IncludeExceptionSeparator => _includeExceptionSeparator;

        /// <summary>
        /// Gets the exception separator.
        /// </summary>
        public string ExceptionSeparator => _exceptionSeparator;

        /// <summary>
        /// Creates plain text formatter specific properties.
        /// </summary>
        /// <returns>Dictionary of plain text formatter properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["UseColors"] = _useColors;
            properties["IncludeBrackets"] = _includeBrackets;
            properties["FieldSeparator"] = _fieldSeparator;
            properties["LineEnding"] = _lineEnding;
            properties["PadLevels"] = _padLevels;
            properties["LevelPadding"] = _levelPadding;
            properties["UseUpperCaseLevels"] = _useUpperCaseLevels;
            properties["AbbreviateLevels"] = _abbreviateLevels;
            properties["IncludeMilliseconds"] = _includeMilliseconds;
            properties["Use24HourFormat"] = _use24HourFormat;
            properties["IncludeDate"] = _includeDate;
            properties["TrimWhitespace"] = _trimWhitespace;
            properties["EscapeNewlines"] = _escapeNewlines;
            properties["LimitMessageLength"] = _limitMessageLength;
            properties["MaxMessageLength"] = _maxMessageLength;
            properties["TruncationIndicator"] = _truncationIndicator;
            properties["FormatExceptionOnNewLine"] = _formatExceptionOnNewLine;
            properties["IndentExceptionDetails"] = _indentExceptionDetails;
            properties["ExceptionIndent"] = _exceptionIndent;
            properties["IncludeExceptionSeparator"] = _includeExceptionSeparator;
            properties["ExceptionSeparator"] = _exceptionSeparator;
            
            return properties;
        }

        /// <summary>
        /// Validates plain text formatter specific configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (_levelPadding < 0)
            {
                errors.Add("Level padding cannot be negative");
            }

            if (_maxMessageLength <= 0)
            {
                errors.Add("Maximum message length must be greater than zero");
            }

            if (string.IsNullOrEmpty(_fieldSeparator))
            {
                errors.Add("Field separator cannot be empty");
            }

            if (string.IsNullOrEmpty(_lineEnding))
            {
                errors.Add("Line ending cannot be empty");
            }

            if (string.IsNullOrEmpty(_truncationIndicator))
            {
                errors.Add("Truncation indicator cannot be empty");
            }

            return errors;
        }

        /// <summary>
        /// Resets to plain text formatter specific defaults.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "Plain Text Formatter";
            _description = "Plain text output formatter";
            _format = LogFormat.PlainText;
            _useColors = false;
            _includeBrackets = true;
            _fieldSeparator = " ";
            _lineEnding = "\n";
            _padLevels = true;
            _levelPadding = 5;
            _useUpperCaseLevels = false;
            _abbreviateLevels = false;
            _includeMilliseconds = true;
            _use24HourFormat = true;
            _includeDate = true;
            _trimWhitespace = true;
            _escapeNewlines = false;
            _limitMessageLength = false;
            _maxMessageLength = 1000;
            _truncationIndicator = "...";
            _formatExceptionOnNewLine = true;
            _indentExceptionDetails = true;
            _exceptionIndent = "  ";
            _includeExceptionSeparator = true;
            _exceptionSeparator = "---";
        }

        /// <summary>
        /// Performs plain text formatter specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values
            _levelPadding = Mathf.Max(0, _levelPadding);
            _maxMessageLength = Mathf.Max(1, _maxMessageLength);

            // Validate strings
            if (string.IsNullOrEmpty(_fieldSeparator))
            {
                _fieldSeparator = " ";
            }

            if (string.IsNullOrEmpty(_lineEnding))
            {
                _lineEnding = "\n";
            }

            if (string.IsNullOrEmpty(_truncationIndicator))
            {
                _truncationIndicator = "...";
            }

            if (string.IsNullOrEmpty(_exceptionIndent))
            {
                _exceptionIndent = "  ";
            }

            if (string.IsNullOrEmpty(_exceptionSeparator))
            {
                _exceptionSeparator = "---";
            }

            // Ensure format is set to PlainText
            _format = LogFormat.PlainText;
        }

        /// <summary>
        /// Creates a sample plain text output for preview.
        /// </summary>
        /// <returns>Sample plain text formatted message</returns>
        public override string CreateSampleOutput()
        {
            var level = _useUpperCaseLevels ? "INFO" : "Info";
            if (_abbreviateLevels)
            {
                level = _useUpperCaseLevels ? "INF" : "Inf";
            }

            if (_padLevels)
            {
                level = level.PadRight(_levelPadding);
            }

            var timestamp = DateTime.Now;
            var timeFormat = _use24HourFormat ? "HH:mm:ss" : "hh:mm:ss tt";
            
            if (_includeMilliseconds)
            {
                timeFormat += ".fff";
            }

            var timestampStr = timestamp.ToString(timeFormat);
            if (_includeDate)
            {
                timestampStr = timestamp.ToString("yyyy-MM-dd") + " " + timestampStr;
            }

            var message = "This is a sample plain text log message";
            if (_limitMessageLength && message.Length > _maxMessageLength)
            {
                message = message.Substring(0, _maxMessageLength - _truncationIndicator.Length) + _truncationIndicator;
            }

            if (_trimWhitespace)
            {
                message = message.Trim();
            }

            if (_escapeNewlines)
            {
                message = message.Replace("\n", "\\n").Replace("\r", "\\r");
            }

            var parts = new List<string>();
            
            if (_includeBrackets)
            {
                parts.Add($"[{timestampStr}]");
                parts.Add($"[{level}]");
                parts.Add($"[SampleChannel]");
            }
            else
            {
                parts.Add(timestampStr);
                parts.Add(level);
                parts.Add("SampleChannel");
            }
            
            parts.Add(message);

            return string.Join(_fieldSeparator, parts) + _lineEnding;
        }

        /// <summary>
        /// Gets the formatted level string.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>Formatted level string</returns>
        public string FormatLevel(LogLevel level)
        {
            var levelStr = level.ToString();
            
            if (_abbreviateLevels)
            {
                levelStr = levelStr switch
                {
                    "Debug" => "Dbg",
                    "Info" => "Inf",
                    "Warning" => "Wrn",
                    "Error" => "Err",
                    "Critical" => "Crt",
                    _ => levelStr
                };
            }

            if (_useUpperCaseLevels)
            {
                levelStr = levelStr.ToUpper();
            }

            if (_padLevels)
            {
                levelStr = levelStr.PadRight(_levelPadding);
            }

            return levelStr;
        }

        /// <summary>
        /// Gets the formatted timestamp string.
        /// </summary>
        /// <param name="timestamp">The timestamp</param>
        /// <returns>Formatted timestamp string</returns>
        public string FormatTimestamp(DateTime timestamp)
        {
            var timeFormat = _use24HourFormat ? "HH:mm:ss" : "hh:mm:ss tt";
            
            if (_includeMilliseconds)
            {
                timeFormat += ".fff";
            }

            var timestampStr = timestamp.ToString(timeFormat);
            
            if (_includeDate)
            {
                timestampStr = timestamp.ToString("yyyy-MM-dd") + " " + timestampStr;
            }

            return timestampStr;
        }

        /// <summary>
        /// Formats a message with length limiting and whitespace handling.
        /// </summary>
        /// <param name="message">The message to format</param>
        /// <returns>Formatted message string</returns>
        public string FormatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            var formattedMessage = message;

            if (_trimWhitespace)
            {
                formattedMessage = formattedMessage.Trim();
            }

            if (_limitMessageLength && formattedMessage.Length > _maxMessageLength)
            {
                formattedMessage = formattedMessage.Substring(0, _maxMessageLength - _truncationIndicator.Length) + _truncationIndicator;
            }

            if (_escapeNewlines)
            {
                formattedMessage = formattedMessage.Replace("\n", "\\n").Replace("\r", "\\r");
            }

            return formattedMessage;
        }
    }
}