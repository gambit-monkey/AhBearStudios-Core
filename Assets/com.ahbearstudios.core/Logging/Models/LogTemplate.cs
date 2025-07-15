using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a reusable log message template with named placeholders.
    /// Provides efficient message formatting with parameter validation.
    /// </summary>
    public sealed class LogTemplate
    {
        private readonly string _template;
        private readonly string[] _parameterNames;
        private readonly Dictionary<string, int> _parameterIndexes;

        /// <summary>
        /// Gets the template string with placeholders.
        /// </summary>
        public string Template => _template;

        /// <summary>
        /// Gets the names of parameters expected by this template.
        /// </summary>
        public IReadOnlyList<string> ParameterNames => _parameterNames;

        /// <summary>
        /// Initializes a new instance of the LogTemplate.
        /// </summary>
        /// <param name="template">The template string with named placeholders (e.g., "User {UserId} performed {Action}")</param>
        /// <param name="parameterNames">The names of parameters in the template</param>
        /// <exception cref="ArgumentNullException">Thrown when template or parameterNames is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameterNames is empty or contains duplicates</exception>
        public LogTemplate(string template, string[] parameterNames)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _parameterNames = parameterNames ?? throw new ArgumentNullException(nameof(parameterNames));

            if (_parameterNames.Length == 0)
                throw new ArgumentException("Parameter names cannot be empty", nameof(parameterNames));

            if (_parameterNames.Distinct().Count() != _parameterNames.Length)
                throw new ArgumentException("Parameter names must be unique", nameof(parameterNames));

            _parameterIndexes = new Dictionary<string, int>();
            for (int i = 0; i < _parameterNames.Length; i++)
            {
                _parameterIndexes[_parameterNames[i]] = i;
            }
        }

        /// <summary>
        /// Formats the template with the provided parameters.
        /// </summary>
        /// <param name="parameters">The parameters to substitute into the template</param>
        /// <returns>The formatted message string</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameters don't match template requirements</exception>
        public string Format(params object[] parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (parameters.Length != _parameterNames.Length)
                throw new ArgumentException($"Expected {_parameterNames.Length} parameters, got {parameters.Length}", nameof(parameters));

            var result = _template;
            for (int i = 0; i < _parameterNames.Length; i++)
            {
                var placeholder = "{" + _parameterNames[i] + "}";
                var value = parameters[i]?.ToString() ?? string.Empty;
                result = result.Replace(placeholder, value);
            }

            return result;
        }

        /// <summary>
        /// Formats the template with the provided named parameters.
        /// </summary>
        /// <param name="namedParameters">The named parameters to substitute into the template</param>
        /// <returns>The formatted message string</returns>
        /// <exception cref="ArgumentNullException">Thrown when namedParameters is null</exception>
        /// <exception cref="ArgumentException">Thrown when required parameters are missing</exception>
        public string Format(IDictionary<string, object> namedParameters)
        {
            if (namedParameters == null)
                throw new ArgumentNullException(nameof(namedParameters));

            var result = _template;
            foreach (var parameterName in _parameterNames)
            {
                if (!namedParameters.TryGetValue(parameterName, out var value))
                    throw new ArgumentException($"Missing required parameter: {parameterName}", nameof(namedParameters));

                var placeholder = "{" + parameterName + "}";
                var valueString = value?.ToString() ?? string.Empty;
                result = result.Replace(placeholder, valueString);
            }

            return result;
        }

        /// <summary>
        /// Formats the template with the provided anonymous object.
        /// </summary>
        /// <param name="parameters">The anonymous object containing parameters</param>
        /// <returns>The formatted message string</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters is null</exception>
        public string Format(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var properties = parameters.GetType().GetProperties();
            var namedParameters = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                namedParameters[property.Name] = property.GetValue(parameters);
            }

            return Format(namedParameters);
        }

        /// <summary>
        /// Validates that the template contains all expected parameter placeholders.
        /// </summary>
        /// <returns>True if the template is valid, false otherwise</returns>
        public bool IsValid()
        {
            foreach (var parameterName in _parameterNames)
            {
                var placeholder = "{" + parameterName + "}";
                if (!_template.Contains(placeholder))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a list of validation errors for this template.
        /// </summary>
        /// <returns>A list of validation errors, empty if template is valid</returns>
        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            foreach (var parameterName in _parameterNames)
            {
                var placeholder = "{" + parameterName + "}";
                if (!_template.Contains(placeholder))
                {
                    errors.Add($"Template does not contain placeholder for parameter: {parameterName}");
                }
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Returns a string representation of this template.
        /// </summary>
        /// <returns>The template string</returns>
        public override string ToString() => _template;
    }

    /// <summary>
    /// Static class containing common log templates for reuse.
    /// </summary>
    public static class LogTemplates
    {
        /// <summary>
        /// Template for player actions.
        /// </summary>
        public static readonly LogTemplate PlayerAction = new LogTemplate(
            "Player {PlayerId} performed {Action} at {Timestamp}",
            new[] { "PlayerId", "Action", "Timestamp" }
        );

        /// <summary>
        /// Template for system errors.
        /// </summary>
        public static readonly LogTemplate SystemError = new LogTemplate(
            "System {SystemName} encountered error: {ErrorMessage}",
            new[] { "SystemName", "ErrorMessage" }
        );

        /// <summary>
        /// Template for performance metrics.
        /// </summary>
        public static readonly LogTemplate PerformanceMetric = new LogTemplate(
            "Performance: {Operation} took {Duration}ms with {AllocatedMemory}KB allocated",
            new[] { "Operation", "Duration", "AllocatedMemory" }
        );

        /// <summary>
        /// Template for network events.
        /// </summary>
        public static readonly LogTemplate NetworkEvent = new LogTemplate(
            "Network {EventType}: {Details} for client {ClientId}",
            new[] { "EventType", "Details", "ClientId" }
        );

        /// <summary>
        /// Template for resource loading.
        /// </summary>
        public static readonly LogTemplate ResourceLoading = new LogTemplate(
            "Resource {ResourceType} '{ResourceName}' {Action} in {Duration}ms",
            new[] { "ResourceType", "ResourceName", "Action", "Duration" }
        );
    }
}