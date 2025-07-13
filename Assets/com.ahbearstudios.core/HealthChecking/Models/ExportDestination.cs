using System.Collections.Generic;
using AhBearStudios.Core.HealthCheck.Configs;

namespace AhBearStudios.Core.HealthChecking.Models;

  /// <summary>
    /// Export destination configuration
    /// </summary>
    public sealed record ExportDestination
    {
        /// <summary>
        /// Name of the export destination
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Type of export destination
        /// </summary>
        public ExportDestinationType Type { get; init; } = ExportDestinationType.Http;

        /// <summary>
        /// Destination endpoint or path
        /// </summary>
        public string Endpoint { get; init; } = string.Empty;

        /// <summary>
        /// Export format for this destination
        /// </summary>
        public ReportFormat Format { get; init; } = ReportFormat.Json;

        /// <summary>
        /// Whether this destination is enabled
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Authentication configuration
        /// </summary>
        public Dictionary<string, object> AuthConfig { get; init; } = new();

        /// <summary>
        /// Custom headers for HTTP exports
        /// </summary>
        public Dictionary<string, string> Headers { get; init; } = new();

        /// <summary>
        /// Retry configuration for failed exports
        /// </summary>
        public RetryConfig RetryConfig { get; init; } = new();

        /// <summary>
        /// Validates export destination
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Export destination Name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(Endpoint))
                errors.Add("Export destination Endpoint cannot be null or empty");

            if (!Enum.IsDefined(typeof(ExportDestinationType), Type))
                errors.Add($"Invalid export destination type: {Type}");

            if (!Enum.IsDefined(typeof(ReportFormat), Format))
                errors.Add($"Invalid export format: {Format}");

            errors.AddRange(RetryConfig.Validate());

            return errors;
        }
    }