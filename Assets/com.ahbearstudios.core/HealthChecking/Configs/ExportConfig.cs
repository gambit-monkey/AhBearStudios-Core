using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
    /// Export configuration for external systems
    /// </summary>
    public sealed record ExportConfig
    {
        /// <summary>
        /// Whether export is enabled
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Export destinations configuration
        /// </summary>
        public List<ExportDestination> Destinations { get; init; } = new();

        /// <summary>
        /// Export frequency
        /// </summary>
        public TimeSpan ExportInterval { get; init; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Batch size for exports
        /// </summary>
        public int BatchSize { get; init; } = 1000;

        /// <summary>
        /// Whether to export only changes since last export
        /// </summary>
        public bool IncrementalExport { get; init; } = true;

        /// <summary>
        /// Creates export configuration for production environments
        /// </summary>
        /// <returns>Production export configuration</returns>
        public static ExportConfig ForProduction()
        {
            return new ExportConfig
            {
                Enabled = true,
                ExportInterval = TimeSpan.FromMinutes(15),
                BatchSize = 5000,
                IncrementalExport = true,
                Destinations = new List<ExportDestination>
                {
                    new()
                    {
                        Name = "Monitoring System",
                        Type = ExportDestinationType.Http,
                        Endpoint = "https://monitoring.company.com/api/health",
                        Format = ReportFormat.Json,
                        Enabled = true
                    },
                    new()
                    {
                        Name = "Data Lake",
                        Type = ExportDestinationType.CloudStorage,
                        Endpoint = "s3://data-lake/health-checks/",
                        Format = ReportFormat.Csv,
                        Enabled = true
                    }
                }
            };
        }

        /// <summary>
        /// Validates export configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (ExportInterval <= TimeSpan.Zero)
                errors.Add("ExportInterval must be greater than zero");

            if (BatchSize <= 0)
                errors.Add("BatchSize must be greater than zero");

            foreach (var destination in Destinations)
            {
                errors.AddRange(destination.Validate());
            }

            return errors;
        }
    }